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
        public double Rmt { get; set; } = 398.5; // предел прочности при работе на изгиб, МПа
        public double Sigma_t { get; set; } = 147.7; // допускаемое напряжение при работе на изгиб в рабочих условиях, МПа
        public double Sigma_20 { get; set; } = 222.7; // допускаемое напряжение при работе на изгиб в условиях испытаний, МПа
        public double Tau_sr_t => Round.RoundDownToStep(Sigma_t * _ratios.ShearRatio, 0.1); // допускаемое напряжение при работе на срез в рабочих условиях
        public double Tau_sr_20 => Round.RoundDownToStep(Sigma_20 * _ratios.ShearRatio, 0.1); // допускаемое напряжение при работе на срез в условиях испытаний
        public double Sigma_sm_t => Round.RoundDownToStep(Sigma_t * _ratios.СrumpleRatio, 0.1); // допускаемое напряжение при работе на смятие в рабочих условиях
        public double Sigma_sm_20 => Round.RoundDownToStep(Sigma_20 * _ratios.СrumpleRatio, 0.1); // допускаемое напряжение при работе на смятие в условиях испытаний
        public double Sigma_ras_t => Round.RoundDownToStep(Sigma_t * _ratios.TensileRatio, 0.1); // допускаемое напряжение при работе на растяжение в рабочих условиях
        public double Sigma_ras_20 => Round.RoundDownToStep(Sigma_20 * _ratios.TensileRatio, 0.1); // допускаемое напряжение при работе на растяжение в условиях испытаний
        public double A { get; set; } = 45000; // Характеристика "А" для расчета на малоцикловую прочность
        public double n_N { get; set; } = 10; // коэффициент запаса прочности по числу циклов
        public double n_sigma { get; set; } = 2.0; // коэффицинет запаса по напряжениям
        public double K_B { get; set; } = 0.4; // коэффициент в формуле характеристики B
        public double T_Ct { get; set; } = 2300; // величина температуры в формуле характеристики Сt
    }
}
