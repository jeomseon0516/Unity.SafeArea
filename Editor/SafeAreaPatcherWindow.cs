using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jeomseon.Unity.SafeArea.Editor
{
    public sealed class SafeAreaPatcherWindow : EditorWindow
    {
        [SerializeField] private TreeViewState<EntityId> treeViewState;

        private SafeAreaHierarchyTreeView _treeView;
        private Scene _scene;

        public static void ShowWindow()
        {
            var window = GetWindow<SafeAreaPatcherWindow>("Safe Area Patcher");
            window.minSize = new Vector2(720f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            treeViewState ??= new TreeViewState<EntityId>();
            _treeView = new SafeAreaHierarchyTreeView(treeViewState);
            _treeView.StateChanged += Repaint;
            EditorApplication.hierarchyChanged += HandleHierarchyChanged;
            EditorSceneManager.activeSceneChangedInEditMode += HandleActiveSceneChanged;
            RefreshFromScene(true);
        }

        private void OnDisable()
        {
            if (_treeView != null)
                _treeView.StateChanged -= Repaint;
            EditorApplication.hierarchyChanged -= HandleHierarchyChanged;
            EditorSceneManager.activeSceneChangedInEditMode -= HandleActiveSceneChanged;
        }

        private void OnGUI()
        {
            DrawHeader();

            if (!_scene.IsValid() || !_scene.isLoaded)
            {
                EditorGUILayout.HelpBox("Open a Scene before configuring Safe Area components.", MessageType.Info);
                return;
            }

            var treeRect = GUILayoutUtility.GetRect(
                0f,
                100000f,
                0f,
                100000f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            _treeView.OnGUI(treeRect);

            DrawFooter();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(_scene.IsValid() ? _scene.name : "No Active Scene", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Expand All", EditorStyles.toolbarButton))
                    _treeView.ExpandAll();
                if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton))
                    _treeView.CollapseAll();
                if (GUILayout.Button("Reset from Scene", EditorStyles.toolbarButton))
                    RefreshFromScene(true);
            }

            EditorGUILayout.HelpBox(
                "The left checkbox controls SafeArea. Runtime Ignore controls the SafeAreaIgnore marker used only by runtime patching. The Scene is not modified until Apply Changes is pressed.",
                MessageType.Info);
        }

        private void DrawFooter()
        {
            var entries = _treeView.CanvasEntries.Values;
            var addCount = entries.Count(entry =>
                entry.Validation.CanChange &&
                !entry.Validation.IsApplied &&
                _treeView.GetTargetState(entry.EntityId));
            var removeCount = entries.Count(entry =>
                entry.Validation.CanChange &&
                entry.Validation.IsApplied &&
                !_treeView.GetTargetState(entry.EntityId));
            var excludedCount = entries.Count(entry => !entry.Validation.CanChange);
            var addIgnoreCount = entries.Count(entry =>
                !entry.Canvas.TryGetComponent<SafeAreaIgnore>(out _) &&
                _treeView.GetRuntimeIgnoreState(entry.EntityId));
            var removeIgnoreCount = entries.Count(entry =>
                entry.Canvas.TryGetComponent<SafeAreaIgnore>(out _) &&
                !_treeView.GetRuntimeIgnoreState(entry.EntityId));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                $"Pending SafeArea: Add {addCount}, Remove {removeCount}  |  Runtime Ignore: Add {addIgnoreCount}, Remove {removeIgnoreCount}  |  Invalid {excludedCount}",
                EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(
                       addCount == 0 && removeCount == 0 && addIgnoreCount == 0 && removeIgnoreCount == 0))
            {
                if (GUILayout.Button("Apply Changes", GUILayout.Height(30f)))
                    ApplyChanges(addCount, removeCount);
            }
        }

        private void ApplyChanges(int addCount, int removeCount)
        {
            if (removeCount > 0 && !EditorUtility.DisplayDialog(
                    "Apply Safe Area Changes",
                    $"Add {addCount} and remove {removeCount} SafeArea component(s)?",
                    "Apply",
                    "Cancel"))
            {
                return;
            }

            var settings = SafeAreaSettings.Resolve();
            var validator = new SafeAreaPatchValidator(settings);

            // Start a fresh Undo group so CollapseUndoOperations only folds this
            // method's operations together, not whatever the user did just before
            // pressing Apply.
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Apply Safe Area Patcher Changes");
            var undoGroup = Undo.GetCurrentGroup();

            foreach (var entry in _treeView.CanvasEntries.Values.ToArray())
            {
                if (entry.Canvas == null)
                    continue;

                var validation = validator.Validate(entry.Canvas);
                var targetState = _treeView.GetTargetState(entry.EntityId);
                var runtimeIgnoreState = _treeView.GetRuntimeIgnoreState(entry.EntityId);
                var existingIgnore = entry.Canvas.GetComponent<SafeAreaIgnore>();
                var safeAreaChanged = validation.CanChange && targetState != validation.IsApplied;
                var ignoreChanged = runtimeIgnoreState != (existingIgnore != null);
                if (!safeAreaChanged && !ignoreChanged)
                    continue;

                Undo.RegisterFullObjectHierarchyUndo(entry.Canvas.gameObject, "Apply Safe Area Patcher Changes");
                if (safeAreaChanged && targetState)
                {
                    SafeAreaPatchCore.EnsureSafeAreaContainer(entry.Canvas, settings);
                }
                else if (safeAreaChanged && validation.ExistingSafeArea != null)
                {
                    Undo.DestroyObjectImmediate(validation.ExistingSafeArea);
                }

                if (ignoreChanged && runtimeIgnoreState)
                    Undo.AddComponent<SafeAreaIgnore>(entry.Canvas.gameObject);
                else if (ignoreChanged)
                    Undo.DestroyObjectImmediate(existingIgnore);
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(_scene);
            RefreshFromScene(true);
        }

        private void HandleHierarchyChanged()
        {
            RefreshFromScene(false);
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            RefreshFromScene(true);
        }

        private void RefreshFromScene(bool resetTargets)
        {
            _scene = SceneManager.GetActiveScene();
            var settings = SafeAreaSettings.Resolve();
            _treeView?.SetScene(_scene, new SafeAreaPatchValidator(settings), resetTargets);
            Repaint();
        }
    }
}
