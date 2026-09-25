using System;
using System.Collections.Generic;
using System.Linq;

namespace Natrix.Utils
{
    public class Optimization
    {
        public double LotNumW { get; set; } = 0.50;
        public double PathLenW { get; set; } = 0.20;
        public double DirShiftW { get; set; } = 0.30;
        // Initial tuning weights, not standards; lower spread means more uniform access.
        public double PathStdDevW { get; set; } = 0.15;
        public double TurnsStdDevW { get; set; } = 0.15;
        public double NonFuncW { get; set; } // Legacy setting; unused by this score.

        public static void OptimizationFunction(Optimization weights, IEnumerable<Parking> parkings)
        {
            double weightSum = weights.LotNumW + weights.PathLenW + weights.DirShiftW + weights.PathStdDevW + weights.TurnsStdDevW;
            if (!Finite(weightSum) || weightSum <= 0 || weights.LotNumW < 0 || weights.PathLenW < 0 || weights.DirShiftW < 0 || weights.PathStdDevW < 0 || weights.TurnsStdDevW < 0)
                throw new ArgumentException("Score weights must be finite, nonnegative and have a positive sum.");

            var candidates = parkings.Where(p => p != null).ToList();
            foreach (var p in candidates) { p.Score = 0; p.HasValidScore = false; }
            // Floating-point averages; distance uses the parking's model-unit cell size.
            var rows = candidates.Where(p => p.IsGenerationValid && p.LotNumber > 0 &&
                    Finite(p.CellSize) && p.CellSize > 0 && p.TotalLengthGrade >= 0 && p.TotalDirShift >= 0)
                .Select(p => new { Parking = p, Length = (double)p.TotalLengthGrade / p.LotNumber * p.CellSize,
                    Turns = (double)p.TotalDirShift / p.LotNumber,
                    PathSpread = p.PathLengthStdDev ?? double.NaN, TurnSpread = p.TurnsStdDev ?? double.NaN })
                // Missing statistics are not zero spread. A zero weight disables that criterion.
                .Where(r => Finite(r.Length) && Finite(r.Turns) &&
                    (weights.PathStdDevW == 0 || Finite(r.PathSpread)) &&
                    (weights.TurnsStdDevW == 0 || Finite(r.TurnSpread))).ToList();
            if (rows.Count == 0) return;

            double minLots = rows.Min(r => r.Parking.LotNumber), maxLots = rows.Max(r => r.Parking.LotNumber);
            double minLength = rows.Min(r => r.Length), maxLength = rows.Max(r => r.Length);
            double minTurns = rows.Min(r => r.Turns), maxTurns = rows.Max(r => r.Turns);
            double minPathSpread = rows.Min(r => r.PathSpread), maxPathSpread = rows.Max(r => r.PathSpread);
            double minTurnSpread = rows.Min(r => r.TurnSpread), maxTurnSpread = rows.Max(r => r.TurnSpread);
            // Scores are collection-relative: rescore every option when the ranges change.
            // Reward capacity; reverse distance and turns so lower values score better.
            // Defaults make turns 1.5 times as influential as distance; maxima add no penalty.
            foreach (var r in rows)
            {
                // Relative weights produce a unit score; clamp floating-point roundoff to [0, 1].
                double score = (weights.LotNumW * Normalize(r.Parking.LotNumber, minLots, maxLots) +
                    weights.PathLenW * Normalize(r.Length, minLength, maxLength, true) +
                    weights.DirShiftW * Normalize(r.Turns, minTurns, maxTurns, true) +
                    (weights.PathStdDevW == 0 ? 0 : weights.PathStdDevW * Normalize(r.PathSpread, minPathSpread, maxPathSpread, true)) +
                    (weights.TurnsStdDevW == 0 ? 0 : weights.TurnsStdDevW * Normalize(r.TurnSpread, minTurnSpread, maxTurnSpread, true))) / weightSum;
                r.Parking.HasValidScore = Finite(score);
                r.Parking.Score = Finite(score) ? Math.Max(0, Math.Min(1, score)) : 0;
            }
        }

        private static double Normalize(double value, double min, double max, bool lowerIsBetter = false)
        {
            if (max == min) return 1; // Equal values contribute equally, including a singleton.
            return lowerIsBetter ? (max - value) / (max - min) : (value - min) / (max - min);
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
