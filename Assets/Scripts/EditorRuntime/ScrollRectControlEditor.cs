using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine.UI;

[CanEditMultipleObjects]
[CustomEditor(typeof(ScrollRectControl), true)]
public class ScrollRectControlEditor : Editor
{
    // SerializedProperty m_ScrollRect;
    SerializedProperty m_TargetParent;
    SerializedProperty m_ViewSnapPivot;
    SerializedProperty m_ViewSnapOffset;
    SerializedProperty m_TargetSnapPivot;
    SerializedProperty m_TargetSnapOffset;
    SerializedProperty m_SmoothTime;
    SerializedProperty m_TweenType;
    SerializedProperty m_ClampWithinContent;
    SerializedProperty m_AutoSnap;
    SerializedProperty m_CutInSpeed;
    SerializedProperty m_OnEndSnap;

    protected virtual void OnEnable()
    {
        // m_ScrollRect = serializedObject.FindProperty("m_ScrollRect");
        m_TargetParent = serializedObject.FindProperty("m_TargetParent");
        m_ViewSnapPivot = serializedObject.FindProperty("m_ViewSnapPivot");
        m_ViewSnapOffset = serializedObject.FindProperty("m_ViewSnapOffset");
        m_TargetSnapPivot = serializedObject.FindProperty("m_TargetSnapPivot");
        m_TargetSnapOffset = serializedObject.FindProperty("m_TargetSnapOffset");
        m_SmoothTime = serializedObject.FindProperty("m_SmoothTime");
        m_TweenType = serializedObject.FindProperty("m_TweenType");
        m_ClampWithinContent = serializedObject.FindProperty("m_ClampWithinContent");
        m_AutoSnap = serializedObject.FindProperty("m_AutoSnap");
        m_CutInSpeed = serializedObject.FindProperty("m_CutInSpeed");
        m_OnEndSnap = serializedObject.FindProperty("m_OnEndSnap");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        // EditorGUILayout.PropertyField(m_ScrollRect);
        EditorGUILayout.PropertyField(m_TargetParent);
        EditorGUILayout.PropertyField(m_ViewSnapPivot);
        EditorGUILayout.PropertyField(m_ViewSnapOffset);
        EditorGUILayout.PropertyField(m_TargetSnapPivot);
        EditorGUILayout.PropertyField(m_TargetSnapOffset);
        EditorGUILayout.PropertyField(m_SmoothTime);
        EditorGUILayout.PropertyField(m_TweenType);
        EditorGUILayout.PropertyField(m_ClampWithinContent);
        EditorGUILayout.PropertyField(m_AutoSnap);
        if (m_AutoSnap.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_CutInSpeed);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.PropertyField(m_OnEndSnap);

        serializedObject.ApplyModifiedProperties();
    }
}