using System;
using System.Drawing;
using System.Drawing.Drawing2D;

using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using ParkingDemo.Component.GUI;

namespace ParkingDemo.Component.Start
{
    public class StartGenerationAttributes :
        GH_ComponentAttributes
    {
        private RectangleF _rampBounds;

        private RectangleF _northBounds;
        private RectangleF _westBounds;
        private RectangleF _eastBounds;
        private RectangleF _southBounds;

        private RectangleF _interval500Bounds;
        private RectangleF _interval750Bounds;
        private RectangleF _interval1000Bounds;
        private RectangleF _interval1500Bounds;
        private RectangleF _interval2000Bounds;
        private RectangleF _interval3000Bounds;

        private RectangleF _startBounds;
        private RectangleF _stopBounds;

        private const float PanelWidth = 196f;
        private const float UiHeight = 188f;

        private StartGenerationAdv OwnerComponent =>
            (StartGenerationAdv)Owner;

        public StartGenerationAttributes(
            StartGenerationAdv owner)
            : base(owner)
        {
        }

        protected override void Layout()
        {
            base.Layout();

            /*
             * Start from the regular Grasshopper component layout,
             * then expand the component body downward.
             */

            RectangleF originalBounds =
                Bounds;

            float width =
                Math.Max(
                    PanelWidth,
                    originalBounds.Width);

            float left =
                originalBounds.X;

            float top =
                originalBounds.Y;

            float defaultHeight =
                originalBounds.Height;

            Bounds = new RectangleF(
                left,
                top,
                width,
                defaultHeight + UiHeight);

            float margin = 8f;
            float contentLeft =
                Bounds.Left + margin;

            float contentWidth =
                Bounds.Width - margin * 2f;

            float uiTop =
                originalBounds.Bottom + 7f;

            _rampBounds = new RectangleF(
                contentLeft,
                uiTop,
                contentWidth,
                25f);

            float sideTop =
                _rampBounds.Bottom + 22f;

            float sideGap = 4f;

            float sideWidth =
                (contentWidth - sideGap) / 2f;

            _northBounds = new RectangleF(
                contentLeft,
                sideTop,
                sideWidth,
                23f);

            _westBounds = new RectangleF(
                _northBounds.Right + sideGap,
                sideTop,
                sideWidth,
                23f);

            _eastBounds = new RectangleF(
                contentLeft,
                _northBounds.Bottom + sideGap,
                sideWidth,
                23f);

            _southBounds = new RectangleF(
                _eastBounds.Right + sideGap,
                _westBounds.Bottom + sideGap,
                sideWidth,
                23f);

            float intervalTop =
                _eastBounds.Bottom + 23f;

            float intervalGap = 3f;

            float intervalWidth =
                (contentWidth - intervalGap * 2f) / 3f;

            _interval500Bounds = new RectangleF(
                contentLeft,
                intervalTop,
                intervalWidth,
                22f);

            _interval750Bounds = new RectangleF(
                _interval500Bounds.Right + intervalGap,
                intervalTop,
                intervalWidth,
                22f);

            _interval1000Bounds = new RectangleF(
                _interval750Bounds.Right + intervalGap,
                intervalTop,
                intervalWidth,
                22f);

            _interval1500Bounds = new RectangleF(
                contentLeft,
                _interval500Bounds.Bottom + intervalGap,
                intervalWidth,
                22f);

            _interval2000Bounds = new RectangleF(
                _interval1500Bounds.Right + intervalGap,
                _interval750Bounds.Bottom + intervalGap,
                intervalWidth,
                22f);

            _interval3000Bounds = new RectangleF(
                _interval2000Bounds.Right + intervalGap,
                _interval1000Bounds.Bottom + intervalGap,
                intervalWidth,
                22f);

            float runTop =
                _interval1500Bounds.Bottom + 10f;

            float runGap = 5f;

            float runWidth =
                (contentWidth - runGap) / 2f;

            _startBounds = new RectangleF(
                contentLeft,
                runTop,
                runWidth,
                27f);

            _stopBounds = new RectangleF(
                _startBounds.Right + runGap,
                runTop,
                runWidth,
                27f);
        }

        protected override void Render(
            GH_Canvas canvas,
            Graphics graphics,
            GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel != GH_CanvasChannel.Objects)
                return;

            graphics.SmoothingMode =
                SmoothingMode.AntiAlias;

            DrawSeparator(graphics);

            DrawSectionLabel(
                graphics,
                "RAMP",
                _rampBounds.Top - 12f);

            DrawToggle(
                graphics,
                _rampBounds,
                "Add ramp",
                OwnerComponent.AddRamp);

            DrawSectionLabel(
                graphics,
                "ENTRANCE SIDE",
                _northBounds.Top - 13f);

            DrawOptionButton(
                graphics,
                _northBounds,
                "North",
                OwnerComponent.EntranceSide ==
                ParkingEntranceSide.North);

            DrawOptionButton(
                graphics,
                _westBounds,
                "West",
                OwnerComponent.EntranceSide ==
                ParkingEntranceSide.West);

            DrawOptionButton(
                graphics,
                _eastBounds,
                "East",
                OwnerComponent.EntranceSide ==
                ParkingEntranceSide.East);

            DrawOptionButton(
                graphics,
                _southBounds,
                "South",
                OwnerComponent.EntranceSide ==
                ParkingEntranceSide.South);

            DrawSectionLabel(
                graphics,
                "GENERATION INTERVAL",
                _interval500Bounds.Top - 13f);

            DrawOptionButton(
                graphics,
                _interval500Bounds,
                "500 ms",
                OwnerComponent.Interval ==
                GenerationInterval.Ms500);

            DrawOptionButton(
                graphics,
                _interval750Bounds,
                "750 ms",
                OwnerComponent.Interval ==
                GenerationInterval.Ms750);

            DrawOptionButton(
                graphics,
                _interval1000Bounds,
                "1 s",
                OwnerComponent.Interval ==
                GenerationInterval.Sec1);

            DrawOptionButton(
                graphics,
                _interval1500Bounds,
                "1.5 s",
                OwnerComponent.Interval ==
                GenerationInterval.Sec15);

            DrawOptionButton(
                graphics,
                _interval2000Bounds,
                "2 s",
                OwnerComponent.Interval ==
                GenerationInterval.Sec2);

            DrawOptionButton(
                graphics,
                _interval3000Bounds,
                "3 s",
                OwnerComponent.Interval ==
                GenerationInterval.Sec3);

            DrawActionButton(
                graphics,
                _startBounds,
                "START",
                OwnerComponent.IsAutoRunning,
                true);

            DrawActionButton(
                graphics,
                _stopBounds,
                "STOP",
                !OwnerComponent.IsAutoRunning,
                false);
        }

