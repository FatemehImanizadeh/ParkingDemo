using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel;
using System.Runtime.InteropServices;
using Grasshopper.Kernel.Data;
using System.CodeDom;
using System.IO;
using Grasshopper.Kernel.Types;
using System.Data;
using Rhino.Commands;
using Eto.Forms;
using System.Diagnostics.Eventing.Reader;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Geometry.Voronoi;

namespace ParkingDemo
{
    public class ParkingUtils
    {
        public class CheckMatrix
        {
            static void Main()
            {
                //int[,] matrix = {{1, 2, 3},{4, 5, 6}, {7, 8, 9} };
                //int rowIndex = GetValidIndex(1, matrix.GetLength(0));
                //int columnIndex = GetValidIndex(2, matrix.GetLength(1));
                //int matrixItem = GetMatrixItem(matrix, rowIndex, columnIndex);
                //if (matrixItem != -1)
                //{
                //   Console.WriteLine($"Matrix item at ({rowIndex}, {columnIndex}): {matrixItem}");
                //}
                //else
                //{
                //    Console.WriteLine("Invalid indices");
                // Handle the case where indices are not valid
                //}
            }
            public static int GetValidIndex(int inputIndex, int maxIndex)
            {
                // Check if inputIndex is within the valid range
                if (inputIndex >= 0 && inputIndex < maxIndex)
                {
                    return inputIndex;
                }
                else
                {
                    // Handle the case where inputIndex is out of range
                    return -1; // or throw an exception, return a default value, etc.
                }
            }
            public static int GetMatrixItem(Matrix mtx, int rowIndex, int columnIndex)
            {
                // Check if both row and column indices are valid
                if (rowIndex != -1 && columnIndex != -1)
                {
                    var Result = mtx[rowIndex, columnIndex];
                    return (int)Result;
                }
                else
                {
                    // Return a default value or handle the case where indices are not valid
                    return -1;
                }
            }
        }
        public class RoundUp
        {
            ////////
            //  توی این کلاس دقیقا میام بازه کرو را به نزدیک ترین مضرب سایز شبکم میرسونم و از همین عدد نهایتا برای تولید گرید استفاده میشه.
            public static double RoundTo(double num, double divisor)
            {
                if (num % divisor == 0)
                    return num;
                else
                    return num - num % divisor;
            }
        }
        public static DataTree<Point3d> CreateGrid(int row, int col, double size)
        {
            var pt_grid = new DataTree<Point3d>();
            for (int i = 0; i < row; i++)
                for (int j = 0; j < col; j++)
                {
                    var path = new GH_Path(row - i - 1);
                    pt_grid.Add(new Point3d(j * size + size / 2, i * size + size / 2, 0), path);
                }
            return pt_grid;
        }
        // the reason to build these flags in the following function:
        // 1. to check if the current point is the closest pt to entrance in the plan set the
        // coresponding value of that in matrix to 3 so it is distinguishable from other cells.
        // 2. when we found the entrance we can set the flag1 to false so the algorithm won't search
        // for the entrance anymore
        public static Matrix GridToMatrix(DataTree<Point3d> ptGrid, int row, int col, Curve crv)
        {
            var mtx = new Matrix(row, col);
            for (int i = 0; i < row; i++)
                for (int j = 0; j < col; j++)
                {
                    var containment = crv.Contains(ptGrid.Branch(i)[j], Plane.WorldXY, 0.01);
                    if (containment == PointContainment.Inside || containment == PointContainment.Coincident)
                        mtx[i, j] = 1;
                    else
                        mtx[i, j] = 0;
                }
            return mtx;
        }
        public static DataTree<Rectangle3d> CellularOutline(DataTree<Point3d> ptGrid, Matrix Gridmtx)
        {
            var recs = new DataTree<Rectangle3d>();
            var transformvec = new Vector3d(-2.5, -2.5, 0);
            for (int i = 0; i < Gridmtx.RowCount; i++)
                for (int j = 0; j < Gridmtx.ColumnCount; j++)
                {
                    if (Gridmtx[i, j] != 0)
                    {
                        var rec = new Rectangle3d(new Plane(ptGrid.Branch(i)[j] + transformvec, Vector3d.ZAxis), 5, 5);
                        var path = new GH_Path(i, j);// چرا اینجا -۲ زدم؟؟؟؟؟!!!!!!
                        recs.Add(rec, path);
                    }
                }
            return recs;
        }
        //^^ the plan matrix besides row and col indx of startcell for the path are input to the class. the algorithm checks for
        //^^  the cell neighbors and the number of neighbors for each neighbor to get the cell with the maximum number of
        //^^ neighbors that are not adjacent to a pathcell in the matrix.
        //^^ the adjacency of cells in matrix is defined by their value and it's regarded as below:
        //^^ matrix item outside the plan : p3
        //^^ matrix item inside the plan{ case it is in access path: 3, case in parking space: 1}
        //^^ in adjacencynum indexes: mtx[n,m-1] = 0, mtx[n, m+1]= 1, mtx[n-1, m]= 2, mtx[n+1, m]= 3
        // این متد زیر یه چیزی شد که بدتر کرد الگوریتممو بنابراین حذفش میکنم از کد.
        /*public static List<int[]> AllCells(Matrix mtx)
        {
        // چرا این قسمتو دارم مینویسم؟
        // توی تابعی که رندم سلول شروع رو پیدا میکنه یک مشکلی هست این که سلول‌های قبلی حذف نمیشن و ممکنه بارها و بارها یک سلول دوباره انتخاب بشه. میخوام برم به اون تابع لیست سلول‌های موجود رو بدم و با هر بار انتخاب از آپشنهای موجود حذفشون کنم.
        var allcells = new List<int[]>();
        for(int i = 0; i < mtx.RowCount; i++)
        for(int j = 0; j < mtx.ColumnCount; j++)
        {
        if (mtx[i, j] != 0)//this part for just selecting cells inside plan:)
        allcells.Add(new int[] {i,j});
        }
        return allcells;
        }*/
        public static int[] FirstPathCell(Matrix mtx)
        {
            var startcell = new int[2];
            int cellrow = 0;
            int cellcol = 0;
            var rows = mtx.RowCount;
            var cols = mtx.ColumnCount;
            var iter = 0;
            var emptyadjacentcells = 0;
            //var matrixitem = -1;
            while ((emptyadjacentcells < 1) && /*(mtx[cellrow, cellcol] != 4 to be not ramp: this is additional and useless scince when it is supposed to be 1 it definitely wont be 4!!!!!!*/  iter < 650)
            {
                emptyadjacentcells = 0;
                iter++;
                var ran0 = new Random();
                var ran1 = new Random();
                cellrow = ran0.Next(rows);
                cellcol = ran1.Next(cols);
                if (mtx[cellrow, cellcol] == 1)
                    for (int i = -1; i < 2; i++)
                        for (int j = -1; j < 2; j++)
                        {
                            if (Math.Abs(i) + Math.Abs(j) == 1)
                            {
                                var row = cellrow + i;
                                var col = cellcol + j;
                                var rowvalidindex = ParkingUtils.CheckMatrix.GetValidIndex(row, mtx.RowCount);
                                var colvalidindex = ParkingUtils.CheckMatrix.GetValidIndex(col, mtx.ColumnCount);
                                var matrixitem = ParkingUtils.CheckMatrix.GetMatrixItem(mtx, rowvalidindex, colvalidindex);
                                if (matrixitem == 1)
                                { emptyadjacentcells++; }
                            }
                        }
            }
            startcell[0] = cellrow;
            startcell[1] = cellcol;
            return startcell;
        }
        //public static int[] FirstPathCell(Matrix mtx, List<int[]> allcells)
        //{
        //  var startcell = new int[2];
        //  int cellrow = 0;
        //  int cellcol = 0;
        //  var iter = 0;
        //  var emptyadjacentcells = 0;
        //  int cellindex = 0;
        //  // to select the start cell I want to choose a cell which has at least to empty adjacent cells as neighbor
        //  while ((mtx[cellrow, cellcol] != 1) & iter < 200 & emptyadjacentcells<2) // for not crashing if it /couldn't/ find any cell with the given condition )
        //  {
        //    emptyadjacentcells = 0;
        //    iter++;
        //    var ran = new Random();
        //    cellindex = ran.Next(allcells.Count);
        //    var randomcell = allcells[cellindex];
        //    cellrow = randomcell[0];
        //    cellcol = randomcell[1];
        //    if (mtx[cellrow, cellcol] == 1)
        //      for (int i = -1; i < 2; i++)
        //      for (int j = -1; j < 2; j++)
        //      {
        //        try
        //        {
        //          if (Math.Abs(i) + Math.Abs(j) == 1)
        //            if (mtx[cellrow + i, cellcol + j] == 1) emptyadjacentcells++;
        //        }
        //        catch { }
        //      }
        //
        //    if (emptyadjacentcells > 1)
        //    {
        //      startcell[0] = cellrow;
        //      startcell[1] = cellcol;
        //      allcells.Remove(randomcell);
        //     // allcells.RemoveAt(cellindex);
        //      break;
        //    }
        //  }
        //  return startcell;
        //}
        //int side; 0:north/ 1:west/ 2:east/ 3:south
        public class Ramp
        {
            //DataTree<Vector3d> Types;
            /// اینجا تایپهای رمپ رو تعریف میکنم و به این صورت هست که با یک i, j
            /// فرضی که سلول شروع رمپ هست نهایتا کار میکنیم. و هر نوع رمپ که توی یک فایل راینو انواعش رو ذخیره کردم بر اساس یک سری وکتور میتونه تعریف بشه. که فرض اینه که سلول شروع i,j
            /// صفر داره و براساس موعیت بقیه سلولها نسبت به سلول شروع بقیه سلولها هم بدست میانم
            /// مختصات z خ
            /// همه رمپهایی که تعریف کردیم هم طبیعتا ۰ هستت.
            /// بعد از تعریف کردن خود تایپ ها باید جهتگیریهاشون هم تعریف بشه. و خوب هشت جهت گیری ممکن بر اساس ۴ تا روتیشن و ۲ تا میرور ممکنه کلا.
            ///  نهایتا ما همه انواع رو با همه جهتگیری ها یک جا خواهیم داشت و
            ///  بعد باید بریم پلان رو چک کنیم ببینیم امکان جایگذاری کدام یک از این رمپها با کدام اورینتیشن در کدام سلول هست. و بعد از بین همه گزینه های ممکن که برای همه سلولهامون اونا رو تولید کردیم باید که رمپمونو انتخاب کنیم که این خودش میتونه توی یک پروسه رندوم باشه که بعدا بهینه یابی بشه.
            ///
            //
            //public Transform[] Orientations;
            public List<Transform> Orientations
            {
                get { return ramporientations(); }
            }
            public static DataTree<Point3d> ramptypes()
            {
                DataTree<Point3d> ramptypes = new DataTree<Point3d>();
                // all Types;
                var type0 = new List<Point3d>();
                var type1 = new List<Point3d>();
                var type2 = new List<Point3d>();
                var type3 = new List<Point3d>();
                var type4 = new List<Point3d>();
                var type5 = new List<Point3d>();
                //type0:
                var path0 = new GH_Path(0);
                var t0pt0 = new Point3d(0, 0, 0); type0.Add(t0pt0);
                var t0pt1 = new Point3d(0, 1, 0); type0.Add(t0pt1);
                var t0pt2 = new Point3d(0, 2, 0); type0.Add(t0pt2);
                var t0pt3 = new Point3d(0, 3, 0); type0.Add(t0pt3);
                var t0pt4 = new Point3d(0, 4, 0); type0.Add(t0pt4);
                ramptypes.AddRange(type0, path0);
                //type1:
                var path1 = new GH_Path(1);
                var t1pt0 = new Point3d(0, 0, 0); type1.Add(t1pt0);
                var t1pt1 = new Point3d(1, 0, 0); type1.Add(t1pt1);
                var t1pt2 = new Point3d(2, 0, 0); type1.Add(t1pt2);
                var t1pt3 = new Point3d(2, 1, 0); type1.Add(t1pt3);
                var t1pt4 = new Point3d(2, 2, 0); type1.Add(t1pt4);
                var t1pt5 = new Point3d(1, 2, 0); type1.Add(t1pt5);
                var t1pt6 = new Point3d(0, 2, 0); type1.Add(t1pt6);
                ramptypes.AddRange(type1, path1);
                //type2:
                var path2 = new GH_Path(2);
                var t2pt0 = new Point3d(0, 0, 0); type2.Add(t2pt0);
                var t2pt1 = new Point3d(1, 0, 0); type2.Add(t2pt1);
                var t2pt2 = new Point3d(2, 0, 0); type2.Add(t2pt2);
                var t2pt3 = new Point3d(2, 1, 0); type2.Add(t2pt3);
                var t2pt4 = new Point3d(1, 1, 0); type2.Add(t2pt4);
                var t2pt5 = new Point3d(0, 1, 0); type2.Add(t2pt5);
                var t2pt6 = new Point3d(-1, 1, 0); type2.Add(t2pt6);
                ramptypes.AddRange(type2, path2);
                //type3:
                var path3 = new GH_Path(3);
                var t3pt0 = new Point3d(0, 0, 0); type3.Add(t3pt0);
                var t3pt1 = new Point3d(1, 0, 0); type3.Add(t3pt1);
                var t3pt2 = new Point3d(2, 0, 0); type3.Add(t3pt2);
                var t3pt3 = new Point3d(3, 0, 0); type3.Add(t3pt3);
                var t3pt4 = new Point3d(3, -1, 0); type3.Add(t3pt4);
                var t3pt5 = new Point3d(2, -1, 0); type3.Add(t3pt5);
                var t3pt6 = new Point3d(1, -1, 0); type3.Add(t3pt6);
                ramptypes.AddRange(type3, path3);
                //type4:
                var path4 = new GH_Path(4);
                var t4pt0 = new Point3d(0, 0, 0); type4.Add(t4pt0);
                var t4pt1 = new Point3d(1, 0, 0); type4.Add(t4pt1);
                var t4pt2 = new Point3d(2, 0, 0); type4.Add(t4pt2);
                var t4pt3 = new Point3d(2, 1, 0); type4.Add(t4pt3);
                var t4pt4 = new Point3d(2, 2, 0); type4.Add(t4pt4);
                var t4pt5 = new Point3d(2, 3, 0); type4.Add(t4pt5);
                ramptypes.AddRange(type4, path4);
                //type5:
                var path5 = new GH_Path(5);
                var t5pt0 = new Point3d(0, 0, 0); type5.Add(t5pt0);
                var t5pt1 = new Point3d(0, -1, 0); type5.Add(t5pt1);
                var t5pt2 = new Point3d(0, -2, 0); type5.Add(t5pt2);
                var t5pt3 = new Point3d(0, -3, 0); type5.Add(t5pt3);
                var t5pt4 = new Point3d(-1, -3, 0); type5.Add(t5pt4);
                var t5pt5 = new Point3d(-2, -3, 0); type5.Add(t5pt5);
                ramptypes.AddRange(type5, path5);
                return ramptypes;
            }
            public static List<Transform> ramporientations()
            {
                // here we define all 8 possible orientations which is 4 rotatios of the ramp and 2 choices for each rotation incase it is mirrored
                var transformations = new List<Transform>();
                var mirrortrnsfrm = new Transform(Transform.Mirror(Plane.WorldZX));
                transformations.Add(new Transform(Transform.Rotation(0, Point3d.Origin)));
                transformations.Add(new Transform(Transform.Rotation(0.5 * Math.PI, Point3d.Origin)));
                transformations.Add(new Transform(Transform.Rotation(Math.PI, Point3d.Origin)));
                transformations.Add(new Transform(Transform.Rotation(1.5 * Math.PI, Point3d.Origin)));
                transformations.Add(new Transform(Transform.Rotation(0, Point3d.Origin) * mirrortrnsfrm));
                transformations.Add(new Transform(Transform.Rotation(0.5 * Math.PI, Point3d.Origin) * mirrortrnsfrm));
                transformations.Add(new Transform(Transform.Rotation(Math.PI, Point3d.Origin) * mirrortrnsfrm));
                transformations.Add(new Transform(Transform.Rotation(1.5 * Math.PI, Point3d.Origin) * mirrortrnsfrm));
                return transformations;
            }
            public static DataTree<Point3d> OutlineSidesFinder(Matrix mtx, DataTree<Point3d> grid, out DataTree<int[]> sideptsaddress)
            {
                //in this part i check all cells and if they are on the border they are added to the data tree based on their side: //int side; 0:north/ 1:west/ 2:east/ 3:south
                var sideptsaddress_ = new DataTree<int[]>();
                var sidepts = new DataTree<Point3d>();
                for (int i = 0; i < mtx.RowCount; i++)
                    for (int j = 0; j < mtx.ColumnCount; j++)
                    {
                        if (mtx[i, j] == 1)
                        {
                            // here we say if the adjacent cell was not in plan"0 value" add it to side pts also if there existed no adjacent cell" again add it to side pts
                            if (i == 0)
                            {
                                var path = new GH_Path(0);
                                sidepts.Add(grid.Branch(i)[j], path);
                                sideptsaddress_.Add(new int[] { i, j }, path);
                            }
                            if (i == mtx.RowCount - 1)
                            {
                                var path = new GH_Path(3);
                                sidepts.Add(grid.Branch(i)[j], path);
                                sideptsaddress_.Add(new int[] { i, j }, path);
                            }
                            if (j == 0)
                            {
                                var path = new GH_Path(1);
                                sidepts.Add(grid.Branch(i)[j], path);
                                sideptsaddress_.Add(new int[] { i, j }, path);
                            }
                            if (j == mtx.ColumnCount - 1)
                            {
                                var path = new GH_Path(2);
                                sidepts.Add(grid.Branch(i)[j], path);
                                sideptsaddress_.Add(new int[] { i, j }, path);
                            }
                            if (i > 0 && i < mtx.RowCount - 1 && j > 0 && j < mtx.ColumnCount - 1)
                            {
                                if (mtx[i - 1, j] == 0)
                                {
                                    var path = new GH_Path(0);
                                    sidepts.Add(grid.Branch(i)[j], path);
                                    sideptsaddress_.Add(new int[] { i, j }, path);
                                }
                                if (mtx[i, j - 1] == 0)
                                {
                                    var path = new GH_Path(1);
                                    sidepts.Add(grid.Branch(i)[j], path);
                                    sideptsaddress_.Add(new int[] { i, j }, path);
                                }
                                if (mtx[i, j + 1] == 0)
                                {
                                    var path = new GH_Path(2);
                                    sidepts.Add(grid.Branch(i)[j], path);
                                    sideptsaddress_.Add(new int[] { i, j }, path);
                                }
                                if (mtx[i + 1, j] == 0)
                                {
                                    var path = new GH_Path(3);
                                    sidepts.Add(grid.Branch(i)[j], path);
                                    sideptsaddress_.Add(new int[] { i, j }, path);
                                }
                            }
                        }
                    }
                sideptsaddress = sideptsaddress_;
                return sidepts;
            }
            /* public static DataTree<Point3d> OutlineSidesFinder(Matrix mtx, DataTree<Point3d> grid, out DataTree<int[]> sideptsaddress)
             {
                 //in this part i check all cells and if they are on the border they are added to the data tree based on their side: //int side; 0:north/ 1:west/ 2:east/ 3:south
                 var sideptsaddress_ = new DataTree<int[]>();
                 var sidepts = new DataTree<Point3d>();
                 for (int i = 0; i < mtx.RowCount; i++)
                     for (int j = 0; j < mtx.ColumnCount; j++)
                     {
                         if (mtx[i, j] == 1)
                         {
                              // here we say if the adjacent cell was not in plan"0 value" add it to side pts also if there existed no adjacent cell" again add it to side pts
                         if(i-1>= 0)
                         if (mtx[i - 1, j] == 0)
                         {
                             var path = new GH_Path(0);
                             sidepts.Add(grid.Branch(i)[j], path);
                             sideptsaddress_.Add(new int[] { i, j }, path);
                         }
                         if (i - 1 < 0)
                         {
                         var path = new GH_Path(0);
                         sidepts.Add(grid.Branch(i)[j], path);
                         sideptsaddress_.Add(new int[] { i, j }, path);
                         }
                         if(j-1>=0)
                         {
                             if (mtx[i, j - 1] == 0)
                             {
                                 var path = new GH_Path(1);
                                 sidepts.Add(grid.Branch(i)[j], path);
                                 sideptsaddress_.Add(new int[] { i, j }, path);
                             }
                         }
                         if (j - 1 < 0)
                         {
                             var path = new GH_Path(1);
                             sidepts.Add(grid.Branch(i)[j], path);
                             sideptsaddress_.Add(new int[] { i, j }, path);
                         }
                         if((j+1)<mtx.ColumnCount)
                         { 
                             if (mtx[i, j + 1] == 0)
                             {
                                 var path = new GH_Path(2);
                                 sidepts.Add(grid.Branch(i)[j], path);
                                 sideptsaddress_.Add(new int[] { i, j }, path);
                             }
                         }
                         if ((j + 1) > mtx.ColumnCount)
                         {
                             var path = new GH_Path(2);
                             sidepts.Add(grid.Branch(i)[j], path);
                             sideptsaddress_.Add(new int[] { i, j }, path);
                         }
                         if(i+1<mtx.RowCount)
                         {
                             if (mtx[i + 1, j] == 0)
                             {
                                 var path = new GH_Path(3);
                                 sidepts.Add(grid.Branch(i)[j], path);
                                 sideptsaddress_.Add(new int[] { i, j }, path);
                             }
                         }
                         if (i + 1 > mtx.RowCount)
                         {
                             var path = new GH_Path(3);
                             sidepts.Add(grid.Branch(i)[j], path);
                             sideptsaddress_.Add(new int[] { i, j }, path);
                         }
                         }
                     }
                 sideptsaddress = sideptsaddress_;
                 return sidepts;
             }
            */
            //  if ((!(i - 1 < 0) & mtx[i - 1, j] == 0) || i - 1 < 0)
            //  {
            //    var path = new GH_Path(0);
            //    sidepts.Add(grid.Branch(i)[j], path);
            //  }
            //  if ((!(j - 1 < 0) & mtx[i, j - 1] == 0) || j - 1 < 0)
            //  {
            //    var path = new GH_Path(1);
            //    sidepts.Add(grid.Branch(i)[j], path);
            //  }
            //  if ((!(j + 1 > mtx.ColumnCount - 1) & mtx[i, j + 1] == 0 ) /*|| j /+ /1> .mtx.ColumnCount - 1*/)
            //  {
            //    var path = new GH_Path(2);
            //    sidepts.Add(grid.Branch(i)[j], path);
            //  }
            //  if (((i + 1 < mtx.RowCount) & mtx[i + 1, j] == 0) /*|| i + 1 > mtx.RowCount -1*/)
            //  {
            //    var path = new GH_Path(3);
            //    sidepts.Add(grid.Branch(i)[j], path);
            //  }
            public static List<int[]> RampPossibleOptions(Matrix mtx, DataTree<Point3d> grid, DataTree<int[]> allsideptsaddress, DataTree<Point3d> sidepts)
            {
                //here i want to save all possible choices for ramp placement in plan the first generation of data tree is based on side(which is totally 4)
                // the second generation of datatree is ramp types(which is totally 6)
                // the last generation is ramp orientation (which is tatally 8 except for the first ramp type which is totally 4)
                // in the branch corrsponding to each side, type and orientation i save a boolean value
                // which is only true if all ramp cells can be placed inside the plan and they won't cross the outline border
                var alloptions = new List<int[]>();
                var ramptypes = Ramp.ramptypes();
                var ramporientations = Ramp.ramporientations();
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < sidepts.Branch(i).Count; j++)
                    {
                        for (int t = 0; t < 6/*the num of ramp types*/; t++)
                        {
                            for (int o = 0; o < 8;/*the num of ramp orientations*/ o++)
                            {
                                var path = new GH_Path(i, j, t, o);
                                // var flag = true;
                                var cell = allsideptsaddress.Branch(i)[j];
                                // item[0] = mtx row
                                // item[1] = mtx col
                                int num = 0;
                                foreach (var pt in ramptypes.Branch(t))
                                {
                                    var ptdup = new Point3d(pt);
                                    ptdup.Transform(ramporientations[o]);
                                    int row = cell[0] - (int)ptdup.Y;
                                    var col = cell[1] + (int)ptdup.X;
                                    var rowvalidindex = CheckMatrix.GetValidIndex(row, mtx.RowCount);
                                    var colvalidindex = CheckMatrix.GetValidIndex(col, mtx.ColumnCount);
                                    var matrixitem = CheckMatrix.GetMatrixItem(mtx, rowvalidindex, colvalidindex);
                                    if (matrixitem != -1)
                                        if (matrixitem != 0)
                                        {
                                            num++;
                                        }
                                }
                                if (num == ramptypes.Branch(t).Count)
                                {
                                    var newopt = new int[4] { i, j, t, o };
                                    alloptions.Add(newopt);
                                }
                            }
                        }
                    }
                }
                return alloptions;
            }
            public static void rampplacement(Matrix mtx, List<int[]> rampoptions, DataTree<int[]> sidepts, DataTree<Point3d> ramptypes, List<Transform> ramporientations, int side, out List<int> rampinformation, out int[] fistpathcell)
            {
                ///rampoptions: contains a list of int[] each contains these information in order: side, sidecellindex, type , orientation
                var startcell = new int[2];
                var rampinfo = new List<int>();
                var random = new Random();
                var ran = random.Next(rampoptions.Count);
                var cellbasedramp = new List<int[]>();
                foreach (var r in rampoptions)
                {
                    //here I want to save all ramp options based on user defined entrance cell
                    if (r[0] == side)
                    {
                        cellbasedramp.Add(r);
                    }
                    else { }
                }
                ran = random.Next(cellbasedramp.Count);
                rampinfo.Add(cellbasedramp[ran][0]);
                rampinfo.Add(cellbasedramp[ran][1]);
                rampinfo.Add(cellbasedramp[ran][2]);
                rampinfo.Add(cellbasedramp[ran][3]);
                var type = ramptypes.Branch(cellbasedramp[ran][2]);
                var orientation = ramporientations[cellbasedramp[ran][3]];
                var startcell_i = sidepts.Branch(cellbasedramp[ran][0])[cellbasedramp[ran][1]][0];
                var startcell_j = sidepts.Branch(cellbasedramp[ran][0])[cellbasedramp[ran][1]][1];
                for (int t = 0; t < type.Count; t++)
                {
                    var newt = type[t];
                    newt.Transform(orientation);
                    if (t == type.Count - 1)
                    {
                        startcell[0] = startcell_i - (int)newt.Y;
                        startcell[1] = startcell_j + (int)newt.X;
                    }
                    if (t != type.Count - 1)
                        mtx[startcell_i - (int)newt.Y, startcell_j + (int)newt.X] = 4;
                    //upper line to include ramp start cell and end cell in plan area for generating parking lots
                    // bcz they are in the floor level and have nothing to do with the ramp slope!!
                }
                rampinformation = rampinfo;
                fistpathcell = startcell;
            }
        }
        public static void pathfinder(Matrix mtx, int[] startcell,
            DataTree<Point3d> grid, ref DataTree<Transform> cartrnsfrms, DataTree<Point3d> mainpathpts, ref int pathindex, ref int currentpathitemcount, DataTree<int[]> pathptsloc, ref int startcellfindingattempt, ref List<ParkingUtils.PathInfo.ParkingPath> parkingpaths)
        {
            //startcellfindignattemp: the number of times we remove a pathindex the iteration for adding a start cell to the algorithm is calculated and if gets higher than a specific value it seems out attempt to find a new path is useless and it's better to jump outside algorithm with the current result!!!!
            //pathptsloc: for saving the n,m location of each path point based on the pathindex (I use this information in the PathConnection algorithm)
            //var cartransforms to return correspinding transformation matrix of each cell that has a car inside of the car base curve
            var n = startcell[0];
            var m = startcell[1];
            //mainpathpts.Add(grid.Branch(n)[m], path);
            // in below i want to check if we have no parking path in out list of paths then this is the first run of out pathfinder algorithem
            // so we should create a parking path and create list of cells to plac path cells inside that. 
            if (parkingpaths.Count == 0)
            {
                var parkingpath = new PathInfo.ParkingPath();
                var cells1 = new List<PathInfo.Cell>();
                parkingpath.cells = cells1;
                parkingpaths.Add(parkingpath);
                var pathcell = new PathInfo.Cell(n, m, parkingpaths[0]);
                parkingpath.cells.Add(pathcell);
            }
            mtx[n, m] = 3;
            var pathpts = new Point3d[4];
            int nextn = 0;
            int nextm = 0;
            List<double> adjacencynum = new List<double>() { 0, 0, 0, 0 };
            for (int i = -1; i < 2; i++)
                for (int j = -1; j < 2; j++)
                    if (Math.Abs(i) + Math.Abs(j) == 1)
                    {
                        var num = 0.0;
                        if (!(n + i < 0 || n + i > mtx.RowCount - 1 || m + j < 0 || m + j > mtx.ColumnCount - 1))
                        {
                            // here for not assigning values for items that are outside the bounds of matrix
                            if (mtx[n + i, m + j] == 1)
                            {
                                for (int k = -1; k < 2; k++)
                                    for (int t = -1; t < 2; t++)
                                    {
                                        if (Math.Abs(k) + Math.Abs(t) == 1)
                                        {
                                            if (!(n + i + k < 0 || n + i + k > mtx.RowCount - 1 || m + j + t < 0 || m + j + t > mtx.ColumnCount - 1))
                                            {
                                                if (mtx[n + i + k, m + j + t] == 1)
                                                {
                                                    num++;
                                                }
                                                if (mtx[n + i + k, m + j + t] == 3)
                                                {
                                                    num += 0.5;
                                                }
                                                if (mtx[n + i + k, m + j + t] == 2)
                                                {
                                                    num += 0.5;
                                                }
                                            }
                                        }
                                    }
                            }
                        }
                        switch (i)
                        {
                            case -1:
                                adjacencynum[0] = num; break;
                            case 0:
                                switch (j) { case -1: adjacencynum[1] = num; break; case 1: adjacencynum[2] = num; break; }
                                break;
                            case 1:
                                adjacencynum[3] = num; break;
                        }
                    }
            // in this part of the code I want to apply probabilities of selection of each index for the next cell of the path .
            // in the previous versions of the algorithm the index was always index of the maximum element in the list.
            // some dose of probability may conclude to interesting results
            var path = new GH_Path(pathindex);
            int nextcell;
            var allmainpathptslocs = new DataTree<int[]>();
            var vplus = new Vector3d(0, 5, 0);
            var vminus = new Vector3d(0, -5, 0);
            var hplus = new Vector3d(5, 0, 0);
            var hminus = new Vector3d(-5, 0, 0);
            var vecbase = new Vector3d(new Point3d(grid.Branch(n)[m]));
            Transform rotation0 = new Transform(Transform.Rotation(-Math.PI / 2, Plane.WorldXY.Origin));
            Transform rotation2 = new Transform(Transform.Rotation(Math.PI, Plane.WorldXY.Origin));
            Transform rotation3 = new Transform(Transform.Rotation(Math.PI / 2, Plane.WorldXY.Origin));
            Transform translation0 = new Transform(Transform.Translation(new Vector3d(vecbase + vplus)));//translation for neighbocell //number0.
            Transform translation1 = new Transform(Transform.Translation(new Vector3d(vecbase + hminus)));
            Transform translation2 = new Transform(Transform.Translation(new Vector3d(vecbase + hplus)));
            Transform translation3 = new Transform(Transform.Translation(new Vector3d(vecbase + vminus)));
            //scince some branches of m and n plus and minus one does not exist and the code gives out of range error so we can consider the 
            // center of our base cell and add the locatioan of neighbor cells vertically and horizontally based on their locatioan 
            var path0 = new GH_Path(pathindex, n - 1, m);
            var path1 = new GH_Path(pathindex, n, m - 1);
            var path2 = new GH_Path(pathindex, n, m + 1);
            var path3 = new GH_Path(pathindex, n + 1, m);
            List<GH_Path> currentlooppaths = new List<GH_Path>();
            // here I want to save all path addresses that we work with in a single iteration of our loop scince at the end of the loop we check if the number of pathpts in the specific 
            // path index so to remove the corresponding paths if the number of pts in mainpath pts was less than a specific number we can just check the recent paths. 
            // I will skip this step bcz the code is full of drawbacks and it's better to fix the previous ones 
            // but just to remember fix it in following steps:)))))))))))
            // we can add the list of paths as an out variable in pathfinder method so it will update in each iteration. 
            if ((adjacencynum.Max() > 2))
            {
                double max = adjacencynum.Max();
                List<int> indices = Enumerable.Range(0, adjacencynum.Count).Where(i => adjacencynum[i] == max).ToList();
                Random random = new Random();
                int randomIndex = indices[random.Next(indices.Count)];
                nextcell = randomIndex;
                // in here to locate the car blocks in the right orientation in the plan we should rotate the block in each case based on
                // the position of cell and also change the basis of the block so that it would be placed in the right position
                // rotation for case 1 is 0 degree because we assume that the input block in standard for the case 1 orientation
                // I assing a cell here to adjust its charrecteristics
                // like row and column in the code below. 
                switch (nextcell)
                {
                    case 0:
                        if (n - 1 >= 0)
                        {
                            mtx[n - 1, m] = 3; mainpathpts.Add(grid.Branch(n - 1)[m], path0);
                            pathptsloc.Add(new int[] { n - 1, m }, path);
                        }
                        if (m - 1 >= 0)
                        {
                            if (mtx[n, m - 1] == 1) { mtx[n, m - 1] = 2; cartrnsfrms.Add(translation1, path1); }
                        }
                        if (m + 1 < mtx.ColumnCount)
                        {
                            if (mtx[n, m + 1] == 1) { mtx[n, m + 1] = 2; cartrnsfrms.Add(translation2 * rotation2, path2); }
                        }
                        if (n + 1 < mtx.RowCount)
                        {
                            if (mtx[n + 1, m] == 1) { mtx[n + 1, m] = 2; cartrnsfrms.Add(translation3 * rotation3, path3); }
                        }
                        nextn = n - 1;
                        nextm = m;
                        break;
                    case 1:
                        if (n - 1 >= 0)
                        {
                            if (mtx[n - 1, m] == 1) { mtx[n - 1, m] = 2; cartrnsfrms.Add(translation0 * rotation0, path0); }
                        }
                        if (m - 1 >= 0)
                        {
                            mtx[n, m - 1] = 3; mainpathpts.Add(grid.Branch(n)[m - 1], path1);
                            pathptsloc.Add(new int[] { n, m - 1 }, path);
                        }
                        if (m + 1 < mtx.ColumnCount)
                        {
                            if (mtx[n, m + 1] == 1) { mtx[n, m + 1] = 2; cartrnsfrms.Add(translation2 * rotation2, path2); }
                        }
                        if (n + 1 < mtx.RowCount)
                        {
                            if (mtx[n + 1, m] == 1) { mtx[n + 1, m] = 2; cartrnsfrms.Add(translation3 * rotation3, path3); }
                        }
                        nextn = n;
                        nextm = m - 1;
                        break;
                    case 2:
                        if (n - 1 >= 0)
                        {
                            if (mtx[n - 1, m] == 1) { mtx[n - 1, m] = 2; cartrnsfrms.Add(translation0 * rotation0, path0); }
                        }
                        if (m - 1 >= 0)
                        {
                            if (mtx[n, m - 1] == 1) { mtx[n, m - 1] = 2; cartrnsfrms.Add(translation1, path1); }
                        }
                        if (m + 1 < mtx.ColumnCount)
                        {
                            mtx[n, m + 1] = 3; mainpathpts.Add(grid.Branch(n)[m + 1], path2);
                            pathptsloc.Add(new int[] { n, m + 1 }, path);
                        }
                        if (n + 1 < mtx.RowCount)
                        {
                            if (mtx[n + 1, m] == 1) { mtx[n + 1, m] = 2; cartrnsfrms.Add(translation3 * rotation3, path3); }
                        }
                        nextn = n;
                        nextm = m + 1;
                        break;
                    case 3:
                        if (n - 1 >= 0)
                        {
                            if (mtx[n - 1, m] == 1) { mtx[n - 1, m] = 2; cartrnsfrms.Add(translation0 * rotation0, path0); }
                        }
                        if (m - 1 >= 0)
                        {
                            if (mtx[n, m - 1] == 1) { mtx[n, m - 1] = 2; cartrnsfrms.Add(translation1, path1); }
                        }
                        if (m + 1 < mtx.ColumnCount)
                        {
                            if (mtx[n, m + 1] == 1) { mtx[n, m + 1] = 2; cartrnsfrms.Add(translation2 * rotation2, path2); }
                        }
                        if (n + 1 < mtx.RowCount)
                        {
                            mtx[n + 1, m] = 3; mainpathpts.Add(grid.Branch(n + 1)[m], path3);
                            pathptsloc.Add(new int[] { n + 1, m }, path);
                        }
                        nextn = n + 1;
                        nextm = m;
                        break;
                }
                n = nextn;
                m = nextm;
                startcell[0] = n;
                startcell[1] = m;
                //....
                var nwecell = new PathInfo.Cell(n, m, parkingpaths[pathindex]);
                parkingpaths[pathindex].cells.Add(nwecell);
                //...
                currentpathitemcount++;
            }
            else
            {
                //in the code we may find some cells that satisfy our condition for the start cell but their neighbor can not satisfy the condition for continuing path. so we may have a number of paths with a single cell, to avoid this befor we jump to the next path we should check the number of elements in the current path and if the items are less than our defined number we should omit the current path and then jump to the next one. and also i reduce path index by 1 to reach the current index in path when I ++pathindex in the next step
                if (currentpathitemcount < 2)
                {
                    parkingpaths.RemoveAt(pathindex);
                    pathptsloc.RemovePath(path); //here we set the current pathindex to save the location of path cells only. and if the number of the cells in path is less than 3 we may omit the location of the path either.
                    foreach (GH_Path p in mainpathpts.Paths)
                    {
                        if (p.Indices[0] == pathindex)
                        {
                            mtx[p.Indices[1], p.Indices[2]] = 1;//bcz we omitted the path with lower that 3 elements we should change the value corresponding to these cells to 1 in the matrix so that we can still choose them for path and other functions
                            mainpathpts.RemovePath(p);
                        }
                    }
                    foreach (GH_Path p in cartrnsfrms.Paths)
                    {
                        if (p.Indices[0] == pathindex)
                        {
                            mtx[p.Indices[1], p.Indices[2]] = 1;// and the same as last part we should change the values of the neighbor cells to the path to 1 to use them in further steps. bcz we omitted the car transformations for those cells eighter.
                            cartrnsfrms.RemovePath(p);
                        }
                    }
                    pathindex--;
                    startcellfindingattempt++;
                }
                else
                {
                    if (n - 1 >= 0)
                        if (mtx[n - 1, m] == 1) { mtx[n - 1, m] = 2; cartrnsfrms.Add(translation0 * rotation0, path0); }
                    if (m - 1 >= 0)
                        if (mtx[n, m - 1] == 1) { mtx[n, m - 1] = 2; cartrnsfrms.Add(translation1, path1); }
                    if (m + 1 < mtx.ColumnCount)
                        if (mtx[n, m + 1] == 1) { mtx[n, m + 1] = 2; cartrnsfrms.Add(translation2 * rotation2, path2); }
                    if (n + 1 < mtx.RowCount)
                        if (mtx[n + 1, m] == 1) { mtx[n + 1, m] = 2; cartrnsfrms.Add(translation3 * rotation3, path3); }
                }
                var newstartcell = FirstPathCell(mtx);
                var newparkingpath = new PathInfo.ParkingPath();
                parkingpaths.Add(newparkingpath);
                newparkingpath.cells = new List<PathInfo.Cell>();
                pathindex++;
                currentpathitemcount = 0;
                startcell[0] = newstartcell[0];
                startcell[1] = newstartcell[1];
                parkingpaths[pathindex].cells.Add(new PathInfo.Cell(newstartcell[0], newstartcell[1], parkingpaths[pathindex]));
                //var allpathpts = new DataTree<Point3d>(mainpathpts);
                //allpathpts.Flatten();
                //do
                //{
                //
                //} while (!allpathpts.Branch(0).Contains(grid.Branch(newstartcell[0])[newstartcell[1]]));		startcell[0]	5	int
            }
        }
        public class PathInfo
        {
            public Cell cell { get { return cell; } set { } }
            public ParkingPath parkingpath { get { return parkingpath; } set { } }
            //public PathCells pathcells { get;  set; }
            public class ParkingPath
            {
                //int type;
                public List<Cell> cells { get; set; }
                private int _pathindex;//{ get { return _pathindex; } set { } }
                public int pathindex { get { return _pathindex; } set { _pathindex = value; } }
                private PathType _pathtype;//{ get { return _pathtype; } set { } }
                public PathType pathtype { get { return _pathtype; } set { _pathtype = value; } }
                private int _cellcount;//{ get { return _cellcount; } set { } }
                public int cellcount { get { return _cellcount; } set { _cellcount = value; } }
                public ParkingPath(List<Cell> cells, int pathindex, PathType pathtype)
                {
                    this.cells = cells;
                    this.pathindex = pathindex;
                    this.pathtype = pathtype;
                    this.cellcount = cells.Count;
                }
                public ParkingPath() { }
                public override string ToString()
                {
                    var cells = new ParkingPath().cells;
                    string str = "";
                    if (this.cells != null)
                    {
                        for (int i = 0; i < cells.Count; i++)
                        {
                            string strnew = i.ToString() + ": " + cells[i].ToString();
                            str += strnew;
                        }
                    }
                    return str;
                }
            }
            public class Cell
            {
                private int _row;//{ get { return _row; } set { } }
                public int row { get { return _row; } set { _row = value; } }
                private int _col;//{ get { return _col; } set { } }
                public int col { get { return _col; } set { _col = value; } }
                private ParkingPath _parkingpath;// { get { return _parkingpath; } set { } }
                public ParkingPath parkingpath { get { return _parkingpath; } set { _parkingpath = value; } }
                public Cell(int row_, int col_, ParkingPath parkingpath_)
                {
                    row = row_;
                    col = col_;
                    parkingpath = parkingpath_;
                }
                public Cell(int row_, int col_)
                {
                    row = row_;
                    col = col_;

                }
                public Cell() { }
                public override string ToString()
                {
                    var str = string.Empty;
                    str += "{";
                    str += _row;
                    str += ",";
                    str += _col;
                    str += "}";
                    return str;
                }
            }
            public enum PathType
            {
                MainPath = 0,
                ConnectionPath = 1
            }
        }
        public class mainPathConnection
        {
            public enum pathbridgetype
            {
                nbased, mbased, alignedhorizontal, alignedvertical
            }
            bool IsNbasedPathValid;
            bool IsMbasedPathValid;
            bool IsHorizontalPathValid;
            bool IsVerticalPathValid;
            //here we set these boolean values to assign them in code methods. 
            // if the path is not valid>> (wheather there is a ramp cell in distance btw cells or there is a cell outside the 
            // plan boundaries )>> then we set the ispathpvalid values to false and it is a filter to decide btw options to find available ones.
            public class PathConnection
            {
            }

            public static void CreateConnectionPath(Matrix mtx, DataTree<Point3d> GridPts, List<PathInfo.ParkingPath> parkingpaths,
                DataTree<Transform> cartrnsfrms, DataTree<Point3d> mainpathpts)
            {
                // in these 2 below for loops i want to choose all 2 posssible combinations in existing paths and check the shortest distance between each couple of paths finally i should take the best choice considering both distance and lotgain.
                var remomvingPaths = new List<GH_Path>();
                for (int i = 0; i < parkingpaths.Count; i++)
                {
                    for (int j = 0; j < parkingpaths.Count; j++)
                    {
                        var pathFirst = parkingpaths[i];
                        var pathSecond = parkingpaths[j];
                        if (pathFirst.cells != null && pathSecond.cells != null)
                        {
                            if (pathFirst.cells.Count > 0 && pathSecond.cells.Count > 0)
                            {
                                var random1 = new Random();
                                var random2 = new Random();
                                var ran1 = random1.Next(pathFirst.cells.Count);
                                var ran2 = random2.Next(pathSecond.cells.Count);
                                var cellRan1 = pathFirst.cells[ran1];
                                var cellRan2 = pathSecond.cells[ran2];
                                //    bool isPossible ; 
                                var lotGain =
                                    ParkingUtils.mainPathConnection.LotGain(cellRan1, cellRan2, mtx, true, out bool isPossible);
                                if (lotGain >= 0)
                                {
                                    var n1 = cellRan1.row;
                                    var m1 = cellRan1.col;
                                    var n2 = cellRan2.row;
                                    var m2 = cellRan2.col;
                                    // var signn2 = ((m2 - m1) / Math.Abs(m2 - m1));
                                    var signn = (n2 - n1 >= 0) ? 1 : -1;
                                    var signm = (m2 - m1 >= 0) ? 1 : -1;
                                    var allBridgePathCells = new List<ParkingUtils.PathInfo.Cell>();
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
                                    var parkingPathNew = new PathInfo.ParkingPath();
                                    parkingpaths.Add(parkingPathNew);
                                    parkingPathNew.pathindex = parkingpaths.Count;

                                    foreach (var cell in allBridgePathCells)
                                    {
                                        foreach (var path in cartrnsfrms.Paths)
                                        {
                                            if (path.Indices[1] == cell.row && path.Indices[2] == cell.col)
                                            {
                                                // cartrnsfrms.RemovePath(path);
                                                remomvingPaths.Add(path);
                                            }
                                            mtx[cell.row, cell.col] = 2;
                                        }
                                        mtx[cell.row, cell.col] = 3;
                                        var pathindex = parkingpaths.Count;
                                        var pathNewCell = new GH_Path(pathindex, cell.row, cell.col);
                                        mainpathpts.Add(new Point3d(GridPts.Branch(cell.row)[cell.col]), pathNewCell);
                                        // parkingPathNew.cells.Add(cell);
                                        for (int k = -1; k < 2; k++)
                                            for (int t = -1; t < 2; t++)
                                            {
                                                if (Math.Abs(k) + Math.Abs(t) == 1)
                                                {
                                                    var rowNew = cell.row + k;
                                                    var colNew = cell.col + t;

                                                    var vplus = new Vector3d(0, 5, 0);
                                                    var vminus = new Vector3d(0, -5, 0);
                                                    var hplus = new Vector3d(5, 0, 0);
                                                    var hminus = new Vector3d(-5, 0, 0);
                                                    var vecbase = new Vector3d(new Point3d(GridPts.Branch(rowNew)[colNew]));
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
                                                    switch (k)
                                                    {
                                                        case -1:
                                                            cartrnsfrms.Add(new Transform(translation0 * rotation0), path0);
                                                            break;
                                                        case 1:
                                                            cartrnsfrms.Add(new Transform(translation3 * rotation3), path3);
                                                            break;
                                                    }

                                                    switch (t)
                                                    {
                                                        case -1:
                                                            cartrnsfrms.Add(new Transform(translation1), path1);
                                                            break;
                                                        case 1:
                                                            cartrnsfrms.Add(new Transform(translation3 * rotation3), path3);
                                                            break;
                                                    }

                                                }
                                            }
                                    }

                                 //   break;
                                }
                            }
                        }


                    }
                }
                foreach (var path in remomvingPaths)
                {
                    cartrnsfrms.RemovePath(path);
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
                        }
                    }

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
                            allbridgepathptsnbased.Append(newint);
                        }
                    }


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
                return nbasedgain;
            }

        }



        public static int ManhatanDistance(int[] p1, int[] p2)
        {
            //to connect paths we should calculate the cells distance and make connection between cells that have shortest distance to make sure we omit least number of parking lots to get the paths all have an access way to the start cell which is the entrance cell to the parking and if we have ramp it is the ramp end cell either.
            var distance = Math.Abs(p1[0] - p2[0]) + Math.Abs(p1[1] - p2[1]);
            if (distance > 0) distance--;
            return distance;
        }
        public static bool CellsAlignedHorizontal(int[] p1, int[] p2)
        {
            if (p1[0] == p2[0]) return true;
            //they have similar n
            else return false;
        }
        public static bool CellsAlignedVertical(int[] p1, int[] p2)
        {
            if (p1[1] == p2[1]) return true;
            //they have similar m
            else return false;
        }
        public static Curve outlinefromcells(DataTree<Rectangle3d> cells)
        {
            List<Curve> recs = new List<Curve>();
            foreach (var b in cells.Branches)
                foreach (var rec in b)
                {
                    recs.Add(rec.ToNurbsCurve());
                }
            IEnumerable<Curve> recsnew = recs as IEnumerable<Curve>;
            var outline = Curve.CreateBooleanUnion(recsnew, 0.01);
            return outline[0];
        }
        public static DataTree<Rectangle3d> extracellfinder(Matrix mtx, DataTree<Rectangle3d> cells)
        {
            var extracells = new DataTree<Rectangle3d>();
            for (int i = 0; i < mtx.RowCount; i++)
            {
                for (int j = 0; j < mtx.ColumnCount; j++)
                {
                    if (mtx[i, j] == 1)
                    {
                        var path = new GH_Path(i, j);
                        extracells.Add(cells.Branch(i, j)[0], path);
                    }
                }
            }
            return extracells;
        }
        public static int emptycell(Matrix mtx)
        {
            int value = 0;
            for (int i = 0; i < mtx.RowCount; i++)
            {
                for (int j = 0; j < mtx.ColumnCount; j++)
                {
                    if (mtx[i, j] == 1)
                    {
                        value++;
                    }
                }
            }
            return value;
        }
        /*public static DataTree<Rectangle3d> pathcellfinder(Matrix mtx, DataTree<Rectangle3d> cells)
        {
          var pathcells = new DataTree<Rectangle3d>();
          for (int i = 0; i < mtx.RowCount; i++)
          {
            for (int j = 0; j < mtx.ColumnCount; j++)
            {
              if (mtx[i, j] == 3)
              {
                var path = new GH_Path(i, j);
                pathcells.Add(cells.Branch(i, j)[0], path);
              }
            }
          }
          return pathcells;
        }*/
        public static DataTree<Point3d> columngridgenerator(Matrix mtx, DataTree<Rectangle3d> cells)
        {
            var xmax = mtx.ColumnCount * 5;
            var ymax = mtx.RowCount * 5;
            var pts = new DataTree<Point3d>();
            for (int i = 0; i < mtx.RowCount; i++)
            {
                for (int j = 0; j < mtx.ColumnCount; j++)
                {
                    if (mtx[i, j] == 1 | mtx[i, j] == 2)
                    {
                        for (int k = 0; k < 3; k++)
                            for (int t = 0; t < 3; t++)
                            {
                                if (!pts.PathExists(i * 2 + k, j * 2 + t))
                                {
                                    var path = new GH_Path(i * 2 + k, j * 2 + t);
                                    pts.Add(new Point3d((5 * j + t * 2.5), ymax - (5 * i + k * 2.5), 0), path);
                                }
                            }
                    }
                    if (mtx[i, j] == 3)
                    {
                        for (int k = 0; k < 3; k += 2)
                            for (int t = 0; t < 3; t += 2)
                            {
                                if (!pts.PathExists(i * 2 + k, j * 2 + t))
                                {
                                    var path = new GH_Path(i * 2 + k, j * 2 + t);
                                    pts.Add(new Point3d((5 * j + t * 2.5), ymax - (5 * i + k * 2.5), 0), path);
                                }
                            }
                    }
                }
            }
            return pts;
        }
        // in this class the inputs is the data tree of collection of points that a column in ligible to be located on them.
        // the purpose is to create a matrix form them where each item of the matrix is 0 if there is no point located on the referenced path
        // which we assess and is 1 if there is a point located in that direction which means we are ligible to place a column in that location
        // in further steps the the structural elements positions should be calculated form this matrix.
        public static Matrix colmatrix(DataTree<Point3d> pts, Matrix cellmtx)
        {
            Matrix mtx = new Matrix(cellmtx.RowCount * 2 + 1, cellmtx.ColumnCount * 2 + 1);
            for (int i = 0; i < mtx.RowCount; i++)
                for (int j = 0; j < mtx.ColumnCount; j++)
                {
                    mtx[i, j] = 1;
                    if (pts.PathExists(i, j))
                        mtx[i, j] = 1;
                    else
                        mtx[i, j] = 0;
                }
            return mtx;
        }
        public static DataTree<Point3d> colpts(Matrix colmtx, DataTree<Point3d> gridpts)
        {
            DataTree<Point3d> columnpts = new DataTree<Point3d>();
            int rowran = 0;
            int colran = 0;
            bool i = true;
            if (colmtx[rowran, colran] == 1) columnpts.Add(gridpts.Branch(rowran, colran)[0], new GH_Path(rowran, colran));
            else
                while (i)
                {
                    Random ran = new Random();
                    var next = ran.Next(1, 2);
                    if (next == 1 && rowran < colmtx.RowCount) rowran += 1;
                    if (next == 2 && colran < colmtx.ColumnCount) colran += 1;
                    if (colmtx[rowran, colran] == 1) { columnpts.Add(gridpts.Branch(rowran, colran)[0], new GH_Path(rowran, colran)); i = false; break; }
                }
            columnpts.Add(gridpts.Branch(2, colran)[0], new GH_Path(rowran, colran));
            return columnpts;
        }
        //این کلاس براساس نقاط اصللی مسیر عبور ماشین گرید در دو راستای افقی و عمودی لیستی از مختتصات را می‌دهد که امکان ستون‌‌گذاری روی آن‌ها
        // را نداریم
        public static void girdexception(List<Point3d> mainpathpts, out List<double> horizontalexceptoin, out List<double> verticalexception)
        {
            List<double> verexeption = new List<double>();
            List<double> horexception = new List<double>();
            for (int i = 0; i < mainpathpts.Count - 1; i++)
            {
                if (mainpathpts[i].Y == mainpathpts[i + 1].Y)
                {
                    horexception.Add(mainpathpts[i].Y);
                }
                if (mainpathpts[i].X == mainpathpts[i + 1].X)
                {
                    verexeption.Add(mainpathpts[i].X);
                }
            }
            horizontalexceptoin = horexception;
            verticalexception = verexeption;
        }
        // در این کلاس لیست استثناهای افقی و عمودی برای  گرید وارد می‌شود. و محدوده ساختمان هم داده می‌شود بر اساس محدوده
        //  گرید بندی در دو راستای افقی و عمودی با حذف مختصات داخل للیستهای استثنا تولید میشود. در خروجی هیچ گریدی از وسط مسیر  عبور نمیکند
        public static List<List<double>> gridcoordinates(Curve crv, List<double> verticalexception, List<double> horizontalexcepton)
        {
            var bbox = crv.GetBoundingBox(true);
            var Xinterval = new Interval(0, bbox.Max.X - bbox.Min.X);
            var Yinterval = new Interval(0, bbox.Max.Y - bbox.Min.Y);
            List<double> verticalcoordinates = new List<double>();
            List<double> horizontalcoordinares = new List<double>();
            double X = 0;
            while (X < Xinterval.T1)
            {
                if (!verticalexception.Contains(X))
                    verticalcoordinates.Add(X);
                X += 2.5;
            }
            double Y = 0;
            while (Y < Yinterval.T1)
            {
                if (!horizontalexcepton.Contains(Y))
                    horizontalcoordinares.Add(Y);
                Y += 2.5;
            }
            var availablegridvalues = new List<List<double>>();
            availablegridvalues.Add(horizontalcoordinares);
            availablegridvalues.Add(verticalcoordinates);
            return availablegridvalues;
        }
        //تو ای کلاس میخوام گوشه‌های کرو اوتلاینو بگیرم که بعد پردازششون کنم و
        // ستونگذاری جوری باشه که حتما روی گوشه های پلان ستون داشته باشیم در هر حال
        public static List<List<double>> outlinegridcoordinates(Curve outline)
        {
            var corners = new List<Point3d>();
            var pts = new Point3d[5];
            var polyline = new Polyline();
            double[] parameters;
            outline.TryGetPolyline(out polyline, out parameters);
            foreach (var t in parameters)
                corners.Add(outline.PointAt(t));
            List<double> listhorizontalcoordinates = new List<double>();
            List<double> listverticalcoordinates = new List<double>();
            for (int i = 0; i < corners.Count - 1; i++)
            {
                if (corners[i].X == corners[i + 1].X)
                    listverticalcoordinates.Add(corners[i].X);
                if (corners[i].Y == corners[i + 1].Y)
                    listhorizontalcoordinates.Add(corners[i].Y);
            }
            var outlinegridcoord = new List<List<double>>();
            listhorizontalcoordinates.Sort();
            listverticalcoordinates.Sort();
            outlinegridcoord.Add(listhorizontalcoordinates.Distinct().ToList());
            outlinegridcoord.Add(listverticalcoordinates.Distinct().ToList());
            return outlinegridcoord;
        }
        public static List<List<double>> gridgenerator(List<List<double>> gridcoordinates, List<List<double>> outlinegridcoords, double maxcoldistance, double preferreddistance)
        {
            var gridfinalcoords = new List<List<double>>();
            var horizontalfinalcoords = new List<double>();
            var verticalfinalcoords = new List<double>();
            for (int i = 0; i < outlinegridcoords[0].Count - 1; i++)
            {
                // in this part to calcualte horizontal grid lines
                if (outlinegridcoords[0][i + 1] - outlinegridcoords[0][i] > maxcoldistance)
                {
                    var num = Math.Round((outlinegridcoords[0][i + 1] - outlinegridcoords[0][i]) / preferreddistance);
                    var num2 = int.Parse(num.ToString());
                    if (num2 > 1) num2--;// here of we succeeded maximum distance and the rounded numberbut is 1 we should keep at least 1 number to add to the grid lines in the specified direction
                                         //but if num is greater than 1 we may reduce the number by 1 since for example we want to divide the distance to 2 segments so we need 1 additional grid
                    var startlimit = outlinegridcoords[0][i];
                    var endlimit = outlinegridcoords[0][i + 1];
                    var random = new Random();
                    var resulthorizontal = gridcoordinates[0].Where(k => k > startlimit && k < endlimit).OrderBy(k => random.Next()).Take(num2).ToList();
                    horizontalfinalcoords.AddRange(resulthorizontal);
                    horizontalfinalcoords.Distinct().ToList().Sort();
                }
            }
            horizontalfinalcoords.AddRange(outlinegridcoords[0]);
            //in this part to calculate vertical grid lines
            for (int i = 0; i < outlinegridcoords[1].Count - 1; i++)
            {
                if (outlinegridcoords[1][i + 1] - outlinegridcoords[1][i] > maxcoldistance)
                {
                    var num = Math.Round((outlinegridcoords[1][i + 1] - outlinegridcoords[1][i]) / preferreddistance);
                    var num2 = int.Parse(num.ToString());
                    if (num2 > 1) num2--;// here of we succeeded maximum distance and the rounded numberbut is 1 we should keep at least 1 number to add to the grid lines in the specified direction
                                         //but if num is greater than 1 we may reduce the number by 1 since for example we want to divide the distance to 2 segments so we need 1 additional grid
                    var startlimit = outlinegridcoords[1][i];
                    var endlimit = outlinegridcoords[1][i + 1];
                    var random = new Random();
                    var resultvertical = gridcoordinates[1].Where(k => k > startlimit && k < endlimit).OrderBy(k => random.Next()).Take(num2).ToList();
                    verticalfinalcoords.AddRange(resultvertical);
                    verticalfinalcoords.Distinct().ToList().Sort();
                }
                verticalfinalcoords.AddRange(outlinegridcoords[1]);
            }
            gridfinalcoords.Add(horizontalfinalcoords);
            gridfinalcoords.Add(verticalfinalcoords);
            return gridfinalcoords;
        }
    }
}
