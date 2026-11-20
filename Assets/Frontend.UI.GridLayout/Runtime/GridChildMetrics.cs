using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frontend.UI
{
    /// <summary>
    /// Per-child data read once per rebuild: the span (from <see cref="GridLayoutCell"/>) and, in FitContent mode,
    /// the preferred size. Buffers are reused between passes.
    /// </summary>
    internal sealed class GridChildMetrics
    {
        private readonly List<int> m_Spans = new List<int>();
        private readonly List<Vector2> m_PreferredSizes = new List<Vector2>();

        /// <summary>Number of children whose span is cached.</summary>
        public int Count => m_Spans.Count;

        /// <summary>Sum of all spans (slots the children need).</summary>
        public int TotalUnits { get; private set; }

        /// <summary>Largest span of any child (at least 1).</summary>
        public int MaxSpan { get; private set; } = 1;

        public bool HasSpans => MaxSpan > 1;

        public int GetSpan(int index) => m_Spans[index];

        public Vector2 GetPreferredSize(int index) => m_PreferredSizes[index];

        /// <summary>Reads every child's span: a single TryGetComponent per child.</summary>
        public void CacheSpans(List<RectTransform> children)
        {
            m_Spans.Clear();
            TotalUnits = 0;
            MaxSpan = 1;

            for (int i = 0; i < children.Count; i++)
            {
                int span = 1;
                if (children[i].TryGetComponent(out GridLayoutCell cell) && cell.isActiveAndEnabled)
                    span = cell.span;

                m_Spans.Add(span);
                TotalUnits += span;

                if (span > MaxSpan)
                    MaxSpan = span;
            }
        }

        /// <summary>Preferred sizes of the children; <paramref name="fallback"/> replaces any size that is not positive.</summary>
        public void MeasurePreferredSizes(List<RectTransform> children, Vector2 fallback)
        {
            m_PreferredSizes.Clear();
            for (int i = 0; i < children.Count; i++)
            {
                RectTransform child = children[i];
                float width = LayoutUtility.GetPreferredWidth(child);
                float height = LayoutUtility.GetPreferredHeight(child);
                m_PreferredSizes.Add(new Vector2(width > 0f ? width : fallback.x, height > 0f ? height : fallback.y));
            }
        }

        public void ClearPreferredSizes() => m_PreferredSizes.Clear();
    }
}
