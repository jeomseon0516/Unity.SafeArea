using UnityEngine;

namespace Jeomseon.Unity.SafeArea.Editor
{
    internal sealed class SafeAreaIgnoreValidator
    {
        public SafeAreaIgnoreValidation Validate(SafeAreaIgnore target)
        {
            if (target == null)
                return new SafeAreaIgnoreValidation(false, "SafeAreaIgnore target is missing.");

            if (!target.TryGetComponent<Canvas>(out _))
            {
                return new SafeAreaIgnoreValidation(
                    false,
                    "SafeAreaIgnore only affects runtime patching when it is attached to the same GameObject as a Canvas.");
            }

            return new SafeAreaIgnoreValidation(true, string.Empty);
        }
    }

    internal readonly struct SafeAreaIgnoreValidation
    {
        public SafeAreaIgnoreValidation(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;
        }

        public bool IsValid { get; }
        public string Message { get; }
    }
}
