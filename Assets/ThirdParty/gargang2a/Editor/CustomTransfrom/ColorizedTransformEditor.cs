using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(Transform))]
public class ColorizedTransformEditor : Editor
{
    // ... 기존 필드 및 상수 정의 (유지) ...
    private SerializedProperty _localPositionProperty;
    private SerializedProperty _localRotationProperty;
    private SerializedProperty _localScaleProperty;

    private static readonly Color PositionColor = new Color(0.7f, 1.0f, 0.7f, 1.0f);
    private static readonly Color RotationColor = new Color(0.7f, 0.7f, 1.0f, 1.0f);
    private static readonly Color ScaleColor = new Color(1.0f, 0.7f, 0.7f, 1.0f);
    private static readonly Color ResetAllColor = new Color(1.0f, 0.8f, 0.8f, 1.0f);
    private static readonly Color BoundsColor = new Color(0.8f, 0.9f, 1.0f, 1.0f); // 바운즈 정보 표시용 색상
    private static readonly Color DefaultColor = Color.white;
    private const float ResetButtonWidth = 20f;

    private static GUIStyle _resetAllButtonStyle;
    private static GUIStyle ResetAllButtonStyle
    {
        // ... 스타일 초기화 로직 (유지) ...
        get
        {
            if (_resetAllButtonStyle == null)
            {
                _resetAllButtonStyle = new GUIStyle(EditorStyles.miniButton);
                _resetAllButtonStyle.fontStyle = FontStyle.Bold;
                _resetAllButtonStyle.alignment = TextAnchor.MiddleCenter;
            }
            return _resetAllButtonStyle;
        }
    }

    private void OnEnable()
    {
        _localPositionProperty = serializedObject.FindProperty("m_LocalPosition");
        _localRotationProperty = serializedObject.FindProperty("m_LocalRotation");
        _localScaleProperty = serializedObject.FindProperty("m_LocalScale");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Transform t = (Transform)target;

        // --- 0. Reset All 버튼 ---
        DrawResetAllButton(t);

        // --- 1. Position, Rotation, Scale 필드 ---
        DrawPropertyWithResetButton(_localPositionProperty, PositionColor, "Position", () => t.localPosition = Vector3.zero);
        DrawPropertyWithResetButton(_localRotationProperty, RotationColor, "Rotation", () => t.localRotation = Quaternion.identity);
        DrawPropertyWithResetButton(_localScaleProperty, ScaleColor, "Scale", () => t.localScale = Vector3.one);

        // --- 4. [NEW] Bounds(크기) 정보 ---
        EditorGUILayout.Separator();
        DrawBoundsInfo(t);

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 오브젝트의 Mesh Renderer를 찾아 Bounds(크기) 정보를 인스펙터에 표시합니다.
    /// </summary>
    /// <param name="t">대상 Transform 컴포넌트</param>
    private void DrawBoundsInfo(Transform t)
    {
        Renderer renderer = t.GetComponent<Renderer>();

        GUI.backgroundColor = BoundsColor;

        EditorGUILayout.BeginVertical("box"); // 시각적 구분을 위해 박스 스타일 사용
        {
            GUI.backgroundColor = DefaultColor; // 박스 안쪽은 기본색으로 복원

            if (renderer != null)
            {
                // 오브젝트의 월드 공간(World Space) 바운드 크기
                Vector3 size = renderer.bounds.size;

                // GUILayout.Label을 사용하여 볼드체로 정보 표시
                GUILayout.Label("Object Bounds (World Size)", EditorStyles.boldLabel);

                // Vector3 필드처럼 X, Y, Z 레이블을 수평으로 표시
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("X", size.x.ToString("F3"), GUILayout.Width(100));
                    EditorGUILayout.LabelField("Y", size.y.ToString("F3"), GUILayout.Width(100));
                    EditorGUILayout.LabelField("Z", size.z.ToString("F3"), GUILayout.Width(100));
                }
                EditorGUILayout.EndHorizontal();

                // 로컬 스케일이 적용되지 않은, 순수한 메시의 크기를 표시하려면 
                // MeshFilter를 찾은 후 mesh.bounds.size를 사용해야 합니다. (이 예제는 World Size를 사용)
            }
            else
            {
                // Renderer 컴포넌트가 없는 경우 (예: Empty GameObject, Light, Camera 등)
                EditorGUILayout.LabelField("Object Bounds (World Size)", "No Renderer/Bounds Found", EditorStyles.miniLabel);
            }
        }
        EditorGUILayout.EndVertical();

        GUI.backgroundColor = DefaultColor;
    }

    // ... DrawResetAllButton, DrawPropertyWithResetButton, ApplyChanges 함수는 이전과 동일하게 유지 ...
    // (길이상 여기서는 생략합니다. 실제 코드 파일에서는 유지되어야 합니다.)

    private void DrawResetAllButton(Transform t)
    {
        GUI.backgroundColor = ResetAllColor;
        if (GUILayout.Button("Reset All", ResetAllButtonStyle, GUILayout.Height(30)))
        {
            Undo.RecordObject(target, "Reset All Transform");
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            ApplyChanges(t);
        }
        GUI.backgroundColor = DefaultColor;
    }

    private void DrawPropertyWithResetButton(SerializedProperty property, Color color, string label, System.Action resetAction)
    {
        EditorGUILayout.BeginHorizontal();
        {
            GUI.backgroundColor = color;
            EditorGUILayout.PropertyField(property, new GUIContent(label));
            GUI.backgroundColor = DefaultColor;

            if (GUILayout.Button("R", GUILayout.Width(ResetButtonWidth)))
            {
                Undo.RecordObject(target, "Reset " + label);
                resetAction?.Invoke();
                ApplyChanges((Transform)target);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ApplyChanges(Transform t)
    {
        if (EditorUtility.IsPersistent(t))
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        else
        {
            EditorUtility.SetDirty(t);
        }
    }
}