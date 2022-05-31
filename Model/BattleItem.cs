using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Dwarf_Fortress_Log.Model
{
    public class BattleItem
    {
        public string UnitA { get; set; }
        public string UnitB { get; set; }
        public bool HasHunter { get; set; }
        public bool HasCitizen { get; set; }
        public bool HasMilitary { get; set; }
        public bool HasAnimal { get; set; }

        public SolidColorBrush ColorBackground { get; set; } = Brushes.Transparent;
        public SolidColorBrush ColorForeground { get; set; } = Brushes.White;

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            if (obj is BattleItem)
            {
                BattleItem a = (BattleItem)obj;
                bool case1 = a.UnitA == UnitA && a.UnitB == UnitB;
                bool case2 = a.UnitA == UnitB && a.UnitB == UnitA;

                // Debug.WriteLine($"{case1} || {case2} == {case1 || case2} ");
                return case1 || case2;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return $"{UnitA} vs {UnitB}";
        }
    }
}
