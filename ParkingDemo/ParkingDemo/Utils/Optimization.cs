using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ParkingDemo.Utils
{
    public class Optimization
    {
        private double _LotNumW = 1;
        private double _PathLenW = 0.2;
        private double _NonFuncW = 1; 

        public double LotNumW { get { return _LotNumW; } set { this._LotNumW = value; } }
        public double PathLenW { get { return _PathLenW; } set { this._PathLenW = value; } }
        public double NonFuncW { get { return _NonFuncW; } set { this._NonFuncW = value; } }
        public Optimization() { }
       /* public static double  CalculateOptimizationScore( Parking Parking )
        {
           *//* float lotnum = Parking.LotNumber;
            float pathCellNum = Parking.PathCellNumber;
            float totalCellNum = Parking.PlanCellNum;
            float emptyCells = Parking.EmptyCells;  
            var emptyCell = totalCellNum - lotnum - pathCellNum; 
            float LotperArea = lotnum / totalCellNum;
            float pathperArea = pathCellNum / totalCellNum;
            float emptyperArea = emptyCell / totalCellNum;
            var score2 = lotnum / (emptyCells * pathCellNum);
            Parking.Score = score2;*//*
            return score2;
        }*/
        public static double OptimizationFunction(Optimization Optimization, Parking Parking )
        {
            double Score = Optimization.LotNumW * (float) Parking.LotNumber  - Optimization.PathLenW * (float)Parking.PathCellNumber - Optimization.NonFuncW * (float)Parking.EmptyCells;

            Score = 1/(1+ Math.Exp(-Score/15));
            Parking.Score = Score;
            return Score;
        }
    }
}
