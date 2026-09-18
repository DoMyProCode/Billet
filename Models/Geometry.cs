using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billet
{
    public class Geometry
    {
        public double b_k { get; set; } = 5; // зазор просвета между внешним радиусом крышки и внутренним радиусом зева корпуса
        public double b_pr { get; set; } = 11; // зазор просвета между вырезом в крышке и внутренним радиусом зуба корпуса
        public double b_z { get; set; } = 20; // минимальный зазор между зубьями корпуса и крышки в открытом положении
        public double D_corp_ex { get; set; } = 0; // внешний диаметр корпуса
        public double D_nom { get; set; } = 1400; // проходное сечение корпуса
        public double D_pr { get; set; } = 1444; // внешний диаметр прокладки
        public double d { get; set; } = 0; // диаметр отверстия в крышке (для штуцера)
        public double f_1 { get; set; } = 10; // ширина фаски на обнижении зуба крышки
        public double f_2 { get; set; } = 3; // глубина фаски на обнижении зуба крышки
        public double f_3 { get; set; } = 15; // ширина фаски по внутренней плоскости крышки в радиальном направлении
        public double f_4 { get; set; } = 60; // ширина фаски по внутренней плоскости крышки в осевом направлении
        public double f_5 { get; set; } = 0; // ширина фаски по внешней плоскости корпуса в радиальном направлении
        public double f_6 { get; set; } = 0; // ширина фаски по внешней плоскости корпуса в осевом направлении
        public double g { get; set; } = 40; // толщина хвостовика под приварку
        public double h { get; set; } = 35; // глубина уклона зуба корпуса
        public double k_pr { get; set; } = 5; // отношение полного угла зуба к углу обнижения
        public double n { get; set; } = 16; // количество зубьев
        public double p { get; set; } = 10; // нахлест крышки на прокладку
        public double R_ex { get; set; } = 0; // внешний радиус крышки
        public double r_1 { get; set; } = 7; // радиус скругления по верхней плоскости в месте присоединения зуба к крышке
        public double r_2 { get; set; } = 2; // радиус скругления зуба корпуса по плоскости контакта
        public double r_3 { get; set; } = 10; // радиус резки
        public double r_4 { get; set; } = 7; // радиус скругления на обнижении зуба
        public double r_5 { get; set; } = 8; // радиус канавки под зубом корпуса
        public double s_1 { get; set; } = 0; // толщина крышки
        public double s_11 { get; set; } = 20; // толщина зуба крышки
        public double s_21 { get; set; } = 10; // толщина зуба корпуса
        public double z { get; set; } = 10; // глубина обнижения зуба крышки

    }
}
