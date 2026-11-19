using System;
using System.Collections.Generic;
using System.Text;

namespace Billet
{
    public class Report
    {
        private readonly Material _material;
        private readonly Loads _loads;
        private readonly Geometry _geometry;
        OtherRatios _otherRatios;
        Additions _additions;
        private readonly List<ElementResult> _elementsResult;

        public Report(Material material, Loads loads, Geometry geometry, OtherRatios otherRatios, Additions additions, Results results)
        {
            _material = material;
            _loads = loads;
            _geometry = geometry;
            _otherRatios = otherRatios;
            _additions = additions;
            _elementsResult = results.Elements;
        }

        public string GetReportText()
        {
            var sb = new StringBuilder();

            sb.AppendLine("РАСЧЕТ");

            foreach (var element in _elementsResult)
            {
                string condition = "";
                switch (element.TypeLoad)
                {
                    case "work":
                        condition = " в рабочих условиях";
                        break;
                    case "test":
                        condition = " в условиях испытаний";
                        break;
                }

                string conclusion = element.Conclusion
                    ? "Условие выполнено"
                    : "Условие НЕ выполнено!!!";

                // Формируем неравенство по инженерным правилам
                string inequality;
                if (element.Unit == "мм")
                {
                    // 1. Толщина заданная >= толщина требуемая
                    inequality = $"{element.Limit} >= {element.Value} {element.Unit}";
                }
                else if (element.IsReversed)
                {
                    // 3. Эксплуатационное кол-во циклов <= допускаемого (вычисленного)
                    inequality = $"{element.Limit} <= {element.Value} {element.Unit}";
                }
                else
                {
                    // 2. Напряжения вычисленные <= напряжения допустимые
                    inequality = $"{element.Value} <= {element.Limit} {element.Unit}";
                }

                sb.AppendLine($"\n{element.Description}{condition}");
                sb.AppendLine($"{element.Name} = {element.Value}");
                sb.AppendLine(inequality);
                sb.AppendLine(conclusion);
                sb.AppendLine($"Коэффициент запаса - {element.SafetyFactor}");
            }
            return sb.ToString();
        }
        public string GetInitialDataText()
        {
            var sb = new StringBuilder();

            string FormatRow(string label, string symbol, object value, string unit = "")
            {
                return $"{label,-60} {symbol,-10} {value,-10} {unit}".TrimEnd();
            }

            sb.AppendLine("ИСХОДНЫЕ ДАННЫЕ");

            sb.AppendLine("\nМАТЕРИАЛ ОСНОВНЫХ ДЕТАЛЕЙ ЗАТВОРА\n");
            sb.AppendLine($"Материал:{_material.Name}");
            sb.AppendLine($"Описание:{_material.Description}");

            sb.AppendLine("Допускаемые напряжения:");
            sb.AppendLine(FormatRow("- для рабочих условий:", "[sigma]", _material.Sigma_t, "МПа"));
            sb.AppendLine(FormatRow("- для условий испытаний:", "[sigma]20", _material.Sigma_20, "МПа"));
            sb.AppendLine(FormatRow("Модуль упругости:", "E", _material.E, "МПа"));

            sb.AppendLine("Данные для расчета на малоцикловую прочность:");
            sb.AppendLine(FormatRow("Временное сопротивление при расчетной температуре:", "Rm/tр", _material.Rmt, "МПа"));
            sb.AppendLine(FormatRow("Характеристика материала:", "А", _material.A, "МПа"));
            sb.AppendLine(FormatRow("Коэффициент в формуле характеристики \"B\":", "K_B", _material.K_B));
            sb.AppendLine(FormatRow("Величина температуры в формуле характеристики \"Сt\":", "T", _material.T_Ct, "К"));
            sb.AppendLine(FormatRow("Коэффициент запаса по числу циклов:", "nN", _material.n_N));
            sb.AppendLine(FormatRow("Коэффициент запаса по напряжениям:", "nSigma", _material.n_sigma));

            sb.AppendLine($"\nНАГРУЗКИ И ВОЗДЕЙСТВИЯ\n");
            sb.AppendLine(FormatRow("Расчетное давление:", "P", _loads.WorkPressure, "МПа"));
            sb.AppendLine(FormatRow("Пробное давление:", "Рпр", _loads.TestPressure, "МПа"));
            sb.AppendLine(FormatRow("Расчетная температура:", "tр", _loads.DesignTemperature, "град.С"));
            sb.AppendLine(FormatRow("Эксплуатационное количество циклов нагружения:", "N", _loads.NumberOfCycles, "цикл"));

            sb.AppendLine($"\nГЕОМЕТРИЧЕСКИЕ ПАРАМЕТРЫ\n");
            sb.AppendLine(FormatRow("Коэффициент учитывающий условия опирания крышки:", "K", _otherRatios.CapSupportRatio));
            sb.AppendLine(FormatRow("Зазор между зубом корпуса и цилиндрической частью крышки:", "b", _geometry.b_pr, "мм"));
            sb.AppendLine(FormatRow("Наружный диаметр затвора:", "D", _geometry.D_corp_ex, "мм"));
            sb.AppendLine(FormatRow("Проходное сечение затвора:", "DN", _geometry.D_nom, "мм"));
            sb.AppendLine(FormatRow("Толщина кромки под приварку:", "g", _geometry.g, "мм"));
            sb.AppendLine(FormatRow("Внутренний диаметр зубьев корпуса:", "Dвн", (_geometry.D_pr / 2 + _geometry.p + _geometry.b_pr) * 2, "мм"));
            sb.AppendLine(FormatRow("Наружный диаметр зубьев крышки:", "Dнар", _geometry.R_ex * 2, "мм"));
            sb.AppendLine(FormatRow("Внешний диаметр уплотнения в рабочем положении:", "Dпр", _geometry.D_pr, "мм"));
            sb.AppendLine(FormatRow("Глубина канавки либо внутреннего уклона зуба (большее):", "h", _geometry.h, "мм"));
            sb.AppendLine(FormatRow("Внутренний уклон зуба:", "alfa", _geometry.alfa_cut, "град"));
            sb.AppendLine(FormatRow("Количество зубьев:", "n", _geometry.n, "шт"));

            sb.AppendLine(FormatRow("Радиус галтели зуба крышки по верхней плоскости:", "r1", _geometry.r_1, "мм"));
            sb.AppendLine(FormatRow("Радиус притупления зуба корпуса по плоскости контакта:", "r2", _geometry.r_2, "мм"));
            sb.AppendLine(FormatRow("Радиус резки:", "r3", _geometry.r_3, "мм"));
            sb.AppendLine(FormatRow("Радиус скругления на обнижении зуба:", "r4", _geometry.r_4, "мм"));
            sb.AppendLine(FormatRow("Ширина фаски на обнижении зуба крышки:", "f1", _geometry.f_1, "мм"));
            sb.AppendLine(FormatRow("Глубина фаски на обнижении зуба крышки:", "f2", _geometry.f_2, "мм"));
            sb.AppendLine(FormatRow("Глубина обнижения зуба крышки:", "z", _geometry.z, "мм"));
            sb.AppendLine(FormatRow("Минимальный зазор между зубьями в открытом положении:", "bz", _geometry.b_z, "мм"));
            sb.AppendLine(FormatRow("Нахлест крышки на уплотнение:", "p", _geometry.p, "мм"));
            sb.AppendLine(FormatRow("Отношение полного угла зуба к углу обнижения:", "kpr", _geometry.k_pr));
            sb.AppendLine(FormatRow("Исполнительная толщина стенки крышки:", "s1", _geometry.s_1, "мм"));
            sb.AppendLine(FormatRow("Исполнительная толщина зуба крышки:", "s11", _geometry.s_11, "мм"));
            sb.AppendLine(FormatRow("Исполнительная толщина зуба корпуса:", "s21", _geometry.s_21, "мм"));
            if(_geometry.f_3 != 0 && _geometry.f_4 != 0)
            {
                sb.AppendLine(FormatRow("Ширина фаски зуба крышки:", "f3", _geometry.f_3, "мм"));
                sb.AppendLine(FormatRow("Высота фаски зуба крышки:", "f4", _geometry.f_4, "мм"));
            }

            sb.AppendLine($"\nРАСЧЕТНЫЕ ПРИБАВКИ\n");
            sb.AppendLine($"Расчетные прибавки для расчета толщины плоской крышки:");
            sb.AppendLine(FormatRow("- для рабочих условий:", "c1р", _additions.c_cap_work, "мм"));
            sb.AppendLine(FormatRow("- для условий испытаний:", "c1и", _additions.c_cap_test, "мм"));
            sb.AppendLine($"Расчетные прибавки для расчета толщины зуба крышки:");
            sb.AppendLine(FormatRow("- для рабочих условий:", "c11р", _additions.c_capTooth_work, "мм"));
            sb.AppendLine(FormatRow("- для условий испытаний:", "c11и", _additions.c_capTooth_test, "мм"));
            sb.AppendLine($"Расчетные прибавки для расчета толщины зуба корпуса:");
            sb.AppendLine(FormatRow("- для рабочих условий:", "c21р", _additions.c_corpus_work, "мм"));
            sb.AppendLine(FormatRow("- для условий испытаний:", "c21и", _additions.c_corpus_test, "мм"));


            return sb.ToString();
        }
    }
}
