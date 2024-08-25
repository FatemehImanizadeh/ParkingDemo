using Grasshopper.Kernel.Data;
using Grasshopper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ParkingDemo.ParkingUtils.PathInfo;
using static ParkingDemo.ParkingUtils;

namespace ParkingDemo.Utils
{
    public class mainPathConnection
    {

        //here we set these boolean values to assign them in code methods. 
        // if the path is not valid>> (wheather there is a ramp cell in distance btw cells or there is a cell outside the 
        // plan boundaries )>> then we set the ispathpvalid values to false and it is a filter to decide btw options to find available ones.
        public class BridgePath
        {
            public enum Type
            {
                RowBased, ColBased
            }
            public int GainValue { get; set; }
            public bool PathPossible { get; set; }
            public Cell CellFirst { get; set; }
            public Cell CellSecond { get; set; }
            public Type TypeValue { get; set; }
            public BridgePath()
            {

            }
        }

        public static BridgePath FindBestMatchPathCellsForConnection(Matrix mtx, ParkingPath PathFirst, ParkingPath PathSecond)
        {
            var selectedBridgePath = new BridgePath();
            if (PathFirst.cells != null && PathSecond.cells != null && PathFirst.cells.Count > 0 && PathSecond.cells.Count > 0)
            {
                var bridgePathValuesDic = new Dictionary<BridgePath, int>();
                for (int i = 0; i < PathFirst.cells.Count; i++)
                {
                    for (int j = 0; j < PathSecond.cells.Count; j++)
                    { //برای این که سلولهایی که حیلی از هم فاصله دارن رو انتخاب نکنه
                        var cell1 = PathFirst.cells[i];
                        var cell2 = PathSecond.cells[j];
                        var manhatanDis = Math.Abs(cell1.row - cell2.row) + Math.Abs(cell1.col - cell2.col) - 1;
                        if (manhatanDis < 4)//
                        {
                            var bridgePathRowBased = new BridgePath();
                            bridgePathRowBased.CellFirst = PathFirst.cells[i];
                            bridgePathRowBased.CellSecond = PathSecond.cells[j];
                            bridgePathRowBased.TypeValue = BridgePath.Type.RowBased;
                            var lotgain = mainPathConnection.LotGain(PathFirst.cells[i], PathSecond.cells[j], mtx, true, out bool isPossible);
                            bridgePathRowBased.GainValue = lotgain;
                            if (isPossible)
                            {
                                bridgePathValuesDic.Add(bridgePathRowBased, lotgain);
                            }
                            var bridgePathColBased = new BridgePath();
                            bridgePathColBased.CellFirst = PathFirst.cells[i];
                            bridgePathColBased.CellSecond = PathSecond.cells[j];
                            bridgePathColBased.TypeValue = BridgePath.Type.ColBased;
                            var lotgain2 =mainPathConnection.LotGain(PathFirst.cells[i], PathSecond.cells[j], mtx, false, out bool isPossible2);
                            bridgePathColBased.GainValue = lotgain2;
                            if (isPossible2)
                            {
                                bridgePathValuesDic.Add(bridgePathColBased, lotgain2);
                            }
                        }
                    }
                }
                if (bridgePathValuesDic.Count > 0)
                {
                    double max = bridgePathValuesDic.Max(kvp => kvp.Value);
                    var allMazBridges = bridgePathValuesDic.Where(kvp => kvp.Value == max).Select(kvp => kvp.Key);
                    var ran = new Random();
                    var ranIndex = ran.Next(allMazBridges.ToList().Count);
                    selectedBridgePath = allMazBridges.ToList()[ranIndex];
                }
                else
                {
                    var rannew = new Random();
                }
            }
            return selectedBridgePath;
        }
        public static void CreateConnectionPath( Parking Parking)
        {
            var mtx = Parking.PlanMatrix;
            var gridPts = Parking.PlanPointsGrid;
            var parkingPaths = Parking.ParkingPaths ; 
            var carTransforms = Parking.CarTransforms;
            var mainPathPts = Parking.PathPoints; 
            // in these 2 below for loops i want to choose all 2 posssible combinations in existing paths and check the shortest distance between each couple of paths finally i should take the best choice considering both distance and lotgain.
            var remomvingPaths = new List<GH_Path>();
            for (int i = 0; i < parkingPaths.Count; i++)
            {
                for (int j = i + 1; j < parkingPaths.Count; j++)
                {
                    var pathFirst = parkingPaths[i];
                    var pathSecond = parkingPaths[j];
                    if (pathFirst.cells != null && pathSecond.cells != null)
                    {
                        if (pathFirst.cells.Count > 0 && pathSecond.cells.Count > 0)
                        {

                            /* int lotGain = 1;
                             int iteration = 0;
                             Cell cellRan1 = null;
                             Cell cellRan2 = null; 
                             iteration++;
                             var random1 = new Random();
                             var random2 = new Random();
                             var ran1 = random1.Next(pathFirst.cells.Count);
                             var ran2 = random2.Next(pathSecond.cells.Count);
                              cellRan1 = pathFirst.cells[ran1];
                              cellRan2 = pathSecond.cells[ran2];
                             //    bool isPossible ; 
                                 lotGain = 
                                 ParkingUtils.mainPathConnection.LotGain(cellRan1, cellRan2, mtx, true, out bool isPossible);*/
                            var bridgePath = FindBestMatchPathCellsForConnection(mtx, pathFirst, pathSecond);
                            var cellRan1 = bridgePath.CellFirst;
                            var cellRan2 = bridgePath.CellSecond;
                            if (cellRan1 != null && cellRan2 != null)
                            {
                                var n1 = cellRan1.row;
                                var m1 = cellRan1.col;
                                var n2 = cellRan2.row;
                                var m2 = cellRan2.col;
                                // var signn2 = ((m2 - m1) / Math.Abs(m2 - m1));
                                var signn = (n2 - n1 >= 0) ? 1 : -1;
                                var signm = (m2 - m1 >= 0) ? 1 : -1;
                                var allBridgePathCells = new List<ParkingUtils.PathInfo.Cell>();
                                if (bridgePath.TypeValue == BridgePath.Type.RowBased)
                                {
                                    if (n2 != n1)
                                    {
                                        for (int k = 1; k <= Math.Abs(n2 - n1); k++)
                                        {
                                            var step = k;
                                            step *= signn;
                                            var newint = new int[2];
                                            var row = n1 + step;
                                            var col = m1;
                                            allBridgePathCells.Add(new PathInfo.Cell(row, col));
                                        }
                                    }
                                    if (m2 != m1)
                                    {
                                        for (int k = 1; k < Math.Abs(m2 - m1); k++)
                                        {
                                            var step = k;
                                            step *= signm;
                                            var newint2 = new int[2];
                                            var row = n2;
                                            var col = m1 + step;
                                            allBridgePathCells.Add(new PathInfo.Cell(row, col));
                                        }
                                    }
                                }
                                else
                                {
                                    if (m2 != m1)
                                    {
                                        for (int k = 1; k <= Math.Abs(m2 - m1); k++)
                                        {
                                            var step = k;
                                            step *= signm;
                                            var newint2 = new int[2];
                                            var row = n1;
                                            var col = m1 + step;
                                            allBridgePathCells.Add(new PathInfo.Cell(row, col));
                                        }
                                    }
                                    if (n2 != n1)
                                    {
                                        for (int k = 1; k < Math.Abs(n2 - n1); k++)
                                        {
                                            var step = k;
                                            step *= signn;
                                            var newint = new int[2];
                                            var row = n1 + step;
                                            var col = m2;
                                            allBridgePathCells.Add(new PathInfo.Cell(row, col));
                                        }
                                    }
                                }
                                var parkingPathNew = new PathInfo.ParkingPath();
                                parkingPaths.Add(parkingPathNew);
                                parkingPathNew.pathindex = parkingPaths.Count;


                                foreach (var cell in allBridgePathCells)
                                {

                                    mtx[cell.row, cell.col] = 3;
                                    var pathindex = parkingPaths.Count;
                                    var pathNewCell = new GH_Path(pathindex, cell.row, cell.col);
                                    mainPathPts.Add(new Point3d(gridPts.Branch(cell.row)[cell.col]), pathNewCell);
                                    // parkingPathNew.cells.Add(cell);
                                    for (int k = -1; k < 2; k++)
                                        for (int t = -1; t < 2; t++)
                                        {
                                            if (Math.Abs(k) + Math.Abs(t) == 1)
                                            {
                                                var rowNew = cell.row;
                                                var colNew = cell.col;
                                                var vplus = new Vector3d(0, 5, 0);
                                                var vminus = new Vector3d(0, -5, 0);
                                                var hplus = new Vector3d(5, 0, 0);
                                                var hminus = new Vector3d(-5, 0, 0);
                                                var vecbase = new Vector3d(new Point3d(gridPts.Branch(rowNew)[colNew]));
                                                Transform rotation0 = new Transform(Transform.Rotation(-Math.PI / 2, Plane.WorldXY.Origin));
                                                Transform rotation2 = new Transform(Transform.Rotation(Math.PI, Plane.WorldXY.Origin));
                                                Transform rotation3 = new Transform(Transform.Rotation(Math.PI / 2, Plane.WorldXY.Origin));
                                                Transform translation0 = new Transform(Transform.Translation(new Vector3d(vecbase + vplus)));//translation for neighbocell //number0.
                                                Transform translation1 = new Transform(Transform.Translation(new Vector3d(vecbase + hminus)));
                                                Transform translation2 = new Transform(Transform.Translation(new Vector3d(vecbase + hplus)));
                                                Transform translation3 = new Transform(Transform.Translation(new Vector3d(vecbase + vminus)));
                                                //scince some branches of m and n plus and minus one does not exist and the code gives out of range error so we can consider the
                                                // center of our base cell and add the locatioan of neighbor cells vertically and horizontally based on their locatioan
                                                // اینجا داریم میگیم که ایندکس مسیرهای اتصالی جدید یکی بیشتر از همه مسیرهای موجود فعلی میشه
                                                var path0 = new GH_Path(pathindex, rowNew - 1, colNew);
                                                var path1 = new GH_Path(pathindex, rowNew, colNew - 1);
                                                var path2 = new GH_Path(pathindex, rowNew, colNew + 1);
                                                var path3 = new GH_Path(pathindex, rowNew + 1, colNew);
                                                var rowValue = CheckMatrix.GetValidIndex(rowNew + k, mtx.RowCount);
                                                var colValue = CheckMatrix.GetValidIndex(colNew + t, mtx.ColumnCount);

                                                var adjacentMtxValue = CheckMatrix.GetMatrixItem(mtx, rowValue, colValue);
                                                if (adjacentMtxValue == 1)
                                                {
                                                    switch (k)
                                                    {
                                                        case -1:
                                                            carTransforms.Add(new Transform(translation0 * rotation0), path0);
                                                            break;
                                                        case 1:
                                                            carTransforms.Add(new Transform(translation3 * rotation3), path3);
                                                            break;
                                                    }
                                                    switch (t)
                                                    {
                                                        case -1:
                                                            carTransforms.Add(new Transform(translation1), path1);
                                                            break;
                                                        case 1:
                                                            carTransforms.Add(new Transform(translation2 * rotation2), path2);
                                                            break;
                                                    }
                                                    mtx[rowNew + k, colNew + t] = 2;
                                                }

                                            }
                                        }
                                }

                                foreach (var cell in allBridgePathCells)
                                {
                                    foreach (var path in carTransforms.Paths)
                                    {
                                        if (path.Indices[1] == cell.row && path.Indices[2] == cell.col)
                                        {
                                            // carTransfroms.RemovePath(path);
                                            remomvingPaths.Add(path);
                                            break;
                                        }
                                        //mtx[cell.row, cell.col] = 2;
                                    }
                                }


                                foreach (var path in remomvingPaths)
                                {
                                    carTransforms.RemovePath(path);
                                    // remomvingPaths.Add(path);
                                }
                            }
                        }
                    }
                    else
                    {
                      //  Parking.IsGenerationValid = false;
                    }
                }
            }
        }

