using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine.UI;

[CanEditMultipleObjects]
[CustomEditor(typeof(ScrollRectSnap), true)]
public class ScrollRectSnapEditor : Editor
{
    SerializedProperty m_ScrollRect;
    SerializedProperty m_ViewOffset;
    SerializedProperty m_ChildOffset;
    SerializedProperty m_SpeedThreshold;
    SerializedProperty m_SmoothTime;
    SerializedProperty m_OnBeginSnap;
    SerializedProperty m_OnSnap;
    SerializedProperty m_OnEndSnap;

    protected virtual void OnEnable()
    {
        m_ScrollRect = serializedObject.FindProperty("m_ScrollRect");
        m_ViewOffset = serializedObject.FindProperty("m_ViewOffset");
        m_ChildOffset = serializedObject.FindProperty("m_ChildOffset");
        m_SpeedThreshold = serializedObject.FindProperty("m_SpeedThreshold");
        m_SmoothTime = serializedObject.FindProperty("m_SmoothTime");
        m_OnBeginSnap = serializedObject.FindProperty("m_OnBeginSnap");
        m_OnSnap = serializedObject.FindProperty("m_OnSnap");
        m_OnEndSnap = serializedObject.FindProperty("m_OnEndSnap");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(m_ScrollRect);
        EditorGUILayout.PropertyField(m_ViewOffset, true);
        EditorGUILayout.PropertyField(m_ChildOffset, true);
        EditorGUILayout.PropertyField(m_SpeedThreshold, true);
        EditorGUILayout.PropertyField(m_SmoothTime, true);
        EditorGUILayout.PropertyField(m_OnBeginSnap, true);
        EditorGUILayout.PropertyField(m_OnSnap);
        EditorGUILayout.PropertyField(m_OnEndSnap);

        serializedObject.ApplyModifiedProperties();
    }
}