using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarf_Fortress_Log.Model
{
    class Configuration
    {
        public List<Rule> Rules = new List<Rule>();
        public int Readback = 0;
        public List<CustomColor> CustomColors = new List<CustomColor>();
    }
}
