using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Jeomseon.Unity.SafeArea;
using Jeomseon.Unity.SafeArea.Editor;

namespace Jeomseon.SafeArea.Tests
{
    public sealed class SafeAreaPatchCoreTests
    {
        private GameObject _canvasObject;
        private SafeAreaSettings _settings;

        [TearDown]
        public void TearDown()
        {
            if (_canvasObject != null)
                Object.DestroyImmediate(_canvasObject);
            if (_settings != null)
                Object.DestroyImmediate(_settings);
        }

        [Test]
        public void EnsureSafeAreaContainer_AddsOfficialComponentAndMovesCanvasChildren()
        {
            _canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            _canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var child = new GameObject("Child", typeof(RectTransform));
            child.transform.SetParent(_canvasObject.transform, false);

            var container = SafeAreaPatchCore.EnsureSafeAreaContainer(_canvasObject.GetComponent<Canvas>());

            var safeArea = container.GetComponent<UnityEngine.UI.SafeArea>();
            Assert.That(safeArea, Is.Not.Null);
            Assert.That(safeArea.Edges, Is.EqualTo(
                UnityEngine.UI.SafeArea.SafeAreaMode.Top |
                UnityEngine.UI.SafeArea.SafeAreaMode.Right |
                UnityEngine.UI.SafeArea.SafeAreaMode.Bottom |
                UnityEngine.UI.SafeArea.SafeAreaMode.Left));
            Assert.That(child.transform.parent, Is.EqualTo(container.transform));
        }

        [Test]
        public void EnsureSafeAreaContainer_ReusesExistingOfficialComponent()
        {
            _canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var first = SafeAreaPatchCore.EnsureSafeAreaContainer(canvas);
            var second = SafeAreaPatchCore.EnsureSafeAreaContainer(canvas);

            Assert.That(second, Is.SameAs(first));
            Assert.That(_canvasObject.GetComponentsInChildren<UnityEngine.UI.SafeArea>(true), Has.Length.EqualTo(1));
        }

        [Test]
        public void EnsureSafeAreaContainer_ReusesNamedContainerAfterComponentRemoval()
        {
            _canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var first = SafeAreaPatchCore.EnsureSafeAreaContainer(canvas);
            Object.DestroyImmediate(first.GetComponent<UnityEngine.UI.SafeArea>());

            var second = SafeAreaPatchCore.EnsureSafeAreaContainer(canvas);

            Assert.That(second, Is.SameAs(first));
            Assert.That(second.GetComponent<UnityEngine.UI.SafeArea>(), Is.Not.Null);
        }

        [Test]
        public void EnsureSafeAreaContainer_AppliesExplicitlyDespiteRuntimeIgnoreMarker()
        {
            _canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(SafeAreaIgnore));
            _canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var container = SafeAreaPatchCore.EnsureSafeAreaContainer(_canvasObject.GetComponent<Canvas>());

            Assert.That(container, Is.Not.Null);
            Assert.That(container.GetComponent<UnityEngine.UI.SafeArea>(), Is.Not.Null);
        }

        [Test]
        public void Validator_ReportsAvailableAndAppliedStates()
        {
            _canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _settings = ScriptableObject.CreateInstance<SafeAreaSettings>();
            var validator = new SafeAreaPatchValidator(_settings);

            Assert.That(
                validator.Validate(canvas).CurrentStatus,
                Is.EqualTo(SafeAreaPatchValidation.Status.Available));

            SafeAreaPatchCore.EnsureSafeAreaContainer(canvas, _settings);

            Assert.That(
                validator.Validate(canvas).CurrentStatus,
                Is.EqualTo(SafeAreaPatchValidation.Status.Applied));
        }

        [Test]
        public void Validator_AllowsIgnoredCanvasButRejectsWorldSpaceCanvas()
        {
            _canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _settings = ScriptableObject.CreateInstance<SafeAreaSettings>();
            var validator = new SafeAreaPatchValidator(_settings);

            canvas.renderMode = RenderMode.WorldSpace;
            Assert.That(
                validator.Validate(canvas).CurrentStatus,
                Is.EqualTo(SafeAreaPatchValidation.Status.WorldSpace));

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvasObject.AddComponent<SafeAreaIgnore>();
            Assert.That(
                validator.Validate(canvas).CurrentStatus,
                Is.EqualTo(SafeAreaPatchValidation.Status.Available));
        }

        [Test]
        public void Validator_RejectsMultipleOwnedSafeAreas()
        {
            _canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _settings = ScriptableObject.CreateInstance<SafeAreaSettings>();
            var validator = new SafeAreaPatchValidator(_settings);
            AddSafeAreaChild("First");
            AddSafeAreaChild("Second");

            var validation = validator.Validate(canvas);

            Assert.That(validation.CurrentStatus, Is.EqualTo(SafeAreaPatchValidation.Status.Ambiguous));
            Assert.That(validation.CanChange, Is.False);
        }

        [Test]
        public void SafeAreaIgnoreValidator_RequiresCanvasOnSameGameObject()
        {
            _canvasObject = new GameObject("Ignore", typeof(SafeAreaIgnore));
            var validator = new SafeAreaIgnoreValidator();

            Assert.That(
                validator.Validate(_canvasObject.GetComponent<SafeAreaIgnore>()).IsValid,
                Is.False);

            _canvasObject.AddComponent<Canvas>();

            Assert.That(
                validator.Validate(_canvasObject.GetComponent<SafeAreaIgnore>()).IsValid,
                Is.True);
        }

        private void AddSafeAreaChild(string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.SafeArea));
            child.transform.SetParent(_canvasObject.transform, false);
        }
    }
}
