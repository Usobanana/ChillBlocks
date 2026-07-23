using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ChillBlocks.Core;
using ChillBlocks.Ads;

namespace ChillBlocks.UI
{
    /// <summary>
    /// SortGemsのScreenManagerと同じ「単一UIDocument・_root.Clear()→CloneTree()で画面を丸ごと差し替える」パターン。
    /// ChillBlocksは盤面もUI Toolkitで完結するため、SortGemsにあったuGUI Canvasの トグル処理は無い。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [Header("Screen UXML")]
        [SerializeField] private VisualTreeAsset _titleScreen;
        [SerializeField] private VisualTreeAsset _gamePlayScreen;
        [SerializeField] private VisualTreeAsset _gameOverScreen;
        [SerializeField] private VisualTreeAsset _companySplash;
        [SerializeField] private VisualTreeAsset _settingsDialog;

        [Header("References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private AdManager _adManager;

        private UIDocument _uiDocument;
        private VisualElement _root;

        private enum Screen { CompanySplash, Title, GamePlay, GameOver }
        private Screen _currentScreen;

        // ---- GamePlay screen state（ShowGamePlay()のたびに再構築） ----
        private BoardView _boardView;
        private PieceTrayView _trayView;
        private Label _scoreLabel;
        private Label _bestLabel;
        private Label _reactionLabel;
        private Coroutine _reactionRoutine;
        private int _draggingHandIndex = -1;
        private int _lastSnapRow = -1;
        private int _lastSnapCol = -1;
        private int _displayedScore = 0;
        private Coroutine _scoreAnimRoutine;
        private Coroutine _gameOverRoutine;
        private SafeAreaHelper _safeAreaHelper;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _uiDocument = GetComponent<UIDocument>();
            _safeAreaHelper = GetComponent<SafeAreaHelper>();
        }

        private void Start()
        {
            _root = _uiDocument.rootVisualElement;

            // GameManagerのイベントは常時購読しておく（画面切り替えのたびの再購読は行わない）。
            // ハンドラ側はGamePlay画面が表示されている時だけ実処理する。
            _gameManager.OnScoreChanged.AddListener(() =>
            {
                if (_currentScreen == Screen.GamePlay)
                {
                    if (_scoreAnimRoutine != null) StopCoroutine(_scoreAnimRoutine);
                    _scoreAnimRoutine = StartCoroutine(ScoreAnimationRoutine(_gameManager.Score));
                    if (_bestLabel != null)
                    {
                        _bestLabel.text = $"Best {_gameManager.BestScore}";
                    }
                }
            });

            _gameManager.OnLinesCleared.AddListener(HandleLinesCleared);

            _gameManager.OnGameOver.AddListener(() =>
            {
                // ゲームオーバー時は即時に広告移行せず、まず盤面上でGameOver演出を再生
                if (_boardView != null)
                {
                    _boardView.PlayGameOverAnimation(() =>
                    {
                        _adManager.ShowInterstitial(() =>
                        {
                            ShowGameOver();
                        });
                    });
                }
                else
                {
                    _adManager.ShowInterstitial(() =>
                    {
                        ShowGameOver();
                    });
                }
            });

            _adManager.ShowBanner();

            ShowCompanySplash();
        }



        // ================= Title =================

        public void ShowTitle()
        {
            _currentScreen = Screen.Title;
            _boardView = null;
            _trayView = null;

            _root.Clear();

#if UNITY_EDITOR
            if (_titleScreen == null)
            {
                _titleScreen = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/TitleScreen.uxml");
            }
#endif

            if (_titleScreen == null)
            {
                Debug.LogError("[ScreenManager] _titleScreen is null!");
                return;
            }

            _titleScreen.CloneTree(_root);
            _safeAreaHelper?.ApplySafeArea();

            _root.Q<Button>("btn-play").clicked += ShowGamePlay;

            var bestLabel = _root.Q<Label>("best-score-label");
            bestLabel.text = _gameManager.BestScore > 0 ? $"BEST {_gameManager.BestScore}" : "";

            var versionLabel = _root.Q<Label>("version-label");
            versionLabel.text = $"v{Application.version}";

            var btnSettings = _root.Q<Button>("btn-settings");
            if (btnSettings != null)
            {
                Debug.Log("[ScreenManager] Settings button found on Title! Registering click listener.");
                btnSettings.clicked += () =>
                {
                    Debug.Log("[ScreenManager] Settings button clicked on Title!");
                    ShowSettings();
                };
            }
            else
            {
                Debug.LogWarning("[ScreenManager] Settings button NOT found on Title!");
            }
        }

        // ================= Game Play =================

        public void ShowGamePlay()
        {
            _currentScreen = Screen.GamePlay;

            _root.Clear();

#if UNITY_EDITOR
            if (_gamePlayScreen == null)
            {
                _gamePlayScreen = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/GamePlayScreen.uxml");
            }
#endif

            if (_gamePlayScreen == null)
            {
                Debug.LogError("[ScreenManager] _gamePlayScreen is null!");
                return;
            }

            _gamePlayScreen.CloneTree(_root);
            _safeAreaHelper?.ApplySafeArea();

            var screenRoot = _root.Q<VisualElement>("gameplay-screen");
            var boardContainer = _root.Q<VisualElement>("board-container");
            var trayContainer = _root.Q<VisualElement>("tray-container");

            _boardView = new BoardView(boardContainer, this);
            _trayView = new PieceTrayView(trayContainer, screenRoot);
            _trayView.PieceDragStarted += OnPieceDragStarted;
            _trayView.PieceDragMoved += OnPieceDragMoved;
            _trayView.PieceDragEnded += OnPieceDragEnded;

            _scoreLabel = _root.Q<Label>("score-label");
            _bestLabel = _root.Q<Label>("best-label");
            _reactionLabel = _root.Q<Label>("reaction-label");

            _displayedScore = 0;
            if (_scoreAnimRoutine != null) { StopCoroutine(_scoreAnimRoutine); _scoreAnimRoutine = null; }
            _gameManager.StartNewGame();
            RefreshGamePlayUI();

            var btnSettings = _root.Q<Button>("btn-settings");
            if (btnSettings != null)
            {
                Debug.Log("[ScreenManager] Settings button found on GamePlay! Registering click listener.");
                btnSettings.clicked += () =>
                {
                    Debug.Log("[ScreenManager] Settings button clicked on GamePlay!");
                    ShowSettings();
                };
            }
            else
            {
                Debug.LogWarning("[ScreenManager] Settings button NOT found on GamePlay!");
            }
        }

        private void GoHome()
        {
            _gameManager.ReturnToTitle();
            ShowTitle();
        }

        private void RetryGame()
        {
            ShowGamePlay();
        }

        private void RefreshGamePlayUI()
        {
            if (_boardView == null) return;

            _boardView.Refresh(_gameManager.Board);
            _trayView.Build(_gameManager.Hand, _gameManager.IsHandSlotUsed, _gameManager.IsPiecePlaceable);
            _displayedScore = _gameManager.Score;
            if (_scoreAnimRoutine != null) { StopCoroutine(_scoreAnimRoutine); _scoreAnimRoutine = null; }
            _scoreLabel.text = $"Score {_displayedScore}";
            _scoreLabel.style.scale = new Scale(Vector3.one);
            _bestLabel.text = $"Best {_gameManager.BestScore}";
        }

        // ---- GameManagerイベントハンドラ（常時購読・GamePlay表示中のみ実処理） ----

        private void HandleScoreChanged()
        {
            if (_currentScreen != Screen.GamePlay || _scoreLabel == null) return;
            _bestLabel.text = $"Best {_gameManager.BestScore}";

            if (_scoreAnimRoutine != null)
            {
                StopCoroutine(_scoreAnimRoutine);
            }
            _scoreAnimRoutine = StartCoroutine(ScoreAnimationRoutine(_gameManager.Score));
        }

        private IEnumerator ScoreAnimationRoutine(int targetScore)
        {
            int startScore = _displayedScore;
            const float duration = 0.4f;
            float elapsed = 0f;
            const float maxScale = 1.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                _displayedScore = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, t));
                _scoreLabel.text = $"Score {_displayedScore}";

