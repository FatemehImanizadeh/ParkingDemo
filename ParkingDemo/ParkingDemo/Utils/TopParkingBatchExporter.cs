using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

namespace ParkingDemo.Utils
{
    internal static class TopParkingBatchExporter
    {
        public static string Export(IReadOnlyList<Parking> selected, InstanceDefinition cars,
            RhinoDoc source, string folder, int imageWidth)
        {
            if (source == null) throw new InvalidOperationException("Open a Rhino document before exporting.");
            if (selected == null || selected.Count == 0) throw new InvalidOperationException("No valid scored parking options are available.");
            if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("Choose an existing export folder.");
            if (imageWidth < 800 || imageWidth > 4096) throw new ArgumentOutOfRangeException(nameof(imageWidth), "PNG width must be between 800 and 4096 pixels.");

            var scenes = new List<TopParkingScene>();
            var carParts = new List<TopParkingScene.Part>();
            double tolerance = source.ModelAbsoluteTolerance;
            string units = source.ModelUnitSystem.ToString();
            string name = "ParkingTop_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff", CultureInfo.InvariantCulture) +
                "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string completed = Path.Combine(folder, name);
            string staging = Path.Combine(folder, "." + name + ".partial");
            bool started = false;
            try
            {
                if (cars != null)
                {
                    TopParkingScene.ReadCarParts(cars, source, Transform.Identity, carParts, new HashSet<Guid>());
                    if (carParts.Count == 0) throw new InvalidOperationException("The selected car block has no supported geometry.");
                }
                // Capture all geometry and statistics before any files are written.
                for (int i = 0; i < selected.Count; i++) scenes.Add(TopParkingScene.Create(selected[i], i + 1, tolerance));
                var rasterCars = TopParkingImage.Tessellate(carParts, tolerance);
                Directory.CreateDirectory(staging);
                started = true;
                foreach (var scene in scenes)
                    TopParkingImage.Save(scene, rasterCars, Path.Combine(staging, scene.FileStem + ".png"), imageWidth, units, tolerance);
                WriteModel(scenes, carParts, source, Path.Combine(staging, "TopParkingOptions.3dm"));
                // Only completed packages receive the final name; previous exports are never overwritten.
                Directory.Move(staging, completed);
                return completed;
            }
            catch (Exception ex)
            {
                if (started)
                    throw new IOException("Export did not finish. Incomplete files remain in: " + staging + Environment.NewLine + ex.Message, ex);
                throw;
            }
            finally
            {
                foreach (var scene in scenes) scene.Dispose();
                foreach (var part in carParts) part.Dispose();
            }
        }

