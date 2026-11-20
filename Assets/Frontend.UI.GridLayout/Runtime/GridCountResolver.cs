using UnityEngine;
using Constraint = UnityEngine.UI.GridLayoutGroup.Constraint;

namespace Frontend.UI
{
    /// <summary>Turns the constraint settings into concrete column/row and slot counts for a layout pass.</summary>
    internal static class GridCountResolver
    {
        public static int GetSlotsPerLine(in GridSettings settings, GridChildMetrics metrics, Constraint mode, int count)
        {
            int mainAxis = settings.MainAxis;
            switch (mode)
            {
                case Constraint.FixedColumnCount:
                    return mainAxis == 0 ? count : GetSlotsForCrossConstraint(metrics, count);
                case Constraint.FixedRowCount:
                    return mainAxis == 1 ? count : GetSlotsForCrossConstraint(metrics, count);
                default:
                    return GetFittingCount(settings, mainAxis);
            }
        }

        /// <summary>Resolves the adaptive constraint (if any) into a regular fixed constraint, using cellSize.</summary>
        public static Constraint GetEffectiveConstraint(in GridSettings settings, out int count)
        {
            switch (settings.Adaptive)
            {
                case GridAdaptiveConstraint.Columns:
                    count = Mathf.Clamp(GetFittingCount(settings, 0), settings.MinCount, settings.MaxCount);
                    return Constraint.FixedColumnCount;
                case GridAdaptiveConstraint.Rows:
                    count = Mathf.Clamp(GetFittingCount(settings, 1), settings.MinCount, settings.MaxCount);
                    return Constraint.FixedRowCount;
                default:
                    count = settings.ConstraintCount;
                    return settings.Constraint;
            }
        }

        /// <summary>
        /// In FitContent mode the count can't be derived from cellSize, so it is searched for. Returns the range to search
        /// (false for a fixed constraint). A Flexible grid behaves like an adaptive one along the start axis.
        /// </summary>
        public static bool TryGetFitContentRange(in GridSettings settings, GridChildMetrics metrics,
            out Constraint mode, out int axis, out int low, out int high)
        {
            if (settings.Adaptive != GridAdaptiveConstraint.None)
            {
                axis = settings.Adaptive == GridAdaptiveConstraint.Columns ? 0 : 1;
                mode = axis == 0 ? Constraint.FixedColumnCount : Constraint.FixedRowCount;
                low = settings.MinCount;
                high = ClampToUnits(metrics, settings.MaxCount, settings.MinCount);
                return true;
            }

            if (settings.Constraint == Constraint.Flexible)
            {
                axis = settings.MainAxis;
                mode = axis == 0 ? Constraint.FixedColumnCount : Constraint.FixedRowCount;
                low = 1;
                high = Mathf.Max(1, metrics.TotalUnits);
                return true;
            }

            mode = settings.Constraint;
            axis = 0;
            low = high = settings.ConstraintCount;
            return false;
        }

        /// <summary>
        /// With <see cref="GridCellSizeMode.Stretch"/>, divides the available space of the constrained axis between the
        /// constrained count so the slots exactly fill the rect (the count was already chosen with the original cellSize).
        /// </summary>
        public static Vector2 ResolveCellSize(in GridSettings settings, Constraint mode, int count)
        {
            Vector2 size = settings.CellSize;
            if (settings.SizeMode != GridCellSizeMode.Stretch || mode == Constraint.Flexible || count <= 0)
                return size;

            int axis = mode == Constraint.FixedColumnCount ? 0 : 1;
            size[axis] = Mathf.Max(0f, (settings.GetAvailableSize(axis) - settings.Spacing[axis] * (count - 1)) / count);
            return size;
        }

        /// <summary>More columns/rows than there are units never changes the result.</summary>
        public static int ClampToUnits(GridChildMetrics metrics, int count, int minimum)
        {
            return Mathf.Max(minimum, Mathf.Min(count, Mathf.Max(1, metrics.TotalUnits)));
        }

        // The constraint limits the number of lines, so each line needs enough slots for all units.
        private static int GetSlotsForCrossConstraint(GridChildMetrics metrics, int lineLimit)
        {
            lineLimit = Mathf.Max(1, lineLimit);
            int perLine = (metrics.TotalUnits + lineLimit - 1) / lineLimit;
            return Mathf.Max(metrics.MaxSpan, Mathf.Max(1, perLine));
        }

        private static int GetFittingCount(in GridSettings settings, int axis)
        {
            float spacing = settings.Spacing[axis];
            float step = settings.CellSize[axis] + spacing;
            if (step <= 0f)
                return int.MaxValue;

            return Mathf.Max(1, Mathf.FloorToInt((settings.GetAvailableSize(axis) + spacing + 0.001f) / step));
        }
    }
}
