using UnityEngine.UI;

namespace Frontend.UI
{
    /// <summary>
    /// Responsive replacement for <see cref="GridLayoutGroup.constraint"/>: the number of columns/rows is the amount
    /// that fits in the rect, clamped to [<see cref="DynamicGridLayoutGroup.minCount"/>, <see cref="DynamicGridLayoutGroup.maxCount"/>].
    /// </summary>
    public enum GridAdaptiveConstraint
    {
        /// <summary>Uses <see cref="GridLayoutGroup.constraint"/> and <see cref="GridLayoutGroup.constraintCount"/>.</summary>
        None = 0,
        /// <summary>Column count adapts to the rect width.</summary>
        Columns = 1,
        /// <summary>Row count adapts to the rect height.</summary>
        Rows = 2,
    }
}
