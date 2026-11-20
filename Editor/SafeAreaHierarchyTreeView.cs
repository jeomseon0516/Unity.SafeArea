using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Jeomseon.Unity.SafeArea.Editor
{
    /// <summary>
    /// Safe Area Patcher 창의 Hierarchy 트리. Scene 데이터와 체크 상태는 이 객체가 보관하고,
    /// UI Toolkit <see cref="TreeView"/>는 <see cref="CreateView"/>에서 창의 CreateGUI 시점에만 만든다.
    /// 펼침·선택·스크롤 상태는 창이 직렬화하는 <see cref="UnityEditor.IMGUI.Controls.TreeViewState{TIdentifier}"/>에 EntityId로 보관해
    /// Scene 재구성이나 도메인 리로드 후에도 복원한다.
    /// </summary>
    internal sealed class SafeAreaHierarchyTreeView
    {
        private const float RowHeight = 22f;
        private const float ToggleWidth = 18f;
        private const float TargetToggleSpace = 20f;
        private const float IgnoreLabelWidth = 95f;
        private const float StatusMaxWidth = 250f;
        private const float Gap = 8f;
        private const int MiniFontSize = 9;

        private readonly Dictionary<EntityId, bool> _targetStates = new();
        private readonly Dictionary<EntityId, bool> _runtimeIgnoreStates = new();
        private readonly Dictionary<EntityId, CanvasEntry> _canvasEntries = new();
        private readonly Dictionary<int, EntityId> _entityIdsByItemId = new();
        private readonly Dictionary<EntityId, int> _itemIdsByEntityId = new();
        private readonly UnityEditor.IMGUI.Controls.TreeViewState<EntityId> _state;
        private List<TreeViewItemData<HierarchyItem>> _rootItems = new();
        private Scene _scene;
        private SafeAreaPatchValidator _validator;
        private TreeView _view;

        public SafeAreaHierarchyTreeView(UnityEditor.IMGUI.Controls.TreeViewState<EntityId> state)
        {
            _state = state;
        }

        public event Action StateChanged;

        public IReadOnlyDictionary<EntityId, CanvasEntry> CanvasEntries => _canvasEntries;

        public void SetScene(Scene scene, SafeAreaPatchValidator validator, bool resetTargets)
        {
            CaptureViewState();
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

        public void ExpandAll()
        {
            _view?.ExpandAll();
            CaptureViewState();
        }

        public void CollapseAll()
        {
            _view?.CollapseAll();
            CaptureViewState();
        }

        /// <summary>창의 CreateGUI에서 호출한다. 호출할 때마다 새 TreeView를 만들어 현재 데이터를 연결한다.</summary>
        public TreeView CreateView()
        {
            _view = new TreeView
            {
                fixedItemHeight = RowHeight,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                selectionType = SelectionType.Multiple,
                makeItem = MakeRow,
                bindItem = BindRow
            };
            _view.style.flexGrow = 1f;
            ApplyItemsToView();
            return _view;
        }

        /// <summary>현재 TreeView의 펼침·선택·스크롤 상태를 직렬화 상태에 EntityId 기준으로 기록한다.</summary>
        public void CaptureViewState()
        {
            if (_view == null || _state == null)
                return;

            _state.expandedIDs.Clear();
            foreach (var pair in _entityIdsByItemId)
            {
                if (_view.IsExpanded(pair.Key))
                    _state.expandedIDs.Add(pair.Value);
            }

            _state.selectedIDs.Clear();
            foreach (var index in _view.selectedIndices)
            {
                if (_entityIdsByItemId.TryGetValue(_view.GetIdForIndex(index), out var entityId))
                    _state.selectedIDs.Add(entityId);
            }

            var scrollView = _view.Q<ScrollView>();
            if (scrollView != null)
                _state.scrollPos = scrollView.scrollOffset;
        }

        private void Reload()
        {
            _canvasEntries.Clear();
            _entityIdsByItemId.Clear();
            _itemIdsByEntityId.Clear();
            _rootItems = new List<TreeViewItemData<HierarchyItem>>();

            if (_scene.IsValid() && _scene.isLoaded && _validator != null)
            {
                var nextItemId = 0;
                foreach (var rootObject in _scene.GetRootGameObjects())
                    _rootItems.Add(BuildItem(rootObject, ref nextItemId));
            }

            ApplyItemsToView();
        }

        private void ApplyItemsToView()
        {
            if (_view == null)
                return;

            _view.SetRootItems(_rootItems);
            _view.Rebuild();
            _view.CollapseAll();

            if (_state == null)
                return;

            foreach (var entityId in _state.expandedIDs)
            {
                if (_itemIdsByEntityId.TryGetValue(entityId, out var itemId))
                    _view.ExpandItem(itemId);
            }

            var selectedItemIds = _state.selectedIDs
                .Where(_itemIdsByEntityId.ContainsKey)
                .Select(entityId => _itemIdsByEntityId[entityId])
                .ToList();
            _view.SetSelectionById(selectedItemIds);

            var scrollPosition = _state.scrollPos;
            _view.schedule.Execute(() =>
            {
                var scrollView = _view?.Q<ScrollView>();
                if (scrollView != null)
                    scrollView.scrollOffset = scrollPosition;
            });
        }

        private TreeViewItemData<HierarchyItem> BuildItem(GameObject gameObject, ref int nextItemId)
        {
            var entityId = gameObject.GetEntityId();
            var itemId = nextItemId++;
            _entityIdsByItemId[itemId] = entityId;
            _itemIdsByEntityId[entityId] = itemId;

            var icon = EditorGUIUtility.ObjectContent(gameObject, typeof(GameObject)).image as Texture2D;

            if (gameObject.TryGetComponent<Canvas>(out var canvas))
            {
                var validation = _validator.Validate(canvas);
                _canvasEntries[entityId] = new CanvasEntry(entityId, canvas, validation);
                if (!_targetStates.ContainsKey(entityId))
                    _targetStates[entityId] = validation.IsApplied;
                if (!_runtimeIgnoreStates.ContainsKey(entityId))
                    _runtimeIgnoreStates[entityId] = canvas.TryGetComponent<SafeAreaIgnore>(out _);
            }

            var children = new List<TreeViewItemData<HierarchyItem>>();
            foreach (Transform child in gameObject.transform)
                children.Add(BuildItem(child.gameObject, ref nextItemId));

            return new TreeViewItemData<HierarchyItem>(
                itemId,
                new HierarchyItem(entityId, gameObject.name, icon),
                children);
        }

        private VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexGrow = 1f;
            row.style.height = RowHeight;
            row.style.paddingRight = 6f;
            row.style.overflow = Overflow.Hidden;

            var targetToggle = new Toggle { name = "target-toggle" };
            targetToggle.style.width = ToggleWidth;
            targetToggle.style.marginLeft = 0f;
            targetToggle.style.marginRight = TargetToggleSpace - ToggleWidth;
            targetToggle.style.flexShrink = 0f;
            targetToggle.RegisterValueChangedCallback(evt =>
            {
                if (targetToggle.userData is not EntityId entityId)
                    return;

                _targetStates[entityId] = evt.newValue;
                StateChanged?.Invoke();
            });
            row.Add(targetToggle);

            var targetSpacer = new VisualElement { name = "target-spacer" };
            targetSpacer.style.width = TargetToggleSpace;
            targetSpacer.style.flexShrink = 0f;
            row.Add(targetSpacer);

            var icon = new Image { name = "icon", scaleMode = ScaleMode.ScaleToFit };
            icon.style.width = 16f;
            icon.style.height = 16f;
            icon.style.flexShrink = 0f;
            icon.style.marginRight = 2f;
            row.Add(icon);

            var nameLabel = new Label { name = "name" };
            nameLabel.style.flexGrow = 1f;
            nameLabel.style.flexShrink = 1f;
            nameLabel.style.minWidth = 0f;
            nameLabel.style.overflow = Overflow.Hidden;
            nameLabel.style.whiteSpace = WhiteSpace.NoWrap;
            row.Add(nameLabel);

            // 오른쪽 묶음은 행 오른쪽 끝에서부터 배치하고, 행이 좁으면 Runtime Ignore 묶음이 먼저
            // 숨겨지도록 한다(상태 메시지는 최대 250px까지 잘려서 표시).
            var ignoreCluster = new VisualElement { name = "ignore-cluster" };
            ignoreCluster.style.flexDirection = FlexDirection.Row;
            ignoreCluster.style.alignItems = Align.Center;
            ignoreCluster.style.flexShrink = 0f;
            ignoreCluster.style.marginLeft = Gap;

            var ignoreToggle = new Toggle { name = "ignore-toggle" };
            ignoreToggle.style.width = ToggleWidth;
            ignoreToggle.style.marginLeft = 0f;
            ignoreToggle.style.marginRight = 2f;
            ignoreToggle.RegisterValueChangedCallback(evt =>
            {
                if (ignoreToggle.userData is not EntityId entityId)
                    return;

                _runtimeIgnoreStates[entityId] = evt.newValue;
                StateChanged?.Invoke();
            });
            ignoreCluster.Add(ignoreToggle);

            var ignoreLabel = new Label("Runtime Ignore");
            ignoreLabel.style.width = IgnoreLabelWidth;
            ignoreLabel.style.fontSize = MiniFontSize;
            ignoreCluster.Add(ignoreLabel);
            row.Add(ignoreCluster);

            var statusLabel = new Label { name = "status" };
            statusLabel.style.width = StatusMaxWidth;
            statusLabel.style.flexShrink = 1f;
            statusLabel.style.minWidth = 0f;
            statusLabel.style.marginLeft = Gap;
            statusLabel.style.overflow = Overflow.Hidden;
            statusLabel.style.whiteSpace = WhiteSpace.NoWrap;
            statusLabel.style.fontSize = MiniFontSize;
            row.Add(statusLabel);

            row.RegisterCallback<GeometryChangedEvent>(_ => UpdateIgnoreClusterVisibility(row));
            return row;
        }

        private void BindRow(VisualElement row, int index)
        {
            var item = _view.GetItemDataForIndex<HierarchyItem>(index);
            var targetToggle = row.Q<Toggle>("target-toggle");
            var targetSpacer = row.Q<VisualElement>("target-spacer");
            var ignoreCluster = row.Q<VisualElement>("ignore-cluster");
            var ignoreToggle = row.Q<Toggle>("ignore-toggle");
            var statusLabel = row.Q<Label>("status");

            row.Q<Image>("icon").image = item.Icon;
            row.Q<Label>("name").text = item.Name;

            var isCanvas = _canvasEntries.TryGetValue(item.EntityId, out var entry);
            targetToggle.userData = isCanvas ? item.EntityId : null;
            ignoreToggle.userData = isCanvas ? item.EntityId : null;
            targetToggle.style.display = isCanvas ? DisplayStyle.Flex : DisplayStyle.None;
            targetSpacer.style.display = isCanvas ? DisplayStyle.None : DisplayStyle.Flex;
            statusLabel.style.display = isCanvas ? DisplayStyle.Flex : DisplayStyle.None;
            ignoreCluster.userData = isCanvas;

            if (isCanvas)
            {
                targetToggle.SetValueWithoutNotify(GetTargetState(entry.EntityId));
                ignoreToggle.SetValueWithoutNotify(GetRuntimeIgnoreState(entry.EntityId));
                // 패치할 수 없는 Canvas는 Runtime Ignore도 의미가 없으므로 두 토글을 함께 비활성화한다.
                targetToggle.SetEnabled(entry.Validation.CanChange);
                ignoreToggle.SetEnabled(entry.Validation.CanChange);
                statusLabel.text = entry.Validation.Message;
            }

            UpdateIgnoreClusterVisibility(row);
        }

        private static void UpdateIgnoreClusterVisibility(VisualElement row)
        {
            var ignoreCluster = row.Q<VisualElement>("ignore-cluster");
            if (ignoreCluster.userData is not true)
            {
                ignoreCluster.style.display = DisplayStyle.None;
                return;
            }

            // IMGUI 구현과 같이, 대상 토글 오른쪽에 상태 메시지와 Runtime Ignore 묶음이 겹치지 않고
            // 들어갈 공간이 있을 때만 Runtime Ignore 묶음을 표시한다.
            var targetToggle = row.Q<Toggle>("target-toggle");
            var rowWidth = row.layout.width;
            if (float.IsNaN(rowWidth) || rowWidth <= 0f)
            {
                ignoreCluster.style.display = DisplayStyle.Flex;
                return;
            }

            var targetToggleRight = targetToggle.layout.xMax;
            var rowRight = rowWidth - 6f;
            var statusWidth = Mathf.Clamp(rowRight - targetToggleRight - Gap, 0f, StatusMaxWidth);
            var ignoreClusterLeft = rowRight - statusWidth - Gap - IgnoreLabelWidth - ToggleWidth - 2f;
            var visible = ignoreClusterLeft > targetToggleRight + Gap;
            ignoreCluster.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private readonly struct HierarchyItem
        {
            public HierarchyItem(EntityId entityId, string name, Texture2D icon)
            {
                EntityId = entityId;
                Name = name;
                Icon = icon;
            }

            public EntityId EntityId { get; }
            public string Name { get; }
            public Texture2D Icon { get; }
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
