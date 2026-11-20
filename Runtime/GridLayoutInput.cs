using System.Collections.Generic;
using UnityEngine;
using Constraint = UnityEngine.UI.GridLayoutGroup.Constraint;

namespace Frontend.UI
{
    /// <summary>Min/preferred size that the grid reports as an <see cref="UnityEngine.UI.ILayoutElement"/>.</summary>
    internal static class GridLayoutInput
    {
        /// <summary>
        /// Calculates the layout input along <paramref name="axis"/>. Returns false when Unity's default
        /// <see cref="UnityEngine.UI.GridLayoutGroup"/> input is already correct.
        /// </summary>
        public static bool TryCalculate(GridLayoutSolver solver, in GridSettings settings, List<RectTransform> children,
            int axis, out float min, out float preferred)
        {
            if (settings.SizeMode == GridCellSizeMode.FitContent)
            {
                CalculateFitContent(solver, settings, children, axis, out min, out preferred);
                return true;
            }

            if (settings.IsAdaptiveOn(axis))
            {
                // The rect size decides the count, so report the allowed range instead of a single size.
                CalculateAdaptive(solver, settings, axis, out min, out preferred);
                return true;
            }

            // Without adaptive/spans Unity's input is already correct. A Flexible grid can't know its column
            // count before the width is set, so its horizontal input is left to Unity too.
            bool hasSpans = solver.Metrics.HasSpans;
            bool useDefault = settings.Adaptive == GridAdaptiveConstraint.None
                              && (!hasSpans || (axis == 0 && settings.Constraint == Constraint.Flexible));
            if (useDefault)
            {
                min = preferred = -1f;
                return false;
            }

            solver.Compute(settings, children);
            int cells = axis == 0 ? solver.ColumnCount : solver.RowCount;
            min = preferred = settings.Padding[axis] + settings.GetExtent(cells, axis);
            return true;
        }

        // min = minCount cells, preferred = maxCount cells (never more than there are items to show).
        private static void CalculateAdaptive(GridLayoutSolver solver, in GridSettings settings, int axis,
            out float min, out float preferred)
        {
            int available = Mathf.Max(1, solver.Metrics.TotalUnits);
            float padding = settings.Padding[axis];
            min = padding + settings.GetExtent(Mathf.Min(settings.MinCount, available), axis);
            preferred = padding + settings.GetExtent(Mathf.Min(settings.MaxCount, available), axis);
        }

        // Content size with the count chosen for the current rect; adaptive axes report the min..max count range.
        private static void CalculateFitContent(GridLayoutSolver solver, in GridSettings settings,
            List<RectTransform> children, int axis, out float min, out float preferred)
        {
            solver.Measure(settings, children);
            float padding = settings.Padding[axis];
            bool adaptiveOnAxis = settings.IsAdaptiveOn(axis);

            min = preferred = -1f;
            if (adaptiveOnAxis)
            {
                Constraint mode = axis == 0 ? Constraint.FixedColumnCount : Constraint.FixedRowCount;
                solver.Pack(settings, mode, settings.MinCount);
                min = padding + solver.GetBlockExtent(settings, axis);
                solver.Pack(settings, mode, GridCountResolver.ClampToUnits(solver.Metrics, settings.MaxCount, settings.MinCount));
                preferred = padding + solver.GetBlockExtent(settings, axis);
            }

            solver.ResolveAndPack(settings);

            if (!adaptiveOnAxis)
                min = preferred = padding + solver.GetBlockExtent(settings, axis);

            preferred = Mathf.Max(min, preferred);
        }
    }
}