        public static int LotGain(ParkingUtils.PathInfo.Cell p1, ParkingUtils.PathInfo.Cell p2, Matrix mtx, bool RowBasedPath, out bool ispathpossible)
        {
            // ispathpossible = true;// I set this parameter to check if there exists a cell
            // in the new path that is ramp we can not have to path to even check it out for the total lot gain
            // row based: first direction if in the direction of rows(vertically) and then in the direciton of colums(horizontally)
            //if rowbased = false: it is first horizontally then vertically
            var removedCells = new List<ParkingUtils.PathInfo.Cell>();
            ispathpossible = true;
            int nbasedgain = 0;
            var n1 = p1.row;
            var m1 = p1.col;
            var n2 = p2.row;
            var m2 = p2.col;
            // var signn2 = ((m2 - m1) / Math.Abs(m2 - m1));
            var signn = (n2 - n1 >= 0) ? 1 : -1;
            var signm = (m2 - m1 >= 0) ? 1 : -1;
            var allbridgepathptsnbased = new int[10][];

            if (RowBasedPath)
            {

                if (n2 != n1)
                {
                    for (int i = 1; i <= Math.Abs(n2 - n1); i++)
                    {
                        var step = i;
                        step *= signn;
                        var newint = new int[2];
                        newint[0] = n1 + step;
                        newint[1] = m1;
                        if (mtx[n1 + step, m1] == 4 || mtx[n1 + step, m1] == 0)
                        {
                            ispathpossible = false;
                            return int.MinValue;
                        }

                        allbridgepathptsnbased.Append(newint);
                    }
                }

                if (m2 != m1)
                {
                    for (int j = 1; j < Math.Abs(m2 - m1); j++)
                    {
                        var step = j;
                        step *= signm;
                        var newint2 = new int[2];
                        newint2[0] = n2;
                        newint2[1] = m1 + step;
                        allbridgepathptsnbased.Append(newint2);

                        if (mtx[n2, step + m1] == 4 || mtx[n2, step + m1] == 0)
                        {
                            ispathpossible = false;
                            return int.MinValue;
                        }



                    }
                }
                if (ispathpossible)
                {
                    if (n1 != n2)
                    {
                        for (int i = 1; i <= Math.Abs(n2 - n1); i++)
                        {
                            var step = i;
                            step *= signn;
                            if (mtx[n1 + step, m1] == 4 || mtx[n1 + step, m1] == 0) { ispathpossible = false; }// scince by default we asume that the path is possible but if there is a cell in the bridge paht which is for ramp we set the value to false }
                            else
                            {
                                try
                                {
                                    if (mtx[n1 + step, m1] == 2)
                                    {
                                        nbasedgain--;
                                        var removedCell = new PathInfo.Cell(n1 + step, m1);
                                        removedCells.Add(removedCell);
                                    }
                                    for (int k = -1; k < 2; k++)
                                        for (int t = -1; t < 2; t++)
                                        {
                                            if (Math.Abs(k) + Math.Abs(t) == 1)
                                            {
                                                var currentCell = new int[] { n1 + step + k, m1 + t };
                                                var containment = false;
                                                for (i = 0; i < allbridgepathptsnbased.Length; i++)
                                                {
                                                    if (allbridgepathptsnbased[i] == currentCell)
                                                    {
                                                        containment = true;
                                                        break;
                                                    }
                                                }
                                                if (!containment)
                                                {
                                                    int value = (int)mtx[n1 + step + k, m1 + t];
                                                    switch (value)
                                                    {
                                                        case 0:
                                                            break;
                                                        case 1:
                                                            nbasedgain++;
                                                            break;
                                                        case 2:
                                                            // nbasedgain--;
                                                            break;
                                                        case 3:
                                                            break;
                                                    }
                                                }
                                                //here step check if the neighbor is path itself or is not in the plan we do consider nothing; if it is lot we subtract from lotgain value and if it is empty cell we add to the lotgain value
                                            }
                                        }
                                }
                                catch { }
                            }
                        }
                    }
                    if (m2 != m1)
                    {
                        for (int j = 1; j < Math.Abs(m2 - m1); j++)
                        {
                            var step = j;
                            step *= signm;
                            if (mtx[n2, m1 + step] == 4 || mtx[n2, m1 + step] == 0) { ispathpossible = false; }// scince by default we asume that the path is possible but if there is a cell in the bridge paht which is for ramp we set the value to false }
                            else
                            {
                                try
                                {
                                    if (mtx[n2, m1 + step] == 2) nbasedgain--;
                                    for (int k = -1; k < 2; k++)
                                        for (int t = -1; t < 2; t++)
                                        {
                                            if (Math.Abs(k) + Math.Abs(t) == 1)
                                            {
                                                //var currentCell = new int[] { n2 + k, m1 + step + t };
                                                var currentCell = new int[] { n1 + k, m1 + step + t };
                                                var containment = false;
                                                for (int i = 0; i < allbridgepathptsnbased.Length; i++)
                                                {
                                                    if (allbridgepathptsnbased[i] == currentCell)
                                                    {
                                                        containment = true;
                                                        break;

                                                    }
                                                }
                                                if (!containment)
                                                {
                                                    int value = (int)mtx[n2 + k, m1 + step + t];
                                                    switch (value)
                                                    {
                                                        case 0:
                                                            break;
                                                        case 1:
                                                            nbasedgain++;
                                                            break;
                                                        case 2:
                                                            // nbasedgain--;
                                                            break;
                                                        case 3:
                                                            break;
                                                    }
                                                }
                                                //here step check if the neighbor is path itself or is not in the plan we do consider nothing; if it is lot we subtract from lotgain value and if it is empty cell we add to the lotgain value
                                            }
                                        }
                                }
                                catch { }
                            }
                        }
                    }

                }
            }

            else

            {


                if (m2 != m1)
                {
                    for (int j = 1; j < Math.Abs(m2 - m1); j++)
                    {
                        var step = j;
                        step *= signm;
                        var newint2 = new int[2];
                        newint2[0] = n1;
                        newint2[1] = m1 + step;
                        if (mtx[n1, m1 + step] == 4 || mtx[n1, step + m1] == 0)
                        {
                            ispathpossible = false;
                            return int.MinValue;
                        }
                        allbridgepathptsnbased.Append(newint2);
                    }
                }
                if (n2 != n1)
                {
                    for (int i = 1; i < Math.Abs(n2 - n1); i++)
                    {
                        var step = i;
                        step *= signn;
                        var newint = new int[2];
                        newint[0] = n1 + step;
                        newint[1] = m2;
                        if (mtx[n1 + step, m1] == 4 || mtx[n1 + step, m1] == 0)
                        {
                            ispathpossible = false;
                            return int.MinValue;
                        }
                        allbridgepathptsnbased.Append(newint);
                    }
                }
                if (ispathpossible)
                {
                    if (m2 != m1)
                    {
                        for (int j = 1; j < Math.Abs(m2 - m1); j++)
                        {
                            var step = j;
                            step *= signm;
                            if (mtx[n1, m1 + step] == 4 || mtx[n1, m1 + step] == 0) { ispathpossible = false; }// scince by default we asume that the path is possible but if there is a cell in the bridge paht which is for ramp we set the value to false }
                            else
                            {
                                try
                                {
                                    if (mtx[n1, m1 + step] == 2) nbasedgain--;
                                    for (int k = -1; k < 2; k++)
                                        for (int t = -1; t < 2; t++)
                                        {
                                            if (Math.Abs(k) + Math.Abs(t) == 1)
                                            {
                                                //var currentCell = new int[] { n2 + k, m1 + step + t };
                                                var currentCell = new int[] { n1 + k, m1 + step + t };
                                                var containment = false;
                                                for (int i = 0; i < allbridgepathptsnbased.Length; i++)
                                                {
                                                    if (allbridgepathptsnbased[i] == currentCell)
                                                    {
                                                        containment = true;
                                                        break;
                                                    }
                                                }
                                                if (!containment)
                                                {
                                                    int value = (int)mtx[n1 + k, m1 + step + t];
                                                    switch (value)
                                                    {
                                                        case 0:
                                                            break;
                                                        case 1:
                                                            nbasedgain++;
                                                            break;
                                                        case 2:
                                                            // nbasedgain--;
                                                            break;
                                                        case 3:
                                                            break;
                                                    }
                                                }
                                                //here step check if the neighbor is path itself or is not in the plan we do consider nothing; if it is lot we subtract from lotgain value and if it is empty cell we add to the lotgain value
                                            }
                                        }
                                }
                                catch { }
                            }
                        }
                    }
                    if (n1 != n2)
                    {
                        for (int i = 1; i < Math.Abs(n2 - n1); i++)
                        {
                            var step = i;
                            step *= signn;
                            if (mtx[n1 + step, m2] == 4 || mtx[n1 + step, m2] == 0) { ispathpossible = false; }// scince by default we asume that the path is possible but if there is a cell in the bridge paht which is for ramp we set the value to false }
                            else
                            {
                                try
                                {
                                    if (mtx[n1 + step, m2] == 2) nbasedgain--;
                                    for (int k = -1; k < 2; k++)
                                        for (int t = -1; t < 2; t++)
                                        {
                                            if (Math.Abs(k) + Math.Abs(t) == 1)
                                            {
                                                var currentCell = new int[] { n1 + step + k, m2 + t };
                                                var containment = false;
                                                for (i = 0; i < allbridgepathptsnbased.Length; i++)
                                                {
                                                    if (allbridgepathptsnbased[i] == currentCell)
                                                    {
                                                        containment = true;
                                                        break;
                                                    }
                                                }
                                                if (!containment)
                                                {
                                                    int value = (int)mtx[n1 + step + k, m2 + t];
                                                    switch (value)
                                                    {
                                                        case 0:
                                                            break;
                                                        case 1:
                                                            nbasedgain++;
                                                            break;
                                                        case 2:
                                                            // nbasedgain--;
                                                            break;
                                                        case 3:
                                                            break;
                                                    }
                                                }
                                                //here step check if the neighbor is path itself or is not in the plan we do consider nothing; if it is lot we subtract from lotgain value and if it is empty cell we add to the lotgain value
                                            }
                                        }
                                }
                                catch { }
                            }
                        }
                    }
                }
            }


            return nbasedgain;
        }

    }

}
