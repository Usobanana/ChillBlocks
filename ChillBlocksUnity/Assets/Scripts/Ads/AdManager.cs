// AdManager.cs — Google AdMob 広告管理（シングルトン）
// SortGems (SortGemsUnity/Assets/Scripts/Ads/AdManager.cs) からの移植版。
// design-spec.html GS4 の移植マッピング表に従い、以下だけ変更している:
//   - OnStageCleared() → OnGameOver()（ステージクリアではなくGame Overのたびに呼ぶ）
//   - 初回Game Over免除ガード（hasShownFirstGameOver）を新規追加
//   - Continue（リワード）1プレイ1回制限（RewardedContinueUsedThisRun）を新規追加
//   - Editor/非モバイルのダミー広告UIは、SortGemsのuGUI版ではなくUI Toolkit版に置き換え
//     （ChillBlocksはuGUI Canvasを持たない構成のため。実機フィードバックで「SortGemsのように
//     バナー/全画面広告のモックを見せてほしい」との要望があり有効化）。
// ※ AdMob SDK for Unity をインポート後に有効化してください
// Unity Package: https://github.com/googleads/googleads-mobile-unity/releases

using System;
using UnityEngine;
using UnityEngine.UIElements;

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using GoogleMobileAds.Api;
#endif

namespace ChillBlocks.Ads
{
    /// <summary>
    /// Google AdMob の初期化・広告ロード・表示を一元管理するシングルトン。
    /// モバイル以外のプラットフォームやエディタ上では、自動的にモック（ダミー広告）として機能します。
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        [Header("AdMob Settings")]
        [Tooltip("Androidバナー広告ユニットID")]
        [SerializeField] private string _androidBannerId = "ca-app-pub-3940256099942544/6300978111";

        [Tooltip("Androidインタースティシャル広告ユニットID")]
        [SerializeField] private string _androidInterstitialId = "ca-app-pub-3940256099942544/1033173712";

        [Tooltip("iOSバナー広告ユニットID")]
        [SerializeField] private string _iosBannerId = "ca-app-pub-3940256099942544/2934735716";

        [Tooltip("iOSインタースティシャル広告ユニットID")]
        [SerializeField] private string _iosInterstitialId = "ca-app-pub-3940256099942544/4411468910";

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        private BannerView _bannerView;
        private InterstitialAd _interstitialAd;
#endif

        // ---- Editor/非モバイルのダミー広告UI（UI Toolkit）。ScreenManagerの画面切替に巻き込まれないよう、
        // 専用のオーバーレイUIDocument（[AdOverlay]、CreateGameScene.csで生成）に乗せる。 ----
        [Header("Editor/Mock専用（実機ビルドでは未使用）")]
        [SerializeField] private UIDocument _overlayDocument;

        // ---- 状態 ----
        private bool _isInterstitialReady;
        private Action _onInterstitialClosed;
#if !(UNITY_ANDROID || UNITY_IOS) || UNITY_EDITOR
        private VisualElement _dummyBanner;
        private VisualElement _dummyInterstitial;
#endif




        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("[AdManager] AdMob initialized");
                LoadInterstitial();
            });
#else
            Debug.Log("[AdManager] Non-Mobile platform: AdMob initialization skipped (Mock active)");
            _isInterstitialReady = true;
#endif
        }

        // ---- バナー広告 ----

        public void ShowBanner()
        {
            if (Core.SettingsManager.Instance != null && Core.SettingsManager.Instance.IsAdsRemoved)
            {
                HideBanner();
                return;
            }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            string adUnitId = Application.platform == RuntimePlatform.IPhonePlayer
                ? _iosBannerId : _androidBannerId;

            if (_bannerView != null)
            {
                _bannerView.Destroy();
            }

            _bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);
            _bannerView.LoadAd(new AdRequest());
#else
            Debug.Log("[AdManager] ShowBanner (Mocked)");
            CreateDummyBanner();
#endif
        }

        public void HideBanner()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_bannerView != null)
            {
                _bannerView.Hide();
            }
#else
            Debug.Log("[AdManager] HideBanner (Mocked)");
            _dummyBanner?.RemoveFromHierarchy();
            _dummyBanner = null;
#endif
        }

