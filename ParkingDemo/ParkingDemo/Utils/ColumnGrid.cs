using Grasshopper.Kernel.Data;
using Grasshopper;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingDemo.Utils
{
    internal class ColumnGrid
    {
        public static DataTree<Point3d> ColumnGridGenerator(Matrix mtx, DataTree<Rectangle3d> cells)
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
        public static Matrix ColMatrix(DataTree<Point3d> pts, Matrix cellmtx)
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
        public static DataTree<Point3d> ColPts(Matrix colmtx, DataTree<Point3d> gridpts)
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
        public static void GridException(List<Point3d> mainpathpts, out List<double> horizontalexceptoin, out List<double> verticalexception)
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
        public static List<List<double>> GridCoordinates(Curve crv, List<double> verticalexception, List<double> horizontalexcepton)
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
        public static List<List<double>> OutlineGridCoordinates(Curve outline)
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
        public static List<List<double>> GridGenerator(List<List<double>> gridcoordinates, List<List<double>> outlinegridcoords, double maxcoldistance, double preferreddistance)
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
        public static List<List<double>> GridGenerator2(List<List<double>> gridcoordinates, List<List<double>> outlinegridcoords, double maxcoldistance, double preferreddistance)
        {
            var gridfinalcoords = new List<List<double>>();
            var horizontalfinalcoords = new List<double>();
            var verticalfinalcoords = new List<double>();
            for (int i = 0; i < outlinegridcoords[0].Count - 1; i++)
            {
                var disGrid = outlinegridcoords[0][i + 1] - outlinegridcoords[0][i];
                if (disGrid <= preferreddistance) continue;
                else
                {
                    var startlimit = outlinegridcoords[0][i];
                    var endlimit = outlinegridcoords[0][i + 1];
                    var num = Math.Floor(disGrid / preferreddistance);
                    var secDis = disGrid / num;
                    //num--;
                    var accessableCords = gridcoordinates[0].Where(k => k > startlimit && k < endlimit).OrderBy(k => k).ToList();
                    for (int k = 1; k < num; k++)
                    {
                        var idealDis = k * secDis+ outlinegridcoords[0][i];
                        var selectedCoord = accessableCords.OrderBy(x => Math.Abs(x - idealDis)).ToList().First() ;
                        horizontalfinalcoords.Add(selectedCoord);
                    }

                }
            }
            for (int i = 0; i < outlinegridcoords[1].Count - 1; i++)
            {
                var disGrid = outlinegridcoords[1][i + 1] - outlinegridcoords[1][i];
                if (disGrid <= preferreddistance) continue;
                else
                {
                    var startlimit = outlinegridcoords[1][i];
                    var endlimit = outlinegridcoords[1][i + 1];
                    var num = Math.Floor(disGrid / preferreddistance);
                    var secDis = disGrid / num + outlinegridcoords[1][i]; ;
                    //num--;
                    var accessableCords = gridcoordinates[1].Where(k => k > startlimit && k < endlimit).OrderBy(k => k).ToList();

                    for (int k = 1; k < num; k++)
                    {
                        var idealDis = k * secDis;
                        var selectedCoord = accessableCords.OrderBy(x => Math.Abs(x - idealDis)).ToList().First();
                        verticalfinalcoords.Add(selectedCoord);
                    }
                }
            }
            horizontalfinalcoords.AddRange(outlinegridcoords[0]);
            verticalfinalcoords.AddRange(outlinegridcoords[1]);

            gridfinalcoords.Add(horizontalfinalcoords);
            gridfinalcoords.Add(verticalfinalcoords);
            return gridfinalcoords;

          
        }
    }
}
