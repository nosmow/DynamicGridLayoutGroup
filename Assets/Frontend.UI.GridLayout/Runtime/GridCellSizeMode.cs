using UnityEngine.UI;

namespace Frontend.UI
{
    /// <summary>How the cells of a <see cref="DynamicGridLayoutGroup"/> are sized.</summary>
    public enum GridCellSizeMode
    {
        /// <summary>Cells keep <see cref="GridLayoutGroup.cellSize"/>; leftover space is distributed by Child Alignment.</summary>
        Preserve = 0,
        /// <summary>
        /// Cells grow or shrink along the constrained axis (columns → width, rows → height) so the constrained count
        /// exactly fills the rect. <see cref="GridLayoutGroup.cellSize"/> still decides how many fit.
        /// Has no effect with a Flexible constraint.
        /// </summary>
        Stretch = 1,
        /// <summary>
        /// Every column is as wide as its widest cell and every row as tall as its tallest cell (preferred sizes,
        /// e.g. from a <see cref="LayoutElement"/> or a Text). With an adaptive or Flexible constraint the count is the
        /// largest one whose content fits. <see cref="GridLayoutGroup.cellSize"/> is only used as fallback for cells
        /// without a preferred size.
        /// </summary>
        FitContent = 2,
    }
}
