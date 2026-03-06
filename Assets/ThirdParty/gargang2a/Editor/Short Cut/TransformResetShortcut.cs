using UnityEditor;
using UnityEngine;

public class TransformResetShortcut
{
    // [MenuItem("메뉴 경로/메뉴 이름 %&r")] 형태로 단축키를 정의합니다.
    // 여기서는 "Tools" 메뉴 아래에 "Reset Transform" 메뉴 항목을 추가하고,
    // 단축키는 Ctrl + Alt + R (%&r)로 지정합니다.
    [MenuItem("Tools/Reset Transform %&r")]
    private static void ResetSelectedTransform()
    {
        // 1. 현재 선택된 GameObject가 있는지 확인합니다.
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning("리셋할 GameObject를 선택해 주세요.");
            return;
        }

        // 2. 선택된 GameObject의 Transform 컴포넌트에 접근합니다.
        Transform targetTransform = selectedObject.transform;

        // 3. Transform 값을 리셋합니다.
        // Hierarchy에서 Transform을 리셋할 때는 Local 값이 기준이 됩니다.

        // Position: (0, 0, 0)
        targetTransform.localPosition = Vector3.zero;

        // Rotation: (0, 0, 0) (Quaternion.identity)
        targetTransform.localRotation = Quaternion.identity;

        // Scale: (1, 1, 1)
        targetTransform.localScale = Vector3.one;

        Debug.Log($"{selectedObject.name}의 Transform 컴포넌트가 초기화되었습니다. ( 단축키: Ctrl + Alt + R [` + 2] )");
    }
}