#if !(UNITY_ANDROID || UNITY_IOS) || UNITY_EDITOR
        private VisualElement GetOverlayRoot()
        {
            if (_overlayDocument != null) return _overlayDocument.rootVisualElement;
            var doc = FindFirstObjectByType<UIDocument>();
            return doc != null ? doc.rootVisualElement : null;
        }

        private void CreateDummyBanner()
        {
            if (_dummyBanner != null) return; // 既に表示中

            var root = GetOverlayRoot();
            if (root == null) return;

            _dummyBanner = new VisualElement();
            _dummyBanner.style.position = Position.Absolute;
            _dummyBanner.style.left = 0;
            _dummyBanner.style.right = 0;
            _dummyBanner.style.bottom = 0;
            _dummyBanner.style.height = 100;
            _dummyBanner.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            _dummyBanner.style.alignItems = Align.Center;
            _dummyBanner.style.justifyContent = Justify.Center;

            var label = new Label("[ Google AdMob Banner Ad (Mocked) ]")
            {
                style =
                {
                    color = Color.white,
                    fontSize = 24
                }
            };
            _dummyBanner.Add(label);

            root.Add(_dummyBanner);
        }
#endif

        // ---- インタースティシャル広告 ----

        private void LoadInterstitial()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
            _isInterstitialReady = false;

            string adUnitId = Application.platform == RuntimePlatform.IPhonePlayer
                ? _iosInterstitialId : _androidInterstitialId;

            InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null) { Debug.LogWarning($"[AdManager] Interstitial load failed: {error}"); return; }
                _interstitialAd = ad;
                _interstitialAd.OnAdFullScreenContentClosed += () =>
                {
                    TriggerInterstitialClosed();
                    LoadInterstitial();
                };
                _interstitialAd.OnAdFullScreenContentFailedToShow += (adError) =>
                {
                    Debug.LogWarning($"[AdManager] Interstitial failed to show: {adError.GetMessage()}");
                    TriggerInterstitialClosed();
                    LoadInterstitial();
                };
                _isInterstitialReady = true;
                Debug.Log("[AdManager] Interstitial loaded successfully");
            });
#endif
        }

        /// <summary>
        /// Game Overの瞬間に呼び出す。
        /// 毎回必ず全画面広告を表示し、閉じられた（あるいは準備できていなくてスキップされた）時点で onAdClosed を呼び出す。
        /// </summary>
        public void ShowInterstitial(Action onAdClosed)
        {
            if (Core.SettingsManager.Instance != null && Core.SettingsManager.Instance.IsAdsRemoved)
            {
                onAdClosed?.Invoke();
                return;
            }

            _onInterstitialClosed = onAdClosed;

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (!_isInterstitialReady || _interstitialAd == null)
            {
                Debug.Log("[AdManager] Interstitial not ready, calling callback immediately");
                TriggerInterstitialClosed();
                return;
            }

            _interstitialAd.Show();
            _isInterstitialReady = false;
#else
            Debug.Log("[AdManager] ShowInterstitial (Mocked)");
            CreateDummyInterstitial(() =>
            {
                TriggerInterstitialClosed();
            });
#endif
        }

        private void TriggerInterstitialClosed()
        {
            _onInterstitialClosed?.Invoke();
            _onInterstitialClosed = null;
        }

#if !(UNITY_ANDROID || UNITY_IOS) || UNITY_EDITOR
        private void CreateDummyInterstitial(Action onClose)
        {
            var root = GetOverlayRoot();
            if (root == null)
            {
                onClose?.Invoke();
                return;
            }

            _dummyInterstitial?.RemoveFromHierarchy();

            _dummyInterstitial = new VisualElement();
            _dummyInterstitial.style.position = Position.Absolute;
            _dummyInterstitial.style.left = 0;
            _dummyInterstitial.style.right = 0;
            _dummyInterstitial.style.top = 0;
            _dummyInterstitial.style.bottom = 0;
            _dummyInterstitial.style.backgroundColor = new Color(0f, 0f, 0f, 0.95f);
            _dummyInterstitial.style.alignItems = Align.Center;
            _dummyInterstitial.style.justifyContent = Justify.Center;

            var label = new Label("[ Google AdMob Interstitial Ad (Mocked) ]\n\nThis is a full-screen ad simulation.\nClick the button to continue.")
            {
                style =
                {
                    color = Color.white,
                    fontSize = 24,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    whiteSpace = WhiteSpace.Normal,
                    maxWidth = 700
                }
            };
            _dummyInterstitial.Add(label);

            var closeButton = new Button(() =>
            {
                _dummyInterstitial?.RemoveFromHierarchy();
                _dummyInterstitial = null;
                onClose?.Invoke();
            })
            {
                text = "CLOSE AD",
                style =
                {
                    marginTop = 24,
                    width = 200,
                    height = 60,
                    backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f),
                    color = Color.white
                }
            };
            _dummyInterstitial.Add(closeButton);

            root.Add(_dummyInterstitial);
        }
#endif

        public bool IsInterstitialReady => _isInterstitialReady;
    }
}
