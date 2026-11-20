using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Billet
{
    public class Calculations
    {
        Loads _loads;
        Geometry _geometry;
        Material _material;
        ConcentratorRatios _concentrator;
        OtherRatios _otherRatios;
        Steps _steps;
        Additions _additions;
        Limits _limits;

        double pi = Math.PI;
        double R_in;
        double R_1; // внутренний радиус контакта = внутренний радиус зубьев корпуса + r_2 (радиус скругления зуба кропуса)
        double alfa;
        double alfa_2;
        public Calculations(Loads loads, Geometry geometry, Material material, ConcentratorRatios concentrator, OtherRatios otherRatios, Steps steps, Additions additions, Limits limits)
        {
            _geometry = geometry;
            _material = material;
            _loads = loads;
            _concentrator = concentrator;
            _otherRatios = otherRatios;
            _steps = steps;
            _additions = additions;
            _limits = limits;

        }

        public void SelectGeometry(string ClosureType)
        {
            switch (ClosureType)
            {
                case "horizontal":

                    SelectCapGeometry();
                    SelectCorpusGeometry();

                    if (_geometry.h < _geometry.r_5)
                    {
                        double minAngleDeg = Math.Atan(_geometry.r_5 / _geometry.s_21) * (180.0 / Math.PI);
                        _geometry.alfa_cut = Round.RoundUpToStep(minAngleDeg, 1);
                        UpdateGrooveDepth();

                        // Пересчитываем корпус сразу после первого изменения _geometry.h, 
                        // так как глубина канавки уже увеличилась
                        SelectCorpusGeometry();
                    }

                    const double maxCuttingAngle = 85.0; // Защита от бесконечного цикла

                    while (!CheckCuttingAngle())
                    {
                        if (_geometry.alfa_cut >= maxCuttingAngle)
                        {
                            throw new InvalidOperationException($"Не удалось подобрать угол резки: достигнут предел {maxCuttingAngle} град.");
                        }

                        _geometry.alfa_cut += 1;

                        UpdateGrooveDepth();
                        SelectCorpusGeometry();
                    }

                    SelectCapThickness();
                    break;

                case "vertical":

                    if (_geometry.h < _geometry.r_5)
                    {
                        double minAngleDeg = Math.Atan(_geometry.r_5 / _geometry.s_21) * (180.0 / Math.PI);
                        _geometry.alfa_cut = Round.RoundUpToStep(minAngleDeg, 1);
                        UpdateGrooveDepth();
                    }

                    SelectCapGeometry();
                    SelectCorpusGeometry();
                    SelectCapThickness();
                    break;

                default: break;
            }
        }

        private void SelectCapGeometry()
        {
            SelectСrumple();
            SelectShear("cap_11");
            SelectBend("cap_11");
        }
        private void SelectCorpusGeometry()
        {
            SelectShear("corpus_22");
            SelectBend("corpus_22");
            SelectBend("corpus_33");
            SelectTensileBend("corpus_34");
            SelectTensileBend("corpus_55");
        }

        private void UpdateGrooveDepth()
        {
            _geometry.h = _geometry.s_21 * Math.Tan(_geometry.alfa_cut * (Math.PI / 180.0));
        }


        public Results GetCheckResults()
        {
            Results results = new Results();

            // 1. Толщина плоской крышки
            AddPairCheck(
                results,
                description: "Толщина плоской крышки",
                baseName: "s1",
                unit: "мм",
                calcFunc: type => CheckCapThickness(type, _geometry.s_1).thickness,
                workLimit: _geometry.s_1,
                testLimit: _geometry.s_1
            );

            // 2. Смятие зоны опирания
            AddPairCheck(
                results,
                description: "Расчет на смятие зоны опирания крышки затвора на корпус",
                baseName: "sigma см.",
                unit: "МПа",
                calcFunc: type => CheckСrumple(type).stress,
                workLimit: _material.Sigma_sm_t,
                testLimit: _material.Sigma_sm_20
            );

            // 3. Зуб крышки в сечении I-I (срез + изгиб и циклы)
            string element = "cap_11";

            AddPairCheck(
                results,
                description: "Расчет на срез зуба крышки в сечении I-I",
                baseName: "tau ср.11.",
                unit: "МПа",
                calcFunc: type => CheckShear(element, type, GetToothWidth(element), _geometry.s_11 - GetAddition(element, type)).stress,
                workLimit: _material.Tau_sr_t,
                testLimit: _material.Tau_sr_20
            );

            AddBendWithFatigueCheck(
                results,
                stressDescription: "Расчет на изгиб зуба крышки в сечении I-I",
                fatigueDescription: "Проверка малоцикловой прочности зуба крышки в сечении I-I",
                stressBaseName: "sigma н.11.",
                cyclesName: "[N]11",
                calcFunc: type => CheckBend(element, type, GetToothWidth(element), _geometry.s_11 - GetAddition(element, type), _geometry.r_1),
                workLimit: _material.Sigma_t,
                testLimit: _material.Sigma_20
            );

            // 4. Зуб корпуса в сечении II-II (срез + изгиб и циклы)
            element = "corpus_22";

            AddPairCheck(
                results,
                description: "Расчет на срез зуба корпуса в сечении II-II",
                baseName: "tau ср.22.",
                unit: "МПа",
                calcFunc: type => CheckShear(element, type, GetToothWidth(element), _geometry.s_21 - GetAddition(element, type)).stress,
                workLimit: _material.Tau_sr_t,
                testLimit: _material.Tau_sr_20
            );

            AddBendWithFatigueCheck(
                results,
                stressDescription: "Расчет на изгиб зуба корпуса в сечении II-II",
                fatigueDescription: "Проверка малоцикловой прочности зуба корпуса в сечении II-II",
                stressBaseName: "sigma н.22.",
                cyclesName: "[N]22",
                calcFunc: type => CheckBend(element, type, GetToothWidth(element), _geometry.s_21 - GetAddition(element, type), _geometry.r_5),
                workLimit: _material.Sigma_t,
                testLimit: _material.Sigma_20
            );

            // 5. Зуб корпуса в сечении III-III (изгиб и циклы)
            element = "corpus_33";

            AddBendWithFatigueCheck(
                results,
                stressDescription: "Расчет на изгиб зуба корпуса в сечении III-III",
                fatigueDescription: "Проверка малоцикловой прочности зуба корпуса в сечении III-III",
                stressBaseName: "sigma н.33.",
                cyclesName: "[N]33",
                calcFunc: type => CheckBend(element, type, GetToothWidth(element), (_geometry.s_21 + _geometry.r_5) - GetAddition(element, type), _geometry.r_5),
                workLimit: _material.Sigma_t,
                testLimit: _material.Sigma_20
            );

            // 6. Корпус в сечении III-IV (растяжение с изгибом и циклы)
            element = "corpus_34";

            AddBendWithFatigueCheck(
                results,
                stressDescription: "Расчет на изгиб и растяжение корпуса в сечении III-IV",
                fatigueDescription: "Проверка малоцикловой прочности корпуса в сечении III-IV",
                stressBaseName: "sigma сум.34.",
                cyclesName: "[N]34",
                calcFunc: type =>
                {
                    var geom = GetSectionGeometry(element, type);
                    var res = CheckTensileBend(element, type, geom.A, GetSectionGeometry(element).l, geom.s, geom.r);
                    return (res.conclusion, res.stress, res.cycles);
                },
                workLimit: _material.Sigma_ras_t,
                testLimit: _material.Sigma_ras_20
            );

            // 7. Корпус в сечении V-V (растяжение с изгибом и циклы)
            element = "corpus_55";

            AddBendWithFatigueCheck(
                results,
                stressDescription: "Расчет на изгиб и растяжение корпуса в сечении V-V",
                fatigueDescription: "Проверка малоцикловой прочности корпуса в сечении V-V",
                stressBaseName: "sigma сум.55.",
                cyclesName: "[N]55",
                calcFunc: type =>
                {
                    var geom = GetSectionGeometry(element, type);
                    var res = CheckTensileBend(element, type, geom.A, GetSectionGeometry(element).l, geom.s, geom.r);
                    return (res.conclusion, res.stress, res.cycles);
                },
                workLimit: _material.Sigma_ras_t,
                testLimit: _material.Sigma_ras_20
            );

            return results;
        }

        private void AddPairCheck(
            Results results,
            string description,
            string baseName,
            string unit,
            Func<string, double> calcFunc,
            double workLimit,
            double testLimit)
        {
            var modes = new[]
            {
        (Type: "work", Suffix: "р", Limit: workLimit),
        (Type: "test", Suffix: "и", Limit: testLimit)
    };

            foreach (var mode in modes)
            {
                results.AddResult(new ElementResult
                {
                    Description = description,
                    TypeLoad = mode.Type,
                    Name = baseName + mode.Suffix,
                    Value = calcFunc(mode.Type),
                    Limit = mode.Limit,
                    Unit = unit
                });
            }
        }

        private void AddBendWithFatigueCheck(
            Results results,
            string stressDescription,
            string fatigueDescription,
            string stressBaseName,
            string cyclesName,
            Func<string, (bool conclusion, double stress, double cycles)> calcFunc,
            double workLimit,
            double testLimit)
        {
            // 1. Рабочие условия (напряжения + циклы за один вызов)
            var workRes = calcFunc("work");

            results.AddResult(new ElementResult
            {
                Description = stressDescription,
                TypeLoad = "work",
                Name = stressBaseName + "р",
                Value = workRes.stress,
                Limit = workLimit,
                Unit = "МПа"
            });

            // 2. Условия испытаний (только напряжения)
            var testRes = calcFunc("test");

            results.AddResult(new ElementResult
            {
                Description = stressDescription,
                TypeLoad = "test",
                Name = stressBaseName + "и",
                Value = testRes.stress,
                Limit = testLimit,
                Unit = "МПа"
            });

            // 3. Проверка малоцикловой прочности (заданное Limit <= допускаемого Value)
            results.AddResult(new ElementResult
            {
                Description = fatigueDescription,
                TypeLoad = "work",
                Name = cyclesName,
                Value = workRes.cycles,          // вычисленное допускаемое кол-во циклов
                Limit = _loads.NumberOfCycles,   // заданное эксплуатационное кол-во циклов
                Unit = "циклов",
                IsReversed = true
            });
        }

        (bool conclusion, double stress) CheckСrumple(string type) // проверка на смятие
        {
            double sigma = Round.RoundUpToStep(GetLoad(type) / GetContactArea(), 0.1);

            switch (type)
            {
                case "work":
                    if (sigma * _concentrator.СrumpleConcentratorRatio <= _material.Sigma_sm_t)
                    {
                        return (true, sigma);
                    }
                    else
                    {
                        return (false, sigma);
                    }

                case "test":
                    if (sigma * _concentrator.СrumpleConcentratorRatio <= _material.Sigma_sm_20)
                    {
                        return (true, sigma);
                    }
                    else
                    {
                        return (false, sigma);
                    }

                default:
                    {
                        return (false, sigma);
                    }
            }
        }

        (bool conclusion, double stress) CheckShear(string element, string type, double l, double s) // проверка на срез
        {
            var geometry = GetGeomCharacter(element, l, s);
            double tau = Round.RoundUpToStep(GetLoad(type) / (geometry.A * _geometry.n), 0.1);
            switch (type)
            {
                case "work":
                    if (tau * _concentrator.ShearConcentratorRatio <= _material.Tau_sr_t)
                    {
                        return (true, tau);
                    }
                    else
                    {
                        return (false, tau);
                    }

                case "test":
                    if (tau * _concentrator.ShearConcentratorRatio <= _material.Tau_sr_20)
                    {
                        return (true, tau);
                    }
                    else
                    {
                        return (false, tau);
                    }

                default:
                    {
                        return (false, tau);
                    }
            }
        }

        (bool conclusion, double stress, double cycles) CheckBend(string element, string type, double l, double s, double r) // проверка на изгиб
        {
            double n = _geometry.n; // количество зубьев

            var geometry = GetGeomCharacter(element, l, s);
            double console = GetConsole(element);
            double sigma = Round.RoundUpToStep(GetLoad(type) * console / (geometry.Wx * n), 0.1);

            switch (type)
            {
                case "work":

                    var fatique = CheckFatique(sigma, s + GetAddition(element, type), console, r);

                    if (sigma * _concentrator.BendConcentratorRatio <= _material.Sigma_t && fatique.conclusion)
                    {
                        return (true, sigma, fatique.NumberOfCycles);
                    }
                    else
                    {
                        return (false, sigma, fatique.NumberOfCycles);
                    }

                case "test":
                    if (sigma * _concentrator.BendConcentratorRatio <= _material.Sigma_20)
                    {
                        return (true, sigma, 0);
                    }
                    else
                    {
                        return (false, sigma, 0);
                    }

                default:
                    {
                        return (false, sigma, 0);
                    }
            }
        }

        (bool conclusion, double stress, double cycles, double amplitude, double stressConcentrat) CheckTensileBend(string element, string type, double area, double l, double s, double r) // проверка на растяжение с изгибом
        {
            double console = GetConsole(element);
            var geometry = GetGeomCharacter(element, l, s);
            double sigma_Tensile = GetLoad(type) / area;
            double sigma_Bend = GetLoad(type) * console / (geometry.Wx);
            double sigma = Round.RoundUpToStep(sigma_Tensile + sigma_Bend, 0.1);

            switch (type)
            {
                case "work":

                    var fatique = CheckFatique(sigma, s + GetAddition(element, type), console, r);

                    if (sigma * _concentrator.TensileConcentratorRatio <= _material.Sigma_ras_t && fatique.conclusion)
                    {
                        return (true, sigma, fatique.NumberOfCycles, fatique.sigma_a, fatique.alfa_sigma);
                    }
                    else
                    {
                        return (false, sigma, fatique.NumberOfCycles, fatique.sigma_a, fatique.alfa_sigma);
                    }

                case "test":
                    if (sigma * _concentrator.TensileConcentratorRatio <= _material.Sigma_ras_20)
                    {
                        return (true, sigma, 0, 0, 0);
                    }
                    else
                    {
                        return (false, sigma, 0, 0, 0);
                    }

                default:
                    {
                        return (false, sigma, 0, 0, 0);
                    }
            }

        }

        (bool conclusion, double thickness) CheckCapThickness(string type, double thickness)
        {
            double D_p = _geometry.R_ex + R_1;
            double K_o = Math.Pow(1 + _geometry.d / D_p + Math.Pow(_geometry.d / D_p, 2), 0.5);
            double sigma = 0; // допускаемое напряжение
            double p = 0; // давление

            switch (type)
            {
                case "work":
                    sigma = _material.Sigma_t;
                    p = _loads.WorkPressure;
                    break;
                case "test":
                    sigma = _material.Sigma_20;
                    p = _loads.TestPressure;
                    break;
            }

            double s = Round.RoundUpToStep(_otherRatios.CapSupportRatio * K_o * D_p * Math.Pow(p / sigma, 0.5), 0.1);

            if (s * _concentrator.CapConcentratorRatio <= thickness)
            {
                return (true, s + GetAddition("cap", type));
            }
            else
            {
                return (false, s + GetAddition("cap", type));
            }
        }

        (bool conclusion, double NumberOfCycles, double sigma_a, double alfa_sigma) CheckFatique(double sigma, double s, double h, double r)
        {
            double B = _material.K_B * _material.Rmt;
            double C_t = Round.RoundDownToStep((_material.T_Ct - _loads.DesignTemperature) / _material.T_Ct, 0.001);
            double alfa_sigma = Round.RoundUpToStep(1 + (0.17 + 0.32 * (s / h)) * Math.Pow((h / r), 0.45), 0.001); // теоретический коэффициент концентрации напряжений
            double sigma_a = Round.RoundUpToStep(alfa_sigma * sigma / 2, 0.1); // аплитуда напряжений цикла
            double N = Math.Floor(1 / _material.n_N * Math.Pow(_material.A * C_t / (sigma_a - B / _material.n_sigma), 2)); // допустимое число циклов
            if (N >= _loads.NumberOfCycles)
            {

                return (true, N, sigma_a, alfa_sigma);
            }
            else
            {
                return (false, N, sigma_a, alfa_sigma);
            }
        }

        bool CheckCuttingAngle()
        {
            double R = _geometry.D_corp_ex / 2 - (_geometry.R_ex + _limits.Clearances) + _geometry.a; // радиус движения внешенего диаметра крышки с учетом зазора b_k
            double h_curve = R - Math.Pow(Math.Pow(R, 2) - Math.Pow(_geometry.s_21, 2), 0.5);
            if (h_curve < _geometry.h)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        void SelectСrumple() // подбор внешнего радиуса зубьев крышки
        {
            while (!CheckСrumple("work").conclusion || !CheckСrumple("test").conclusion)
            {
                _geometry.R_ex = _geometry.R_ex + _steps.СrumpleStep;
            }
        }
        void SelectShear(string element)  // подбор подбор толщины зуба из условия среза 
        {
            switch (element)
            {
                case "cap_11":

                    while (!CheckShear(element, "work", GetToothWidth(element), _geometry.s_11 - GetAddition(element, "work")).conclusion || !CheckShear(element, "test", GetToothWidth(element), _geometry.s_11 - GetAddition(element, "test")).conclusion)
                    {
                        _geometry.s_11 = _geometry.s_11 + _steps.ToothStep;
                    }
                    break;
                case "corpus_22":
                    while (!CheckShear(element, "work", GetToothWidth(element), _geometry.s_21 - GetAddition(element, "work")).conclusion || !CheckShear(element, "test", GetToothWidth(element), _geometry.s_21 - GetAddition(element, "test")).conclusion)
                    {
                        _geometry.s_21 = _geometry.s_21 + _steps.ToothStep;
                    }
                    break;
                default: break;
            }
        }
        void SelectBend(string element)  // подбор толщины зуба из условия изгиба 
        {
            switch (element)
            {
                case "cap_11":

                    while (!CheckBend(element, "work", GetToothWidth(element), _geometry.s_11 - GetAddition(element, "work"), _geometry.r_1).conclusion || !CheckBend(element, "test", GetToothWidth(element), _geometry.s_11 - GetAddition(element, "test"), _geometry.r_1).conclusion)
                    {
                        _geometry.s_11 = _geometry.s_11 + _steps.ToothStep;
                    }
                    break;
                case "corpus_22":
                    while (!CheckBend(element, "work", GetToothWidth(element), _geometry.s_21 - GetAddition(element, "work"), _geometry.r_5).conclusion || !CheckBend(element, "test", GetToothWidth(element), _geometry.s_21 - GetAddition(element, "test"), _geometry.r_5).conclusion)
                    {
                        _geometry.s_21 = _geometry.s_21 + _steps.ToothStep;
                    }
                    break;
                case "corpus_33":
                    while (!CheckBend(element, "work", GetToothWidth(element), (_geometry.s_21 + _geometry.r_5) - GetAddition(element, "work"), _geometry.r_5).conclusion || !CheckBend(element, "test", GetToothWidth(element), (_geometry.s_21 + _geometry.r_5) - GetAddition(element, "test"), _geometry.r_5).conclusion)
                    {
                        _geometry.s_21 = _geometry.s_21 + _steps.ToothStep;
                    }
                    break;

                default: break;
            }
        }

        void SelectTensileBend(string element)  // подбор внешнего диаметра из условия растяжения
        {

            if (_geometry.D_corp_ex <= (_geometry.R_ex + _geometry.b_k + _geometry.r_5) * 2)
            {
                _geometry.D_corp_ex = Round.RoundUpToStep((_geometry.R_ex + _geometry.b_k + _geometry.r_5) * 2 + 2, 2);
            }

            while (!CheckTensileBend(element, "work", GetSectionGeometry(element, "work").A, GetSectionGeometry(element).l, GetSectionGeometry(element, "work").s, GetSectionGeometry(element).r).conclusion
                || !CheckTensileBend(element, "test", GetSectionGeometry(element, "test").A, GetSectionGeometry(element).l, GetSectionGeometry(element, "test").s, GetSectionGeometry(element).r).conclusion)
            {
                _geometry.D_corp_ex = _geometry.D_corp_ex + _steps.DiameterStep;
            }
        }

        void SelectCapThickness()  // подбор толщины крышки
        {
            while (!CheckCapThickness("work", _geometry.s_1 - GetAddition("cap", "work")).conclusion || !CheckCapThickness("test", _geometry.s_1 - GetAddition("cap", "test")).conclusion)
            {
                _geometry.s_1 = _geometry.s_1 + _steps.CapStep;
            }

            if (_geometry.s_1 < _geometry.s_11 + _geometry.r_1 + _geometry.f_7 + _geometry.h_ob)
            {
                _geometry.s_1 = _geometry.s_11 + _geometry.r_1 + _geometry.f_7 + _geometry.h_ob;
            }
        }

         public double GetLoad(string type)
        {
            double S = Round.RoundUpToStep(pi * _geometry.D_pr * _geometry.D_pr / 4, 1); // площадь крышки находящаяся под давлением
            double F = 0; // сила на крышке от давления, Н
            double P = 0; // давление, МПа

            switch (type)
            {
                case "work": P = _loads.WorkPressure; break;
                case "test": P = _loads.TestPressure; break;
            }

            F = Round.RoundUpToStep(S * P, 1); // сила на крышке от давления, Н

            return F;
        }

        public double GetContactArea()
        {
            R_in = _geometry.D_pr / 2 + _geometry.p; // внутренний диаметр зубьев крышки
            R_1 = R_in + _geometry.b_pr + _geometry.r_2; // внутренний диаметр контакта зубьев
            if (_geometry.R_ex == 0)
            {
                _geometry.R_ex = R_1 + _geometry.r_3 * 2 + _geometry.r_2; // установка начального значения внешнего радиуса крышки
            }
            double alfa_1 = Round.RoundUpToStep(Math.Asin(_geometry.b_z / (2 * R_in)) * 2 * (180 / pi * 60), 5); // угол зазора между зубьями в минутах
            alfa = 21600 / (2 * _geometry.n) - alfa_1; // угол зуба в минутах
            alfa_2 = Round.RoundUpToStep(alfa / _geometry.k_pr, 30); // угол обнижения
            double alfa_3 = alfa - alfa_2; // угол контакта включая площадь фаски
            double F_1 = pi * (_geometry.R_ex * _geometry.R_ex - R_1 * R_1) * (alfa_3 / 21600); // полная контактная площадь без учета вырезов
            double F_2 = (_geometry.R_ex - R_1) * _geometry.f_1; // площадь фаски
            double F_3 = _geometry.r_3 * _geometry.r_3 - pi * _geometry.r_3 * _geometry.r_3 / 4; // площадь отбираемая одним скруглением от реза
            double F_4 = F_1 - F_2 - 2 * F_3; // контактная площадь одного зуба
            double F_sm = Round.RoundDownToStep(F_4 * _geometry.n, 1); // суммарная площадь смятых зон

            return F_sm;
        }

        public double GetToothWidth(string element)
        {
            double l = 0;

            switch (element)
            {
                case "cap_11":
                    l = Round.RoundDownToStep(R_in * Math.Sin(alfa * pi / 10800), 1);
                    break;
                case "corpus_22":
                    l = Round.RoundDownToStep((_geometry.R_ex + _geometry.b_k) * Math.Sin(alfa * pi / 10800), 1);
                    break;
                case "corpus_33":
                    l = Round.RoundDownToStep((_geometry.R_ex + _geometry.b_k + _geometry.r_5) * Math.Sin(alfa * pi / 10800), 1);
                    break;
            }
            return l;
        }

        public (double A, double l, double s, double r) GetSectionGeometry(string element, string type = "")
        {
            double A = 0;
            double l = 0;
            double s = 0;
            double r = 0;

            switch (element)
            {
                case "corpus_34":
                    l = 2 * pi * (_geometry.R_ex + _geometry.b_k + _geometry.r_5);
                    r = _geometry.r_5;

                    switch (type)
                    {
                        case "work":
                            A = pi * (Math.Pow(_geometry.D_corp_ex / 2 - GetAddition(element, "work") / 2, 2) - Math.Pow(_geometry.R_ex + _geometry.b_k + _geometry.r_5 + GetAddition(element, "work") / 2, 2));
                            s = (_geometry.D_corp_ex / 2 - GetAddition(element, "work") / 2) - (_geometry.R_ex + _geometry.b_k + _geometry.r_5 + GetAddition(element, "work") / 2);
                            break;
                        case "test":
                            A = pi * (Math.Pow(_geometry.D_corp_ex / 2 - GetAddition(element, "test") / 2, 2) - Math.Pow(_geometry.R_ex + _geometry.b_k + _geometry.r_5 + GetAddition(element, "test") / 2, 2));
                            s = (_geometry.D_corp_ex / 2 - GetAddition(element, "test") / 2) - (_geometry.R_ex + _geometry.b_k + _geometry.r_5 + GetAddition(element, "test") / 2);
                            break;
                        default: break;
                    }
                    break;

                case "corpus_55":
                    l = 2 * pi * (_geometry.R_ex + _geometry.b_k);
                    r = _geometry.r_6;

                    switch (type)
                    {
                        case "work":
                            A = pi * (Math.Pow(_geometry.D_corp_ex / 2 - GetAddition(element, "work") / 2, 2) - Math.Pow(_geometry.R_ex + _geometry.b_k + GetAddition(element, "work") / 2, 2));
                            s = (_geometry.D_corp_ex / 2 - GetAddition(element, "work") / 2) - (_geometry.R_ex + _geometry.b_k + GetAddition(element, "work") / 2);

                            break;
                        case "test":
                            A = pi * (Math.Pow(_geometry.D_corp_ex / 2 - GetAddition(element, "test") / 2, 2) - Math.Pow(_geometry.R_ex + _geometry.b_k + GetAddition(element, "test") / 2, 2));
                            s = (_geometry.D_corp_ex / 2 - GetAddition(element, "test") / 2) - (_geometry.R_ex + _geometry.b_k + GetAddition(element, "test") / 2);
                            break;
                        default: break;
                    }
                    break;
                default: break;
            }
            return (A, l, s, r);
        }


        public double GetConsole(string element)
        {
            double L = 0;

            switch (element)
            {
                case "cap_11":
                    L = _geometry.b_pr + 2 * (_geometry.R_ex - R_1) / 3;
                    break;
                case "corpus_22":
                    L = _geometry.b_k + 2 * (_geometry.R_ex - R_1) / 3;
                    break;
                case "corpus_33":
                    L = Math.Max(_geometry.h, _geometry.r_5) + _geometry.b_k + 2 * (_geometry.R_ex - R_1) / 3;
                    break;
                case "corpus_34":
                    L = Math.Max(_geometry.h, _geometry.r_5) + _geometry.b_k + 2 * (_geometry.R_ex - R_1) / 3;
                    break;
                case "corpus_55":
                    L = _geometry.R_ex + _geometry.b_k - _geometry.D_nom / 2;
                    break;
            }
            return L;
        }

        public (double A, double Jx, double Jy, double Wx, double Wy) GetGeomCharacter(string element, double l, double s)
        {
            double b_v = 0; // ширина обнижения зуба в расчетном сечении
            double f_1 = 0;
            double f_2 = 0;
            double f_3 = 0;
            double f_4 = 0;
            double r_4 = 0;
            double z = 0;

            switch (element)
            {
                case "cap_11":
                    b_v = R_in * Math.Sin(alfa_2 * pi / 10800);
                    f_1 = _geometry.f_1;
                    f_2 = _geometry.f_2;
                    f_3 = _geometry.f_3;
                    f_4 = _geometry.f_4;
                    r_4 = _geometry.r_4;
                    z = _geometry.z;
                    break;
                case "corpus_22":
                    f_3 = _geometry.f_5;
                    f_4 = _geometry.f_6;
                    break;
                case "corpus_33":
                    f_3 = _geometry.f_5;
                    f_4 = _geometry.f_6;
                    break;
                case "corpus_55":
                    f_3 = 0;
                    f_4 = 0;
                    break;
            }

            List<double> Area = new List<double>();
            double A_1 = s * l; // площадь сечения зуба без учета вырезов
            Area.Add(A_1);
            double A_21 = f_1 * f_2 / 2; // площади вырезов начало
            Area.Add(A_21);
            double A_22 = b_v * f_2;
            Area.Add(A_22);
            double A_23 = pi * r_4 * r_4 / 4;
            Area.Add(A_23);
            double A_24 = (b_v - r_4) * (z - f_2);
            Area.Add(A_24);
            double A_3 = f_3 * f_4 / 2;
            Area.Add(A_3);
            double A_4 = A_3;
            Area.Add(A_4); // площади вырезов конец

            double A = Area[0]; // суммарная площадь с учетом вырезов
            for (int i = 1; i < Area.Count; i++)
            {
                A = A - Area[i];
            }

            List<double> CoordinateX = new List<double>(); // координаты площадей начало
            List<double> CoordinateY = new List<double>();
            double x_1 = l / 2; double y_1 = s / 2;
            CoordinateX.Add(x_1); CoordinateY.Add(y_1);
            double x_21 = l - b_v - f_1 / 3; double y_21 = s - f_2 / 3;
            CoordinateX.Add(x_21); CoordinateY.Add(y_21);
            double x_22 = l - b_v / 2; double y_22 = s - f_2 / 2;
            CoordinateX.Add(x_22); CoordinateY.Add(y_22);
            double x_23 = l - b_v + 0.58 * r_4; double y_23 = s - f_2 - 0.42 * r_4;
            CoordinateX.Add(x_23); CoordinateY.Add(y_23);
            double x_24 = l - (b_v - r_4) / 2; double y_24 = s - f_2 - (z - f_2) / 2;
            CoordinateX.Add(x_24); CoordinateY.Add(y_24);
            double x_3 = f_3 / 3; double y_3 = f_4 / 3;
            CoordinateX.Add(x_3); CoordinateY.Add(y_3);
            double x_4 = l - f_3 / 3; double y_4 = f_4 / 3;
            CoordinateX.Add(x_4); CoordinateY.Add(y_4); // координаты площадей конец

            double Sx = Area[0] * CoordinateY[0]; // статический момент относительно оси X
            for (int i = 1; i < Area.Count(); i++)
            {
                Sx = Sx - Area[i] * CoordinateY[i];
            }

            double Sy = Area[0] * CoordinateX[0]; // статический момент относительно оси Y
            for (int i = 1; i < Area.Count(); i++)
            {
                Sy = Sy - Area[i] * CoordinateX[i];
            }

            double Xc = Sy / A; // координаты центра тяжести сечения
            double Yc = Sx / A;

            for (int i = 0; i < CoordinateX.Count(); i++) // пересчет координат X относительно цетра тяжести
            {
                CoordinateX[i] = CoordinateX[i] - Xc;
            }
            for (int i = 0; i < CoordinateY.Count(); i++) // пересчет координат Y относительно цетра тяжести
            {
                CoordinateY[i] = CoordinateY[i] - Yc;
            }

            List<double> Jxi = new List<double>(); // определение собственных центрально осевых моментов инерции площадей начало
            List<double> Jyi = new List<double>();
            double Jx_1 = l * Math.Pow(s, 3) / 12; double Jy_1 = s * Math.Pow(l, 3) / 12;
            Jxi.Add(Jx_1); Jyi.Add(Jy_1);
            double Jx_21 = f_1 * Math.Pow(f_2, 3) / 36; double Jy_21 = f_2 * Math.Pow(f_1, 3) / 36;
            Jxi.Add(Jx_21); Jyi.Add(Jy_21);
            double Jx_22 = b_v * Math.Pow(f_2, 3) / 12; double Jy_22 = f_2 * Math.Pow(b_v, 3) / 12;
            Jxi.Add(Jx_22); Jyi.Add(Jy_22);
            double Jx_23 = 0.055 * Math.Pow(r_4, 4); double Jy_23 = Jx_23;
            Jxi.Add(Jx_23); Jyi.Add(Jy_23);
            double Jx_24 = (b_v - r_4) * Math.Pow(z - f_2, 3) / 12; double Jy_24 = (z - f_2) * Math.Pow(b_v - r_4, 3) / 12;
            Jxi.Add(Jx_24); Jyi.Add(Jy_24);
            double Jx_3 = f_3 * Math.Pow(f_4, 3) / 36; double Jy_3 = f_4 * Math.Pow(f_3, 3) / 36;
            Jxi.Add(Jx_3); Jyi.Add(Jy_3);
            double Jx_4 = Jx_3; double Jy_4 = Jy_3;
            Jxi.Add(Jx_4); Jyi.Add(Jy_4); // определение собственных центрально осевых моментов инерции площадей конец

            double Jx = Jxi[0] + CoordinateY[0] * CoordinateY[0] * Area[0]; // определение момента инерции сечения
            double Jy = Jyi[0] + CoordinateX[0] * CoordinateX[0] * Area[0];
            for (int i = 1; i < Area.Count(); i++)
            {
                Jx = Jx - (Jxi[i] + CoordinateY[i] * CoordinateY[i] * Area[i]);
                Jy = Jy - (Jyi[i] + CoordinateX[i] * CoordinateX[i] * Area[i]);
            }

            double Xmax = Math.Max(l - Xc, Xc); // наиболее удаленная точка сечения вдоль оси X
            double Ymax = Math.Max(s - Yc, Yc); // наиболее удаленная точка сечения вдоль оси Y

            double Wx = Jx / Ymax; // определение момента сопротивления сечения
            double Wy = Jy / Xmax;

            if (A <= 0 || Jx <= 0 || Jy <= 0 || Wx <= 0 || Wy <= 0)
            {
                return (1, 1, 1, 1, 1);
            }
            else
            {
                return (A, Jx, Jy, Wx, Wy);
            }

        }

        double GetAddition(string element, string type)
        {
            string group = element;

            if (group == "corpus_22" || group == "corpus_33")
            {
                group = "corpusTooth";
            }
            if (group == "corpus_34" || group == "corpus_55")
            {
                group = "corpus";
            }

            switch (group)
            {
                case "cap":
                    switch (type)
                    {
                        case "work":
                            return _additions.c_cap_work;
                        case "test":
                            return _additions.c_cap_test;
                        default: throw new ArgumentException($"Неизвестный режим: {type}", nameof(type));
                    }

                case "cap_11":
                    switch (type)
                    {
                        case "work":
                            return _additions.c_capTooth_work;
                        case "test":
                            return _additions.c_capTooth_test;
                        default: throw new ArgumentException($"Неизвестный режим: {type}", nameof(type));
                    }

                case "corpusTooth":
                    switch (type)
                    {
                        case "work":
                            return _additions.c_corpus_work;
                        case "test":
                            return _additions.c_corpus_test;
                        default: throw new ArgumentException($"Неизвестный режим: {type}", nameof(type));
                    }
                case "corpus":
                    switch (type)
                    {
                        case "work":
                            return _additions.c_corpus_work * 2;
                        case "test":
                            return _additions.c_corpus_test * 2;
                        default: throw new ArgumentException($"Неизвестный режим: {type}", nameof(type));
                    }

                default: throw new ArgumentException($"Неизвестный элемент: {element}", nameof(element));
            }
        }
    }
}
