using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ParkingDemo.Utils
{
    /// <summary>Owns export geometry and scalar metrics; never edits the source Parking or active document.</summary>
    internal sealed class TopParkingScene : IDisposable
    {
        internal sealed class Part : IDisposable
        {
            public string Section { get; }
            public GeometryBase Geometry { get; }
            public Color Color { get; }
            public Part(string section, GeometryBase geometry, Color color)
            { Section = section; Geometry = geometry; Color = color; }
            public void Dispose() => Geometry.Dispose();
        }

        public int Rank { get; private set; }
        public Guid ParkingId { get; private set; }
        public double Score { get; private set; }
        public int ParkingCount { get; private set; }
        public double GrossArea { get; private set; }
        public double NetArea { get; private set; }
        public double CellSize { get; private set; }
        public double? AverageDistance { get; private set; }
        public double? MaximumDistance { get; private set; }
        public double? AverageTurns { get; private set; }
        public int MaximumTurns { get; private set; }
        public List<Part> Parts { get; } = new List<Part>();
        public List<Transform> Cars { get; } = new List<Transform>();
        public BoundingBox Bounds { get; private set; }
        public string FileStem => "Rank_" + Rank.ToString("D2", CultureInfo.InvariantCulture) + "_" + ParkingId.ToString("N").Substring(0, 8);

        public static List<Parking> SelectBest(IEnumerable<Parking> parkings, int count)
        {
            if (parkings == null) throw new ArgumentNullException(nameof(parkings));
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Top count must be at least 1.");
            return parkings.Where(p => p != null && p.IsGenerationValid &&
                    !double.IsNaN(p.Score) && !double.IsInfinity(p.Score))
                .OrderByDescending(p => p.Score).Take(count).ToList();
        }

        public static TopParkingScene Create(Parking parking, int rank, double tolerance)
        {
            if (parking.Outline == null || !parking.Outline.IsClosed || !parking.Outline.IsPlanar(tolerance))
                throw new InvalidOperationException("Rank " + rank + " must have a closed planar outline.");
            var scene = new TopParkingScene
            {
                Rank = rank, ParkingId = parking.ParkingID, Score = parking.Score,
                ParkingCount = parking.LotNumber, MaximumTurns = parking.PathDirectionShift
            };
            try
            {
                if (double.IsNaN(parking.CellSize) || double.IsInfinity(parking.CellSize) || parking.CellSize <= 0)
                    throw new InvalidOperationException("Rank " + rank + " has no valid grid cells.");
                scene.CellSize = parking.CellSize;
                if (parking.LotNumber > 0)
                {
                    scene.AverageDistance = (double)parking.TotalLengthGrade / parking.LotNumber * scene.CellSize;
                    scene.MaximumDistance = parking.MaxLengthGrade * scene.CellSize;
                    scene.AverageTurns = (double)parking.TotalDirShift / parking.LotNumber;
                }
                scene.GrossArea = Area(parking.Outline);
                scene.NetArea = NetOutlineArea(parking, tolerance, scene.GrossArea);

                // Call the same individual builders as Preview Parking Result, without replacing PreviewGeometry.
                scene.Add("Gradient cells", ParkingPreviewGeometryBuilder.BuildGradientCells(parking, tolerance));
                scene.Add("Excluded cells", ParkingPreviewGeometryBuilder.BuildExcludedCells(parking, tolerance));
                scene.Add("Circulation", ParkingPreviewGeometryBuilder.BuildContinuousPath(parking, 0.30, tolerance));
                var entrance = ParkingPreviewGeometryBuilder.BuildEntranceCell(parking, tolerance);
                if (entrance?.Geometry != null)
                    scene.Parts.Add(new Part("Entrance", entrance.Geometry, entrance.Color));
                scene.Add("Walls", ParkingPreviewGeometryBuilder.BuildParkingWall(parking, 0.20, tolerance));
                scene.Parts.Add(new Part("Outline", parking.Outline.DuplicateCurve(), Color.FromArgb(35, 35, 35)));
                if (parking.CarTransforms != null)
                    scene.Cars.AddRange(parking.CarTransforms.Branches.Where(b => b != null).SelectMany(b => b));
                var box = parking.Outline.GetBoundingBox(true);
                foreach (var part in scene.Parts) box.Union(part.Geometry.GetBoundingBox(true));
                scene.Bounds = box;
                return scene;
            }
            catch { scene.Dispose(); throw; }
        }

        private void Add(string section, IEnumerable<GeometryColorPair> pairs)
        {
            foreach (var pair in pairs)
                if (pair?.Geometry != null) Parts.Add(new Part(section, pair.Geometry, pair.Color));
        }

        private static double Area(Curve curve)
        {
            using (var area = AreaMassProperties.Compute(curve))
            {
                if (area == null) throw new InvalidOperationException("Unable to calculate outline area.");
                return Math.Abs(area.Area);
            }
        }

        private static double NetOutlineArea(Parking parking, double tolerance, double gross)
        {
            if (parking.ExcludeCells == null || parking.ExcludeCells.Count == 0) return gross;
            var excluded = parking.ExcludeCells.Select(cell => cell.ToNurbsCurve()).Cast<Curve>().ToArray();
            Curve[] boundaries = null;
            Brep[] regions = null;
            try
            {
                // Boolean difference clips exclusions to the outline and avoids double-subtracting overlaps.
                boundaries = Curve.CreateBooleanDifference(parking.Outline, excluded, tolerance);
                if (boundaries == null) throw new InvalidOperationException("Unable to subtract excluded cells from the outline.");
                if (boundaries.Length == 0) return 0;
                regions = Brep.CreatePlanarBreps(boundaries, tolerance);
                if (regions == null || regions.Length == 0)
                    throw new InvalidOperationException("Unable to calculate net parking area.");
                double area = 0;
                foreach (var region in regions)
                    using (var properties = AreaMassProperties.Compute(region))
                    {
                        if (properties == null) throw new InvalidOperationException("Unable to calculate net parking area.");
                        area += Math.Abs(properties.Area);
                    }
                return Math.Min(gross, area);
            }
            finally
            {
                foreach (var curve in excluded) curve.Dispose();
                if (boundaries != null) foreach (var curve in boundaries) curve.Dispose();
                if (regions != null) foreach (var region in regions) region.Dispose();
            }
        }

        public string[] Description(string units) => new[]
        {
            "Rank " + Rank + " | Score: " + Number(Score),
            "Parking count: " + ParkingCount,
            "Gross area: " + Number(GrossArea) + " " + units + " squared",
            "Net area: " + Number(NetArea) + " " + units + " squared",
            "Average path: " + Number(AverageDistance) + " " + units,
            "Maximum path: " + Number(MaximumDistance) + " " + units,
            "Average turns: " + Number(AverageTurns) + " | Maximum turns: " + MaximumTurns,
            "ID: " + ParkingId.ToString("D")
        };

        private static string Number(double? value) => value?.ToString("0.###", CultureInfo.InvariantCulture) ?? "N/A";

        public void Dispose() { foreach (var part in Parts) part.Dispose(); Parts.Clear(); }

        /// <summary>Flattens nested block definitions into owned template geometry; placements remain block instances in 3dm.</summary>
        public static void ReadCarParts(InstanceDefinition definition, RhinoDoc source,
            Transform transform, List<Part> parts, HashSet<Guid> ancestors)
        {
            if (!ancestors.Add(definition.Id)) throw new InvalidOperationException("The car block contains a cyclic block reference.");
            try
            {
                foreach (var obj in definition.GetObjects())
                {
                    if (obj is InstanceObject nested)
                    {
                        ReadCarParts(nested.InstanceDefinition, source, transform * nested.InstanceXform, parts, ancestors);
                        continue;
                    }
                    GeometryBase geometry = obj.Geometry?.Duplicate();
                    if (geometry == null) continue;
                    if (geometry is Extrusion extrusion)
                    {
                        geometry = extrusion.ToBrep(true);
                        extrusion.Dispose();
                    }
                    else if (geometry is Surface surface)
                    {
                        geometry = surface.ToBrep();
                        surface.Dispose();
                    }
                    if (!(geometry is Brep) && !(geometry is Mesh) && !(geometry is Curve))
                    {
                        geometry?.Dispose();
                        throw new InvalidOperationException("Car blocks must contain curves, meshes, surfaces or Breps.");
                    }
                    if (!geometry.Transform(transform))
                    { geometry.Dispose(); throw new InvalidOperationException("Unable to transform car-block geometry."); }
                    parts.Add(new Part("Cars", geometry, obj.Attributes.DrawColor(source)));
                }
            }
            finally { ancestors.Remove(definition.Id); }
        }
    }
}