        private void DrawSeparator(
            Graphics graphics)
        {
            float y =
                _rampBounds.Top - 17f;

            using var pen =
                new Pen(
                    Color.FromArgb(
                        70,
                        80,
                        80,
                        80),
                    1f);

            graphics.DrawLine(
                pen,
                Bounds.Left + 6f,
                y,
                Bounds.Right - 6f,
                y);
        }

        private void DrawSectionLabel(
            Graphics graphics,
            string text,
            float y)
        {
            using var font = new Font(
                GH_FontServer.Standard.FontFamily,
                6.5f,
                FontStyle.Bold);

            using var brush = new SolidBrush(
                Color.FromArgb(
                    150,
                    45,
                    45,
                    45));

            graphics.DrawString(
                text,
                font,
                brush,
                Bounds.Left + 9f,
                y);
        }

        private static GraphicsPath CreateRoundedRectangle(
            RectangleF rectangle,
            float radius)
        {
            float diameter =
                radius * 2f;

            var path =
                new GraphicsPath();

            path.AddArc(
                rectangle.Left,
                rectangle.Top,
                diameter,
                diameter,
                180,
                90);

            path.AddArc(
                rectangle.Right - diameter,
                rectangle.Top,
                diameter,
                diameter,
                270,
                90);

            path.AddArc(
                rectangle.Right - diameter,
                rectangle.Bottom - diameter,
                diameter,
                diameter,
                0,
                90);

            path.AddArc(
                rectangle.Left,
                rectangle.Bottom - diameter,
                diameter,
                diameter,
                90,
                90);

            path.CloseFigure();

            return path;
        }

