using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingDemo.Utils
{
    public class Optimization
    {
        
        public Optimization() { }
        public static double  CalculateOptimizationScore( Parking Parking )
        {
            float lotnum = Parking.LotNumber;
            float pathCellNum = Parking.PathCellNumber;
            float totalCellNum = Parking.PlanCellNum;
            float LotperArea = lotnum / totalCellNum;
            float pathperArea = pathCellNum / totalCellNum;
            var score = LotperArea / pathperArea;
            Parking.Score = score;
            return score;
        }
    }
}
