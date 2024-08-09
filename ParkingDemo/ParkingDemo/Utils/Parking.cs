using Eto.Forms;
using Grasshopper;
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
        public DataTree<Transform> CarTransforms { get; set; }
        public DataTree<Point3d> PathPoints { get; set; }
        public double Score { get; set; }
        private Guid _parkingID = Guid.NewGuid();


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
        public Parking() { }
    }
}
