using Eto.Forms;
using Grasshopper;
using Rhino.Geometry;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace ParkingDemo.Utils
{
    public class PathLength
    {
        public static int GetPathLength(Parking Parking)
        {
            var mtx = Parking.PlanMatrix.Duplicate();
            
            var startCell = Parking.PathStartCell;
            var visitedList = new List<int[]>();
            var totalLengthByCars = 0;
            var totalPathCellsVisited = 0;
            var grid = Parking.PlanPointsGrid;
            var pathList = new List<Point3d>(); 
            var linesList = new List<Line>();
            //var cellsGrade = new ParkingUtils.PathInfo.Cell[100][];
            var cellsGrade = new List<List<int[]>>(); 
            var currentGrade = 0;
            var visitedLast = false;
            var iteration = 0;
            //var cellsGrade = new ParkingUtils.PathInfo.Cell[100][];
            var cell0 = new int[2] { startCell.row, startCell.col };
            var cellsGrade0 = new List<int[]>(); 
            cellsGrade0.Add(cell0 );
            cellsGrade.Add(cellsGrade0);
            cellsGrade[0] = cellsGrade0;
            var cell3 = cellsGrade0[0];
            visitedList.Add(cell0); 
            while (!visitedLast && iteration<400)
            {
                iteration++;
                var  lastGradeArray = new List<int[]>();
                lastGradeArray = cellsGrade[currentGrade];
                var count = lastGradeArray.Count;
                currentGrade++; 
                var cell2 = lastGradeArray[0];       
               var currentGradeList = new List<int[]>(); 
                var index = 0;
                if (!visitedLast)
                {
                    foreach (var cell in lastGradeArray)
                    {
                        //var currentGradeList = new List<int[]>();
                        var row = cell[0];
                        var col = cell[1];
                        var lastcellPt = grid.Branch(row) [col];
                        for (int i = -1; i < 2; i++)
                        
                        {
                            for (int j = -1; j < 2; j++)
                            {
                                if (Math.Abs(i) + Math.Abs(j) == 1)
                                {
                                    var item = ParkingUtils.CheckMatrix.GetMatrixItem(mtx, row + i, col + j);
                                    {
                                        var cellnew = new ParkingUtils.PathInfo.Cell(row + i, col + j);
                                        var cellintNew = new int[2] {cellnew.row, cellnew.col};
                                        var itemIndex = -1; 
                                        foreach(var itemcell in visitedList)
                                        {
                                            if ((itemcell[0] == cellintNew[0] && itemcell[1] == cellintNew[1]))
                                                itemIndex = 1;
                                        }
                                        if (itemIndex == -1)
                                        {
                                            visitedList.Add(cellintNew);
                                            if (item == 3)
                                            {
                                                currentGradeList.Add(cellintNew);
                                                totalPathCellsVisited++;
                                                var newcellPt = grid.Branch(cellnew.row)[ cellnew.col];
                                                var ln = new Line(lastcellPt, newcellPt); 
                                                linesList.Add(ln);
                                            }
                                            if(item == 2 )
                                            {
                                                totalLengthByCars += currentGrade; 
                                            }
                                            index++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    var len = currentGradeList.Count;
                    if (len == 0 )
                    {
                        visitedLast = true;
                        Parking.TotalLengthGrade = totalLengthByCars;
                        Parking.MaxLengthGrade = currentGrade;
                        Parking.TotalPathCellsVisited = totalPathCellsVisited;
                        Parking.PathLines = linesList; 
                        return currentGrade;
                        currentGradeList.Clear(); 
                    }
                    else
                    {
                        var newList = new List<int[]>();
                        for (int k = 0;  k< currentGradeList.Count; k++)
                        {
                            newList.Add(currentGradeList[k]);
                            currentGradeList.RemoveAt(k);
                            k--; 
                        }
                        var newList2 = new List<int[]>();
                        currentGradeList = newList2; 
                        cellsGrade.Add(newList); 
                       /* var newListCurrent = new List<ParkingUtils.PathInfo.Cell>();
                        foreach (var cell in currentGradeList)
                        {
                            var cellNew = new ParkingUtils.PathInfo.Cell(cell.row, cell.col);
                            newListCurrent.Add(cellNew);
                        }
                        cellsGrade[currentGrade] = newListCurrent.ToArray();
                        currentGradeList.Clear();*/
                    }
                }
                else
                {
                    visitedLast = true;
                    Parking.TotalLengthGrade = totalLengthByCars;
                    Parking.MaxLengthGrade = currentGrade;
                    Parking.TotalPathCellsVisited = totalPathCellsVisited; 
                    Parking.PathLines = linesList;
                    return currentGrade;
                }
                //return currentGrade; 
            }
            return currentGrade; 
        }

        public static int GetPathLength2(Parking Parking)
        {
            var mtx = Parking.PlanMatrix.Duplicate();

            var startCell = Parking.PathStartCell;
            var visitedList = new List<int[]>();
            var totalLengthByCars = 0;
            var totalPathCellsVisited = 0;
            var totalDirShift = 0;
            var dirShift = 0; 
            var grid = Parking.PlanPointsGrid;
            var pathList = new List<Point3d>();
            var linesList = new List<Line>();
            int lotNum = 0; 
            //var cellsGrade = new ParkingUtils.PathInfo.Cell[100][];
            var allLotTransforms = new DataTree<Transform>();
            var allCellsWithGrade = new DataTree<Rectangle3d>();
            var cellsGrade = new List<List<ParkingUtils.PathInfo.Cell>>();
            var currentGrade = 0;
            var visitedLast = false;
            var iteration = 0;
            //var cellsGrade = new ParkingUtils.PathInfo.Cell[100][];
            var cell0 = startCell;
            visitedList.Add(new int[] {cell0.row, cell0.col});
            cell0.DirShift = 0; 
            var cellsGrade0 = new List<ParkingUtils.PathInfo.Cell>();
            cellsGrade0.Add(cell0);
            cellsGrade.Add(cellsGrade0);
            cellsGrade[0] = cellsGrade0;
            var cell3 = cellsGrade0[0];
            while (!visitedLast && iteration < 400)
            {
                iteration++;
                var lastGradeArray = new List<ParkingUtils.PathInfo.Cell>();
                lastGradeArray = cellsGrade[currentGrade];
                var count = lastGradeArray.Count;
                currentGrade++;
                var cell2 = lastGradeArray[0];
                var currentGradeList = new List<ParkingUtils.PathInfo.Cell>();
                var index = 0;
                if (!visitedLast)
                {
                    foreach (var cell in lastGradeArray)
                    {
                        //var currentGradeList = new List<int[]>();
                        var row = cell.row;
                        var col = cell.col;
                        var preDir = cell.Direction; 
                        var lastcellPt = grid.Branch(row)[col];
                        var currentCell = Parking.PlanCells.Branch(cell.row, cell.col)[0];
                        var giveParkingAccess = false;

                        allCellsWithGrade.Add(currentCell, new Grasshopper.Kernel.Data.GH_Path(currentGrade));
                        for (int i = -1; i < 2; i++)
                        {
                            for (int j = -1; j < 2; j++)
                            {
                                if (Math.Abs(i) + Math.Abs(j) == 1)
                                {
                                    var item = ParkingUtils.CheckMatrix.GetMatrixItem(mtx, row + i, col + j);
                                    {
                                        var cellnew = new ParkingUtils.PathInfo.Cell(row + i, col + j);
                                        var cellintNew = new int[2] { cellnew.row, cellnew.col };
                                        var itemIndex = -1;
                                        foreach (var itemcell in visitedList)
                                        {   
                                            if ((itemcell[0] == cellintNew[0] && itemcell[1] == cellintNew[1]))
                                                itemIndex = 1;
                                        }
                                        if (itemIndex == -1)
                                        {
                                            visitedList.Add(cellintNew);
                                            DetectCellDirection(cellnew, i, j);
                                            if (item == 3)
                                            {
                                                giveParkingAccess = true;
                                                var currentDir = cellnew.Direction;
                                                if (currentDir != preDir)
                                                {
                                                    if(currentGrade>1 && currentGrade != 1)
                                                    dirShift++;
                                                    cellnew.DirShift = cell.DirShift + 1; 
                                                }
                                                else
                                                {
                                                    cellnew.DirShift = cell.DirShift; 
                                                }
                                                currentGradeList.Add(cellnew);
                                                totalPathCellsVisited++;
                                                var newcellPt = grid.Branch(cellnew.row)[cellnew.col];
                                                var ln = new Line(lastcellPt, newcellPt);
                                                linesList.Add(ln);
                                            }
                                            if (item == 2 || item == 1)
                                            {
                                                giveParkingAccess = true; 
                                                totalLengthByCars += currentGrade;
                                                totalDirShift += cell.DirShift;
                                                var cellTransform = SetCarTransformations(Parking,  cellnew);
                                                allLotTransforms.Add(cellTransform, new Grasshopper.Kernel.Data.GH_Path(currentGrade));
                                                lotNum++; 
                                            }
                                            index++;
                                        }

                                    }
                                }
                            }
                        }
                        if (!giveParkingAccess)
                        {
                            var X = cell.Direction; 
                           var cellTransform =  SetCarTransformations(Parking, cell);
                            allLotTransforms.Add(cellTransform, new Grasshopper.Kernel.Data.GH_Path(currentGrade-1));
                        }
                    }
                    var len = currentGradeList.Count;
                    if (len == 0)
                    {
                        visitedLast = true;
                        Parking.TotalLengthGrade = totalLengthByCars;
                        Parking.MaxLengthGrade = currentGrade;
                        Parking.TotalPathCellsVisited = totalPathCellsVisited;
                        Parking.PathLines = linesList;
                        Parking.PathDirectionShift = dirShift;
                        Parking.TotalDirShift = totalDirShift;
                        Parking.CarTransforms = allLotTransforms;
                        Parking.CellsWithGrade = allCellsWithGrade;
                        Parking.LotNumber = lotNum; 
                        return currentGrade;
                        currentGradeList.Clear();
                    }
                    else
                    {
                        var newList = new List<ParkingUtils.PathInfo.Cell>();
                        for (int k = 0; k < currentGradeList.Count; k++)
                        {
                            newList.Add(currentGradeList[k]);
                            currentGradeList.RemoveAt(k);
                            k--;
                        }
                        var newList2 = new List<ParkingUtils.PathInfo.Cell>();
                        currentGradeList = newList2;
                        cellsGrade.Add(newList);
                    }
                }
                else
                {
                    visitedLast = true;
                    Parking.TotalLengthGrade = totalLengthByCars;
                    Parking.MaxLengthGrade = currentGrade;
                    Parking.TotalPathCellsVisited = totalPathCellsVisited;
                    Parking.PathLines = linesList;
                    Parking.PathDirectionShift = dirShift;
                    Parking.TotalDirShift = totalDirShift;
                    Parking.CarTransforms = allLotTransforms;
                    Parking.CellsWithGrade = allCellsWithGrade;
                    Parking.LotNumber = lotNum; 
                    return currentGrade;
                }
                //return currentGrade; 
            }
            return currentGrade;
        }

        public static void DetectCellDirection(ParkingUtils.PathInfo.Cell Cell, int i, int j)
        {
            switch (i)
            {
                case -1:
                    Cell.Direction = ParkingUtils.PathInfo.Cell.CellDirection.North; 
                    break;
                case 0:
                    switch (j)
                    {
                        case -1:
                            Cell.Direction = ParkingUtils.PathInfo.Cell.CellDirection.West; 
                            break;
                        case 1:
                            Cell.Direction = ParkingUtils.PathInfo.Cell.CellDirection.East; 
                            break; 
                    }
                    break; 
                case 1:
                    Cell.Direction = ParkingUtils.PathInfo.Cell.CellDirection.South; 
                    break; 
            }
        }

        public static Transform SetCarTransformations(Parking parking, ParkingUtils.PathInfo.Cell CellNew)
        {
            var grid = parking.PlanPointsGrid; 
            var vplus = new Vector3d(0, 5, 0);
            var vminus = new Vector3d(0, -5, 0);
            var hplus = new Vector3d(5, 0, 0);
            var hminus = new Vector3d(-5, 0, 0);
            var vecbase = new Vector3d(new Point3d(grid.Branch(CellNew.row)[CellNew.col]));
            Transform rotation0 = new Transform(Transform.Rotation(-Math.PI / 2, Plane.WorldXY.Origin));
            Transform rotation2 = new Transform(Transform.Rotation(Math.PI, Plane.WorldXY.Origin));
            Transform rotation3 = new Transform(Transform.Rotation(Math.PI / 2, Plane.WorldXY.Origin));
            var translation = Transform.Translation(vecbase); 
            var lotTransform = new Transform();
            switch (CellNew.Direction)
            {
                case ParkingUtils.PathInfo.Cell.CellDirection.North:
                    lotTransform = translation * rotation0; 
                    break;
                case ParkingUtils.PathInfo.Cell.CellDirection.West:
                    lotTransform = translation; 
                    break;
                case ParkingUtils.PathInfo.Cell.CellDirection.East:
                    lotTransform = translation * rotation2;
                    break;
                case ParkingUtils.PathInfo.Cell.CellDirection.South:
                    lotTransform = translation * rotation3;
                    break; 
            }
            return lotTransform; 
        }

    }
}
