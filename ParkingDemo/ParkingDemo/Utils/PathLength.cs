using System;
using System.CodeDom;
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
            while (!visitedLast && iteration<40)
            {
                
                iteration++;
                //ParkingUtils.PathInfo.Cell[] lastGradeArray = new ParkingUtils.PathInfo.Cell[1];
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
                        for (int i = -1; i < 2; i++)
                        {
                            for (int j = -1; j < 2; j++)
                            {
                                if (Math.Abs(i) + Math.Abs(j) == 1)
                                {
                                    var item = ParkingUtils.CheckMatrix.GetMatrixItem(mtx, row + i, col + j);
                                    if (item == 3)
                                    {
                                       // mtx[row + i, col + j] = 6;
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
                                            currentGradeList.Add(cellintNew);
                                            visitedList.Add(cellintNew); 
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
                        return currentGrade;
                        visitedLast = true;
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
                    return currentGrade;
                    visitedLast = true;
                    break;

                }
                //return currentGrade; 
            }
            return currentGrade; 
        }
    }
}
