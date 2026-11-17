using System.Collections.Generic;
using System.Text;

namespace Billet
{
    public class Report
    {
        private readonly List<ElementResult> _elementsResult;

        public Report(Results results)
        {
            _elementsResult = results.Elements;
        }

        public string GetReportText()
        {
            var sb = new StringBuilder();
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
    }
}
