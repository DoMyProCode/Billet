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
        public string Name { get; set; }        // имя переменной
        public string TypeLoad { get; set; }    // вид нагрузки "work" или "test"
        public double Value { get; set; }       // вычисленное значение (возникающие напряжения, требуемая толщина, допускаемое кол-во циклов)
        public double Limit { get; set; }       // исходное/нормативное значение (допустимые напряжения, выбранная толщина, заданное кол-во циклов)
        public string Unit { get; set; }        // единица измерения

        // Флаг обратного условия: true только для циклов (Limit <= Value), false для напряжений и толщин (Value <= Limit)
        public bool IsReversed { get; set; } = false;

        public bool Conclusion => IsReversed
            ? Limit <= Value
            : Value <= Limit;

        public double? SafetyFactor
        {
            get
            {
                if (IsReversed)
                    return Limit != 0 ? Round.RoundDownToStep(Value / Limit, 0.01) : (double?)null;
                else
                    return Value != 0 ? Round.RoundDownToStep(Limit / Value, 0.01) : (double?)null;
            }
        }
    }
}

