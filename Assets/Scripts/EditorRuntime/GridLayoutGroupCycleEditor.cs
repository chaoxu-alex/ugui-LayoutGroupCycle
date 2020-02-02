using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(GridLayoutGroupCycle), true)]
[CanEditMultipleObjects]
public class GridLayoutGroupCycleEditor : GridLayoutGroupEditor
{
    // SerializedProperty m_ScrollRect;
    SerializedProperty m_Capacity;
    
    protected override void OnEnable()
    {
        base.OnEnable();

        // m_ScrollRect = serializedObject.FindProperty("m_ScrollRect");
        m_Capacity = serializedObject.FindProperty("m_Capacity");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        // EditorGUILayout.PropertyField(m_ScrollRect);
        EditorGUILayout.PropertyField(m_Capacity, true);

        serializedObject.ApplyModifiedProperties();
    }
}
