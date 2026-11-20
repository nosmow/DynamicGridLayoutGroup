using UnityEngine;
using Corner = UnityEngine.UI.GridLayoutGroup.Corner;

namespace Frontend.UI
{
    /// <summary>Per-pass values needed to place the cells.</summary>
    internal readonly struct GridPlacementContext
    {
        public readonly int MainAxis;
        public readonly bool ReverseMain;
        public readonly bool ReverseCross;
        /// <summary>Start offset (padding + child alignment) of the grid block along each axis.</summary>
        public readonly float MainStart;
        public readonly float CrossStart;
        /// <summary>0..1 position of partial lines inside the block.</summary>
        public readonly float LineAlignment;

        public GridPlacementContext(int mainAxis, bool reverseMain, bool reverseCross,
            float mainStart, float crossStart, float lineAlignment)
        {
            MainAxis = mainAxis;
            ReverseMain = reverseMain;
            ReverseCross = reverseCross;
            MainStart = mainStart;
            CrossStart = crossStart;
            LineAlignment = lineAlignment;
        }
    }

    /// <summary>Final position of each cell: start corner mirroring and partial-line alignment.</summary>
    internal static class GridCellPlacement
    {
        /// <summary>Which axes run backwards for <paramref name="corner"/> (e.g. UpperRight reverses x).</summary>
        public static void GetReversedAxes(Corner corner, int mainAxis, out bool reverseMain, out bool reverseCross)
        {
            int cornerX = (int)corner % 2;
            int cornerY = (int)corner / 2;
            reverseMain = (mainAxis == 0 ? cornerX : cornerY) == 1;
            reverseCross = (mainAxis == 0 ? cornerY : cornerX) == 1;
        }

        public static float GetLineAlignmentFactor(GridLineAlignment alignment, TextAnchor childAlignment,
            int mainAxis, bool reverseMain)
        {
            switch (alignment)
            {
                case GridLineAlignment.Start:
                    return 0f;
                case GridLineAlignment.Center:
                    return 0.5f;
                case GridLineAlignment.End:
                    return 1f;
                case GridLineAlignment.FollowChildAlignment:
                    int anchor = (int)childAlignment;
                    return (mainAxis == 0 ? anchor % 3 : anchor / 3) * 0.5f;
                default:
                    return reverseMain ? 1f : 0f;
            }
        }

        /// <summary>Position and size of the cell at <paramref name="index"/> along both axes, in rect space.</summary>
        public static void Place(int index, GridLines lines, GridTracks tracks, in GridPlacementContext context,
            out Vector2 position, out Vector2 size)
        {
            GridCell cell = lines.GetCell(index);
            int lineUnits = lines.GetLineUnits(cell.Line);
            float lineExtent = lineUnits > 0 ? tracks.GetSpanExtent(0, lineUnits) : 0f;

            float cellStart = tracks.GetSlotOffset(cell.Slot);
            float cellExtent = tracks.GetSpanExtent(cell.Slot, cell.Span);

            // Mirror inside the line's own extent so partial lines stay contiguous when the corner is reversed.
            float mainPosition = context.ReverseMain ? lineExtent - cellStart - cellExtent : cellStart;
            float lineOffset = (tracks.MainExtent - lineExtent) * context.LineAlignment;

            float lineSize = tracks.GetLineSize(cell.Line);
            float lineStart = tracks.GetLineOffset(cell.Line);
            float crossPosition = context.ReverseCross ? tracks.CrossExtent - lineStart - lineSize : lineStart;

            int mainAxis = context.MainAxis;
            position = default;
            size = default;
            position[mainAxis] = context.MainStart + lineOffset + mainPosition;
            position[1 - mainAxis] = context.CrossStart + crossPosition;
            size[mainAxis] = cellExtent;
            size[1 - mainAxis] = lineSize;
        }
    }
}