                float scale;
                if (t < 0.25f)
                {
                    scale = Mathf.Lerp(1.0f, maxScale, t / 0.25f);
                }
                else
                {
                    scale = Mathf.Lerp(maxScale, 1.0f, (t - 0.25f) / 0.75f);
                }
                _scoreLabel.style.scale = new Scale(new Vector3(scale, scale, 1f));

                yield return null;
            }

            _displayedScore = targetScore;
            _scoreLabel.text = $"Score {_displayedScore}";
            _scoreLabel.style.scale = new Scale(Vector3.one);
            _scoreAnimRoutine = null;
        }

        private void HandleHandRefilled()
        {
            if (_currentScreen != Screen.GamePlay || _trayView == null) return;
            _trayView.Build(_gameManager.Hand, _gameManager.IsHandSlotUsed, _gameManager.IsPiecePlaceable);
        }

        private void HandleLinesCleared(int lines)
        {
            if (_currentScreen != Screen.GamePlay) return;

            SoundManager.Instance?.PlayLineClear();

            if (_reactionLabel == null) return;
            bool allClear = _gameManager.Board.IsEmpty();
            string text = ReactionText.GetText(lines, _gameManager.Combo, allClear);
            if (string.IsNullOrEmpty(text)) return;

            if (_reactionRoutine != null) StopCoroutine(_reactionRoutine);
            _reactionRoutine = StartCoroutine(ShowReactionRoutine(text, lines, _gameManager.Combo, allClear));
        }

        private void HandleCellsCleared(IReadOnlyList<ClearedCell> cells)
        {
            if (_currentScreen != Screen.GamePlay || _boardView == null) return;
            _boardView.PlayClearEffect(cells);
        }

        private IEnumerator ShowReactionRoutine(string text, int lines, int combo, bool allClear)
        {
            const float popDuration = 0.15f;
            const float holdDuration = 0.5f;
            const float fadeDuration = 0.3f;
            const float totalDuration = popDuration + holdDuration + fadeDuration;

            _reactionLabel.text = text;
            _reactionLabel.style.display = DisplayStyle.Flex;
            _reactionLabel.style.opacity = 1f;

            // コンボやPerfectによる強調の決定
            bool isHighCombo = combo >= 3;
            bool isSpecial = isHighCombo || allClear || lines >= 3;

            Color normalColor = new Color32(241, 238, 251, 255); // --color-text
            Color specialColor = new Color32(248, 233, 161, 255); // --color-accent
            _reactionLabel.style.color = isSpecial ? specialColor : normalColor;

            float baseTargetScale = isSpecial ? 1.3f : 1.0f;
            float startScale = baseTargetScale * 0.6f;

            float elapsed = 0f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;

                // フローティング（上昇）：0px から -100px まで（Yマイナスが上方向）
                float floatProgress = elapsed / totalDuration;
                float currentY = Mathf.Lerp(0f, -100f, EaseOutQuad(floatProgress));
                _reactionLabel.style.translate = new Translate(0f, currentY, 0f);

                if (elapsed < popDuration)
                {
                    float t = elapsed / popDuration;
                    const float overshoot = 1.70158f;
                    float tt = t - 1f;
                    float eased = tt * tt * ((overshoot + 1f) * tt + overshoot) + 1f;
                    float scale = Mathf.LerpUnclamped(startScale, baseTargetScale, eased);
                    _reactionLabel.style.scale = new Scale(new Vector3(scale, scale, 1f));
                }
                else if (elapsed < popDuration + holdDuration)
                {
                    _reactionLabel.style.scale = new Scale(new Vector3(baseTargetScale, baseTargetScale, 1f));
                }
                else
                {
                    float t = (elapsed - popDuration - holdDuration) / fadeDuration;
                    _reactionLabel.style.opacity = 1f - Mathf.Clamp01(t);
                    float scale = Mathf.Lerp(baseTargetScale, baseTargetScale * 0.8f, t);
                    _reactionLabel.style.scale = new Scale(new Vector3(scale, scale, 1f));
                }

                yield return null;
            }

            _reactionLabel.style.display = DisplayStyle.None;
            _reactionLabel.style.translate = new Translate(0f, 0f, 0f);
            _reactionLabel.style.scale = new Scale(Vector3.one);
            _reactionLabel.style.opacity = 1f;
            _reactionRoutine = null;
        }

        private static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        private void HandleGameOver()
        {
            if (_currentScreen != Screen.GamePlay) return;

            _adManager.ShowInterstitial(() =>
            {
                ShowGameOver();
            });
        }

        public void ShowGameOver()
        {
            _currentScreen = Screen.GameOver;
            _boardView = null;
            _trayView = null;

            _root.Clear();

#if UNITY_EDITOR
            if (_gameOverScreen == null)
            {
                _gameOverScreen = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/GameOverScreen.uxml");
            }
#endif

            if (_gameOverScreen == null)
            {
                Debug.LogError("[ScreenManager] _gameOverScreen is null! Please assign it in the inspector.");
                return;
            }

            _gameOverScreen.CloneTree(_root);
            _safeAreaHelper?.ApplySafeArea();

            if (_gameOverRoutine != null)
            {
                StopCoroutine(_gameOverRoutine);
            }
            _gameOverRoutine = StartCoroutine(GameOverPresentationRoutine());
        }

        private IEnumerator GameOverPresentationRoutine()
        {
            var congratsLabel = _root.Q<Label>("congrats-label");
            var scoreLabel = _root.Q<Label>("gameover-score-label");
            var bestLabel = _root.Q<Label>("gameover-best-label");
            var buttonsContainer = _root.Q<VisualElement>("gameover-buttons");

            // 初期状態設定
            congratsLabel.style.display = DisplayStyle.None;
            buttonsContainer.style.display = DisplayStyle.None;
            scoreLabel.text = "0";
            
            // ベストスコアの表示（小さく）
            bestLabel.text = $"Best: {_gameManager.BestScore}";

            // 1. スコアのカウントアップ (0.8秒)
            int targetScore = _gameManager.Score;
            float countDuration = 0.8f;
            float elapsed = 0f;
            
            while (elapsed < countDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / countDuration);
                float easedT = 1f - Mathf.Pow(1f - t, 3); // イージング（少しずつ減速）
                int currentScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, easedT));
                scoreLabel.text = currentScore.ToString();
                yield return null;
            }
            scoreLabel.text = targetScore.ToString();

            // 2. ハイスコア更新判定（今回のスコアがBestScoreと一致すれば更新とみなす）
            bool isNewBest = (targetScore > 0 && targetScore == _gameManager.BestScore);

            if (isNewBest)
            {
                // Congratulations表示
                congratsLabel.style.display = DisplayStyle.Flex;
                
                // 花火エフェクト開始 (完了まで待機)
                yield return StartCoroutine(PlayFireworkRoutine(scoreLabel.parent));
            }
            else
            {
                // 通常時は少しの間を置く
                yield return new WaitForSeconds(0.4f);
            }

            // 3. ボタンの表示
            buttonsContainer.style.display = DisplayStyle.Flex;

            // ボタンクリックイベント登録
            _root.Q<Button>("btn-retry").clicked += RetryGame;
            _root.Q<Button>("btn-home-gameover").clicked += GoHome;

            _gameOverRoutine = null;
        }

        private class UIFrameParticle
        {
            public VisualElement Element;
            public Vector2 Position;
            public Vector2 Velocity;
        }

        private IEnumerator PlayFireworkRoutine(VisualElement container)
        {
            // 3つの花火を時間差で打ち上げる
            for (int f = 0; f < 3; f++)
            {
                // UI Toolkitの基準解像度 1080x1920 の中心付近に配置
                float centerX = 540f + UnityEngine.Random.Range(-200f, 200f);
                float centerY = 750f + UnityEngine.Random.Range(-150f, 100f);

                int particleCount = 24;
                List<UIFrameParticle> particles = new List<UIFrameParticle>();

                Color[] colors = new Color[] {
                    new Color32(248, 233, 161, 255), // イエロー
                    new Color32(217, 135, 168, 255), // ピンク
                    new Color32(255, 125, 125, 255), // レッド
                    new Color32(138, 217, 185, 255)  // グリーン
                };
                Color color = colors[UnityEngine.Random.Range(0, colors.Length)];

                for (int i = 0; i < particleCount; i++)
                {
                    var element = new VisualElement();
                    element.style.position = Position.Absolute;
                    element.style.width = 12f;
                    element.style.height = 12f;
                    element.style.backgroundColor = color;

                    float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    float speed = UnityEngine.Random.Range(150f, 450f);
                    Vector2 vel = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);

                    container.Add(element);

                    particles.Add(new UIFrameParticle
                    {
                        Element = element,
                        Position = new Vector2(centerX, centerY),
                        Velocity = vel
                    });

                    element.style.left = centerX;
                    element.style.top = centerY;
                }

                // 各花火の拡散を並列実行
                StartCoroutine(AnimateFireworkParticles(particles));

                // 打ち上げ時のSE（LineClearを使用）
                SoundManager.Instance?.PlayLineClear();

                yield return new WaitForSeconds(0.25f);
            }

            // 花火が消えるのを待つ
            yield return new WaitForSeconds(0.8f);
        }

        private IEnumerator AnimateFireworkParticles(List<UIFrameParticle> particles)
        {
            float duration = 1.0f;
            float elapsed = 0f;
            const float gravity = 250f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                foreach (var p in particles)
                {
                    if (p.Element == null || p.Element.parent == null) continue;

                    p.Velocity.y += gravity * Time.deltaTime;
                    p.Position += p.Velocity * Time.deltaTime;

                    p.Element.style.left = p.Position.x;
                    p.Element.style.top = p.Position.y;

                    p.Element.style.opacity = 1f - t;
                    float scale = Mathf.Lerp(1.0f, 0.1f, t);
                    p.Element.style.scale = new Scale(new Vector3(scale, scale, 1f));
                }

                yield return null;
            }

            foreach (var p in particles)
            {
                p.Element?.RemoveFromHierarchy();
            }
        }

        // ---- ドラッグ&ドロップ（PieceTrayView発火 → BoardViewでヒットテスト → GameManagerで配置判定） ----

        // ポインターが指すマスにそのまま置けない時、近くの置ける場所へスナップする探索半径。
        private const int SnapSearchRadius = 2;

        private bool _snapGhostVisible;

        private void OnPieceDragStarted(int handIndex, Vector2 panelPosition)
        {
            _draggingHandIndex = handIndex;
            _snapGhostVisible = false;
            _lastSnapRow = -1;
            _lastSnapCol = -1;
        }

        private void OnPieceDragMoved(Vector2 panelPosition)
        {
            if (_draggingHandIndex < 0 || _boardView == null) return;

            bool foundSnap = false;
            int snapRow = -1;
            int snapCol = -1;
            var localPoint = _boardView.Container.WorldToLocal(panelPosition);
            if (_boardView.TryGetCellAtLocalPoint(localPoint, out int row, out int col))
            {
                var piece = _gameManager.Hand[_draggingHandIndex];
                GetOriginForCenteredDrag(piece, row, col, out int originRow, out int originCol);

                if (_gameManager.Board.TryFindNearbyValidPlacement(piece, originRow, originCol, SnapSearchRadius, out snapRow, out snapCol))
                {
                    foundSnap = true;
                    _boardView.ShowPreview(piece, snapRow, snapCol);
                }
            }

            if (!foundSnap)
            {
                _boardView.ClearPreview();
            }

            // 置ける場所が見つかった瞬間（無し→あり）、または置ける場所の位置が変わった瞬間に振動+SEを鳴らす。
            if (foundSnap)
            {
                if (!_snapGhostVisible || snapRow != _lastSnapRow || snapCol != _lastSnapCol)
                {
                    SoundManager.Instance?.PlaySnapTick();
                    Haptics.Tick();
                }
                _lastSnapRow = snapRow;
                _lastSnapCol = snapCol;
            }
            else
            {
                _lastSnapRow = -1;
                _lastSnapCol = -1;
            }
            _snapGhostVisible = foundSnap;
        }

        private void OnPieceDragEnded(Vector2 panelPosition)
        {
            if (_draggingHandIndex < 0 || _boardView == null) return;

            _boardView.ClearPreview();
            var localPoint = _boardView.Container.WorldToLocal(panelPosition);
            bool placed = false;
            int snapRow = -1;
            int snapCol = -1;
            PieceDefinitions.Definition piece = default;

            if (_boardView.TryGetCellAtLocalPoint(localPoint, out int row, out int col))
            {
                piece = _gameManager.Hand[_draggingHandIndex];
                GetOriginForCenteredDrag(piece, row, col, out int originRow, out int originCol);

                if (_gameManager.Board.TryFindNearbyValidPlacement(piece, originRow, originCol, SnapSearchRadius, out snapRow, out snapCol)
                    && _gameManager.TryPlacePiece(_draggingHandIndex, snapRow, snapCol))
                {
                    SoundManager.Instance?.PlayPlace();
                    placed = true;
                }
            }

            _draggingHandIndex = -1;
            _snapGhostVisible = false;
            _lastSnapRow = -1;
            _lastSnapCol = -1;
            RefreshGamePlayUI();

            if (placed)
            {
                _boardView.PlayPlaceEffect(piece, snapRow, snapCol);
            }
        }

        /// <summary>
        /// ドラッグ中はピースの中央がポインター位置に来るようにしている（PieceTrayView側の実機フィードバック対応）ため、
        /// 配置判定側でも「ポインターが指すマス」をピースの中心とみなし、そこから左上原点(row,col)へ逆算する。
        /// </summary>
        private static void GetOriginForCenteredDrag(PieceDefinitions.Definition piece, int pointerRow, int pointerCol, out int originRow, out int originCol)
        {
            originRow = pointerRow - piece.Rows / 2;
            originCol = pointerCol - piece.Cols / 2;
        }

        // ================= Company Splash =================

        public void ShowCompanySplash()
        {
            _currentScreen = Screen.CompanySplash;
            _boardView = null;
            _trayView = null;

            _root.Clear();

#if UNITY_EDITOR
            if (_companySplash == null)
            {
                _companySplash = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/CompanySplash.uxml");
            }
#endif

            if (_companySplash == null)
            {
                ShowTitle();
                return;
            }

            _companySplash.CloneTree(_root);
            _safeAreaHelper?.ApplySafeArea();

            StartCoroutine(CompanySplashPresentationRoutine());
        }

        private IEnumerator CompanySplashPresentationRoutine()
        {
            yield return new WaitForSeconds(2.0f);
            ShowTitle();
        }

        // ================= Settings PopUp =================

        public void ShowSettings()
        {
#if UNITY_EDITOR
            if (_settingsDialog == null)
            {
                _settingsDialog = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/SettingsDialog.uxml");
            }
#endif

            if (_settingsDialog == null)
            {
                Debug.LogError("[ScreenManager] _settingsDialog asset is null!");
                return;
            }

            var dialog = _settingsDialog.Instantiate();
            var overlay = dialog.Q<VisualElement>(className: "settings-overlay");
            if (overlay == null)
            {
                Debug.LogError("[ScreenManager] settings-overlay root element not found in SettingsDialog UXML!");
                return;
            }

            _root.Add(overlay);

            var sliderBgm = overlay.Q<Slider>("slider-bgm");
            var sliderSe = overlay.Q<Slider>("slider-se");
            var btnVibeOn = overlay.Q<Button>("btn-vibe-on");
            var btnVibeOff = overlay.Q<Button>("btn-vibe-off");
            var btnRemoveAds = overlay.Q<Button>("btn-remove-ads");
            var btnClose = overlay.Q<Button>("btn-close-settings");

            // 初期値をセット
            if (sliderBgm != null && SettingsManager.Instance != null)
            {
                sliderBgm.value = SettingsManager.Instance.BgmVolume;
                sliderBgm.RegisterValueChangedCallback(evt =>
                {
                    SettingsManager.Instance.SetBgmVolume(evt.newValue);
                });
            }

            if (sliderSe != null && SettingsManager.Instance != null)
            {
                sliderSe.value = SettingsManager.Instance.SeVolume;
                sliderSe.RegisterValueChangedCallback(evt =>
                {
                    SettingsManager.Instance.SetSeVolume(evt.newValue);
                });
            }

            if (btnVibeOn != null && btnVibeOff != null && SettingsManager.Instance != null)
            {
                System.Action<bool> updateVibeUI = (enabled) =>
                {
                    if (enabled)
                    {
                        btnVibeOn.AddToClassList("btn-accent");
                        btnVibeOff.RemoveFromClassList("btn-accent");
                    }
                    else
                    {
                        btnVibeOn.RemoveFromClassList("btn-accent");
                        btnVibeOff.AddToClassList("btn-accent");
                    }
                };

                // 初期状態を適用
                updateVibeUI(SettingsManager.Instance.VibeEnabled);

                btnVibeOn.clicked += () =>
                {
                    SettingsManager.Instance.SetVibeEnabled(true);
                    updateVibeUI(true);
                };

                btnVibeOff.clicked += () =>
                {
                    SettingsManager.Instance.SetVibeEnabled(false);
                    updateVibeUI(false);
                };
            }

            if (btnRemoveAds != null)
            {
                if (SettingsManager.Instance != null && SettingsManager.Instance.IsAdsRemoved)
                {
                    btnRemoveAds.style.display = DisplayStyle.None;
                }
                else
                {
                    btnRemoveAds.clicked += () =>
                    {
                        StartCoroutine(ShowDummyIAPDialogRoutine(overlay, btnRemoveAds));
                    };
                }
            }

            var btnGoTitle = overlay.Q<Button>("btn-go-title");
            if (btnGoTitle != null)
            {
                if (_currentScreen == Screen.GamePlay)
                {
                    btnGoTitle.style.display = DisplayStyle.Flex;
                    btnGoTitle.clicked += () =>
                    {
                        // タイトルに戻る前にインタースティシャル広告を表示（課金無効時は即コールバック）
                        _adManager.ShowInterstitial(() =>
                        {
                            overlay.RemoveFromHierarchy();
                            GoHome();
                        });
                    };
                }
                else
                {
                    btnGoTitle.style.display = DisplayStyle.None;
                }
            }

            if (btnClose != null)
            {
                btnClose.clicked += () =>
                {
                    overlay.RemoveFromHierarchy();
                };
            }
        }

        private IEnumerator ShowDummyIAPDialogRoutine(VisualElement settingsRoot, Button removeAdsButton)
        {
            var iapOverlay = new VisualElement();
            iapOverlay.style.position = Position.Absolute;
            iapOverlay.style.top = 0;
            iapOverlay.style.left = 0;
            iapOverlay.style.right = 0;
            iapOverlay.style.bottom = 0;
            iapOverlay.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            iapOverlay.style.justifyContent = Justify.Center;
            iapOverlay.style.alignItems = Align.Center;

            var panel = new VisualElement();
            panel.AddToClassList("panel");
            panel.style.minWidth = 360;
            panel.style.paddingTop = 24;
            panel.style.paddingBottom = 24;
            panel.style.paddingLeft = 24;
            panel.style.paddingRight = 24;
            panel.style.alignItems = Align.Center;

            var title = new Label("App Store Purchase");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            title.style.marginBottom = 16;
            panel.Add(title);

            var desc = new Label("Do you want to buy 'Remove Ads' for ¥500?");
            desc.style.fontSize = 18;
            desc.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            desc.style.marginBottom = 24;
            desc.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(desc);

            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.SpaceBetween;
            btnRow.style.width = Length.Percent(100);

            var btnBuy = new Button(() => {
#if UNITY_EDITOR
                bool storeSuccess = UnityEditor.EditorUtility.DisplayDialog(
                    "App Store / Google Play (Mock)",
                    "Do you want to confirm this in-app purchase of ¥500?",
                    "Confirm (Buy)", "Cancel");
                if (!storeSuccess) return;
#endif
                SettingsManager.Instance?.RemoveAds();
                removeAdsButton.style.display = DisplayStyle.None;
                iapOverlay.RemoveFromHierarchy();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayDialog("Success", "In-app purchase completed successfully. Ads removed!", "OK");
#endif
            }) { text = "Buy" };
            btnBuy.AddToClassList("btn");
            btnBuy.AddToClassList("btn-accent");
            btnBuy.style.width = Length.Percent(45);
            btnRow.Add(btnBuy);

            var btnCancel = new Button(() => {
                iapOverlay.RemoveFromHierarchy();
            }) { text = "Cancel" };
            btnCancel.AddToClassList("btn");
            btnCancel.style.width = Length.Percent(45);
            btnRow.Add(btnCancel);

            panel.Add(btnRow);
            iapOverlay.Add(panel);
            settingsRoot.Add(iapOverlay);

            yield return null;
        }
    }
}
