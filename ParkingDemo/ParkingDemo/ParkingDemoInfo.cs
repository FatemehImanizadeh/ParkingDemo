using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace ParkingDemo
{
    public class ParkingDemoInfo : GH_AssemblyInfo
    {
        public override string Name => "ParkingDemo";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("d363a739-4da8-461e-a85d-3cf70295601c");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}