using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Jeomseon.Unity.SafeArea.Editor
{
    public sealed class SafeAreaPatcherWindow : EditorWindow
    {
        [SerializeField] private UnityEditor.IMGUI.Controls.TreeViewState<EntityId> treeViewState;

        private SafeAreaHierarchyTreeView _treeView;
        private Scene _scene;

        private Label _sceneNameLabel;
        private HelpBox _noSceneHelpBox;
        private VisualElement _sceneContent;
        private Label _pendingLabel;
        private Button _applyButton;

        public static void ShowWindow()
        {
            var window = GetWindow<SafeAreaPatcherWindow>("Safe Area Patcher");
            window.minSize = new Vector2(720f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            treeViewState ??= new UnityEditor.IMGUI.Controls.TreeViewState<EntityId>();
            _treeView = new SafeAreaHierarchyTreeView(treeViewState);
            _treeView.StateChanged += RefreshView;
            EditorApplication.hierarchyChanged += HandleHierarchyChanged;
            EditorSceneManager.activeSceneChangedInEditMode += HandleActiveSceneChanged;
            AssemblyReloadEvents.beforeAssemblyReload += CaptureTreeViewState;
            RefreshFromScene(true);
        }

        private void OnDisable()
        {
            CaptureTreeViewState();
            if (_treeView != null)
                _treeView.StateChanged -= RefreshView;
            EditorApplication.hierarchyChanged -= HandleHierarchyChanged;
            EditorSceneManager.activeSceneChangedInEditMode -= HandleActiveSceneChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= CaptureTreeViewState;
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // ----- Header -----
            var toolbar = new Toolbar();
            _sceneNameLabel = new Label();
            _sceneNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _sceneNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            toolbar.Add(_sceneNameLabel);
            var toolbarSpace = new VisualElement();
            toolbarSpace.style.flexGrow = 1f;
            toolbar.Add(toolbarSpace);
            toolbar.Add(new ToolbarButton(() => _treeView.ExpandAll()) { text = "Expand All" });
            toolbar.Add(new ToolbarButton(() => _treeView.CollapseAll()) { text = "Collapse All" });
            toolbar.Add(new ToolbarButton(() => RefreshFromScene(true)) { text = "Reset from Scene" });
            root.Add(toolbar);

            root.Add(new HelpBox(
                "The left checkbox controls SafeArea. Runtime Ignore controls the SafeAreaIgnore marker used only by runtime patching. The Scene is not modified until Apply Changes is pressed.",
                HelpBoxMessageType.Info));

            _noSceneHelpBox = new HelpBox(
                "Open a Scene before configuring Safe Area components.",
                HelpBoxMessageType.Info);
            root.Add(_noSceneHelpBox);

            // ----- Body -----
            _sceneContent = new VisualElement();
            _sceneContent.style.flexGrow = 1f;
            _sceneContent.Add(_treeView.CreateView());

            // ----- Footer -----
            _pendingLabel = new Label();
            _pendingLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _pendingLabel.style.marginTop = 4f;
            _sceneContent.Add(_pendingLabel);

            _applyButton = new Button(() =>
            {
                var counts = CountPendingChanges();
                ApplyChanges(counts.Add, counts.Remove);
            })
            {
                text = "Apply Changes"
            };
            _applyButton.style.height = 30f;
            _sceneContent.Add(_applyButton);
            root.Add(_sceneContent);

            RefreshView();
        }

        private void CaptureTreeViewState()
        {
            _treeView?.CaptureViewState();
        }

        private void RefreshView()
        {
            if (_sceneContent == null)
                return;

            _sceneNameLabel.text = _scene.IsValid() ? _scene.name : "No Active Scene";

            var hasScene = _scene.IsValid() && _scene.isLoaded;
            _noSceneHelpBox.style.display = hasScene ? DisplayStyle.None : DisplayStyle.Flex;
            _sceneContent.style.display = hasScene ? DisplayStyle.Flex : DisplayStyle.None;
            if (!hasScene)
                return;

            var counts = CountPendingChanges();
            _pendingLabel.text =
                $"Pending SafeArea: Add {counts.Add}, Remove {counts.Remove}  |  Runtime Ignore: Add {counts.AddIgnore}, Remove {counts.RemoveIgnore}  |  Invalid {counts.Excluded}";
            _applyButton.SetEnabled(
                counts.Add != 0 || counts.Remove != 0 || counts.AddIgnore != 0 || counts.RemoveIgnore != 0);
        }

        private (int Add, int Remove, int AddIgnore, int RemoveIgnore, int Excluded) CountPendingChanges()
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
            return (addCount, removeCount, addIgnoreCount, removeIgnoreCount, excludedCount);
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
            RefreshView();
        }
    }
}
