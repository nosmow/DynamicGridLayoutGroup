using System.Collections.Generic;
using UnityEngine;
using Constraint = UnityEngine.UI.GridLayoutGroup.Constraint;

namespace Frontend.UI
{
    /// <summary>
    /// Layout pass of <see cref="DynamicGridLayoutGroup"/>: measures the children, chooses the column/row count,
    /// packs the cells into lines and sizes the tracks. Holds the result of the last pass. Allocation free.
    /// </summary>
    internal sealed class GridLayoutSolver
    {
        public GridChildMetrics Metrics { get; } = new GridChildMetrics();
        public GridLines Lines { get; } = new GridLines();
        public GridTracks Tracks { get; } = new GridTracks();

        public int ColumnCount { get; private set; }
        public int RowCount { get; private set; }

        /// <summary>Cell size of the last pass in Preserve/Stretch (the authored one in FitContent).</summary>
        public Vector2 ResolvedCellSize { get; private set; }

        public void Compute(in GridSettings settings, List<RectTransform> children)
        {
            Measure(settings, children);
            ResolveAndPack(settings);
        }

        /// <summary>Refreshes spans (if stale) and, in FitContent mode, the children's preferred sizes.</summary>
        public void Measure(in GridSettings settings, List<RectTransform> children)
        {
            if (Metrics.Count != children.Count)
                Metrics.CacheSpans(children);

            if (settings.SizeMode == GridCellSizeMode.FitContent)
                Metrics.MeasurePreferredSizes(children, settings.CellSize);
            else
                Metrics.ClearPreferredSizes();
        }

        /// <summary>Chooses the column/row count for this pass and lays the cells out with it.</summary>
        public void ResolveAndPack(in GridSettings settings)
        {
            Constraint mode = GridCountResolver.GetEffectiveConstraint(settings, out int count);

            if (settings.SizeMode == GridCellSizeMode.FitContent
                && GridCountResolver.TryGetFitContentRange(settings, Metrics, out mode, out int axis, out int low, out int high))
            {
                count = FindLargestFittingCount(settings, mode, axis, low, high);
            }

            Pack(settings, mode, count);
        }

        /// <summary>Packs the children into lines along the start axis and sizes every track. O(n).</summary>
        public void Pack(in GridSettings settings, Constraint mode, int count)
        {
            int slotsPerLine = Metrics.Count > 0 ? GridCountResolver.GetSlotsPerLine(settings, Metrics, mode, count) : 0;
            Lines.Pack(Metrics, slotsPerLine);

            bool horizontal = settings.MainAxis == 0;
            ColumnCount = horizontal ? Lines.BlockUnits : Lines.LineCount;
            RowCount = horizontal ? Lines.LineCount : Lines.BlockUnits;

            if (settings.SizeMode == GridCellSizeMode.FitContent)
            {
                ResolvedCellSize = settings.CellSize;
                Tracks.SetFromContent(Lines, Metrics, settings.MainAxis, settings.Spacing);
            }
            else
            {
                ResolvedCellSize = GridCountResolver.ResolveCellSize(settings, mode, count);
                Tracks.SetUniform(Lines, ResolvedCellSize, settings.MainAxis, settings.Spacing);
            }
        }

        /// <summary>Total size of the grid block (without padding) along an axis, from the last pack.</summary>
        public float GetBlockExtent(in GridSettings settings, int axis)
        {
            return axis == settings.MainAxis ? Tracks.MainExtent : Tracks.CrossExtent;
        }

        /// <summary>Column (axis 0) or row (axis 1) size; columns are slots when the start axis is horizontal.</summary>
        public float GetTrackSize(int index, int axis, int mainAxis)
        {
            return axis == mainAxis ? Tracks.GetSlotSizeOrZero(index) : Tracks.GetLineSizeOrZero(index);
        }

        /// <summary>Logical (column, row) of a laid-out child, before mirroring and alignment.</summary>
        public bool TryGetCellCoordinates(int index, int mainAxis, out Vector2Int coordinates)
        {
            if (index < 0 || index >= Lines.CellCount)
            {
                coordinates = new Vector2Int(-1, -1);
                return false;
            }

            GridCell cell = Lines.GetCell(index);
            coordinates = mainAxis == 0 ? new Vector2Int(cell.Slot, cell.Line) : new Vector2Int(cell.Line, cell.Slot);
            return true;
        }

        /// <summary>Largest count in [low, high] whose content fits the rect (low if none does). O(n log range).</summary>
        private int FindLargestFittingCount(in GridSettings settings, Constraint mode, int axis, int low, int high)
        {
            float available = settings.GetAvailableSize(axis) + 0.001f;
            int best = low;
            int min = low + 1;
            int max = high;

            while (min <= max)
            {
                int candidate = (min + max) / 2;
                Pack(settings, mode, candidate);

                if (GetBlockExtent(settings, axis) <= available)
                {
                    best = candidate;
                    min = candidate + 1;
                }
                else
                {
                    max = candidate - 1;
                }
            }

            return best;
        }
    }
}