        private void DrawToggle(
            Graphics graphics,
            RectangleF bounds,
            string text,
            bool isOn)
        {
            using var path =
                CreateRoundedRectangle(
                    bounds,
                    6f);

            Color background =
                Color.FromArgb(
                    235,
                    242,
                    242,
                    242);

            using var backgroundBrush =
                new SolidBrush(background);

            graphics.FillPath(
                backgroundBrush,
                path);

            using var borderPen =
                new Pen(
                    Color.FromArgb(
                        90,
                        80,
                        80,
                        80),
                    1f);

            graphics.DrawPath(
                borderPen,
                path);

            using var textFont =
                new Font(
                    GH_FontServer.Standard.FontFamily,
                    8f,
                    FontStyle.Regular);

            using var textBrush =
                new SolidBrush(
                    Color.FromArgb(
                        220,
                        40,
                        40,
                        40));

            graphics.DrawString(
                text,
                textFont,
                textBrush,
                bounds.Left + 7f,
                bounds.Top + 5f);

            RectangleF switchBounds =
                new RectangleF(
                    bounds.Right - 47f,
                    bounds.Top + 4f,
                    39f,
                    17f);

            using var switchPath =
                CreateRoundedRectangle(
                    switchBounds,
                    8f);

            Color switchColor =
                isOn
                    ? Color.FromArgb(
                        105,
                        168,
                        75)
                    : Color.FromArgb(
                        165,
                        165,
                        165);

            using var switchBrush =
                new SolidBrush(switchColor);

            graphics.FillPath(
                switchBrush,
                switchPath);

            float knobX =
                isOn
                    ? switchBounds.Right - 15f
                    : switchBounds.Left + 2f;

            var knobBounds =
                new RectangleF(
                    knobX,
                    switchBounds.Top + 2f,
                    13f,
                    13f);

            using var knobBrush =
                new SolidBrush(Color.White);

            graphics.FillEllipse(
                knobBrush,
                knobBounds);
        }

        private void DrawOptionButton(
            Graphics graphics,
            RectangleF bounds,
            string text,
            bool selected)
        {
            using var path =
                CreateRoundedRectangle(
                    bounds,
                    5f);

            Color fillColor =
                selected
                    ? Color.FromArgb(
                        105,
                        168,
                        75)
                    : Color.FromArgb(
                        238,
                        238,
                        238);

            Color borderColor =
                selected
                    ? Color.FromArgb(
                        70,
                        125,
                        45)
                    : Color.FromArgb(
                        115,
                        115,
                        115);

            Color textColor =
                selected
                    ? Color.White
                    : Color.FromArgb(
                        55,
                        55,
                        55);

            using var fillBrush =
                new SolidBrush(fillColor);

            using var borderPen =
                new Pen(
                    borderColor,
                    1f);

            graphics.FillPath(
                fillBrush,
                path);

            graphics.DrawPath(
                borderPen,
                path);

            using var font =
                new Font(
                    GH_FontServer.Standard.FontFamily,
                    7.2f,
                    selected
                        ? FontStyle.Bold
                        : FontStyle.Regular);

            using var textBrush =
                new SolidBrush(textColor);

            using var format =
                new StringFormat
                {
                    Alignment =
                        StringAlignment.Center,

                    LineAlignment =
                        StringAlignment.Center
                };

            graphics.DrawString(
                text,
                font,
                textBrush,
                bounds,
                format);
        }

