using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billet
{
    public class Loads
    {
        public double WorkPressure { get; set; } = 8; // рабочее давление
        public double TestPressure { get; set; } = 12; // испытательное давление
        public double DesignTemperature { get; set; } = 80; // расчетная температура
    }
}
