using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billet
{
    public class Report
    {
        List<ElementResult> _elementsResult;
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
                string conclusion = "";

                switch (element.TypeLoad)
                {
                    case "work":
                        condition = " в рабочих условиях";
                        break;
                    case "test":
                        condition = " в условиях испытаний";
                        break;
                    default: break;
                }

                if (element.Conclusion)
                {
                    conclusion = "Условие выполнено";
                }
                else
                {
                    conclusion = "Условие НЕ выполнено!!!";
                }

                sb.AppendLine($"\n{element.Description}{condition}");
                sb.AppendLine($"{element.Name} = {element.Value}");
                sb.AppendLine($"{element.Limit} >= {element.Value} {element.Unit}");
                sb.AppendLine($"{conclusion}");
                sb.AppendLine($"Коэффициент запаса - {element.SafetyFactor}");
            }
            return sb.ToString();
        }
    }
}
