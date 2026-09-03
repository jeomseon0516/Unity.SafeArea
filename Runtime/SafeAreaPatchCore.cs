using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jeomseon.Unity.SafeArea
{
    /// <summary>
    /// Canvas 아래를 공식 uGUI SafeArea 컨테이너로 감싸는 공통 로직.
    /// Runtime / Editor 양쪽에서 재사용.
    /// </summary>
    internal static class SafeAreaPatchCore
    {
        /// <summary>
        /// 주어진 Canvas를 Safe Area 컨테이너로 감싸고 공식 <see cref="UnityEngine.UI.SafeArea"/>
        /// 컴포넌트를 부착한다. 이미 패치되어 있으면 기존 컨테이너를 반환한다.
        /// settings를 생략하면 SafeAreaSettings.Resolve()로 얻은 기본 정책을 사용한다.
        /// </summary>
        /* TODO(P1-02, editor-settings): 자식 재배치 정책(현재는 항상 전체 이동)을 SafeAreaSettings로
         * 추가 노출하고, Custom Inspector에서 적용 대상을 미리 확인하고 씬 변경 전 Undo와 preview를
         * 지원합니다.
         */
        public static GameObject EnsureSafeAreaContainer(Canvas canvas, SafeAreaSettings settings = null)
        {
            if (canvas == null)
                return null;

            var canvasTransform = canvas.transform as RectTransform;
            if (canvasTransform == null)
                return null;

            var effectiveSettings = settings != null ? settings : SafeAreaSettings.Resolve();

            if (effectiveSettings.SkipWorldSpaceCanvases && canvas.renderMode == RenderMode.WorldSpace)
                return null;

            var existingSafeArea = FindOwnedSafeAreas(canvas);
            if (existingSafeArea != null)
                return existingSafeArea.gameObject;

            for (var i = 0; i < canvasTransform.childCount; i++)
            {
                var child = canvasTransform.GetChild(i);
                if (child.name != effectiveSettings.RootName)
                    continue;

                var reusableSafeArea = child.gameObject.AddComponent<UnityEngine.UI.SafeArea>();
                ConfigureSafeArea(reusableSafeArea);
                return child.gameObject;
            }

            // 새 Safe Area 컨테이너 생성
            var safeRootGO = new GameObject(effectiveSettings.RootName);
            var safeRootRect = safeRootGO.AddComponent<RectTransform>();

            safeRootRect.SetParent(canvasTransform, false);
            safeRootRect.anchorMin = Vector2.zero;
            safeRootRect.anchorMax = Vector2.one;
            safeRootRect.pivot = new Vector2(0.5f, 0.5f);
            safeRootRect.offsetMin = Vector2.zero;
            safeRootRect.offsetMax = Vector2.zero;
            safeRootRect.localScale = Vector3.one;
            safeRootRect.localPosition = Vector3.zero;

            // 기존 Canvas 직속 자식들을 Safe Area 컨테이너 아래로 이동
            var children = new List<Transform>();
            for (int i = 0; i < canvasTransform.childCount; i++)
            {
                var child = canvasTransform.GetChild(i);
                if (child == safeRootRect.transform)
                    continue;
                children.Add(child);
            }

            foreach (var child in children)
            {
                child.SetParent(safeRootRect, true);
            }

            var safeArea = safeRootGO.AddComponent<UnityEngine.UI.SafeArea>();
            ConfigureSafeArea(safeArea);

            return safeRootGO;
        }

        internal static UnityEngine.UI.SafeArea FindOwnedSafeAreas(Canvas canvas)
        {
            if (canvas == null)
                return null;

            var safeAreas = canvas.GetComponentsInChildren<UnityEngine.UI.SafeArea>(true);
            foreach (var safeArea in safeAreas)
            {
                if (safeArea.GetComponentInParent<Canvas>(true) == canvas)
                    return safeArea;
            }

            return null;
        }

        private static void ConfigureSafeArea(UnityEngine.UI.SafeArea safeArea)
        {
            safeArea.ReferenceOrientation = ScreenOrientation.Portrait;
            safeArea.Edges = UnityEngine.UI.SafeArea.SafeAreaMode.Top |
                             UnityEngine.UI.SafeArea.SafeAreaMode.Right |
                             UnityEngine.UI.SafeArea.SafeAreaMode.Bottom |
                             UnityEngine.UI.SafeArea.SafeAreaMode.Left;
        }
    }
}
