using UnityEngine;
using UnityEngine.UIElements;

namespace ChillBlocks.UI
{
    /// <summary>
    /// スマートフォンのノッチ（Safe Area）に対応するためのヘルパーコンポーネント。
    /// UI Toolkitのルート要素下にある「screen」クラスを持つコンテナに対して、
    /// Screen.safeAreaから計算されたパディング比率を動的に適用します。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SafeAreaHelper : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _root;
        
        private Rect _lastSafeArea;
        private Vector2 _lastScreenSize;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            _root = _uiDocument.rootVisualElement;
            
            ApplySafeArea();
        }

        private void Update()
        {
            // 実行中に画面解像度やSafe Areaが変化（画面の回転など）したかチェックし、
            // 変化があった場合のみ再適用する（毎フレーム再レイアウトする負荷を回避）。
            Rect currentSafeArea = Screen.safeArea;
            Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);

            if (currentSafeArea != _lastSafeArea || currentScreenSize != _lastScreenSize)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (_root == null) _root = _uiDocument?.rootVisualElement;
            if (_root == null) return;

            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2(Screen.width, Screen.height);

            float screenWidth = _lastScreenSize.x;
            float screenHeight = _lastScreenSize.y;

            if (screenWidth <= 0 || screenHeight <= 0) return;

            // Safe Area外のパディング比率を算出 (%)
            float padTopPct = (screenHeight - _lastSafeArea.yMax) / screenHeight * 100f;
            float padBottomPct = _lastSafeArea.yMin / screenHeight * 100f;
            float padLeftPct = _lastSafeArea.xMin / screenWidth * 100f;
            float padRightPct = (screenWidth - _lastSafeArea.xMax) / screenWidth * 100f;

            // 一番外側の「.screen」クラスを持つコンテナに適用する。
            // 背景色は画面全体に広げ、中身のUI要素だけをSafe Area内側に収めるため、マージンではなく「パディング」を使用する。
            var screens = _root.Query<VisualElement>(className: "screen").ToList();
            
            if (screens.Count == 0)
            {
                // もし.screenが見つからなければ、rootVisualElement自身に直接適用する
                ApplyPaddingToElement(_root, padTopPct, padBottomPct, padLeftPct, padRightPct);
            }
            else
            {
                foreach (var screen in screens)
                {
                    ApplyPaddingToElement(screen, padTopPct, padBottomPct, padLeftPct, padRightPct);
                }
            }
        }

        private void ApplyPaddingToElement(VisualElement element, float top, float bottom, float left, float right)
        {
            element.style.paddingTop = new Length(top, LengthUnit.Percent);
            element.style.paddingBottom = new Length(bottom, LengthUnit.Percent);
            element.style.paddingLeft = new Length(left, LengthUnit.Percent);
            element.style.paddingRight = new Length(right, LengthUnit.Percent);
        }
    }
}
