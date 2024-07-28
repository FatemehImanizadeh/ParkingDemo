using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingDemo.Utils
{
    public class GenerationCollection
    {
        private List<Parking> _Parkings = new List<Parking>();
        public List<Parking> parkings { get { return this._Parkings; } set { this._Parkings  = value; } }
        public GenerationCollection() { }
    }
}
