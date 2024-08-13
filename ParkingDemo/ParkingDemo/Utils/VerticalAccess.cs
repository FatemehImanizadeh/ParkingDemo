using Eto.Forms;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingDemo.Utils
{
    public  class VerticalAccess
    {
        public Parking Parking { get; set; }    
        public List<int> AccessSides { get; set; }
        public ParkingUtils.PathInfo.Cell Cell { get; set; }
        public int Side { get; set; }
        public Transform Transform { get; set; }
        public VerticalAccess(ParkingUtils.PathInfo.Cell verticalCell, Parking parking)
        {
            this.Cell = verticalCell;
            this.Parking = parking; 
        }
        public void SetAccessSides(VerticalAccess VerticalAccess)
        {
            var parking = VerticalAccess.Parking; 
            var planMatix = Parking.PlanMatrix;
            var verticalCell = VerticalAccess.Cell;
            var northcellValue = ParkingUtils.CheckMatrix.GetMatrixItem( planMatix, verticalCell.row-1, verticalCell.col);
            var westcellValue  =  ParkingUtils.CheckMatrix.GetMatrixItem( planMatix, verticalCell.row, verticalCell.col-1);
            var esstcellValue  =  ParkingUtils.CheckMatrix.GetMatrixItem( planMatix, verticalCell.row, verticalCell.col+1);
            var southcellValue = ParkingUtils.CheckMatrix.GetMatrixItem( planMatix, verticalCell.row+1, verticalCell.col);
            var accessSides = new List<int>(); 
            if(northcellValue == 3)
            {
                accessSides.Add(0);
            }
            if (westcellValue == 3)
            {
                accessSides.Add(1);
            }
            if (esstcellValue == 3)
            {
                accessSides.Add(2);
            }
            if (southcellValue == 3)
            {
                accessSides.Add(3);
            }

            VerticalAccess.AccessSides = accessSides;
        }
        public void SelectAccessSide( VerticalAccess access)
        {
            var accessSides = access.AccessSides;
            var ran = new Random(); 
            
            var side = accessSides[ran.Next(0, accessSides.Count)];
            access.Side = side;
        }
        public void SetAccessTransforms(VerticalAccess access)
        {
            var cell = access.Cell;
            var vplus = new Vector3d(0, 5, 0);
            var vminus = new Vector3d(0, -5, 0);
            var hplus = new Vector3d(5, 0, 0);
            var hminus = new Vector3d(-5, 0, 0);
            var grid = access.Parking.PlanPointsGrid; 
            var vecbase = new Vector3d(new Point3d(grid.Branch(cell.row)[cell.col]));
            Transform rotation0 = new Transform(Transform.Rotation(-Math.PI / 2, Plane.WorldXY.Origin));
            Transform rotation2 = new Transform(Transform.Rotation(Math.PI, Plane.WorldXY.Origin));
            Transform rotation3 = new Transform(Transform.Rotation(Math.PI / 2, Plane.WorldXY.Origin));
            Transform translation0 = new Transform(Transform.Translation(new Vector3d(vecbase + vplus)));//translation for neighbocell //number0.
            Transform translation1 = new Transform(Transform.Translation(new Vector3d(vecbase + hminus)));
            Transform translation2 = new Transform(Transform.Translation(new Vector3d(vecbase + hplus)));
            Transform translation3 = new Transform(Transform.Translation(new Vector3d(vecbase + vminus)));
            Transform TR0 = rotation0 * translation0;
            Transform TR1 = translation1;
            Transform TR2 = rotation2 * translation2;
            Transform TR3 = rotation3 * translation3;
            switch (access.Side)
            {
                case 0:
                    access.Transform = TR0; 
                    break;
                case 1:
                    access.Transform = TR1;
                    break; 
                case 2:
                    access.Transform = TR2;
                    break; 
                case 3:
                    access.Transform = TR3;
                    break; 
            } 
        }
        public void SetAccessProperties(VerticalAccess access, Parking Parking)
        {
            SetAccessSides(access);
            SelectAccessSide(access);
            SetAccessTransforms(access);
        }
    }
}
