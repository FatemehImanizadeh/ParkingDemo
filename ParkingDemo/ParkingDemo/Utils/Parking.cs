using Eto.Forms;
using Grasshopper;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingDemo.Utils
{
    public class Parking
    {

        public int LotNumber { get; set; }
        public int PathCellNumber { get; set; }
        public int PlanCellNum { get; set; }
        public int EmptyCells { get; set; }
        private DataTree<Transform> _CarTransforms = new DataTree<Transform>();
        public DataTree<Transform> CarTransforms { get => _CarTransforms; set { _CarTransforms = value; } }
        private DataTree<Point3d> _PathPoints = new DataTree<Point3d>();
        public DataTree<Point3d> PathPoints { get => _PathPoints; set { this._PathPoints = value; } }
        private List<Line> _PathLines = new List<Line>();
        public List<Line> PathLines  { get => _PathLines;  set { this._PathLines = value; }
}
        public double Score { get; set; }
        private Guid _parkingID = Guid.NewGuid();
        private bool _IsGenerationValid = true;
        public bool IsGenerationValid { get { return _IsGenerationValid; } set { _IsGenerationValid = value; } }
        private List<Rectangle3d> _ExcludeCells = new List<Rectangle3d>();
        public List<Rectangle3d> ExcludeCells
        {
            get { return _ExcludeCells; }
            set { _ExcludeCells = value;}
        }
        private List<ParkingUtils.PathInfo.ParkingPath> _ParkingPaths = new List<ParkingUtils.PathInfo.ParkingPath>();
 
        public List<ParkingUtils.PathInfo.ParkingPath> ParkingPaths { get=> _ParkingPaths; set { _ParkingPaths = value; } }
        public Rectangle3d ParkingEntranceCell { get; set; }
        public ParkingUtils.PathInfo.Cell CurrentStartCell { get; set; }
        private int _CurrentPathIndex = 0;
        private List<ParkingUtils.PathInfo.Cell> _NotFunctionalCells =new List<ParkingUtils.PathInfo.Cell>();
        public List<ParkingUtils.PathInfo.Cell> NotFunctionalCells { get => _NotFunctionalCells; set { _NotFunctionalCells = value;  } }

        public int CurrentPathIndex { get => _CurrentPathIndex; set { _CurrentPathIndex = value; } }
        private int _CurrentPathItemCount = 0 ; 
        public int CurrentPathItemCount { get => _CurrentPathItemCount; set { _CurrentPathItemCount = value; } }
        private int _MaxLengthGrade = 0;
        public int MaxLengthGrade { get => _MaxLengthGrade; set { _MaxLengthGrade = value; } }
        private int _TotalLengthGrade = 0;
        public int TotalLengthGrade { get => _TotalLengthGrade; set { _TotalLengthGrade = value; } }
        private int _TotalPathCellsVisited = 0;
        public int TotalPathCellsVisited { get => _TotalPathCellsVisited; set { _TotalPathCellsVisited = value; } }
        public DataTree<Point3d> PlanPointsGrid { get; set; } 
        public DataTree<Rectangle3d>PlanCells { get; set; }
        public Matrix PlanMatrix { get; set; }
        public Curve Outline {  get; set; }
        public DataTree <Point3d> SidePoints { get; set; }
        public List<int> RampInfo { get; set; }
        public ParkingUtils.PathInfo.Cell RampEndCell { get; set; }
        public ParkingUtils.PathInfo.Cell PathStartCell { get; set; }
        public Guid ParkingID { get => this._parkingID; set { this._parkingID = value; } }
        /// <summary>
        /// first for horizontal and second for vertical coordinates
        /// </summary>
        public List<List<double>> GridCoordinates { get; set; }

        public VerticalAccess VerticalAccess { get; set; }

        public Parking() { }
    }
}
