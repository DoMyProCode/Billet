using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billet
{
    public class MaterialStressesRatios
    {
        public double ShearRatio { get; set; } = 0.5; // коэффициент работы на срез
        public double СrumpleRatio { get; set; } = 1.25; // коэффициент работы на смятие
        public double TensileRatio { get; set; } = 0.85; // коэффициент работы на растяжение
    }

    public class ConcentratorRatios
    {
        public double BendConcentratorRatio { get; set; } = 1.3; // коэффициент учета концентраторов напряжений при работе на изгиб
        public double ShearConcentratorRatio { get; set; } = 1; // коэффициент учета концентраторов напряжений при работе на срез
        public double СrumpleConcentratorRatio { get; set; } = 1; // коэффициент учета концентраторов напряжений при работе на смятие
        public double TensileConcentratorRatio { get; set; } = 1; // коэффициент учета концентраторов напряжений при работе на растяжение
    }
}
