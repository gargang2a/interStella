using UnityEditor;
using System.Reflection;
using UnityEngine; // Debug.LogWarning 사용을 위해 추가

public static class InspectorLocker
{
    // 사용자 지정 단축키: Ctrl + ` (Windows) 또는 Cmd + ` (Mac)
    [MenuItem("Tools/Toggle Inspector Lock %`")]
    private static void ToggleLock()
    {
        // 1. 리플렉션을 사용하여 InspectorWindow의 내부 타입을 가져옵니다.
        System.Type inspectorType = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");

        if (inspectorType == null)
        {
            Debug.LogError("InspectorWindow 타입을 찾을 수 없습니다. 유니티 버전을 확인해 주세요.");
            return;
        }

        // 2. 현재 열려 있는 InspectorWindow 인스턴스를 가져옵니다.
        // GetFocusedWindow 대신 GetWindow를 사용해 안정성을 높입니다.
        EditorWindow inspector = EditorWindow.GetWindow(inspectorType);

        if (inspector == null)
        {
            return;
        }

        // 3. isLocked 상태를 찾고 토글합니다. (Field와 Property 둘 다 시도)

        // FieldInfo 시도 (최신 버전에서 자주 사용되는 이름: m_Lock)
        FieldInfo fieldInfo = inspectorType.GetField("m_Lock", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // m_Lock이 아니면 isLocked 필드 이름으로 시도
        if (fieldInfo == null)
        {
            fieldInfo = inspectorType.GetField("isLocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        // FieldInfo 접근 성공
        if (fieldInfo != null)
        {
            bool currentLockState = (bool)fieldInfo.GetValue(inspector);
            fieldInfo.SetValue(inspector, !currentLockState);
        }
        else
        {
            // PropertyInfo 시도 (이전 버전에서 사용됨)
            PropertyInfo propertyInfo = inspectorType.GetProperty("isLocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (propertyInfo != null)
            {
                bool currentLockState = (bool)propertyInfo.GetValue(inspector, null);
                propertyInfo.SetValue(inspector, !currentLockState, null);
            }
            else
            {
                Debug.LogWarning("Inspector Lock 상태 변수(m_Lock/isLocked)를 찾을 수 없습니다. 유니티 버전에 맞게 코드를 조정해야 할 수 있습니다.");
                return;
            }
        }

        // 4. 상태 변경 반영
        inspector.Repaint();
    }
}