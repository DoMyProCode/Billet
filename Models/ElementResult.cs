using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billet
{
    public class ElementResult
    {
        public string Description { get; set; } // описание элемента
        public string Name { get; set; } // имя переменной
        public string TypeLoad { get; set; } // вид нагрузки "work" или "test"
        public double Value { get; set; } // возникающие напряжения, требуемые толщины, допускаемое кол-во циклов
        public double Limit { get; set; } // допустимые напряжения, выбранные толщины, заданное кол-во циклов
        public string Unit {  get; set; } // единица измерения
        public bool Conclusion => Value <= Limit;  // автоматический расчет условия выполнения
        public double? SafetyFactor => Value != 0 ? Round.RoundDownToStep(Limit / Value, 0.01) : (double?)null; // коэффициент безопасности на концентраторы напряжения
    }
}
