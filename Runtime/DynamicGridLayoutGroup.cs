using UnityEngine;
using UnityEngine.UI;

namespace Frontend.UI
{
    /// <summary>
    /// Drop-in replacement for <see cref="GridLayoutGroup"/> that adds cell spans (<see cref="GridLayoutCell"/>),
    /// partial-line alignment, an adaptive column/row count with min/max, and Preserve/Stretch/FitContent sizing.
    /// </summary>
    /// <remarks>
    /// This component only holds the settings and talks to uGUI. The layout itself is computed by
    /// <see cref="GridLayoutSolver"/>, the reported size by <see cref="GridLayoutInput"/> and the final positions
    /// by <see cref="GridCellPlacement"/>. No per-frame allocations, one component lookup per child per rebuild.
    /// </remarks>
    [AddComponentMenu("Layout/Dynamic Grid Layout Group", 153)]
    public class DynamicGridLayoutGroup : GridLayoutGroup
    {
        private const DrivenTransformProperties DrivenProperties =
            DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta;

        [SerializeField]
        [Tooltip("Alignment of lines that are not completely filled (typically the last row).")]
        private GridLineAlignment m_PartialLineAlignment = GridLineAlignment.FollowChildAlignment;

        [SerializeField]
        [Tooltip("When set, overrides Constraint: the column/row count is whatever fits in the rect, clamped to [Min, Max].")]
        private GridAdaptiveConstraint m_AdaptiveConstraint = GridAdaptiveConstraint.None;

        [SerializeField, Min(1)]
        private int m_MinCount = 1;

        [SerializeField, Min(1)]
        private int m_MaxCount = 4;

        [SerializeField]
        [Tooltip("Preserve keeps Cell Size. Stretch fills the constrained axis. FitContent sizes each column/row to its largest cell.")]
        private GridCellSizeMode m_CellSizeMode = GridCellSizeMode.Preserve;

        private readonly GridLayoutSolver m_Solver = new GridLayoutSolver();

        protected DynamicGridLayoutGroup() { }

        /// <summary>Alignment of lines that are not completely filled (typically the last row).</summary>
        public GridLineAlignment partialLineAlignment
        {
            get => m_PartialLineAlignment;
            set => SetProperty(ref m_PartialLineAlignment, value);
        }

        /// <summary>Which count (columns or rows) adapts to the rect size. <see cref="GridAdaptiveConstraint.None"/> disables it.</summary>
        public GridAdaptiveConstraint adaptiveConstraint
        {
            get => m_AdaptiveConstraint;
            set => SetProperty(ref m_AdaptiveConstraint, value);
        }

        /// <summary>Minimum columns/rows when <see cref="adaptiveConstraint"/> is enabled (at least 1).</summary>
        public int minCount
        {
            get => m_MinCount;
            set
            {
                value = Mathf.Max(1, value);
                if (m_MaxCount < value)
                    m_MaxCount = value;
                SetProperty(ref m_MinCount, value);
            }
        }

        /// <summary>Maximum columns/rows when <see cref="adaptiveConstraint"/> is enabled (at least <see cref="minCount"/>).</summary>
        public int maxCount
        {
            get => m_MaxCount;
            set => SetProperty(ref m_MaxCount, Mathf.Max(m_MinCount, value));
        }

        /// <summary>Whether cells keep <see cref="GridLayoutGroup.cellSize"/>, stretch, or fit their content.</summary>
        public GridCellSizeMode cellSizeMode
        {
            get => m_CellSizeMode;
            set => SetProperty(ref m_CellSizeMode, value);
        }

        /// <summary>Cell size of the last pass in Preserve/Stretch. In FitContent use <see cref="GetColumnWidth"/>/<see cref="GetRowHeight"/>.</summary>
        public Vector2 resolvedCellSize => m_Solver.ResolvedCellSize;

        /// <summary>Number of columns produced by the last layout pass.</summary>
        public int columnCount => m_Solver.ColumnCount;

