using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Billet
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            Geometry geometry = new Geometry();
            Loads loads = new Loads();
            MaterialStressesRatios ratios = new MaterialStressesRatios();
            Material material = new Material(ratios);
            Steps steps = new Steps();
            ConcentratorRatios concentrator = new ConcentratorRatios();
            Additions additions = new Additions();

            Calculations calculations = new Calculations(loads, geometry, material, concentrator, steps, additions);

            double result = calculations.GetContactArea();
        }
    }
}
