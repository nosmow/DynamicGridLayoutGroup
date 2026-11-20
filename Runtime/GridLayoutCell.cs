using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Frontend.UI
{
    /// <summary>
    /// Per-child options for <see cref="DynamicGridLayoutGroup"/>.
    /// <see cref="span"/> is measured along the grid's start axis: columns for a horizontal grid, rows for a vertical one.
    /// </summary>
    [AddComponentMenu("Layout/Grid Layout Cell", 154)]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class GridLayoutCell : UIBehaviour
    {
        [SerializeField, Min(1)]
        [Tooltip("Number of slots this cell takes along the grid's start axis. Clamped to the slots available per line.")]
        private int m_Span = 1;

        /// <summary>Number of slots this cell takes along the grid's start axis (minimum 1).</summary>
        public int span
        {
            get => m_Span;
            set
            {
                value = Mathf.Max(1, value);
                if (m_Span == value)
                    return;

                m_Span = value;
                SetDirty();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            SetDirty();
            base.OnDisable();
        }

        // Rebuild both the old and the new parent.
        protected override void OnBeforeTransformParentChanged() => SetDirty();

        protected override void OnTransformParentChanged() => SetDirty();

        protected override void OnDidApplyAnimationProperties() => SetDirty();

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_Span = Mathf.Max(1, m_Span);
            SetDirty();
        }
#endif

        private void SetDirty()
        {
            // Not gated by IsActive(): the parent must also rebuild when this cell gets disabled.
            if (transform is RectTransform rect)
                LayoutRebuilder.MarkLayoutForRebuild(rect);
        }
    }
}
