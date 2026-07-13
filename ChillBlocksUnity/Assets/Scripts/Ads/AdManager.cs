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

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        // ---- AdUnit ID（テスト用ID → リリース前に本番IDに差し替え） ----
        // iOS
        private const string IOS_BANNER_ID        = "ca-app-pub-3940256099942544/2934735716";
        private const string IOS_INTERSTITIAL_ID  = "ca-app-pub-3940256099942544/4411468910";
        private const string IOS_REWARDED_ID      = "ca-app-pub-3940256099942544/1712485313";

        // Android
        private const string AND_BANNER_ID        = "ca-app-pub-3940256099942544/6300978111";
        private const string AND_INTERSTITIAL_ID  = "ca-app-pub-3940256099942544/1033173712";
        private const string AND_REWARDED_ID      = "ca-app-pub-3940256099942544/5224354917";

        private BannerView _bannerView;
        private InterstitialAd _interstitialAd;
        private RewardedAd _rewardedAd;
#endif

        // ---- Editor/非モバイルのダミー広告UI（UI Toolkit）。ScreenManagerの画面切替に巻き込まれないよう、
        // 専用のオーバーレイUIDocument（[AdOverlay]、CreateGameScene.csで生成）に乗せる。 ----
        [Header("Editor/Mock専用（実機ビルドでは未使用）")]
        [SerializeField] private UIDocument _overlayDocument;

        // ---- 状態 ----
        private bool _isRewardedReady;
        private bool _isInterstitialReady;
        private Action _onRewardEarned;
        private Action _onInterstitialClosed;
#if !(UNITY_ANDROID || UNITY_IOS) || UNITY_EDITOR
        private VisualElement _dummyBanner;
        private VisualElement _dummyInterstitial;
#endif

        // ---- インタースティシャル表示間隔制御（GS4: Game Over 3回に1回） ----
        [SerializeField] private int _interstitialEveryNGameOvers = 3;
        private int _gameOversSinceLastInterstitial;

        // ---- GS4新規: 初回Game Over免除ガード ----
        private bool _hasShownFirstGameOver;

        // ---- GS4新規: Continue（リワード）は1プレイにつき1回まで ----
        public bool RewardedContinueUsedThisRun { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        // ---- 初期化 ----

        private void Initialize()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("[AdManager] AdMob initialized");
                LoadRewarded();
                LoadInterstitial();
            });
#else
            Debug.Log("[AdManager] Non-Mobile platform: AdMob initialization skipped (Mock active)");
            _isRewardedReady = true;
            _isInterstitialReady = true;
#endif
        }

        // ---- バナー広告 ----

        public void ShowBanner()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            string adUnitId = Application.platform == RuntimePlatform.IPhonePlayer
                ? IOS_BANNER_ID : AND_BANNER_ID;

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
                ? IOS_INTERSTITIAL_ID : AND_INTERSTITIAL_ID;

            InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null) { Debug.LogWarning($"[AdManager] Interstitial load failed: {error}"); return; }
                _interstitialAd = ad;
                _interstitialAd.OnAdFullScreenContentClosed += () =>
                {
                    _onInterstitialClosed?.Invoke();
                    _onInterstitialClosed = null;
                    LoadInterstitial();
                };
                _isInterstitialReady = true;
                Debug.Log("[AdManager] Interstitial loaded successfully");
            });
#endif
        }

        /// <summary>
        /// Game Overのたびに呼ぶ（GS4: SortGemsのOnStageCleared()を改名・移植）。
        /// 初回Game Overは必ずスキップ（Day1離脱防止）。以降はNGame Overに1回の頻度キャップ。
        /// </summary>
        public void OnGameOver()
        {
            if (!_hasShownFirstGameOver)
            {
                _hasShownFirstGameOver = true;
                return;
            }

            _gameOversSinceLastInterstitial++;
            if (_gameOversSinceLastInterstitial >= _interstitialEveryNGameOvers)
            {
                ShowInterstitial();
                _gameOversSinceLastInterstitial = 0;
            }
        }

        private void ShowInterstitial()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (!_isInterstitialReady || _interstitialAd == null)
            {
                Debug.Log("[AdManager] Interstitial not ready");
                return;
            }

            _interstitialAd.Show();
            _isInterstitialReady = false;
#else
            Debug.Log("[AdManager] ShowInterstitial (Mocked)");
            CreateDummyInterstitial();
#endif
        }

#if !(UNITY_ANDROID || UNITY_IOS) || UNITY_EDITOR
        private void CreateDummyInterstitial()
        {
            var root = GetOverlayRoot();
            if (root == null) return;

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

        // ---- リワード広告 ----

        private void LoadRewarded()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
            _isRewardedReady = false;

            string adUnitId = Application.platform == RuntimePlatform.IPhonePlayer
                ? IOS_REWARDED_ID : AND_REWARDED_ID;

            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                if (error != null) { Debug.LogWarning($"[AdManager] Rewarded load failed: {error}"); return; }
                _rewardedAd = ad;
                _rewardedAd.OnAdFullScreenContentClosed += () => { LoadRewarded(); };
                _isRewardedReady = true;
                Debug.Log("[AdManager] Rewarded loaded successfully");
            });
#endif
        }

        /// <summary>
        /// リワード広告を表示する。視聴完了で onRewardEarned が呼ばれる。
        /// 広告が準備できていない場合は onFallback（省略可）を呼ぶ。
        /// </summary>
        public void ShowRewardedAd(Action onRewardEarned, Action onFallback = null)
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (!_isRewardedReady || _rewardedAd == null)
            {
                Debug.Log("[AdManager] Rewarded not ready, calling fallback");
                onFallback?.Invoke();
                return;
            }

            _onRewardEarned = onRewardEarned;

            _rewardedAd.Show(reward =>
            {
                _onRewardEarned?.Invoke();
                _isRewardedReady = false;
            });
#else
            Debug.Log("[AdManager] ShowRewardedAd (Mocked): Automatically granting reward");
            onRewardEarned?.Invoke();
#endif
        }

        /// <summary>
        /// GS4新規: Game OverのContinueボタンから呼ぶ。1プレイにつき1回まで。
        /// 既に使用済み、または広告未準備の場合は onFallback を呼ぶ（GameOver UI側でUnavailable表示にする）。
        /// </summary>
        public void TryShowRewardedContinue(Action onRewardEarned, Action onFallback = null)
        {
            if (RewardedContinueUsedThisRun)
            {
                onFallback?.Invoke();
                return;
            }

            ShowRewardedAd(
                () =>
                {
                    RewardedContinueUsedThisRun = true;
                    onRewardEarned?.Invoke();
                },
                onFallback);
        }

        /// <summary>新しいプレイ開始時にGameManager/UI側から呼ぶ。Continue使用済みフラグをリセットする。</summary>
        public void ResetRunState()
        {
            RewardedContinueUsedThisRun = false;
        }

        public bool IsRewardedReady => _isRewardedReady;
        public bool IsInterstitialReady => _isInterstitialReady;
    }
}
