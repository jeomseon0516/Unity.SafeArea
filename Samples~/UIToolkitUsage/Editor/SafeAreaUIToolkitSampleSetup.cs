using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Jeomseon.Unity.SafeArea.UIToolkit;

namespace Jeomseon.Samples.SafeArea.UIToolkit.Editor
{
    // UIDocument/PanelSettings는 native 모듈 타입이라 손으로 .unity YAML을 작성하면 classID를
    // 잘못 추측해 Missing Script로 조용히 깨질 위험이 크다. 대신 Unity 자체 API로 생성해 Unity의
    // 직렬화 결과를 그대로 커밋한다(Jeomseon.Unity.Localization의 LocalizationSampleTableSetup과
    // 같은 패턴).
    internal static class SafeAreaUIToolkitSampleSetup
    {
        private const string SceneFileName = "SafeAreaUIToolkitSample.unity";
        private const string PanelSettingsFileName = "PanelSettings.asset";
        private const string UxmlFileName = "SafeAreaUIToolkitSample.uxml";

        [MenuItem("Jeomseon/Safe Area/Setup UI Toolkit Sample")]
        private static void Setup()
        {
            string sampleFolder = GetSampleFolder();
            string scenePath = $"{sampleFolder}/{SceneFileName}";

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                Debug.Log($"[PASS] '{scenePath}' already exists. Delete it first if you want to " +
                    $"regenerate. / '{scenePath}'가 이미 있습니다. 다시 생성하려면 먼저 삭제해 주세요.");
                return;
            }

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{sampleFolder}/{UxmlFileName}");
            if (uxml == null)
            {
                Debug.LogError($"[FAIL] Could not find '{UxmlFileName}' next to this script. / " +
                    $"이 스크립트 옆에서 '{UxmlFileName}'을 찾지 못했습니다.");
                return;
            }

            // PanelSettings 생성보다 NewScene을 먼저 호출한다. 순서가 반대이면, 아직 아무 오브젝트에도
            // 물려있지 않은(C# 지역 변수만 참조하는) 새 PanelSettings가 NewScene(..., Single)의
            // "사용되지 않는 에셋 언로드" 과정에서 함께 파괴되는(fake-null) 버그가 있었다.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            PanelSettings panelSettings = CreatePanelSettings(sampleFolder);
            if (panelSettings == null)
            {
                Debug.LogError("[FAIL] PanelSettings 생성/조회에 실패해 중단합니다. 위 로그를 확인해 주세요.");
                return;
            }

            var go = new GameObject("Safe Area UI Toolkit Sample");
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = uxml;

            ConfigureRoot(go.AddComponent<SafeAreaVisualElementRoot>(),
                targetElementName: "safe-area-panel",
                useLeft: true, useRight: true, useTop: true, useBottom: true);

            ConfigurePadding(go.AddComponent<SafeAreaVisualElementPadding>(),
                targetElementName: "header",
                useLeft: false, useRight: false, useTop: true, useBottom: false,
                basePaddingLeft: 16, basePaddingRight: 16, basePaddingTop: 8, basePaddingBottom: 8);

            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[PASS] Created '{scenePath}'. Open it, select a notched device in the Game " +
                $"View or Device Simulator, and enter Play Mode. / '{scenePath}'를 생성했습니다. " +
                $"열어서 Game View나 Device Simulator에서 노치 있는 디바이스를 선택한 뒤 Play Mode로 " +
                $"확인해 주세요.");
        }

        private static PanelSettings CreatePanelSettings(string sampleFolder)
        {
            string path = $"{sampleFolder}/{PanelSettingsFileName}";
            PanelSettings existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (existing != null)
                return existing;

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;

            // 이 Sample은 VisualElement/Label만 쓰고 테마가 필요한 내장 컨트롤(Button 등)이
            // 없으므로 테마를 생략하고, "테마 없음" 콘솔 경고만 끈다. themeStyleSheet에는 public
            // setter가 없어(읽기 전용 프로퍼티) SerializedObject로 private 필드를 직접 설정한다.
            var so = new SerializedObject(settings);
            so.FindProperty("m_DisableNoThemeWarning").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static void ConfigureRoot(
            SafeAreaVisualElementRoot root,
            string targetElementName,
            bool useLeft, bool useRight, bool useTop, bool useBottom)
        {
            var so = new SerializedObject(root);
            so.FindProperty("targetElementName").stringValue = targetElementName;
            so.FindProperty("useLeft").boolValue = useLeft;
            so.FindProperty("useRight").boolValue = useRight;
            so.FindProperty("useTop").boolValue = useTop;
            so.FindProperty("useBottom").boolValue = useBottom;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePadding(
            SafeAreaVisualElementPadding padding,
            string targetElementName,
            bool useLeft, bool useRight, bool useTop, bool useBottom,
            float basePaddingLeft, float basePaddingRight, float basePaddingTop, float basePaddingBottom)
        {
            var so = new SerializedObject(padding);
            so.FindProperty("targetElementName").stringValue = targetElementName;
            so.FindProperty("useLeft").boolValue = useLeft;
            so.FindProperty("useRight").boolValue = useRight;
            so.FindProperty("useTop").boolValue = useTop;
            so.FindProperty("useBottom").boolValue = useBottom;
            so.FindProperty("basePaddingLeft").floatValue = basePaddingLeft;
            so.FindProperty("basePaddingRight").floatValue = basePaddingRight;
            so.FindProperty("basePaddingTop").floatValue = basePaddingTop;
            so.FindProperty("basePaddingBottom").floatValue = basePaddingBottom;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string GetSampleFolder([CallerFilePath] string sourceFilePath = "")
        {
            string editorFolder = Path.GetDirectoryName(sourceFilePath);
            string sampleFolder = Path.GetDirectoryName(editorFolder);
            return FileUtil.GetProjectRelativePath(sampleFolder);
        }
    }
}
