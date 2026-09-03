using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Jeomseon.Unity.SafeArea.Editor
{
    [CustomEditor(typeof(SafeAreaIgnore))]
    [CanEditMultipleObjects]
    internal sealed class SafeAreaIgnoreEditor : UnityEditor.Editor
    {
        private readonly SafeAreaIgnoreValidator _validator = new();

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            foreach (var selectedTarget in targets)
            {
                var validation = _validator.Validate(selectedTarget as SafeAreaIgnore);
                if (!validation.IsValid)
                    root.Add(new HelpBox(validation.Message, HelpBoxMessageType.Warning));
            }

            return root;
        }
    }
}
