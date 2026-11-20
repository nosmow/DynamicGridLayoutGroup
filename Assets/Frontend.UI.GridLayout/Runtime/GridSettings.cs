using UnityEngine;
using UnityEngine.UI;

namespace Frontend.UI
{
    /// <summary>Snapshot of the grid settings and rect size used by one layout pass.</summary>
    internal readonly struct GridSettings
    {
        public readonly Vector2 CellSize;
        public readonly Vector2 Spacing;
        /// <summary>Total padding per axis (x = horizontal, y = vertical).</summary>
        public readonly Vector2 Padding;
        public readonly Vector2 RectSize;
        /// <summary>0 when the start axis is horizontal, 1 when vertical.</summary>
        public readonly int MainAxis;
        public readonly GridLayoutGroup.Constraint Constraint;
        public readonly int ConstraintCount;
        public readonly GridAdaptiveConstraint Adaptive;
        public readonly int MinCount;
        public readonly int MaxCount;
        public readonly GridCellSizeMode SizeMode;

        public GridSettings(DynamicGridLayoutGroup grid)
        {
            RectOffset padding = grid.padding;
            CellSize = grid.cellSize;
            Spacing = grid.spacing;
            Padding = new Vector2(padding.horizontal, padding.vertical);
            RectSize = ((RectTransform)grid.transform).rect.size;
            MainAxis = (int)grid.startAxis;
            Constraint = grid.constraint;
            ConstraintCount = grid.constraintCount;
            Adaptive = grid.adaptiveConstraint;
            MinCount = grid.minCount;
            MaxCount = grid.maxCount;
            SizeMode = grid.cellSizeMode;
        }

        public float GetAvailableSize(int axis) => RectSize[axis] - Padding[axis];

        /// <summary>Size of <paramref name="cells"/> authored cells plus spacing (layout input in Preserve/Stretch).</summary>
        public float GetExtent(int cells, int axis)
        {
            return cells <= 0 ? 0f : cells * CellSize[axis] + (cells - 1) * Spacing[axis];
        }

        /// <summary>True if the adaptive constraint controls the count along <paramref name="axis"/>.</summary>
        public bool IsAdaptiveOn(int axis)
        {
            return (axis == 0 && Adaptive == GridAdaptiveConstraint.Columns)
                   || (axis == 1 && Adaptive == GridAdaptiveConstraint.Rows);
        }
    }
}
