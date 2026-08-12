using System;
using System.Collections.Generic;
using System.Drawing;

using Rhino.DocObjects;
using Rhino.Geometry;


namespace ParkingDemo.Utils
{
    // ========================================================================
    // DISPLAY SECTION
    // ========================================================================

    /// <summary>
    /// Every visual / bakeable part of one generated parking result.
    /// </summary>
    public enum ParkingDisplaySection
    {
        GradientCells,
        MainPath,
        ExcludedCells,
        EntranceCell,
        Walls
    }


    // ========================================================================
    // DISPLAY ITEM
    // ========================================================================

    /// <summary>
    /// One piece of geometry belonging to one parking result section.
    ///
    /// IMPORTANT:
    /// Geometry is the actual result geometry.
    /// It is NOT a special preview-only copy.
    ///
    /// Preview reads this geometry.
    /// Bake later writes this same geometry into Rhino.
    /// </summary>
    public sealed class ParkingDisplayItem
    {
        public ParkingDisplaySection Section
        {
            get;
            private set;
        }


        public GeometryBase Geometry
        {
            get;
            private set;
        }


        public Color FillColor
        {
            get;
            private set;
        }


        public Color WireColor
        {
            get;
            private set;
        }


        public int WireThickness
        {
            get;
            private set;
        }


        public bool DrawFill
        {
            get;
            private set;
        }


        public bool DrawWire
        {
            get;
            private set;
        }


        public ParkingDisplayItem(
            ParkingDisplaySection section,
            GeometryBase geometry,
            Color fillColor,
            Color wireColor,
            int wireThickness,
            bool drawFill,
            bool drawWire)
        {
            Section =
                section;


            Geometry =
                geometry;


            FillColor =
                fillColor;


            WireColor =
                wireColor;


            WireThickness =
                Math.Max(
                    1,
                    wireThickness);


            DrawFill =
                drawFill;


            DrawWire =
                drawWire;
        }
    }


    // ========================================================================
    // PARKING DISPLAY DATA
    // ========================================================================

    /// <summary>
    /// Complete visual representation of one generated parking.
    ///
    /// This object sits BETWEEN:
    ///
    /// Parking generation
    ///
    /// and
    ///
    /// Preview / Bake / Export.
    ///
    /// Preview and Bake therefore consume identical geometry.
    /// </summary>
    public sealed class ParkingDisplayData
    {
        // ====================================================================
        // 1. GRADED CELLS
        // ====================================================================

        public List<ParkingDisplayItem> GradientCells
        {
            get;
            private set;
        }


        // ====================================================================
        // 2. MAIN PATH
        // ====================================================================

        public List<ParkingDisplayItem> MainPath
        {
            get;
            private set;
        }


        // ====================================================================
        // 3. EXCLUDED CELLS
        // ====================================================================

        public List<ParkingDisplayItem> ExcludedCells
        {
            get;
            private set;
        }


        // ====================================================================
        // 4. ENTRANCE
        // ====================================================================

        public List<ParkingDisplayItem> EntranceCell
        {
            get;
            private set;
        }


        // ====================================================================
        // 5. WALLS
        // ====================================================================

        public List<ParkingDisplayItem> Walls
        {
            get;
            private set;
        }


        // ====================================================================
        // 6. CARS
        //
        // Cars are treated differently because we want to preserve them as
        // block instances rather than converting every car to copied geometry.
        // ====================================================================

        public InstanceDefinition CarDefinition
        {
            get;
            set;
        }


        public List<Transform> CarTransforms
        {
            get;
            private set;
        }


        // ====================================================================
        // CLIPPING BOX
        // ====================================================================

        private BoundingBox _clippingBox =
            BoundingBox.Empty;


        public BoundingBox ClippingBox
        {
            get
            {
                return
                    _clippingBox;
            }
        }


        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public ParkingDisplayData()
        {
            GradientCells =
                new List<ParkingDisplayItem>();


            MainPath =
                new List<ParkingDisplayItem>();


            ExcludedCells =
                new List<ParkingDisplayItem>();


            EntranceCell =
                new List<ParkingDisplayItem>();


            Walls =
                new List<ParkingDisplayItem>();


            CarTransforms =
                new List<Transform>();
        }


        // ====================================================================
        // ALL GEOMETRY
        // ====================================================================

        /// <summary>
        /// Iterates through all non-car parking geometry.
        /// </summary>
        public IEnumerable<ParkingDisplayItem> AllGeometry
        {
            get
            {
                foreach (ParkingDisplayItem item
                    in GradientCells)
                {
                    yield return
                        item;
                }


                foreach (ParkingDisplayItem item
                    in MainPath)
                {
                    yield return
                        item;
                }


                foreach (ParkingDisplayItem item
                    in ExcludedCells)
                {
                    yield return
                        item;
                }


                foreach (ParkingDisplayItem item
                    in EntranceCell)
                {
                    yield return
                        item;
                }


                foreach (ParkingDisplayItem item
                    in Walls)
                {
                    yield return
                        item;
                }
            }
        }


