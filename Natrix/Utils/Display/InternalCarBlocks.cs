using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

namespace Natrix.Utils
{
    internal static class InternalCarBlocks
    {
        private static readonly Lazy<Dictionary<int, Curve[]>> Templates = new Lazy<Dictionary<int, Curve[]>>(Load);
        public static int CountForCell(double size, UnitSystem units) =>
            size * RhinoMath.UnitScale(units, UnitSystem.Meters) >= 6.5 - 1e-9 ? 3 : 2;

        private static Dictionary<int, Curve[]> Load()
        {
            var result = new Dictionary<int, Curve[]>();
            using (var stream = typeof(InternalCarBlocks).Assembly.GetManifestResourceStream("Natrix.Cars.3dm"))
            using (var bytes = new MemoryStream())
            {
                if (stream == null) throw new InvalidOperationException("Embedded car blocks are missing.");
                stream.CopyTo(bytes);
                using (var model = File3dm.FromByteArray(bytes.ToArray()))
                {
                    if (model == null) throw new InvalidOperationException("Cannot read embedded car blocks.");
                    var objects = model.Objects.ToDictionary(o => o.Attributes.ObjectId);
                    foreach (int count in new[] { 2, 3 })
                    {
                        var definition = model.AllInstanceDefinitions.First(d => d.Name == "cars_" + count);
                        var curves = definition.GetObjectIds().Select(id =>
                            (objects[id].Geometry as Curve)?.DuplicateCurve() ??
                            throw new InvalidOperationException("Expected curves in cars_" + count)).ToArray();
                        // Supplied cars_2 faces along Y; cars_3 along X. Match the existing local-X car transforms.
                        var rotate = count == 2 ? Transform.Rotation(-Math.PI / 2, Point3d.Origin) : Transform.Identity;
                        var bounds = BoundingBox.Empty;
                        foreach (var curve in curves) { curve.Transform(rotate); bounds.Union(curve.GetBoundingBox(true)); }
                        var center = Transform.Translation(-bounds.Center.X, -bounds.Center.Y, 0);
                        foreach (var curve in curves) curve.Transform(center);
                        result.Add(count, curves);
                    }
                }
            }
            return result; // Immutable process-lifetime templates; callers transform owned copies or display matrices.
        }

        public static List<TopParkingScene.Part> CreateParts(int count, UnitSystem units)
        {
            var scale = Transform.Scale(Point3d.Origin, RhinoMath.UnitScale(UnitSystem.Meters, units));
            return Templates.Value[count].Select(curve =>
            {
                var copy = curve.DuplicateCurve();
                copy.Transform(scale); // Unit conversion only; never stretch cars to fill a cell.
                return new TopParkingScene.Part("Cars", copy, Color.Black);
            }).ToList();
        }

        public static InstanceDefinition Definition(Parking parking, RhinoDoc doc)
        {
            string name = "Natrix_Cars_" + parking.CarsPerCell + "_v1_" + doc.ModelUnitSystem;
            var existing = doc.InstanceDefinitions.Find(name, true);
            if (existing != null) return existing;
            var parts = CreateParts(parking.CarsPerCell, doc.ModelUnitSystem);
            var attributes = parts.Select(p => new ObjectAttributes
                { ObjectColor = p.Color, ColorSource = ObjectColorSource.ColorFromObject }).ToArray();
            try
            {
                int index = doc.InstanceDefinitions.Add(name, "Embedded parking cars", Point3d.Origin,
                    parts.Select(p => p.Geometry), attributes);
                if (index < 0) throw new InvalidOperationException("Cannot create the internal car block.");
                return doc.InstanceDefinitions[index];
            }
            finally
            {
                foreach (var part in parts) part.Dispose();
                foreach (var attribute in attributes) attribute.Dispose();
            }
        }

        public static void Draw(IGH_PreviewArgs args, Parking parking)
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null || parking?.CarTransforms == null) return;
            var scale = Transform.Scale(Point3d.Origin, RhinoMath.UnitScale(UnitSystem.Meters, doc.ModelUnitSystem));
            foreach (var transform in parking.CarTransforms.Branches.SelectMany(b => b))
            {
                args.Display.PushModelTransform(transform * scale);
                try { foreach (var curve in Templates.Value[parking.CarsPerCell]) args.Display.DrawCurve(curve, Color.Black); }
                finally { args.Display.PopModelTransform(); }
            }
        }
    }
}
