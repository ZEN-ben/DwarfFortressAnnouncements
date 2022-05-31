using System;
using System.Windows.Media;

namespace Dwarf_Fortress_Log.ViewModel
{
    public class MissingItem
    {
        public string Job { get; set; }
        public string Item { get; set; }

        public SolidColorBrush ColorBackground { get; set; } = Brushes.Transparent;
        public SolidColorBrush ColorForeground { get; set; } = Brushes.White;

        public DateTime Expires;

        public override string ToString()
        {
            return $"{Item}";
        }
    }
}