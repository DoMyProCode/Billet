using System;
using System.Collections.Generic;
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
        Steps _steps;
        Additions _additions;

        double pi = Math.PI;
        double R_in;
        double alfa;
        double alfa_2;
        public Calculations(Loads loads, Geometry geometry, Material material, ConcentratorRatios concentrator, Steps steps, Additions additions)
        {
            _geometry = geometry;
            _material = material;
            _loads = loads;
            _concentrator = concentrator;
            _steps = steps;
           _additions = additions;

            SelectСrumple();
            SelectShear("cap");
            Console.WriteLine($" толщина зуба крышки {geometry.s_11} мм");
            SelectShear("corpus");
            Console.WriteLine($" толщина зуба корпуса {geometry.s_21} мм");
        }

        bool СrumpleCheck(string type) // проверка на смятие
        {
            double sigma = GetLoad(type) / GetContactArea() * _concentrator.СrumpleConcentratorRatio;

            switch (type)
            {
                case "w":
                    if (sigma <= _material.Sigma_sm_t)
                    {
                        //Console.WriteLine($"{_material.Sigma_sm_t} > {sigma} - проходит");
                        //Console.WriteLine($" Площадь смятия {GetContactArea()} мм");
                        return true;
                    }
                    else
                    {
                        //Console.WriteLine($"{_material.Sigma_sm_t} < {sigma} - НЕ проходит");
                        return false;
                    }

                case "t":
                    if (sigma <= _material.Sigma_sm_20)
                    {
                        //Console.WriteLine($"{_material.Sigma_sm_20} > {sigma} - проходит");
                        //Console.WriteLine($" Площадь смятия {GetContactArea()} мм");
                        return true;
                    }
                    else
                    {
                        //Console.WriteLine($"{_material.Sigma_sm_20} < {sigma} - НЕ проходит");
                        return false;
                    }

                default:
                    {
                        return false;
                    }
            }
        }

        bool ShearCheck(string element, string type, double l, double s) // проверка на срез
        {
            var geometry = GetGeomCharacter(element, l, s);
            double tau = GetLoad(type) / (geometry.A * _geometry.n) * _concentrator.ShearConcentratorRatio;
            switch (type)
            {
                case "w":
                    if (tau <= _material.Tau_sr_t)
                    {
                        //Console.WriteLine($"{_material.Tau_sr_t} > {tau} - проходит");
                        //Console.WriteLine($" Площадь среза {geometry.A} мм");
                        return true;
                    }
                    else
                    {
                        //Console.WriteLine($"{_material.Tau_sr_t} < {tau} - НЕ проходит");
                        return false;
                    }

                case "t":
                    if (tau <= _material.Tau_sr_20)
                    {
                        //Console.WriteLine($"{_material.Tau_sr_20} > {tau} - проходит");
                        //Console.WriteLine($" Площадь среза {geometry.A} мм");
                        return true;
                    }
                    else
                    {
                        //Console.WriteLine($"{_material.Tau_sr_20} < {tau} - НЕ проходит");
                        return false;
                    }

                default:
                    {
                        return false;
                    }
            }
        }

        void SelectСrumple() // подбор внешнего радиуса зубьев крышки
        {
            while (!СrumpleCheck("w") || !СrumpleCheck("t"))
            {
                _geometry.R_ex = _geometry.R_ex + _steps.СrumpleStep;
            }
        }
        void SelectShear(string element)  // подбор подбор толщины зуба из условия среза
        {
            switch (element)
            {
                case "cap":

                    while (!ShearCheck(element, "w", GetToothWidth(element), _geometry.s_11 - _additions.c_capTooth_w) || !ShearCheck(element, "t", GetToothWidth(element), _geometry.s_11 - _additions.c_capTooth_t))
                    {
                        _geometry.s_11 = _geometry.s_11 + _steps.ToothStep;
                    }
                    break;
                case "corpus":
                    while (!ShearCheck(element, "w", GetToothWidth(element), _geometry.s_21 - _additions.c_corpus_w) || !ShearCheck(element, "t", GetToothWidth(element), _geometry.s_21 - _additions.c_corpus_t))
                    {
                        _geometry.s_21 = _geometry.s_21 + _steps.ToothStep;
                    }
                    break;
                default: break;
            }
        }



        public double GetLoad(string type)
        {
            double S = Round.RoundUpToStep(pi * _geometry.D_pr * _geometry.D_pr / 4, 1); // площадь крышки находящаяся под давлением
            double F = 0; // сила на крышке от давления, Н
            double P = 0; // давление, МПа

            switch (type)
            {
                case "w": P = _loads.WorkPressure; break;
                case "t": P = _loads.TestPressure; break;
            }

            F = Round.RoundUpToStep(S * P, 1); // сила на крышке от давления, Н

            return F;
        }

        public double GetContactArea()
        {
            R_in = _geometry.D_pr / 2 + _geometry.p; // внутренний диаметр зубьев крышки
            double R_1 = R_in + _geometry.b_pr + _geometry.r_2; // внутренний диаметр контакта зубьев
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
                case "corpus":
                    l = Round.RoundDownToStep((_geometry.R_ex + _geometry.b_k) * Math.Sin(alfa * pi / 10800), 1);
                    break;
            }
            return l;
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
                case "corpus":
                    f_3 = _geometry.f_5;
                    f_4 = _geometry.f_6;
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

            return (A, Jx, Jy, Wx, Wy);
        }
    }
}