        /// <summary>Number of rows produced by the last layout pass.</summary>
        public int rowCount => m_Solver.RowCount;

        /// <summary>Width of <paramref name="column"/> in the last layout pass (0 if out of range).</summary>
        public float GetColumnWidth(int column) => m_Solver.GetTrackSize(column, 0, (int)startAxis);

        /// <summary>Height of <paramref name="row"/> in the last layout pass (0 if out of range).</summary>
        public float GetRowHeight(int row) => m_Solver.GetTrackSize(row, 1, (int)startAxis);

        /// <summary>
        /// Logical (column, row) of the child at <paramref name="layoutIndex"/> (index in the list of laid-out children)
        /// from the last layout pass, before start-corner mirroring and partial-line alignment.
        /// </summary>
        public bool TryGetCellCoordinates(int layoutIndex, out Vector2Int coordinates)
        {
            return m_Solver.TryGetCellCoordinates(layoutIndex, (int)startAxis, out coordinates);
        }

        public override void CalculateLayoutInputHorizontal()
        {
            // Rebuilds rectChildren and sets Unity's default horizontal input, which is kept if still valid.
            base.CalculateLayoutInputHorizontal();
            m_Solver.Metrics.CacheSpans(rectChildren);

            var settings = new GridSettings(this);
            if (GridLayoutInput.TryCalculate(m_Solver, settings, rectChildren, 0, out float min, out float preferred))
                SetLayoutInputForAxis(min, preferred, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            var settings = new GridSettings(this);
            if (GridLayoutInput.TryCalculate(m_Solver, settings, rectChildren, 1, out float min, out float preferred))
                SetLayoutInputForAxis(min, preferred, -1, 1);
            else
                base.CalculateLayoutInputVertical();
        }

        public override void SetLayoutHorizontal()
        {
            // Like Unity's grid: size the children first so their own vertical input (e.g. wrapped text) sees the final width.
            var settings = new GridSettings(this);
            m_Solver.Compute(settings, rectChildren);

            int mainAxis = settings.MainAxis;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                m_Tracker.Add(this, child, DrivenProperties);

                GridCell cell = m_Solver.Lines.GetCell(i);
                Vector2 size = default;
                size[mainAxis] = m_Solver.Tracks.GetSpanExtent(cell.Slot, cell.Span);
                size[1 - mainAxis] = m_Solver.Tracks.GetLineSize(cell.Line);

                child.anchorMin = Vector2.up;
                child.anchorMax = Vector2.up;
                child.sizeDelta = size;
            }
        }

        public override void SetLayoutVertical()
        {
            // Width is final at this point (and children's preferred heights too), so positions are computed here.
            var settings = new GridSettings(this);
            m_Solver.Compute(settings, rectChildren);

            GridLines lines = m_Solver.Lines;
            if (lines.LineCount == 0)
                return;

            GridTracks tracks = m_Solver.Tracks;
            int mainAxis = settings.MainAxis;
            GridCellPlacement.GetReversedAxes(startCorner, mainAxis, out bool reverseMain, out bool reverseCross);
            var context = new GridPlacementContext(mainAxis, reverseMain, reverseCross,
                GetStartOffset(mainAxis, tracks.MainExtent),
                GetStartOffset(1 - mainAxis, tracks.CrossExtent),
                GridCellPlacement.GetLineAlignmentFactor(m_PartialLineAlignment, childAlignment, mainAxis, reverseMain));

            for (int i = 0; i < lines.CellCount; i++)
            {
                GridCellPlacement.Place(i, lines, tracks, context, out Vector2 position, out Vector2 size);
                RectTransform child = rectChildren[i];
                SetChildAlongAxis(child, 0, position.x, size.x);
                SetChildAlongAxis(child, 1, position.y, size.y);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_MinCount = Mathf.Max(1, m_MinCount);
            m_MaxCount = Mathf.Max(m_MinCount, m_MaxCount);
            base.OnValidate();
        }
#endif
    }
}