        private void DrawActionButton(
            Graphics graphics,
            RectangleF bounds,
            string text,
            bool active,
            bool isStart)
        {
            using var path =
                CreateRoundedRectangle(
                    bounds,
                    6f);

            Color activeColor =
                isStart
                    ? Color.FromArgb(
                        65,
                        145,
                        75)
                    : Color.FromArgb(
                        175,
                        75,
                        70);

            Color inactiveColor =
                Color.FromArgb(
                    218,
                    218,
                    218);

            Color fillColor =
                active
                    ? activeColor
                    : inactiveColor;

            using var fillBrush =
                new SolidBrush(fillColor);

            graphics.FillPath(
                fillBrush,
                path);

            using var borderPen =
                new Pen(
                    Color.FromArgb(
                        110,
                        80,
                        80,
                        80),
                    1f);

            graphics.DrawPath(
                borderPen,
                path);

            using var font =
                new Font(
                    GH_FontServer.Standard.FontFamily,
                    8f,
                    FontStyle.Bold);

            Color textColor =
                active
                    ? Color.White
                    : Color.FromArgb(
                        75,
                        75,
                        75);

            using var textBrush =
                new SolidBrush(textColor);

            using var format =
                new StringFormat
                {
                    Alignment =
                        StringAlignment.Center,

                    LineAlignment =
                        StringAlignment.Center
                };

            string symbol =
                isStart
                    ? "▶  "
                    : "■  ";

            graphics.DrawString(
                symbol + text,
                font,
                textBrush,
                bounds,
                format);
        }

        public override GH_ObjectResponse RespondToMouseDown(
            GH_Canvas sender,
            GH_CanvasMouseEvent e)
        {
            if (e.Button !=
                System.Windows.Forms.MouseButtons.Left)
            {
                return base.RespondToMouseDown(
                    sender,
                    e);
            }

            PointF location =
                e.CanvasLocation;

            if (_rampBounds.Contains(location))
            {
                OwnerComponent.SetAddRamp(
                    !OwnerComponent.AddRamp);

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            if (_northBounds.Contains(location))
            {
                OwnerComponent.SetEntranceSide(
                    ParkingEntranceSide.North);

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            if (_westBounds.Contains(location))
            {
                OwnerComponent.SetEntranceSide(
                    ParkingEntranceSide.West);

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            if (_eastBounds.Contains(location))
            {
                OwnerComponent.SetEntranceSide(
                    ParkingEntranceSide.East);

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            if (_southBounds.Contains(location))
            {
                OwnerComponent.SetEntranceSide(
                    ParkingEntranceSide.South);

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            if (_interval500Bounds.Contains(location))
            {
                OwnerComponent.SetInterval(
                    GenerationInterval.Ms500);

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            if (_interval750Bounds.Contains(location))
            {
                OwnerComponent.SetInterval(
                    GenerationInterval.Ms750);

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            if (_interval1000Bounds.Contains(location))
            {
                OwnerComponent.SetInterval(
                    GenerationInterval.Sec1);

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            if (_interval1500Bounds.Contains(location))
            {
                OwnerComponent.SetInterval(
                    GenerationInterval.Sec15);

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            if (_interval2000Bounds.Contains(location))
            {
                OwnerComponent.SetInterval(
                    GenerationInterval.Sec2);

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            if (_interval3000Bounds.Contains(location))
            {
                OwnerComponent.SetInterval(
                    GenerationInterval.Sec3);

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            if (_startBounds.Contains(location))
            {
                OwnerComponent.StartGeneration();

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            if (_stopBounds.Contains(location))
            {
                OwnerComponent.StopGeneration();

                sender.Refresh();

                return GH_ObjectResponse.Handled;
            }

            return base.RespondToMouseDown(
                sender,
                e);
        }
    }
}