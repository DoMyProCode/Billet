using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
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

        double pi = Math.PI;
        double R_in;
        double R_1; // внутренний радиус контакта = внутренний радиус зубьев корпуса + r_2 (радиус скругления зуба кропуса)
        double alfa;
        double alfa_2;
        public Calculations(Loads loads, Geometry geometry, Material material, ConcentratorRatios concentrator, OtherRatios otherRatios, Steps steps, Additions additions)
        {
            _geometry = geometry;
            _material = material;
            _loads = loads;
            _concentrator = concentrator;
            _otherRatios = otherRatios;
            _steps = steps;
            _additions = additions;

            SelectСrumple();
            SelectShear("cap");
            SelectBend("cap");
            //SelectShear("corpus_22");
            //SelectBend("corpus_22");
            //SelectBend("corpus_33");
            //SelectTensileBend("corpus_34");
            //SelectTensileBend("corpus_55");
            //SelectCapThickness();


            Console.WriteLine(GetCheckReport());
            Console.WriteLine(_geometry.s_11);

        }

        string GetCheckReport()
        {
            var sb = new StringBuilder();

            // sb.AppendLine($"ГЕОМЕТРИЧЕСКИЕ ПАРАМЕТРЫ");

            sb.AppendLine("\n- Расчет плоской крышки в рабочих условиях");
            sb.AppendLine("Толщина плоской крышки работающей под внутренним избыточным давлением");

            var s1p = CheckCapThickness("work", _geometry.s_1);
            sb.AppendLine($"s1р = {s1p.thickness}");
            sb.AppendLine($"{_geometry.s_1} >= {s1p.thickness + _additions.c_cap_work} мм");
            sb.AppendLine($"{GetConclusion(s1p.conclusion)}");

            sb.AppendLine("\n- Расчет на смятие зоны опирания крышки затвора на корпус");
            var sigma_sm_r = CheckСrumple("work");
            sb.AppendLine($"Сигма_см.р = {sigma_sm_r.stress} МПа");
            sb.AppendLine($"{_material.Sigma_sm_t} >= {sigma_sm_r.stress} МПа");
            sb.AppendLine($"{GetConclusion(sigma_sm_r.conclusion)}");

            sb.AppendLine("\n- Расчет на срез зуба крышки в сечении I-I");
            var tau_sr_r11 = CheckShear("cap", "work", GetToothWidth("cap"), _geometry.s_11 - _additions.c_capTooth_work);
            sb.AppendLine($"Тау_ср.р11 = {tau_sr_r11.stress} МПа");
            sb.AppendLine($"{_material.Tau_sr_t} >= {tau_sr_r11.stress} МПа");
            sb.AppendLine($"{GetConclusion(tau_sr_r11.conclusion)}");

            sb.AppendLine("\n- Расчет на изгиб зуба крышки в сечении I-I");
            var sigma_n_r11 = CheckBend("cap", "work", GetToothWidth("cap"), _geometry.s_11 - _additions.c_capTooth_work, _geometry.r_1);
            sb.AppendLine($"Сигма_n.р11 = {sigma_n_r11.stress} МПа");
            sb.AppendLine($"{_material.Sigma_t} >= {sigma_n_r11.stress} МПа");
            sb.AppendLine($"[N11] = {sigma_n_r11.cycles}");
            sb.AppendLine($"{_loads.NumberOfCycles} >= {sigma_n_r11.cycles}");
            sb.AppendLine($"{GetConclusion(sigma_n_r11.conclusion)}");

            //sb.AppendLine("\n- Расчет корпуса в рабочих условиях");
            //sb.AppendLine("\n- Расчет на срез зуба корпуса в сечении II-II");
            //var tau_sr_r22 = CheckShear("corpus_22", "work", GetToothWidth("corpus_22"), _geometry.s_21 - _additions.c_corpus_work);
            //sb.AppendLine($"Тау_ср.р12 = {tau_sr_r22.stress} МПа");
            //sb.AppendLine($"{_material.Tau_sr_t} >= {tau_sr_r22.stress} МПа");
            //sb.AppendLine($"{GetConclusion(tau_sr_r22.conclusion)}");

            //sb.AppendLine("\n- Расчет на изгиб зуба корпуса в сечении II-II");
            //var sigma_n_r22 = CheckBend("corpus_22", "work", GetToothWidth("corpus_22"), _geometry.s_21 - _additions.c_corpus_work, _geometry.r_5);
            //sb.AppendLine($"Сигма_n.р22= = {sigma_n_r22.stress} МПа");
            //sb.AppendLine($"{_material.Sigma_t} >= {sigma_n_r22.stress} МПа");
            //sb.AppendLine($"{GetConclusion(sigma_n_r22.conclusion)}");

            //sb.AppendLine("\n- Расчет на изгиб зуба корпуса в сечении III-III");
            //var sigma_n_r33 = CheckBend("corpus_33", "work", GetToothWidth("corpus_33"), (_geometry.s_21 + _geometry.r_5) - _additions.c_corpus_work, _geometry.r_5);
            //sb.AppendLine($"Сигма_n.р33= = {sigma_n_r33.stress} МПа");
            //sb.AppendLine($"{_material.Sigma_t} >= {sigma_n_r33.stress} МПа");
            //sb.AppendLine($"{GetConclusion(sigma_n_r33.conclusion)}");

            //sb.AppendLine("\n- Расчет на изгиб и растяжение корпуса в сечении III-IV");
            //var sigma_sum_r34 = CheckTensileBend("corpus_34", "work", GetSectionGeometry("corpus_34", "work").A, GetSectionGeometry("corpus_34").l, GetSectionGeometry("corpus_34", "work").s, GetSectionGeometry("corpus_34", "work").r);
            //sb.AppendLine($"Сигма_сум.р34= = {sigma_sum_r34.stress} МПа");
            //sb.AppendLine($"{_material.Sigma_t} >= {sigma_sum_r34.stress} МПа");
            //sb.AppendLine($"{GetConclusion(sigma_sum_r34.conclusion)}");

            //sb.AppendLine("\n- Расчет на изгиб и растяжение корпуса в сечении V-V");
            //var sigma_sum_r55 = CheckTensileBend("corpus_55", "work", GetSectionGeometry("corpus_55", "work").A, GetSectionGeometry("corpus_55").l, GetSectionGeometry("corpus_55", "work").s, GetSectionGeometry("corpus_55", "work").r);
            //sb.AppendLine($"Сигма_сум.р55= = {sigma_sum_r55.stress} МПа");
            //sb.AppendLine($"{_material.Sigma_t} >= {sigma_sum_r55.stress} МПа");
            //sb.AppendLine($"{GetConclusion(sigma_sum_r55.conclusion)}");

            return sb.ToString();



        }

        (bool conclusion, double stress) CheckСrumple(string type) // проверка на смятие
        {
            double sigma = Round.RoundUpToStep(GetLoad(type) / GetContactArea() * _concentrator.СrumpleConcentratorRatio, 0.1);

            switch (type)
            {
                case "work":
                    if (sigma <= _material.Sigma_sm_t)
                    {
                        return (true, sigma);
                    }
                    else
                    {
                        return (false, sigma);
                    }

                case "test":
                    if (sigma <= _material.Sigma_sm_20)
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
            double tau = Round.RoundUpToStep(GetLoad(type) / (geometry.A * _geometry.n) * _concentrator.ShearConcentratorRatio, 0.1);
            switch (type)
            {
                case "work":
                    if (tau <= _material.Tau_sr_t)
                    {
                        //Console.WriteLine($"{_material.Tau_sr_t} > {tau} - проходит");
                        //Console.WriteLine($" Площадь среза {geometry.A} мм");
                        return (true, tau);
                    }
                    else
                    {
                        //Console.WriteLine($"{_material.Tau_sr_t} < {tau} - НЕ проходит");
                        return (false, tau);
                    }

                case "test":
                    if (tau <= _material.Tau_sr_20)
                    {
                        //Console.WriteLine($"{_material.Tau_sr_20} > {tau} - проходит");
                        //Console.WriteLine($" Площадь среза {geometry.A} мм");
                        return (true, tau);
                    }
                    else
                    {
                        //Console.WriteLine($"{_material.Tau_sr_20} < {tau} - НЕ проходит");
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
            double sigma = Round.RoundUpToStep(GetLoad(type) * console / (geometry.Wx * n) * _concentrator.BendConcentratorRatio, 0.1);
            var fatique = CheckFatique(sigma, s + _additions.c_capTooth_work, console, r);

            switch (type)
            {
                case "work":
                    if (sigma <= _material.Sigma_t && fatique.conclusion)
                    {
                        return (true, sigma, fatique.NumberOfCycles);
                    }
                    else
                    {
                        return (false, sigma, fatique.NumberOfCycles);
                    }

                case "test":
                    if (sigma <= _material.Sigma_20)
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

        (bool conclusion, double stress) CheckTensileBend(string element, string type, double area, double l, double s, double r) // проверка на растяжение с изгибом
        {
            double console = GetConsole(element);
            var geometry = GetGeomCharacter(element, l, s);
            double sigma_Tensile = GetLoad(type) / area * _concentrator.TensileConcentratorRatio;
            double sigma_Bend = GetLoad(type) * console / (geometry.Wx) * _concentrator.TensileConcentratorRatio;
            double sigma = Round.RoundUpToStep(sigma_Tensile + sigma_Bend, 0.1);

            switch (type)
            {
                case "work":
                    if (sigma <= _material.Sigma_ras_t && CheckFatique(sigma, s, console, r).conclusion)
                    {
                        return (true, sigma);
                    }
                    else
                    {
                        return (false, sigma);
                    }

                case "test":
                    if (sigma <= _material.Sigma_ras_20)
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

            if (s <= thickness)
            {
                //Console.WriteLine(D_p);
                //Console.WriteLine(_geometry.R_ex * 2);
                //Console.WriteLine(R_1 * 2);
                return (true, s);
            }
            else
            {
                return (false, s);
            }
        }

        (bool conclusion, double NumberOfCycles) CheckFatique(double sigma, double s, double h, double r)
        {
            double B = _material.K_B * _material.Rmt;
            double C_t = Round.RoundDownToStep((_material.T_Ct - _loads.DesignTemperature) / _material.T_Ct, 0.001);
            double alfa_sigma = Round.RoundUpToStep(1 + (0.17 + 0.32 * (s / h)) * Math.Pow((h / r), 0.45), 0.001); // теоретический коэффициент концентрации напряжений
            double sigma_a = Round.RoundUpToStep(alfa_sigma * sigma / 2, 0.1); // аплитуда напряжений цикла
            double N = Math.Floor(1 / _material.n_N * Math.Pow(_material.A * C_t / (sigma_a - B / _material.n_sigma), 2)); // допустимое число циклов
            if (N >= _loads.NumberOfCycles)
            {
                return (true, N);
            }
            else
            {
                return (false, N);
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
                case "cap":

                    while (!CheckShear(element, "work", GetToothWidth(element), _geometry.s_11 - _additions.c_capTooth_work).conclusion || !CheckShear(element, "test", GetToothWidth(element), _geometry.s_11 - _additions.c_capTooth_test).conclusion)
                    {
                        _geometry.s_11 = _geometry.s_11 + _steps.ToothStep;
                    }
                    break;
                case "corpus_22":
                    while (!CheckShear(element, "work", GetToothWidth(element), _geometry.s_21 - _additions.c_corpus_work).conclusion || !CheckShear(element, "test", GetToothWidth(element), _geometry.s_21 - _additions.c_corpus_test).conclusion)
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
                case "cap":

                    while (!CheckBend(element, "work", GetToothWidth(element), _geometry.s_11 - _additions.c_capTooth_work, _geometry.r_1).conclusion || !CheckBend(element, "test", GetToothWidth(element), _geometry.s_11 - _additions.c_capTooth_test, _geometry.r_1).conclusion)
                    {
                        _geometry.s_11 = _geometry.s_11 + _steps.ToothStep;
                    }
                    break;
                case "corpus_22":
                    while (!CheckBend(element, "work", GetToothWidth(element), _geometry.s_21 - _additions.c_corpus_work, _geometry.r_5).conclusion || !CheckBend(element, "test", GetToothWidth(element), _geometry.s_21 - _additions.c_corpus_test, _geometry.r_5).conclusion)
                    {
                        _geometry.s_21 = _geometry.s_21 + _steps.ToothStep;
                    }
                    break;
                case "corpus_33":
                    while (!CheckBend(element, "work", GetToothWidth(element), (_geometry.s_21 + _geometry.r_5) - _additions.c_corpus_work, _geometry.r_5).conclusion || !CheckBend(element, "test", GetToothWidth(element), (_geometry.s_21 + _geometry.r_5) - _additions.c_corpus_test, _geometry.r_5).conclusion)
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
            while (!CheckCapThickness("work", _geometry.s_1 - _additions.c_cap_work).conclusion || !CheckCapThickness("test", _geometry.s_1 - _additions.c_cap_test).conclusion)
            {
                _geometry.s_1 = _geometry.s_1 + _steps.CapStep;
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
                case "cap":
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
                            A = pi * (Math.Pow(_geometry.D_corp_ex / 2 - _additions.c_corpus_work, 2) - Math.Pow(_geometry.R_ex + _geometry.b_k + _geometry.r_5 + _additions.c_corpus_work, 2));
                            s = (_geometry.D_corp_ex / 2 - _additions.c_corpus_work) - (_geometry.R_ex + _geometry.b_k + _geometry.r_5 + _additions.c_corpus_work);
                            break;
                        case "test":
                            A = pi * (Math.Pow(_geometry.D_corp_ex / 2 - _additions.c_corpus_test, 2) - Math.Pow(_geometry.R_ex + _geometry.b_k + _geometry.r_5 + _additions.c_corpus_test, 2));
                            s = (_geometry.D_corp_ex / 2 - _additions.c_corpus_test) - (_geometry.R_ex + _geometry.b_k + _geometry.r_5 + _additions.c_corpus_test);
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
                            A = pi * (Math.Pow(_geometry.D_corp_ex / 2 - _additions.c_corpus_work, 2) - Math.Pow(_geometry.R_ex + _geometry.b_k + _additions.c_corpus_work, 2));
                            s = (_geometry.D_corp_ex / 2 - _additions.c_corpus_work) - (_geometry.R_ex + _geometry.b_k + _additions.c_corpus_work);

                            break;
                        case "test":
                            A = pi * (Math.Pow(_geometry.D_corp_ex / 2 - _additions.c_corpus_test, 2) - Math.Pow(_geometry.R_ex + _geometry.b_k + _additions.c_corpus_test, 2));
                            s = (_geometry.D_corp_ex / 2 - _additions.c_corpus_test) - (_geometry.R_ex + _geometry.b_k + _additions.c_corpus_test);
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
                case "cap":
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
                    L = (_geometry.D_corp_ex / 2 - (_geometry.D_nom - (_geometry.R_ex + _geometry.b_k)) - _geometry.g) / 2;
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
                case "cap":
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


            //Console.WriteLine(Wx);
            //Console.WriteLine(Wy);

            //Console.WriteLine("------------------------------");
            //foreach (double item in CoordinateX)
            //{
            //    Console.WriteLine(item);
            //}
            //Console.WriteLine("------------------------------");
            //foreach (double item in CoordinateY)
            //{
            //    Console.WriteLine(item);
            //}
            //Console.WriteLine("------------------------------");
            if (A <= 0 || Jx <= 0 || Jy <= 0 || Wx <= 0 || Wy <= 0)
            {
                return (1, 1, 1, 1, 1);
            }
            else
            {
                return (A, Jx, Jy, Wx, Wy);
            }

        }

        string GetConclusion(bool conclusion)
        {
            if (conclusion)
            {
                return "Условие выполнено";
            }
            else
            {
                return "Условие НЕ выполнено!!!";
            }
        }
    }
}
