using System;
using System.Diagnostics;
using Grasshopper.Kernel;
using GH_IO.Serialization;
using ParkingDemo.Component.GUI;

namespace ParkingDemo.Component.Start
{
    // Reuse the existing generator unchanged; only the run controller is different.
    public sealed class StartGenerationFast : StartGenerationAdv
    {
        private readonly Stopwatch _clock = new Stopwatch();
        private GH_Document _document;
        private bool _running, _produced;
        private int _ticket, _completed;
        public int DurationSeconds { get; private set; } = 10; // Zero means until Stop.
        public bool Running => _running;

        public StartGenerationFast()
        {
            Name = "Start Generation Fast";
            NickName = "FastStart";
            Description = "Generates at the fastest scheduled solution rate for a duration or until Stop. Connect to SortResults.";
        }

        public override Guid ComponentGuid => new Guid("71338C6C-8DA5-47F8-A8C1-74A447B94FD0");
        public override void CreateAttributes() => m_attributes = new StartGenerationFastAttributes(this);

        public void SetDuration(int seconds)
        {
            if (_running || (seconds != 0 && seconds != 10 && seconds != 20 && seconds != 60)) return;
            RecordUndoEvent("Change run duration");
            DurationSeconds = seconds;
            OnDisplayExpired(true);
        }

        public void StartFast()
        {
            var document = OnPingDocument();
            if (_running || Locked || document == null || !document.Enabled || !GH_Document.EnableSolutions) return;
            _document = document;
            _document.SolutionEnd += SolutionEnded;
            _completed = 0;
            _produced = false;
            _running = true;
            _ticket++;
            _clock.Restart();
            // The inherited interval runner stays off. Each solve builds a fresh Parking.
            ExpireSolution(true);
        }

        public void StopFast()
        {
            _running = false;
            _ticket++; // Pending callbacks from this run must not restart it.
            _clock.Stop();
            if (_document != null) _document.SolutionEnd -= SolutionEnded;
            _document = null;
            Message = $"Stopped · {_completed} solves · {_clock.Elapsed.TotalSeconds:F1}s";
            OnDisplayExpired(true);
        }

        private bool TimeExpired => DurationSeconds > 0 && _clock.Elapsed.TotalSeconds >= DurationSeconds;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!_running) { Message = "Press Start"; return; }
            if (DA.Iteration > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Supply one outline and one cell size per component.");
                StopFast();
                return;
            }
            if (TimeExpired) { StopFast(); return; }
            _produced = false;
            try
            {
                base.SolveInstance(DA);
                if (RuntimeMessages(GH_RuntimeMessageLevel.Error).Count > 0 || Params.Output[0].VolatileDataCount == 0)
                { StopFast(); return; }
                _produced = true;
                Message = $"Running · {_completed} solves";
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                StopFast();
            }
        }

        private void SolutionEnded(object sender, GH_SolutionEventArgs e)
        {
            if (!_running) return;
            if (_produced) { _completed++; _produced = false; }
            // Missing required inputs may prevent SolveInstance from being called at all.
            if (RuntimeMessages(GH_RuntimeMessageLevel.Error).Count > 0 ||
                Params.Input[0].VolatileDataCount == 0 || Params.Input[2].VolatileDataCount == 0)
            { StopFast(); return; }
            if (TimeExpired || Locked || !_document.Enabled || !GH_Document.EnableSolutions)
            { StopFast(); return; }
            Message = $"Running · {_completed} solves · {_clock.Elapsed.TotalSeconds:F1}s";
            int ticket = ++_ticket;
            // 1 ms is GH's shortest asynchronous schedule. Zero recurses and can overflow the stack.
            // Yield between complete document solutions so UI events, including Stop, can be processed.
            _document.ScheduleSolution(1, document =>
            {
                if (!_running || ticket != _ticket || document != OnPingDocument()) return;
                if (TimeExpired || Locked || !document.Enabled || !GH_Document.EnableSolutions)
                { StopFast(); return; }
                ExpireSolution(false);
            });
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            StopFast();
            base.RemovedFromDocument(document);
        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            if (context == GH_DocumentContext.Close || context == GH_DocumentContext.Unloaded || context == GH_DocumentContext.Lock)
                StopFast();
            base.DocumentContextChanged(document, context);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("FastDurationSeconds", DurationSeconds);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            StopFast(); // Opening a file never starts generation automatically.
            bool result = base.Read(reader);
            int seconds = reader.ItemExists("FastDurationSeconds") ? reader.GetInt32("FastDurationSeconds") : 10;
            DurationSeconds = seconds == 0 || seconds == 20 || seconds == 60 ? seconds : 10;
            return result;
        }
    }
}
