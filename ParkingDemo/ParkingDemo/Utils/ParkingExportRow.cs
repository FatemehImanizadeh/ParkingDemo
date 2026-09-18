using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ParkingDemo.Utils
{
    /// <summary>
    /// Scalar snapshot of one result; exporting never modifies the generated layout.
    /// Converts existing grid-step grades to physical distances in Rhino model units.
    /// </summary>
    internal sealed class ParkingExportRow
    {
        public int Rank { get; }
        public Guid ParkingId { get; }
        public int ParkingCount { get; }
        public double? AveragePathLength { get; }
        public double? MaximumPathLength { get; }
        public double? AverageTurns { get; }
        public int? MaximumTurns { get; }
        public double? Score { get; }

        private ParkingExportRow(Parking parking, int rank)
        {
            Rank = rank;
            ParkingId = parking.ParkingID;
            ParkingCount = parking.LotNumber;
            Score = IsFinite(parking.Score) ? (double?)parking.Score : null;
            if (parking.LotNumber > 0)
            {
                if (IsFinite(parking.CellSize) && parking.CellSize > 0)
                {
                    AveragePathLength = (double)parking.TotalLengthGrade / parking.LotNumber * parking.CellSize;
                    MaximumPathLength = parking.MaxLengthGrade * parking.CellSize;
                }
                AverageTurns = (double)parking.TotalDirShift / parking.LotNumber;
                // Use the existing maximum-turn metric defined by the generator.
                MaximumTurns = parking.PathDirectionShift;
            }
        }

        public static IReadOnlyList<ParkingExportRow> Snapshot(IEnumerable<Parking> parkings)
        {
            if (parkings == null) throw new ArgumentNullException(nameof(parkings));

            // LINQ's stable ordering preserves collection order for equal scores.
            // Unscored results remain visible, after all finite scores.
            return parkings.Where(parking => parking != null)
                .OrderByDescending(parking => IsFinite(parking.Score))
                .ThenByDescending(parking => IsFinite(parking.Score) ? parking.Score : 0)
                .Select((parking, index) => new ParkingExportRow(parking, index + 1))
                .ToArray();
        }

        public static string ToCsv(IReadOnlyList<ParkingExportRow> rows)
        {
            var csv = new StringBuilder();
            csv.Append("Rank,ParkingId,ParkingCount,AveragePathLength_ModelUnits,")
                .Append("MaximumPathLength_ModelUnits,AverageTurns,MaximumTurns,ParkingScore\r\n");
            foreach (var row in rows)
            {
                // Every field is numeric or a GUID, so none needs CSV escaping.
                csv.Append(row.Rank.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(row.ParkingId.ToString("D")).Append(',')
                    .Append(row.ParkingCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(Number(row.AveragePathLength)).Append(',')
                    .Append(Number(row.MaximumPathLength)).Append(',')
                    .Append(Number(row.AverageTurns)).Append(',')
                    .Append(row.MaximumTurns?.ToString(CultureInfo.InvariantCulture) ?? "").Append(',')
                    .Append(Number(row.Score)).Append("\r\n");
            }
            return csv.ToString();
        }

        private static string Number(double? value) =>
            value.HasValue ? value.Value.ToString("G17", CultureInfo.InvariantCulture) : "";

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
