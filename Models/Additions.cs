using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billet
{
    public class Additions
    {
        public double c_cap_w { get; set; } = 5.5; // прибавка для расчета толщины плоской крышки в рабочих условиях
        public double c_cap_t { get; set; } = 0.5; // прибавка для расчета толщины плоской крышки в условиях испытаний
        public double c_capTooth_w { get; set; } = 5.1; // прибавка для расчета толщины зуба крышки в рабочих условиях
        public double c_capTooth_t { get; set; } = 0.1; // прибавка для расчета толщины зуба крышки в условиях испытаний
        public double c_corpus_w { get; set; } = 0.5; // прибавка для расчета толщины зуба корпуса в рабочих условиях
        public double c_corpus_t { get; set; } = 0.5; // прибавка для расчета толщины зуба корпуса в условиях испытаний
    }
}
