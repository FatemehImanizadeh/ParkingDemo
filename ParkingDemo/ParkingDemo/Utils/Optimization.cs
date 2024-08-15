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
            float emptyCells = Parking.EmptyCells;  
            var emptyCell = totalCellNum - lotnum - pathCellNum; 
            float LotperArea = lotnum / totalCellNum;
            float pathperArea = pathCellNum / totalCellNum;
            float emptyperArea = emptyCell / totalCellNum;
            var score = LotperArea / (pathperArea * emptyperArea);
            var score2 = lotnum / (emptyCell * pathCellNum);
            Parking.Score = score2;
            return score2;
        }
    }
}