        // ====================================================================
        // ADD GEOMETRY
        // ====================================================================

        public void Add(
            ParkingDisplaySection section,
            GeometryBase geometry,
            Color fillColor,
            Color wireColor,
            int wireThickness = 1,
            bool drawFill = true,
            bool drawWire = true)
        {
            if (geometry == null)
                return;


            ParkingDisplayItem item =new ParkingDisplayItem(
                    section,
                    geometry,
                    fillColor,
                    wireColor,
                    wireThickness,
                    drawFill,
                    drawWire);


            GetSection(section).Add(item);
        }


        // ====================================================================
        // ADD WIRE-ONLY GEOMETRY
        // ====================================================================

        public void AddWire(
            ParkingDisplaySection section,
            GeometryBase geometry,
            Color color,
            int thickness = 2)
        {
            Add(
                section,
                geometry,
                Color.Empty,
                color,
                thickness,
                false,
                true);
        }


        // ====================================================================
        // ADD FILLED GEOMETRY
        // ====================================================================

        public void AddFilled(
            ParkingDisplaySection section,
            GeometryBase geometry,
            Color fillColor,
            Color wireColor,
            int wireThickness = 1)
        {
            Add(
                section,
                geometry,
                fillColor,
                wireColor,
                wireThickness,
                true,
                true);
        }


        // ====================================================================
        // SECTION ACCESS
        // ====================================================================

        private List<ParkingDisplayItem> GetSection(
            ParkingDisplaySection section)
        {
            switch (section)
            {
                case ParkingDisplaySection.GradientCells:

                    return
                        GradientCells;


                case ParkingDisplaySection.MainPath:

                    return
                        MainPath;


                case ParkingDisplaySection.ExcludedCells:

                    return
                        ExcludedCells;


                case ParkingDisplaySection.EntranceCell:

                    return
                        EntranceCell;


                case ParkingDisplaySection.Walls:

                    return
                        Walls;


                default:

                    throw new ArgumentOutOfRangeException(
                        nameof(section));
            }
        }


        // ====================================================================
        // REBUILD CLIPPING BOX
        // ====================================================================

        /// <summary>
        /// Must be called after ParkingDisplayBuilder has finished adding
        /// geometry and car transforms.
        /// </summary>
        public void RebuildClippingBox()
        {
            bool hasBox =
                false;


            BoundingBox combined =
                BoundingBox.Empty;


            // ---------------------------------------------------------------
            // Normal geometry
            // ---------------------------------------------------------------

            foreach (ParkingDisplayItem item
                in AllGeometry)
            {
                if (item == null ||
                    item.Geometry == null)
                {
                    continue;
                }


                BoundingBox box =
                    item.Geometry.GetBoundingBox(
                        Transform.Identity);


                if (!box.IsValid)
                    continue;


                if (!hasBox)
                {
                    combined =
                        box;


                    hasBox =
                        true;
                }
                else
                {
                    combined.Union(
                        box);
                }
            }


            // ---------------------------------------------------------------
            // Car block instances
            // ---------------------------------------------------------------

            if (CarDefinition != null &&
                CarTransforms != null &&
                CarTransforms.Count > 0)
            {
                RhinoObject[] definitionObjects =
                    CarDefinition.GetObjects();


                if (definitionObjects != null)
                {
                    foreach (Transform transform
                        in CarTransforms)
                    {
                        foreach (RhinoObject obj
                            in definitionObjects)
                        {
                            if (obj == null ||
                                obj.Geometry == null)
                            {
                                continue;
                            }


                            BoundingBox box =
                                obj.Geometry.GetBoundingBox(
                                    transform);


                            if (!box.IsValid)
                                continue;


                            if (!hasBox)
                            {
                                combined =
                                    box;


                                hasBox =
                                    true;
                            }
                            else
                            {
                                combined.Union(
                                    box);
                            }
                        }
                    }
                }
            }


            _clippingBox =
                hasBox
                    ? combined
                    : BoundingBox.Empty;
        }


        // ====================================================================
        // CLEAR
        // ====================================================================

        public void Clear()
        {
            GradientCells.Clear();

            MainPath.Clear();

            ExcludedCells.Clear();

            EntranceCell.Clear();

            Walls.Clear();

            CarTransforms.Clear();

            CarDefinition =
                null;

            _clippingBox =
                BoundingBox.Empty;
        }
    }
}