        private static void WriteModel(IReadOnlyList<TopParkingScene> scenes,
            IReadOnlyList<TopParkingScene.Part> carParts, RhinoDoc source, string path)
        {
            using (var model = new File3dm())
            {
                model.Settings.ModelUnitSystem = source.ModelUnitSystem;
                model.Settings.ModelAbsoluteTolerance = source.ModelAbsoluteTolerance;
                model.Settings.ModelAngleToleranceRadians = source.ModelAngleToleranceRadians;
                int templateLayer = model.AllLayers.AddLayer("Car block geometry", Color.Gray);
                int blockIndex = -1;
                if (carParts.Count > 0)
                {
                    var attributes = carParts.Select(part => Attributes(templateLayer, part.Color, null)).ToArray();
                    try
                    {
                        blockIndex = model.AllInstanceDefinitions.Add("Parking car block", "Copied from the selected car block",
                            Point3d.Origin, carParts.Select(part => part.Geometry), attributes);
                        if (blockIndex < 0) throw new InvalidOperationException("Unable to embed the car block in the Rhino file.");
                    }
                    finally { foreach (var attribute in attributes) attribute.Dispose(); }
                }

                var bounds = scenes.Select(scene => ContentBounds(scene, carParts)).ToArray();
                double maxWidth = bounds.Max(box => box.Max.X - box.Min.X);
                double maxHeight = bounds.Max(box => box.Max.Y - box.Min.Y);
                double textHeight = Math.Max(Math.Max(maxWidth, maxHeight) / 85.0, scenes.Max(s => s.CellSize) * 0.15);
                double tileWidth = Math.Max(maxWidth, textHeight * 60) + textHeight * 8;
                double tileHeight = maxHeight + textHeight * 34;
                int columns = (int)Math.Ceiling(Math.Sqrt(scenes.Count));
                var totalBounds = BoundingBox.Empty;

                for (int i = 0; i < scenes.Count; i++)
                {
                    var scene = scenes[i];
                    double x = (i % columns) * tileWidth;
                    double y = -(i / columns) * tileHeight;
                    var move = Transform.Translation(x - bounds[i].Min.X, y - bounds[i].Min.Y, -bounds[i].Min.Z);
                    string prefix = "Rank " + scene.Rank.ToString("D2", CultureInfo.InvariantCulture);
                    foreach (var section in scene.Parts.GroupBy(part => part.Section))
                    {
                        int layer = model.AllLayers.AddLayer(prefix + " - " + section.Key, section.First().Color);
                        if (layer < 0) throw new InvalidOperationException("Unable to create an export layer.");
                        if (section.Key == "Gradient cells" || section.Key == "Circulation")
                        {
                            using (var mesh = ParkingBakeMesh.Create(
                                section.Select(part => new GeometryColorPair(part.Geometry, part.Color)),
                                source.ModelAbsoluteTolerance))
                            using (var attributes = Attributes(layer, Color.White, scene))
                            {
                                if (!mesh.Transform(move)) throw new InvalidOperationException("Unable to arrange parking mesh.");
                                if (mesh.Faces.Count > 0) EnsureAdded(model.Objects.AddMesh(mesh, attributes));
                            }
                            continue;
                        }
                        foreach (var part in section)
                            using (var geometry = part.Geometry.Duplicate())
                            using (var attributes = Attributes(layer, part.Color, scene))
                            {
                                if (!geometry.Transform(move)) throw new InvalidOperationException("Unable to arrange parking geometry.");
                                AddGeometry(model, geometry, attributes);
                            }
                    }
                    if (blockIndex >= 0)
                    {
                        int layer = model.AllLayers.AddLayer(prefix + " - Cars", Color.Gray);
                        using (var attributes = Attributes(layer, Color.Gray, scene))
                            foreach (var transform in scene.Cars)
                                EnsureAdded(model.Objects.AddInstanceObject(blockIndex, move * transform, attributes));
                    }

                    int annotationLayer = model.AllLayers.AddLayer(prefix + " - Information", Color.FromArgb(30, 40, 50));
                    using (var attributes = Attributes(annotationLayer, Color.FromArgb(30, 40, 50), scene))
                    {
                        AddText(model, "Parking option " + scene.Rank, new Point3d(x, y + maxHeight + textHeight * 3, 0),
                            textHeight * 1.5, attributes);
                        string[] description = scene.Description(source.ModelUnitSystem.ToString());
                        for (int line = 0; line < description.Length; line++)
                            AddText(model, description[line], new Point3d(x, y - textHeight * (3 + line * 1.8), 0), textHeight, attributes);
                        AddText(model, "Gross: outline. Net: outline minus excluded cells.",
                            new Point3d(x, y - textHeight * 18, 0), textHeight * 0.8, attributes);
                        AddText(model, "Path distance = grade x cell size. Units: " + source.ModelUnitSystem,
                            new Point3d(x, y - textHeight * 19.6, 0), textHeight * 0.8, attributes);
                        if (blockIndex < 0)
                            AddText(model, "Car block not supplied; cars are omitted.",
                                new Point3d(x, y - textHeight * 21.2, 0), textHeight * 0.8, attributes);
                    }
                    AddLegend(model, annotationLayer, scene, x, y - textHeight * 24, textHeight);
                    totalBounds.Union(new Point3d(x - textHeight * 2, y - textHeight * 28, 0));
                    totalBounds.Union(new Point3d(x + tileWidth - textHeight * 2, y + maxHeight + textHeight * 6, 0));
                }

                using (var viewport = new RhinoViewport())
                {
                    viewport.Size = new Size(1600, 1000);
                    viewport.SetProjection(DefinedViewportProjection.Top, "Top parking options", false);
                    var shaded = DisplayModeDescription.GetDisplayMode(DisplayModeDescription.ShadedId);
                    if (shaded != null) viewport.DisplayMode = shaded;
                    viewport.ZoomBoundingBox(totalBounds);
                    using (var view = new ViewInfo(viewport)) model.Views.Add(view);
                }
                if (!model.Write(path, 7)) throw new IOException("Rhino could not write the 3dm file.");
            }
        }

