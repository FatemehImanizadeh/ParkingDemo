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
        // here we set these boolean values to assign them in code methods.
        // if the path is not valid (whether there is a ramp cell in the distance between cells, or
        // there is a cell outside the plan boundaries) then we set ispathpossible to false, and it is
        // a filter to decide between options to find available ones.
        public class BridgePath
        {
            public enum Type
            {
                RowBased, ColBased
            }
            public int GainValue { get; set; }
            // Combined selection score = GainValue weighted against distance to the entrance.
            // This is what candidates are actually compared on now, instead of GainValue alone.
            public double Score { get; set; }
            public bool PathPossible { get; set; }
            public Cell CellFirst { get; set; }
            public Cell CellSecond { get; set; }
            public Type TypeValue { get; set; }
            public BridgePath()
            {
                Score = double.NegativeInfinity;
            }
        }

        private static int ManhattanDistance(Cell a, Cell b) =>
            Math.Abs(a.row - b.row) + Math.Abs(a.col - b.col);

        // Finds the best bridge cell pair between two specific paths, now scored by a mix of
        // LotGain and proximity to the entrance rather than by LotGain alone.
        public static BridgePath FindBestMatchPathCellsForConnection(Matrix mtx, ParkingPath PathFirst, ParkingPath PathSecond, Cell entranceCell)
        {
            // ==== TUNABLE PARAMETER =====================================================
            // proximityWeight controls how strongly a bridge's distance to the entrance is
            // penalized relative to its LotGain when picking between candidate bridges.
            //   0    -> distance ignored entirely (old behavior: highest LotGain wins)
            //   > 0  -> bridges closer to the entrance become more competitive even against
            //           a higher-gain option further away
            // Raise this if connections still end up too far from the entrance; lower it
            // (towards 0) if connections start ignoring genuinely better LotGain options.
            const double proximityWeight = 1.0;
            // =============================================================================

            var selectedBridgePath = new BridgePath();

            if (PathFirst.cells != null && PathSecond.cells != null && PathFirst.cells.Count > 0 && PathSecond.cells.Count > 0)
            {
                for (int i = 0; i < PathFirst.cells.Count; i++)
                {
                    for (int j = 0; j < PathSecond.cells.Count; j++)
                    {
                        // to avoid picking cells that are too far apart from each other
                        var cell1 = PathFirst.cells[i];
                        var cell2 = PathSecond.cells[j];
                        var manhatanDis = Math.Abs(cell1.row - cell2.row) + Math.Abs(cell1.col - cell2.col) - 1;
                        if (manhatanDis >= 4) continue;

                        ConsiderCandidate(cell1, cell2, BridgePath.Type.RowBased);
                        ConsiderCandidate(cell1, cell2, BridgePath.Type.ColBased);
                    }
                }
            }

            return selectedBridgePath;

            void ConsiderCandidate(Cell cell1, Cell cell2, BridgePath.Type type)
            {
                var gain = LotGain(cell1, cell2, mtx, type == BridgePath.Type.RowBased, out bool isPossible);
                if (!isPossible) return;

                // Distance of the bridge to the entrance: the smaller of its two endpoints'
                // distances, used as a cheap proxy for "how close does this connection sit
                // to the entrance" (see proximityWeight comment above for the trade-off vs.
                // true graph distance along the built path).
                double distanceToEntrance = entranceCell != null
                    ? Math.Min(ManhattanDistance(cell1, entranceCell), ManhattanDistance(cell2, entranceCell))
                    : 0;
                double score = gain - proximityWeight * distanceToEntrance;

                if (score > selectedBridgePath.Score)
                {
                    selectedBridgePath.Score = score;
                    selectedBridgePath.GainValue = gain;
                    selectedBridgePath.CellFirst = cell1;
                    selectedBridgePath.CellSecond = cell2;
                    selectedBridgePath.TypeValue = type;
                    selectedBridgePath.PathPossible = true;
                }
            }
        }

        public static void CreateConnectionPath(Parking Parking)
        {
            var mtx = Parking.PlanMatrix;
            var parkingPaths = Parking.ParkingPaths;
            var entranceCell = Parking.EntryCell;

            // Grow the connection network outward from the entrance (Prim's-style minimum
            // spanning tree), instead of connecting every possible pair of paths. This
            // guarantees every path joins the network through the best entrance-aware bridge
            // available at the time it's added, rather than ending up connected through some
            // arbitrary, possibly far-away, pairing.
            var connectedPathIndices = new HashSet<int>();
            var unconnectedPathIndices = new HashSet<int>(Enumerable.Range(0, parkingPaths.Count)
                .Where(i => parkingPaths[i].cells != null && parkingPaths[i].cells.Count > 0));

            if (unconnectedPathIndices.Count == 0) return;

            // Seed the network with whichever path already starts closest to the entrance.
            int seedIndex = unconnectedPathIndices
                .OrderBy(i => parkingPaths[i].cells.Min(c => entranceCell != null ? ManhattanDistance(c, entranceCell) : 0))
                .First();
            connectedPathIndices.Add(seedIndex);
            unconnectedPathIndices.Remove(seedIndex);

            while (unconnectedPathIndices.Count > 0)
            {
                BridgePath bestBridge = null;
                int bestUnconnectedIndex = -1;

                foreach (var ci in connectedPathIndices)
                {
                    foreach (var ui in unconnectedPathIndices)
                    {
                        var bridge = FindBestMatchPathCellsForConnection(mtx, parkingPaths[ci], parkingPaths[ui], entranceCell);
                        if (bridge.CellFirst == null || bridge.CellSecond == null) continue;

                        if (bestBridge == null || bridge.Score > bestBridge.Score)
                        {
                            bestBridge = bridge;
                            bestUnconnectedIndex = ui;
                        }
                    }
                }

                if (bestBridge == null)
                {
                    // Nothing left can be reached with a valid bridge (e.g. fully isolated
                    // paths); stop instead of looping forever.
                    break;
                }

                BuildBridgeCells(Parking, bestBridge);

                connectedPathIndices.Add(bestUnconnectedIndex);
                unconnectedPathIndices.Remove(bestUnconnectedIndex);
            }
        }

        // Builds the actual bridge cells (matrix updates, path points, car transforms) for one
        // accepted BridgePath. Extracted out of CreateConnectionPath so it can be called once per
        // accepted connection in the entrance-grown loop above, and so each call gets its own
        // local removingPaths list instead of one shared list leaking state across connections.
        private static void BuildBridgeCells(Parking Parking, BridgePath bridgePath)
        {
            var mtx = Parking.PlanMatrix;
            var gridPts = Parking.PlanPointsGrid;
            var parkingPaths = Parking.ParkingPaths;
            var carTransforms = Parking.CarTransforms;
            var mainPathPts = Parking.PathPoints;

            var cellRan1 = bridgePath.CellFirst;
            var cellRan2 = bridgePath.CellSecond;
            var n1 = cellRan1.row;
            var m1 = cellRan1.col;
            var n2 = cellRan2.row;
            var m2 = cellRan2.col;
            var signn = (n2 - n1 >= 0) ? 1 : -1;
            var signm = (m2 - m1 >= 0) ? 1 : -1;
            var allBridgePathCells = new List<Cell>();

            if (bridgePath.TypeValue == BridgePath.Type.RowBased)
            {
                if (n2 != n1)
                    for (int k = 1; k <= Math.Abs(n2 - n1); k++)
                    {
                        var row = n1 + k * signn;
                        var col = m1;
                        if (mtx[row, col] != 0) allBridgePathCells.Add(new Cell(row, col));
                    }
                if (m2 != m1)
                    for (int k = 1; k < Math.Abs(m2 - m1); k++)
                    {
                        var row = n2;
                        var col = m1 + k * signm;
                        if (mtx[row, col] != 0) allBridgePathCells.Add(new Cell(row, col));
                    }
            }
            else
            {
                if (m2 != m1)
                    for (int k = 1; k <= Math.Abs(m2 - m1); k++)
                    {
                        var row = n1;
                        var col = m1 + k * signm;
                        if (mtx[row, col] != 0) allBridgePathCells.Add(new Cell(row, col));
                    }
                if (n2 != n1)
                    for (int k = 1; k < Math.Abs(n2 - n1); k++)
                    {
                        var row = n1 + k * signn;
                        var col = m2;
                        if (mtx[row, col] != 0) allBridgePathCells.Add(new Cell(row, col));
                    }
            }

            var parkingPathNew = new ParkingPath();
            parkingPaths.Add(parkingPathNew);
            parkingPathNew.pathindex = parkingPaths.Count;

            var removingPaths = new List<GH_Path>();

            foreach (var cell in allBridgePathCells)
            {
                mtx[cell.row, cell.col] = 3;
                var pathindex = parkingPaths.Count;
                var pathNewCell = new GH_Path(pathindex, cell.row, cell.col);
                mainPathPts.Add(new Point3d(gridPts.Branch(cell.row)[cell.col]), pathNewCell);

                for (int k = -1; k < 2; k++)
                    for (int t = -1; t < 2; t++)
                    {
                        if (Math.Abs(k) + Math.Abs(t) != 1) continue;

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
                        Transform translation0 = new Transform(Transform.Translation(new Vector3d(vecbase + vplus)));
                        Transform translation1 = new Transform(Transform.Translation(new Vector3d(vecbase + hminus)));
                        Transform translation2 = new Transform(Transform.Translation(new Vector3d(vecbase + hplus)));
                        Transform translation3 = new Transform(Transform.Translation(new Vector3d(vecbase + vminus)));

                        var path0 = new GH_Path(pathindex, rowNew - 1, colNew);
                        var path1 = new GH_Path(pathindex, rowNew, colNew - 1);
                        var path2 = new GH_Path(pathindex, rowNew, colNew + 1);
                        var path3 = new GH_Path(pathindex, rowNew + 1, colNew);
                        var rowValue = CheckMatrix.GetValidIndex(rowNew + k, mtx.RowCount);
                        var colValue = CheckMatrix.GetValidIndex(colNew + t, mtx.ColumnCount);
                        var adjacentMtxValue = CheckMatrix.GetMatrixItem(mtx, rowValue, colValue);

                        if (adjacentMtxValue != 1) continue;

                        switch (k)
                        {
                            case -1: carTransforms.Add(new Transform(translation0 * rotation0), path0); break;
                            case 1: carTransforms.Add(new Transform(translation3 * rotation3), path3); break;
                        }
                        switch (t)
                        {
                            case -1: carTransforms.Add(new Transform(translation1), path1); break;
                            case 1: carTransforms.Add(new Transform(translation2 * rotation2), path2); break;
                        }
                        mtx[rowNew + k, colNew + t] = 2;
                    }
            }

            foreach (var cell in allBridgePathCells)
            {
                foreach (var path in carTransforms.Paths)
                {
                    if (path.Indices[1] == cell.row && path.Indices[2] == cell.col)
                    {
                        removingPaths.Add(path);
                        break;
                    }
                }
            }

            foreach (var path in removingPaths)
            {
                carTransforms.RemovePath(path);
            }
        }

        public static int LotGain(Cell p1, Cell p2, Matrix mtx, bool RowBasedPath, out bool ispathpossible)
        {
            // row based: first direction is in the direction of rows (vertically), then columns (horizontally).
            // if RowBasedPath = false: it is first horizontally then vertically.
            ispathpossible = true;
            int nbasedgain = 0;
            var n1 = p1.row;
            var m1 = p1.col;
            var n2 = p2.row;
            var m2 = p2.col;
            var signn = (n2 - n1 >= 0) ? 1 : -1;
            var signm = (m2 - m1 >= 0) ? 1 : -1;

            // Cells that belong to the bridge path itself, so its own neighbor-scan doesn't
            // count them as newly-gained or newly-lost lots. Fixed from the previous version,
            // which called Enumerable.Append on a fixed-size array without ever storing the
            // result, so this containment check silently never worked.
            var bridgeOwnCells = new HashSet<(int row, int col)>();

            if (RowBasedPath)
            {
                if (n2 != n1)
                {
                    for (int i = 1; i <= Math.Abs(n2 - n1); i++)
                    {
                        var step = i * signn;
                        bridgeOwnCells.Add((n1 + step, m1));
                        if (mtx[n1 + step, m1] == 4 || mtx[n1 + step, m1] == 0)
                            ispathpossible = false;
                    }
                }
                if (m2 != m1)
                {
                    for (int j = 1; j < Math.Abs(m2 - m1); j++)
                    {
                        var step = j * signm;
                        bridgeOwnCells.Add((n2, m1 + step));
                        if (mtx[n2, m1 + step] == 4 || mtx[n2, m1 + step] == 0)
                            ispathpossible = false;
                    }
                }

                if (ispathpossible)
                {
                    if (n1 != n2)
                    {
                        for (int i = 1; i <= Math.Abs(n2 - n1); i++)
                        {
                            var step = i * signn;
                            if (mtx[n1 + step, m1] == 2)
                                nbasedgain--;

                            for (int k = -1; k < 2; k++)
                                for (int t = -1; t < 2; t++)
                                {
                                    if (Math.Abs(k) + Math.Abs(t) != 1) continue;
                                    if (bridgeOwnCells.Contains((n1 + step + k, m1 + t))) continue;

                                    int value = GetMatrixValueSafe(mtx, n1 + step + k, m1 + t);
                                    if (value == 1) nbasedgain++;
                                    // here we check if the neighbor is the path itself, or not part of the
                                    // plan: we consider nothing; if it is a lot, we subtract from lotgain,
                                    // and if it is an empty cell, we add to lotgain.
                                }
                        }
                    }
                    if (m2 != m1)
                    {
                        for (int j = 1; j < Math.Abs(m2 - m1); j++)
                        {
                            var step = j * signm;
                            if (mtx[n2, m1 + step] == 2)
                                nbasedgain--;

                            for (int k = -1; k < 2; k++)
                                for (int t = -1; t < 2; t++)
                                {
                                    if (Math.Abs(k) + Math.Abs(t) != 1) continue;
                                    if (bridgeOwnCells.Contains((n1 + k, m1 + step + t))) continue;

                                    int value = GetMatrixValueSafe(mtx, n2 + k, m1 + step + t);
                                    if (value == 1) nbasedgain++;
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
                        var step = j * signm;
                        bridgeOwnCells.Add((n1, m1 + step));
                        if (mtx[n1, m1 + step] == 4 || mtx[n1, m1 + step] == 0)
                            ispathpossible = false;
                    }
                }
                if (n2 != n1)
                {
                    for (int i = 1; i < Math.Abs(n2 - n1); i++)
                    {
                        var step = i * signn;
                        bridgeOwnCells.Add((n1 + step, m2));
                        if (mtx[n1 + step, m1] == 4 || mtx[n1 + step, m1] == 0)
                            ispathpossible = false;
                    }
                }

                if (ispathpossible)
                {
                    if (m2 != m1)
                    {
                        for (int j = 1; j < Math.Abs(m2 - m1); j++)
                        {
                            var step = j * signm;
                            if (mtx[n1, m1 + step] == 2)
                                nbasedgain--;

                            for (int k = -1; k < 2; k++)
                                for (int t = -1; t < 2; t++)
                                {
                                    if (Math.Abs(k) + Math.Abs(t) != 1) continue;
                                    if (bridgeOwnCells.Contains((n1 + k, m1 + step + t))) continue;

                                    int value = GetMatrixValueSafe(mtx, n1 + k, m1 + step + t);
                                    if (value == 1) nbasedgain++;
                                }
                        }
                    }
                    if (n1 != n2)
                    {
                        for (int i = 1; i < Math.Abs(n2 - n1); i++)
                        {
                            var step = i * signn;
                            if (mtx[n1 + step, m2] == 2)
                                nbasedgain--;

                            for (int k = -1; k < 2; k++)
                                for (int t = -1; t < 2; t++)
                                {
                                    if (Math.Abs(k) + Math.Abs(t) != 1) continue;
                                    if (bridgeOwnCells.Contains((n1 + step + k, m2 + t))) continue;

                                    int value = GetMatrixValueSafe(mtx, n1 + step + k, m2 + t);
                                    if (value == 1) nbasedgain++;
                                }
                        }
                    }
                }
            }

            return ispathpossible ? nbasedgain : -100;
        }

        // Small helper to replace the try/catch-and-ignore pattern from the previous version:
        // returns -1 (a value that matches no case below) for any out-of-range access instead
        // of silently swallowing an IndexOutOfRangeException.
        private static int GetMatrixValueSafe(Matrix mtx, int row, int col)
        {
            if (row < 0 || row > mtx.RowCount - 1 || col < 0 || col > mtx.ColumnCount - 1) return -1;
            return (int)mtx[row, col];
        }
    }
}
