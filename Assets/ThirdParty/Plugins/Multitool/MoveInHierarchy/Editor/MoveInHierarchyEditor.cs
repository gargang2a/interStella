using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Multitool.MoveInHierarchy
{
    [InitializeOnLoad]
    public static class MoveInHierarchyEditor
    {
        private const string PREF_OVERLAY_ENABLED = "MIH_OverlayEnabled";

        public static bool OverlayEnabled
        {
            get => EditorPrefs.GetBool(PREF_OVERLAY_ENABLED, true);
            set
            {
                EditorPrefs.SetBool(PREF_OVERLAY_ENABLED, value);
                SceneView.RepaintAll();
            }
        }

        private static void HandleHotkeyEvent(Event e)
        {

            if (e.shift && e.control && !e.alt && e.keyCode == KeyCode.RightBracket)
            {
                if (IsSelectionOperationValid())
                {
                    MoveSelectionToBottom();
                }
                else
                {
                    ShowInvalidSelectionNotification();
                }
                e.Use();
            }

            else if (e.shift && e.control && !e.alt && e.keyCode == KeyCode.LeftBracket)
            {
                if (IsSelectionOperationValid())
                {
                    MoveSelectionToTop();
                }
                else
                {
                    ShowInvalidSelectionNotification();
                }
                e.Use();
            }

            else if (e.control && !e.shift && !e.alt && e.keyCode == KeyCode.RightBracket)
            {
                if (IsSelectionOperationValid())
                {
                    MoveSelectionDown();
                }
                else
                {
                    ShowInvalidSelectionNotification();
                }
                e.Use();
            }

            else if (e.control && !e.shift && !e.alt && e.keyCode == KeyCode.LeftBracket)
            {
                if (IsSelectionOperationValid())
                {
                    MoveSelectionUp();
                }
                else
                {
                    ShowInvalidSelectionNotification();
                }
                e.Use();
            }
        }

        private static void ShowInvalidSelectionNotification()
        {
            EditorWindow window = EditorWindow.focusedWindow;
            if (window != null)
            {
                window.ShowNotification(new GUIContent(
                    "Operation unavailable\n" +
                    "All objects must have the same parent\n" +
                    "Cannot select parent and child together"));
            }
        }

        [MenuItem("Tools/Multitool/Move In Hierarchy/Move To Top (Ctrl+Shift+[)", false, 1)]
        public static void MoveToTop()
        {
            MoveSelectionToTop();
        }

        [MenuItem("Tools/Multitool/Move In Hierarchy/Move To Top (Ctrl+Shift+[)", true)]
        public static bool ValidateMoveToTop()
        {
            return IsSelectionOperationValid();
        }

        [MenuItem("Tools/Multitool/Move In Hierarchy/Show Overlay", false, 0)]
        public static void ToggleOverlayMenu()
        {
            OverlayEnabled = !OverlayEnabled;
        }

        [MenuItem("Tools/Multitool/Move In Hierarchy/Show Overlay", true)]
        public static bool ToggleOverlayMenuValidate()
        {
            Menu.SetChecked("Tools/Multitool/Move In Hierarchy/Show Overlay", OverlayEnabled);
            return true;
        }

        [MenuItem("Tools/Multitool/Move In Hierarchy/Move To Bottom (Ctrl+Shift+])", false, 2)]
        public static void MoveToBottom()
        {
            MoveSelectionToBottom();
        }

        [MenuItem("Tools/Multitool/Move In Hierarchy/Move To Bottom (Ctrl+Shift+])", true)]
        public static bool ValidateMoveToBottom()
        {
            return IsSelectionOperationValid();
        }

        [MenuItem("Tools/Multitool/Move In Hierarchy/Move Up (Ctrl+[)", false, 3)]
        public static void MoveUp()
        {
            MoveSelectionUp();
        }

        [MenuItem("Tools/Multitool/Move In Hierarchy/Move Up (Ctrl+[)", true)]
        public static bool ValidateMoveUp()
        {
            return IsSelectionOperationValid();
        }

        [MenuItem("Tools/Multitool/Move In Hierarchy/Move Down (Ctrl+])", false, 4)]
        public static void MoveDown()
        {
            MoveSelectionDown();
        }

        [MenuItem("Tools/Multitool/Move In Hierarchy/Move Down (Ctrl+])", true)]
        public static bool ValidateMoveDown()
        {
            return IsSelectionOperationValid();
        }

        // --------------------------------------------------------------------
        // Shortcuts (Unity Shortcut Manager)
        // These provide configurable hotkeys visible in Edit > Shortcuts.
        // Default bindings:
        // - Ctrl/Cmd + [           : Move Up
        // - Ctrl/Cmd + ]           : Move Down
        // - Ctrl/Cmd + Shift + [   : Move To Top
        // - Ctrl/Cmd + Shift + ]   : Move To Bottom
        // --------------------------------------------------------------------

        [Shortcut("Multitool/Move In Hierarchy/Move To Top",
            KeyCode.LeftBracket, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        private static void ShortcutMoveToTop()
        {
            var e = Event.current;
            if (IsSelectionOperationValid())
            {
                MoveSelectionToTop();
            }
            else
            {
                ShowInvalidSelectionNotification();
            }

            e?.Use();
        }

        [Shortcut("Multitool/Move In Hierarchy/Move To Bottom",
            KeyCode.RightBracket, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        private static void ShortcutMoveToBottom()
        {
            var e = Event.current;
            if (IsSelectionOperationValid())
            {
                MoveSelectionToBottom();
            }
            else
            {
                ShowInvalidSelectionNotification();
            }

            e?.Use();
        }

        [Shortcut("Multitool/Move In Hierarchy/Move Up",
            KeyCode.LeftBracket, ShortcutModifiers.Action)]
        private static void ShortcutMoveUp()
        {
            var e = Event.current;
            if (IsSelectionOperationValid())
            {
                MoveSelectionUp();
            }
            else
            {
                ShowInvalidSelectionNotification();
            }

            e?.Use();
        }

        [Shortcut("Multitool/Move In Hierarchy/Move Down",
            KeyCode.RightBracket, ShortcutModifiers.Action)]
        private static void ShortcutMoveDown()
        {
            var e = Event.current;
            if (IsSelectionOperationValid())
            {
                MoveSelectionDown();
            }
            else
            {
                ShowInvalidSelectionNotification();
            }

            e?.Use();
        }


        private static bool HasValidSelection()
        {
            return Selection.transforms != null && Selection.transforms.Length > 0;
        }

        private static void FocusHierarchyOnSelection(bool focusTop)
        {
            if (Selection.transforms == null || Selection.transforms.Length == 0)
                return;


            var selection = Selection.transforms;
            var ordered = selection.OrderBy(t => t.GetSiblingIndex());
            var targetTransform = focusTop
                ? ordered.FirstOrDefault()
                : ordered.LastOrDefault();

            if (targetTransform == null)
                return;

            var targetObject = targetTransform.gameObject;
            if (targetObject == null)
                return;


            EditorApplication.delayCall += () =>
            {
                if (targetObject != null)
                {
                    EditorGUIUtility.PingObject(targetObject);
                }
            };
        }

        public static bool IsSelectionOperationValid()
        {
            if (!HasValidSelection()) return false;

            var selection = Selection.transforms;


            if (selection.Length == 1) return true;


            Transform firstParent = selection[0].parent;
            foreach (var transform in selection)
            {
                if (transform.parent != firstParent)
                {
                    return false;
                }
            }


            for (int i = 0; i < selection.Length; i++)
            {
                for (int j = 0; j < selection.Length; j++)
                {
                    if (i == j) continue;


                    Transform current = selection[j].parent;
                    while (current != null)
                    {
                        if (current == selection[i])
                        {
                            return false;
                        }
                        current = current.parent;
                    }
                }
            }

            return true;
        }

        public static void MoveSelectionToTop()
        {
            var selection = Selection.transforms;
            if (selection == null || selection.Length == 0) return;


            var groups = selection.GroupBy(t => t.parent);


            var allTransforms = selection.ToList();
            var allParents = groups.Select(g => g.Key).Where(p => p != null).Distinct().ToList();

            Undo.SetCurrentGroupName("Move To Top");
            int undoGroup = Undo.GetCurrentGroup();


            foreach (var transform in allTransforms)
            {
                Undo.RegisterCompleteObjectUndo(transform, "Move To Top");
            }
            foreach (var parent in allParents)
            {
                Undo.RegisterCompleteObjectUndo(parent, "Move To Top");
            }


            foreach (var group in groups)
            {
                var transforms = group.OrderByDescending(t => t.GetSiblingIndex()).ToList();

                foreach (var transform in transforms)
                {
                    transform.SetAsFirstSibling();
                }
            }

            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(undoGroup);
            Selection.objects = selection.Select(t => t.gameObject).ToArray();

            FocusHierarchyOnSelection(true);
        }

        public static void MoveSelectionToBottom()
        {
            var selection = Selection.transforms;
            if (selection == null || selection.Length == 0) return;


            var groups = selection.GroupBy(t => t.parent);


            var allTransforms = selection.ToList();
            var allParents = groups.Select(g => g.Key).Where(p => p != null).Distinct().ToList();

            Undo.SetCurrentGroupName("Move To Bottom");
            int undoGroup = Undo.GetCurrentGroup();


            foreach (var transform in allTransforms)
            {
                Undo.RegisterCompleteObjectUndo(transform, "Move To Bottom");
            }
            foreach (var parent in allParents)
            {
                Undo.RegisterCompleteObjectUndo(parent, "Move To Bottom");
            }


            foreach (var group in groups)
            {
                var transforms = group.OrderBy(t => t.GetSiblingIndex()).ToList();

                foreach (var transform in transforms)
                {
                    transform.SetAsLastSibling();
                }
            }

            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(undoGroup);
            Selection.objects = selection.Select(t => t.gameObject).ToArray();

            FocusHierarchyOnSelection(false);
        }

        public static void MoveSelectionUp()
        {
            var selection = Selection.transforms;
            if (selection == null || selection.Length == 0) return;


            var groups = selection.GroupBy(t => t.parent);


            var allTransforms = selection.ToList();
            var allParents = groups.Select(g => g.Key).Where(p => p != null).Distinct().ToList();

            Undo.SetCurrentGroupName("Move Up");
            int undoGroup = Undo.GetCurrentGroup();


            foreach (var transform in allTransforms)
            {
                Undo.RegisterCompleteObjectUndo(transform, "Move Up");
            }
            foreach (var parent in allParents)
            {
                Undo.RegisterCompleteObjectUndo(parent, "Move Up");
            }


            foreach (var group in groups)
            {
                var transforms = group.OrderBy(t => t.GetSiblingIndex()).ToList();

                if (transforms.Count == 0) continue;


                int minIndex = transforms.Min(t => t.GetSiblingIndex());

                if (minIndex == 0)
                {

                    continue;
                }


                int targetIndex = minIndex - 1;


                foreach (var transform in transforms)
                {
                    transform.SetSiblingIndex(targetIndex);
                    targetIndex++;
                }
            }

            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(undoGroup);
            Selection.objects = selection.Select(t => t.gameObject).ToArray();

            FocusHierarchyOnSelection(true);
        }

        public static void MoveSelectionDown()
        {
            var selection = Selection.transforms;
            if (selection == null || selection.Length == 0) return;


            var groups = selection.GroupBy(t => t.parent);


            var allTransforms = selection.ToList();
            var allParents = groups.Select(g => g.Key).Where(p => p != null).Distinct().ToList();

            Undo.SetCurrentGroupName("Move Down");
            int undoGroup = Undo.GetCurrentGroup();


            foreach (var transform in allTransforms)
            {
                Undo.RegisterCompleteObjectUndo(transform, "Move Down");
            }
            foreach (var parent in allParents)
            {
                Undo.RegisterCompleteObjectUndo(parent, "Move Down");
            }


            foreach (var group in groups)
            {
                var parent = group.Key;
                var transforms = group.OrderBy(t => t.GetSiblingIndex()).ToList();

                if (transforms.Count == 0) continue;


                int maxIndex = transforms.Max(t => t.GetSiblingIndex());


                int siblingCount = parent == null
                    ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Length
                    : parent.childCount;

                if (maxIndex >= siblingCount - 1)
                {

                    continue;
                }


                int targetIndex = maxIndex + 2 - transforms.Count;


                for (int i = transforms.Count - 1; i >= 0; i--)
                {
                    var transform = transforms[i];
                    transform.SetSiblingIndex(targetIndex + i);
                }
            }

            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(undoGroup);
            Selection.objects = selection.Select(t => t.gameObject).ToArray();
            FocusHierarchyOnSelection(false);
        }

        [Overlay(typeof(SceneView), "\u200B", true)]
        public class MoveInHierarchyOverlay : IMGUIOverlay, ITransientOverlay
        {

            private static GUIStyle _buttonStyle;
            private static Texture2D _iconMoveTop;
            private static Texture2D _iconMoveUp;
            private static Texture2D _iconMoveDown;
            private static Texture2D _iconMoveBottom;
            private const float ButtonWidth = 40f;
            private const float ButtonHeight = 20f;
            private const float IconSize = 16f;
            private const float Gap = 2f;

            bool ITransientOverlay.visible => MoveInHierarchyEditor.OverlayEnabled;

            private static void LoadIcons()
            {
                if (_iconMoveTop == null)
                    _iconMoveTop = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Plugins/Multitool/MoveInHierarchy/Icons/MoveTop.png");
                if (_iconMoveUp == null)
                    _iconMoveUp = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Plugins/Multitool/MoveInHierarchy/Icons/MoveUp.png");
                if (_iconMoveDown == null)
                    _iconMoveDown = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Plugins/Multitool/MoveInHierarchy/Icons/MoveDown.png");
                if (_iconMoveBottom == null)
                    _iconMoveBottom = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Plugins/Multitool/MoveInHierarchy/Icons/MoveBottom.png");
            }

            public override void OnGUI()
            {
                if (!MoveInHierarchyEditor.OverlayEnabled)
                    return;

                LoadIcons();

                float buttonWidth = ButtonWidth;
                float buttonHeight = ButtonHeight;
                float gap = Gap;

                float panelWidth = buttonWidth;
                float panelHeight = buttonHeight * 4f + gap * 6f;

                Rect panelRect = GUILayoutUtility.GetRect(panelWidth, panelHeight, GUILayout.Width(panelWidth), GUILayout.Height(panelHeight));
                EditorGUI.DrawRect(panelRect, new Color(0f, 0f, 0f, 0.85f));

                if (_buttonStyle == null)
                {
                    _buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter,
                        margin = new RectOffset(0, 0, 0, 0),
                        padding = new RectOffset(0, 0, 0, 0)
                    };
                }


                GUILayout.BeginArea(panelRect);
                using (new EditorGUI.DisabledScope(!MoveInHierarchyEditor.IsSelectionOperationValid()))
                {
                    Vector2 oldIconSize = EditorGUIUtility.GetIconSize();
                    EditorGUIUtility.SetIconSize(new Vector2(IconSize, IconSize));

                    GUILayout.BeginVertical();

                    // Move Top
                    if (GUILayout.Button(new GUIContent(_iconMoveTop), _buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                    {
                        MoveInHierarchyEditor.MoveSelectionToTop();
                    }
                    GUILayout.Space(gap);

                    // Move Up
                    if (GUILayout.Button(new GUIContent(_iconMoveUp), _buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                    {
                        MoveInHierarchyEditor.MoveSelectionUp();
                    }
                    GUILayout.Space(gap * 2);

                    // Move Down
                    if (GUILayout.Button(new GUIContent(_iconMoveDown), _buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                    {
                        MoveInHierarchyEditor.MoveSelectionDown();
                    }
                    GUILayout.Space(gap);

                    // Move Bottom
                    if (GUILayout.Button(new GUIContent(_iconMoveBottom), _buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                    {
                        MoveInHierarchyEditor.MoveSelectionToBottom();
                    }

                    GUILayout.EndVertical();

                    EditorGUIUtility.SetIconSize(oldIconSize);
                }

                GUILayout.EndArea();
            }
        }
    }
}
