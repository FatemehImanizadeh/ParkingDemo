using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using ParkingDemo.Component.GUI;
using ParkingDemo.Component.Start;

namespace ParkingDemo
{
    internal sealed class StartGenerationFastAttributes : GH_ComponentAttributes
    {
        private readonly RectangleF[] _buttons = new RectangleF[14];
        private readonly double[] _sizes = { 5.0, 5.5, 6.5 };
        private readonly int[] _durations = { 10, 20, 60, 0 };
        private StartGenerationFast Component => (StartGenerationFast)Owner;
        public StartGenerationFastAttributes(StartGenerationFast owner) : base(owner) { }

        protected override void Layout()
        {
            base.Layout();
            var body = Bounds;
            float width = Math.Max(220, body.Width), x = body.X + (body.Width - width) / 2, y = body.Bottom + 6;
            float sizeWidth = (width - 12) / 3;
            for (int i = 0; i < 3; i++)
                _buttons[11 + i] = new RectangleF(x + 4 + i * (sizeWidth + 2), y + 17, sizeWidth, 23);
            y += 50;
            _buttons[0] = new RectangleF(x + 4, y, width - 8, 23);
            float cell = (width - 14) / 4;
            for (int i = 0; i < 4; i++)
            {
                _buttons[1 + i] = new RectangleF(x + 4 + i * (cell + 2), y + 43, cell, 23);
                _buttons[5 + i] = new RectangleF(x + 4 + i * (cell + 2), y + 86, cell, 23);
            }
            _buttons[9] = new RectangleF(x + 4, y + 116, (width - 12) / 2, 26);
            _buttons[10] = new RectangleF(_buttons[9].Right + 4, y + 116, (width - 12) / 2, 26);
            Bounds = RectangleF.Union(body, new RectangleF(x, body.Bottom, width, _buttons[10].Bottom + 8 - body.Bottom));
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);
            if (channel != GH_CanvasChannel.Objects) return;
            string[] labels = { Component.AddRamp ? "Ramp: On" : "Ramp: Off", "North", "West", "East", "South",
                "10 s", "20 s", "1 min", "Until Stop", "START", "STOP", "5 m", "5.5 m", "6.5 m" };
            graphics.DrawString("Entrance side", GH_FontServer.Standard, Brushes.DimGray, _buttons[1].X, _buttons[1].Y - 17);
            graphics.DrawString("Run duration", GH_FontServer.Standard, Brushes.DimGray, _buttons[5].X, _buttons[5].Y - 17);
            graphics.DrawString("Cell / aisle size", GH_FontServer.Standard, Brushes.DimGray, _buttons[11].X, _buttons[11].Y - 17);
            for (int i = 0; i < _buttons.Length; i++)
            {
                bool active = i == 0 ? Component.AddRamp : i < 5 ? (int)Component.EntranceSide == i - 1 :
                    i < 9 ? Component.DurationSeconds == _durations[i - 5] : i == 9 ? Component.Running :
                    i == 10 ? !Component.Running : Component.CellSizeMeters == _sizes[i - 11];
                using (var capsule = GH_Capsule.CreateTextCapsule(_buttons[i], _buttons[i],
                    active ? GH_Palette.Black : GH_Palette.Normal, labels[i]))
                    capsule.Render(graphics, Selected, Owner.Locked || (Component.Running && i != 9 && i != 10), false);
            }
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button != MouseButtons.Left || Owner.Locked) return base.RespondToMouseDown(sender, e);
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (!_buttons[i].Contains(e.CanvasLocation)) continue;
                if (i == 10) Component.StopFast();
                else if (i == 9) Component.StartFast();
                else if (!Component.Running)
                {
                    if (i == 0) Component.SetAddRamp(!Component.AddRamp);
                    else if (i < 5) Component.SetEntranceSide((ParkingEntranceSide)(i - 1));
                    else if (i < 9) Component.SetDuration(_durations[i - 5]);
                    else Component.SetCellSize(_sizes[i - 11]);
                }
                sender.Refresh();
                return GH_ObjectResponse.Handled;
            }
            return base.RespondToMouseDown(sender, e);
        }
    }
}
