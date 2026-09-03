using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jeomseon.Unity.SafeArea.Editor
{
    internal sealed class SafeAreaHierarchyTreeView : TreeView<EntityId>
    {
        private readonly Dictionary<EntityId, bool> _targetStates = new();
        private readonly Dictionary<EntityId, bool> _runtimeIgnoreStates = new();
        private readonly Dictionary<EntityId, CanvasEntry> _canvasEntries = new();
        private Scene _scene;
        private SafeAreaPatchValidator _validator;

        public SafeAreaHierarchyTreeView(TreeViewState<EntityId> state) : base(state)
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            rowHeight = 22f;
            extraSpaceBeforeIconAndLabel = 20f;
        }

        public event Action StateChanged;

        public IReadOnlyDictionary<EntityId, CanvasEntry> CanvasEntries => _canvasEntries;

        public void SetScene(Scene scene, SafeAreaPatchValidator validator, bool resetTargets)
        {
            _scene = scene;
            _validator = validator;
            if (resetTargets)
            {
                _targetStates.Clear();
                _runtimeIgnoreStates.Clear();
            }
            Reload();
        }

        public bool GetTargetState(EntityId entityId)
        {
            return _targetStates.TryGetValue(entityId, out var value) && value;
        }

        public bool GetRuntimeIgnoreState(EntityId entityId)
        {
            return _runtimeIgnoreStates.TryGetValue(entityId, out var value) && value;
        }

        protected override TreeViewItem<EntityId> BuildRoot()
        {
            _canvasEntries.Clear();
            var root = new TreeViewItem<EntityId>(default, -1, "Root");
            if (!_scene.IsValid() || !_scene.isLoaded || _validator == null)
                return root;

            foreach (var rootObject in _scene.GetRootGameObjects())
                root.AddChild(BuildItem(rootObject, 0));

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);

            if (!_canvasEntries.TryGetValue(args.item.id, out var entry))
                return;

            var toggleRect = args.rowRect;
            toggleRect.x += GetContentIndent(args.item);
            toggleRect.width = 18f;

            using (new EditorGUI.DisabledScope(!entry.Validation.CanChange))
            {
                EditorGUI.BeginChangeCheck();
                var targetState = EditorGUI.Toggle(toggleRect, GetTargetState(entry.EntityId));
                if (EditorGUI.EndChangeCheck())
                {
                    _targetStates[entry.EntityId] = targetState;
                    StateChanged?.Invoke();
                }
            }

            // Lay the right-hand cluster out from the row's right edge and clamp it
            // against the target toggle so nothing overlaps when the row is narrow.
            const float ignoreToggleWidth = 18f;
            const float ignoreLabelWidth = 95f;
            const float statusMaxWidth = 250f;
            const float gap = 8f;

            float rowRight = args.rowRect.xMax - 6f;
            float targetToggleRight = toggleRect.xMax;

            float statusWidth = Mathf.Clamp(rowRight - targetToggleRight - gap, 0f, statusMaxWidth);
            var statusRect = new Rect(rowRight - statusWidth, args.rowRect.y, statusWidth, args.rowRect.height);
            if (statusWidth > 40f)
                GUI.Label(statusRect, entry.Validation.Message, EditorStyles.miniLabel);

            float ignoreClusterLeft = statusRect.x - gap - ignoreLabelWidth - ignoreToggleWidth - 2f;
            if (ignoreClusterLeft > targetToggleRight + gap)
            {
                var ignoreToggleRect = new Rect(ignoreClusterLeft, args.rowRect.y + 2f, ignoreToggleWidth, 18f);
                var ignoreLabelRect = new Rect(
                    ignoreClusterLeft + ignoreToggleWidth + 2f, args.rowRect.y, ignoreLabelWidth, args.rowRect.height);

                // A Canvas that cannot take SafeArea (World Space, invalid) cannot be
                // runtime-patched either, so its Runtime Ignore marker is meaningless.
                using (new EditorGUI.DisabledScope(!entry.Validation.CanChange))
                {
                    EditorGUI.BeginChangeCheck();
                    var runtimeIgnore = EditorGUI.Toggle(ignoreToggleRect, GetRuntimeIgnoreState(entry.EntityId));
                    if (EditorGUI.EndChangeCheck())
                    {
                        _runtimeIgnoreStates[entry.EntityId] = runtimeIgnore;
                        StateChanged?.Invoke();
                    }
                }

                GUI.Label(ignoreLabelRect, "Runtime Ignore", EditorStyles.miniLabel);
            }
        }

        private TreeViewItem<EntityId> BuildItem(GameObject gameObject, int depth)
        {
            var entityId = gameObject.GetEntityId();
            var item = new TreeViewItem<EntityId>(entityId, depth, gameObject.name)
            {
                icon = EditorGUIUtility.ObjectContent(gameObject, typeof(GameObject)).image as Texture2D
            };

            if (gameObject.TryGetComponent<Canvas>(out var canvas))
            {
                var validation = _validator.Validate(canvas);
                _canvasEntries[entityId] = new CanvasEntry(entityId, canvas, validation);
                if (!_targetStates.ContainsKey(entityId))
                    _targetStates[entityId] = validation.IsApplied;
                if (!_runtimeIgnoreStates.ContainsKey(entityId))
                    _runtimeIgnoreStates[entityId] = canvas.TryGetComponent<SafeAreaIgnore>(out _);
            }

            foreach (Transform child in gameObject.transform)
                item.AddChild(BuildItem(child.gameObject, depth + 1));

            return item;
        }

        internal readonly struct CanvasEntry
        {
            public CanvasEntry(EntityId entityId, Canvas canvas, SafeAreaPatchValidation validation)
            {
                EntityId = entityId;
                Canvas = canvas;
                Validation = validation;
            }

            public EntityId EntityId { get; }
            public Canvas Canvas { get; }
            public SafeAreaPatchValidation Validation { get; }
        }
    }
}
