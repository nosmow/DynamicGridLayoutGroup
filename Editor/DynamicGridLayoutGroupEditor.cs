using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Frontend.UI.Editor
{
    /// <summary>
    /// Same layout as Unity's grid inspector; Constraint/Constraint Count are replaced by Min/Max
    /// when an adaptive constraint is selected.
    /// </summary>
    [CustomEditor(typeof(DynamicGridLayoutGroup), true)]
    [CanEditMultipleObjects]
    public class DynamicGridLayoutGroupEditor : UnityEditor.Editor
    {
        private static readonly GUIContent MinLabel = new GUIContent("Min Count", "Minimum columns/rows, even if they don't fit.");
        private static readonly GUIContent MaxLabel = new GUIContent("Max Count", "Maximum columns/rows, even if more would fit.");

        private SerializedProperty m_Padding;
        private SerializedProperty m_CellSize;
        private SerializedProperty m_Spacing;
        private SerializedProperty m_StartCorner;
        private SerializedProperty m_StartAxis;
        private SerializedProperty m_ChildAlignment;
        private SerializedProperty m_AdaptiveConstraint;
        private SerializedProperty m_MinCount;
        private SerializedProperty m_MaxCount;
        private SerializedProperty m_Constraint;
        private SerializedProperty m_ConstraintCount;
        private SerializedProperty m_PartialLineAlignment;
        private SerializedProperty m_CellSizeMode;

        protected virtual void OnEnable()
        {
            m_Padding = serializedObject.FindProperty("m_Padding");
            m_CellSize = serializedObject.FindProperty("m_CellSize");
            m_Spacing = serializedObject.FindProperty("m_Spacing");
            m_StartCorner = serializedObject.FindProperty("m_StartCorner");
            m_StartAxis = serializedObject.FindProperty("m_StartAxis");
            m_ChildAlignment = serializedObject.FindProperty("m_ChildAlignment");
            m_AdaptiveConstraint = serializedObject.FindProperty("m_AdaptiveConstraint");
            m_MinCount = serializedObject.FindProperty("m_MinCount");
            m_MaxCount = serializedObject.FindProperty("m_MaxCount");
            m_Constraint = serializedObject.FindProperty("m_Constraint");
            m_ConstraintCount = serializedObject.FindProperty("m_ConstraintCount");
            m_PartialLineAlignment = serializedObject.FindProperty("m_PartialLineAlignment");
            m_CellSizeMode = serializedObject.FindProperty("m_CellSizeMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Padding, true);
            EditorGUILayout.PropertyField(m_CellSize);
            EditorGUILayout.PropertyField(m_Spacing);
            EditorGUILayout.PropertyField(m_StartCorner);
            EditorGUILayout.PropertyField(m_StartAxis);
            EditorGUILayout.PropertyField(m_ChildAlignment);
            EditorGUILayout.PropertyField(m_PartialLineAlignment);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_AdaptiveConstraint);

            bool adaptive = m_AdaptiveConstraint.hasMultipleDifferentValues
                            || m_AdaptiveConstraint.enumValueIndex != (int)GridAdaptiveConstraint.None;

            using (new EditorGUI.IndentLevelScope())
            {
                if (adaptive)
                    DrawAdaptiveRange();
                else
                    DrawFixedConstraint();
            }

            EditorGUILayout.PropertyField(m_CellSizeMode);
            DrawCellSizeModeHelp(adaptive);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCellSizeModeHelp(bool adaptive)
        {
            if (m_CellSizeMode.hasMultipleDifferentValues)
                return;

            var mode = (GridCellSizeMode)m_CellSizeMode.enumValueIndex;
            bool flexible = !adaptive
                            && !m_Constraint.hasMultipleDifferentValues
                            && m_Constraint.enumValueIndex == (int)GridLayoutGroup.Constraint.Flexible;

            if (mode == GridCellSizeMode.Stretch && flexible)
            {
                EditorGUILayout.HelpBox("Stretch needs a constrained axis: use Adaptive Constraint or a Fixed Column/Row Count.", MessageType.Warning);
            }
            else if (mode == GridCellSizeMode.FitContent)
            {
                EditorGUILayout.HelpBox(
                    "Each column/row takes the size of its largest cell (preferred size from LayoutElement, Text, etc.). " +
                    "Cell Size is only used for cells without a preferred size.", MessageType.Info);
            }
        }

        private void DrawAdaptiveRange()
        {
            EditorGUILayout.PropertyField(m_MinCount, MinLabel);
            EditorGUILayout.PropertyField(m_MaxCount, MaxLabel);

            // Keep the range valid while editing (OnValidate also enforces it).
            if (m_MinCount.intValue < 1)
                m_MinCount.intValue = 1;
            if (m_MaxCount.intValue < m_MinCount.intValue)
                m_MaxCount.intValue = m_MinCount.intValue;
        }

        private void DrawFixedConstraint()
        {
            EditorGUILayout.PropertyField(m_Constraint);

            if (m_Constraint.hasMultipleDifferentValues || m_Constraint.enumValueIndex != (int)GridLayoutGroup.Constraint.Flexible)
            {
                using (new EditorGUI.IndentLevelScope())
                    EditorGUILayout.PropertyField(m_ConstraintCount);
            }
        }
    }
}
