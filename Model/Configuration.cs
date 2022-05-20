using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarf_Fortress_Log.Model
{
    public class Configuration : ObservableObject
    {
        public List<Rule> Rules = new List<Rule>();
        public int Readback = 0;
        public List<CustomColor> CustomColors = new List<CustomColor>();

        private double _height = 200;
        public double Height
        {
            get { return _height; }
            set { SetProperty(ref _height, value); }
        }
        private double _width = 350;
        public double Width
        {
            get { return _width; }
            set { SetProperty(ref _width, value); }
        }
        public int OffsetX = 14;
        public int OffsetY = 40;

        private float _opacity = 0.8f;
        public float Opacity
        {
            get { return _opacity; }
            set { SetProperty(ref _opacity, value); }
        }
    }
}