        private static BoundingBox ContentBounds(TopParkingScene scene, IReadOnlyList<TopParkingScene.Part> cars)
        {
            var box = scene.Bounds;
            foreach (var transform in scene.Cars)
                foreach (var part in cars)
                {
                    var carBox = part.Geometry.GetBoundingBox(true);
                    carBox.Transform(transform);
                    box.Union(carBox);
                }
            return box;
        }

        private static ObjectAttributes Attributes(int layer, Color color, TopParkingScene scene)
        {
            var attributes = new ObjectAttributes { LayerIndex = layer, ObjectColor = color, ColorSource = ObjectColorSource.ColorFromObject };
            if (scene != null)
            {
                attributes.SetUserString("ParkingId", scene.ParkingId.ToString("D"));
                attributes.SetUserString("Rank", scene.Rank.ToString(CultureInfo.InvariantCulture));
                attributes.SetUserString("Score", scene.Score.ToString("R", CultureInfo.InvariantCulture));
            }
            return attributes;
        }

        private static void AddGeometry(File3dm model, GeometryBase geometry, ObjectAttributes attributes)
        {
            Guid id;
            if (geometry is Brep brep) id = model.Objects.AddBrep(brep, attributes);
            else if (geometry is Mesh mesh) id = model.Objects.AddMesh(mesh, attributes);
            else if (geometry is Curve curve) id = model.Objects.AddCurve(curve, attributes);
            else throw new InvalidOperationException("Unsupported parking geometry: " + geometry.GetType().Name);
            EnsureAdded(id);
        }

        private static void EnsureAdded(Guid id)
        { if (id == Guid.Empty) throw new InvalidOperationException("Rhino could not add an exported object."); }

        private static void AddText(File3dm model, string text, Point3d point, double height, ObjectAttributes attributes)
        {
            EnsureAdded(model.Objects.AddText(text, new Plane(point, Vector3d.ZAxis), height, "Arial", false, false, attributes));
        }

        private static void AddLegend(File3dm model, int layer, TopParkingScene scene, double x, double y, double size)
        {
            var colors = new[] { Color.FromArgb(60, 160, 160), Color.FromArgb(110, 220, 235), Color.FromArgb(80, 80, 80), Color.FromArgb(35, 35, 35) };
            var labels = new[] { "Circulation", "Entrance", "Excluded", "Walls" };
            for (int i = 0; i < colors.Length; i++)
            {
                double keyX = x + i * size * 14;
                using (var attributes = Attributes(layer, colors[i], scene))
                using (var rectangle = new Rectangle3d(new Plane(new Point3d(keyX, y, 0), Vector3d.ZAxis), size, size).ToNurbsCurve())
                {
                    EnsureAdded(model.Objects.AddCurve(rectangle, attributes));
                    AddText(model, labels[i], new Point3d(keyX + size * 1.5, y, 0), size * 0.8, attributes);
                }
            }
            using (var attributes = Attributes(layer, Color.FromArgb(40, 40, 40), scene))
                AddText(model, "Path cells: yellow (near entrance) > orange > red (farther away)",
                    new Point3d(x, y - size * 2.2, 0), size * 0.8, attributes);
        }
    }
}
