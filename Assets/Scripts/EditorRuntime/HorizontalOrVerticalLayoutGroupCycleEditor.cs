using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(HorizontalOrVerticalLayoutGroupCycle), true)]
[CanEditMultipleObjects]
public class HorizontalOrVerticalLayoutGroupCycleEditor : HorizontalOrVerticalLayoutGroupEditor
{
    // SerializedProperty m_ScrollRect;
    SerializedProperty m_Capacity;
    SerializedProperty m_Reversed;

    protected override void OnEnable()
    {
        base.OnEnable();

        // m_ScrollRect = serializedObject.FindProperty("m_ScrollRect");
        m_Capacity = serializedObject.FindProperty("m_Capacity");
        m_Reversed = serializedObject.FindProperty("m_Reversed");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        // EditorGUILayout.PropertyField(m_ScrollRect);
        EditorGUILayout.PropertyField(m_Capacity, true);
        EditorGUILayout.PropertyField(m_Reversed, true);

        serializedObject.ApplyModifiedProperties();
    }
}
