using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Frontend.UI.Editor
{
    /// <summary>Context menu to migrate an existing <see cref="GridLayoutGroup"/> keeping its settings.</summary>
    internal static class GridLayoutGroupConversion
    {
        private const string MenuPath = "CONTEXT/GridLayoutGroup/Convert to Dynamic Grid Layout Group";

        [MenuItem(MenuPath, true)]
        private static bool CanConvert(MenuCommand command)
        {
            return command.context is GridLayoutGroup source
                   && !(source is DynamicGridLayoutGroup)
                   && (!PrefabUtility.IsPartOfPrefabInstance(source) || PrefabUtility.IsAddedComponentOverride(source));
        }

        [MenuItem(MenuPath)]
        private static void Convert(MenuCommand command)
        {
            var source = (GridLayoutGroup)command.context;
            GameObject gameObject = source.gameObject;

            // Snapshot settings before destroying (LayoutGroup is DisallowMultipleComponent).
            RectOffset padding = source.padding;
            var paddingCopy = new RectOffset(padding.left, padding.right, padding.top, padding.bottom);
            TextAnchor childAlignment = source.childAlignment;
            Vector2 cellSize = source.cellSize;
            Vector2 spacing = source.spacing;
            GridLayoutGroup.Corner startCorner = source.startCorner;
            GridLayoutGroup.Axis startAxis = source.startAxis;
            GridLayoutGroup.Constraint constraint = source.constraint;
            int constraintCount = source.constraintCount;
            bool enabled = source.enabled;

            Undo.SetCurrentGroupName("Convert to Dynamic Grid Layout Group");
            int undoGroup = Undo.GetCurrentGroup();

            Undo.DestroyObjectImmediate(source);
            var target = Undo.AddComponent<DynamicGridLayoutGroup>(gameObject);

            target.padding = paddingCopy;
            target.childAlignment = childAlignment;
            target.cellSize = cellSize;
            target.spacing = spacing;
            target.startCorner = startCorner;
            target.startAxis = startAxis;
            target.constraint = constraint;
            target.constraintCount = constraintCount;
            // Keep Unity's look after converting; users can opt in to partial-line alignment.
            target.partialLineAlignment = GridLineAlignment.None;
            target.enabled = enabled;

            Undo.CollapseUndoOperations(undoGroup);
        }
    }
}
