using UnityEngine.UI;

namespace Frontend.UI
{
    /// <summary>How lines that use less space than the widest line are positioned along the start axis.</summary>
    public enum GridLineAlignment
    {
        /// <summary>Same behaviour as Unity's <see cref="GridLayoutGroup"/>: packed towards the start corner.</summary>
        None = 0,
        /// <summary>Uses the horizontal/vertical component of <see cref="LayoutGroup.childAlignment"/>.</summary>
        FollowChildAlignment = 1,
        /// <summary>Left (horizontal start axis) or top (vertical start axis).</summary>
        Start = 2,
        /// <summary>Centered inside the grid block.</summary>
        Center = 3,
        /// <summary>Right (horizontal start axis) or bottom (vertical start axis).</summary>
        End = 4,
    }
}
