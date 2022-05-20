using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Dwarf_Fortress_Log.Model
{
    public class LogItem
    {
        public string Content { get; set; } = string.Empty;
        public SolidColorBrush ColorBackground { get; set; } = Brushes.Transparent;
        public SolidColorBrush ColorForeground { get; set; } = Brushes.White;
        public bool Skipped = false;

        public override string ToString()
        {
            return Content;
        }
    }
}
