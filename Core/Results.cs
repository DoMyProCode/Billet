using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Billet
{
    public class Results
    {
        public List<ElementResult> Elements { get; set; } = new List<ElementResult>();
        public void AddResult(ElementResult elementResult)
        {
            Elements.Add(elementResult);
        }
    }
}
