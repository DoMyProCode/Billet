using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billet
{
    public class Material
    {
        MaterialStressesRatios _ratios = new MaterialStressesRatios();
        public Material(MaterialStressesRatios ratios)
        {
            _ratios = ratios;
        }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Sigma_t { get; set; } = 147.7; // допускаемое напряжение при работе на изгиб в рабочих условиях
        public double Sigma_20 { get; set; } = 222.7; // допускаемое напряжение при работе на изгиб в условиях испытаний
        public double Tau_sr_t => Sigma_t * _ratios.ShearRatio; // допускаемое напряжение при работе на срез в рабочих условиях
        public double Tau_sr_20 => Sigma_20 * _ratios.ShearRatio; // допускаемое напряжение при работе на срез в условиях испытаний
        public double Sigma_sm_t => Sigma_t * _ratios.СrumpleRatio; // допускаемое напряжение при работе на смятие в рабочих условиях
        public double Sigma_sm_20 => Sigma_20 * _ratios.СrumpleRatio; // допускаемое напряжение при работе на смятие в условиях испытаний
        public double Sigma_ras_t => Sigma_t * _ratios.TensileRatio; // допускаемое напряжение при работе на растяжение в рабочих условиях
        public double Sigma_ras_20 => Sigma_20 * _ratios.TensileRatio; // допускаемое напряжение при работе на растяжение в условиях испытаний
    }
}
