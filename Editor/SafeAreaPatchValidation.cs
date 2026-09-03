using UnityEngine.UI;

namespace Jeomseon.Unity.SafeArea.Editor
{
    internal readonly struct SafeAreaPatchValidation
    {
        internal enum Status
        {
            Available,
            Applied,
            WorldSpace,
            InvalidScene,
            MissingRectTransform,
            Ambiguous
        }

        public SafeAreaPatchValidation(
            Status status,
            string message,
            UnityEngine.UI.SafeArea existingSafeArea = null)
        {
            CurrentStatus = status;
            Message = message;
            ExistingSafeArea = existingSafeArea;
        }

        public Status CurrentStatus { get; }
        public string Message { get; }
        public UnityEngine.UI.SafeArea ExistingSafeArea { get; }
        public bool CanChange => CurrentStatus is Status.Available or Status.Applied;
        public bool IsApplied => CurrentStatus == Status.Applied;
    }
}
