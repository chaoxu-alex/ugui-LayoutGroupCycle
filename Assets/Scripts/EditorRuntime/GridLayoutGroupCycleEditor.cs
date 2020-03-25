using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(GridLayoutGroupCycle), true)]
[CanEditMultipleObjects]
public class GridLayoutGroupCycleEditor : GridLayoutGroupEditor
{
    // SerializedProperty m_ScrollRect;
    SerializedProperty m_Size;
    
    protected override void OnEnable()
    {
        base.OnEnable();

        // m_ScrollRect = serializedObject.FindProperty("m_ScrollRect");
        m_Size = serializedObject.FindProperty("m_Size");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        // EditorGUILayout.PropertyField(m_ScrollRect);
        EditorGUILayout.PropertyField(m_Size, true);

        serializedObject.ApplyModifiedProperties();
    }
}
