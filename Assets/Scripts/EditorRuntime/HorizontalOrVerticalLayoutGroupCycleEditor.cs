using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(HorizontalOrVerticalLayoutGroupCycle), true)]
[CanEditMultipleObjects]
public class HorizontalOrVerticalLayoutGroupCycleEditor : HorizontalOrVerticalLayoutGroupEditor
{
    // SerializedProperty m_ScrollRect;
    SerializedProperty m_Size;
    SerializedProperty m_Reversed;

    protected override void OnEnable()
    {
        base.OnEnable();

        // m_ScrollRect = serializedObject.FindProperty("m_ScrollRect");
        m_Size = serializedObject.FindProperty("m_Size");
        m_Reversed = serializedObject.FindProperty("m_Reversed");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        // EditorGUILayout.PropertyField(m_ScrollRect);
        EditorGUILayout.PropertyField(m_Size, true);
        EditorGUILayout.PropertyField(m_Reversed, true);

        serializedObject.ApplyModifiedProperties();
    }
}
