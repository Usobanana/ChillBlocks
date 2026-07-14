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

        [Header("References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private AdManager _adManager;

        private UIDocument _uiDocument;
        private VisualElement _root;

        private enum Screen { Title, GamePlay }
        private Screen _currentScreen;

        // ---- GamePlay screen state（ShowGamePlay()のたびに再構築） ----
        private BoardView _boardView;
        private PieceTrayView _trayView;
        private Label _scoreLabel;
        private Label _bestLabel;
        private VisualElement _gameOverPanel;
        private Label _gameOverScoreLabel;
        private Button _continueButton;
        private Label _reactionLabel;
        private Coroutine _reactionRoutine;
        private int _draggingHandIndex = -1;
        private int _lastSnapRow = -1;
        private int _lastSnapCol = -1;
        private int _displayedScore = 0;
        private Coroutine _scoreAnimRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            _root = _uiDocument.rootVisualElement;

            // GameManagerのイベントは常時購読しておく（画面切り替えのたびの再購読は行わない）。
            // ハンドラ側はGamePlay画面が表示されている時だけ実処理する。
            _gameManager.OnScoreChanged.AddListener(HandleScoreChanged);
            _gameManager.OnHandRefilled.AddListener(HandleHandRefilled);
            _gameManager.OnGameOver.AddListener(HandleGameOver);
            _gameManager.OnLinesCleared.AddListener(HandleLinesCleared);
            _gameManager.OnCellsCleared += HandleCellsCleared;

            // バナー広告のモック表示（実機フィードバックで有効化。SortGems同様、常時表示）。
            _adManager.ShowBanner();

            ShowTitle();
        }

        // ================= Title =================

        public void ShowTitle()
        {
            _currentScreen = Screen.Title;
            _boardView = null;
            _trayView = null;

            _root.Clear();
            _titleScreen.CloneTree(_root);

            _root.Q<Button>("btn-play").clicked += ShowGamePlay;

            var bestLabel = _root.Q<Label>("best-score-label");
            bestLabel.text = _gameManager.BestScore > 0 ? $"BEST {_gameManager.BestScore}" : "";

            var versionLabel = _root.Q<Label>("version-label");
            versionLabel.text = $"v{Application.version}";
        }

        // ================= Game Play =================

        public void ShowGamePlay()
        {
            _currentScreen = Screen.GamePlay;

            _root.Clear();
            _gamePlayScreen.CloneTree(_root);

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
            _gameOverPanel = _root.Q<VisualElement>("gameover-panel");
            _gameOverScoreLabel = _root.Q<Label>("gameover-score-label");
            _continueButton = _root.Q<Button>("btn-continue");
            _reactionLabel = _root.Q<Label>("reaction-label");

            _root.Q<Button>("btn-home").clicked += GoHome;
            _root.Q<Button>("btn-home-gameover").clicked += GoHome;
            _root.Q<Button>("btn-retry").clicked += RetryGame;
            _continueButton.clicked += OnContinueClicked;

            _adManager.ResetRunState();
            _displayedScore = 0;
            if (_scoreAnimRoutine != null) { StopCoroutine(_scoreAnimRoutine); _scoreAnimRoutine = null; }
            _gameManager.StartNewGame();
            RefreshGamePlayUI();
        }

        private void GoHome()
        {
            _gameManager.ReturnToTitle();
            ShowTitle();
        }

        private void RetryGame()
        {
            _gameOverPanel.style.display = DisplayStyle.None;
            _adManager.ResetRunState();
            _displayedScore = 0;
            if (_scoreAnimRoutine != null) { StopCoroutine(_scoreAnimRoutine); _scoreAnimRoutine = null; }
            _gameManager.StartNewGame();
            RefreshGamePlayUI();
        }

        private void OnContinueClicked()
        {
            _adManager.TryShowRewardedContinue(
                onRewardEarned: () =>
                {
                    _gameManager.ContinueAfterAd();
                    _gameOverPanel.style.display = DisplayStyle.None;
                    RefreshGamePlayUI();
                },
                onFallback: () =>
                {
                    _continueButton.SetEnabled(false);
                    _continueButton.text = "Ad not available";
                });
        }

        private void RefreshGamePlayUI()
        {
            if (_boardView == null) return;

            _boardView.Refresh(_gameManager.Board);
            _trayView.Build(_gameManager.Hand, _gameManager.IsHandSlotUsed);
            _displayedScore = _gameManager.Score;
            if (_scoreAnimRoutine != null) { StopCoroutine(_scoreAnimRoutine); _scoreAnimRoutine = null; }
            _scoreLabel.text = $"Score {_displayedScore}";
            _scoreLabel.transform.scale = Vector3.one;
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
                _scoreLabel.transform.scale = new Vector3(scale, scale, 1f);

                yield return null;
            }

            _displayedScore = targetScore;
            _scoreLabel.text = $"Score {_displayedScore}";
            _scoreLabel.transform.scale = Vector3.one;
            _scoreAnimRoutine = null;
        }

        private void HandleHandRefilled()
        {
            if (_currentScreen != Screen.GamePlay || _trayView == null) return;
            _trayView.Build(_gameManager.Hand, _gameManager.IsHandSlotUsed);
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
                _reactionLabel.transform.position = new Vector3(0f, currentY, 0f);

                if (elapsed < popDuration)
                {
                    float t = elapsed / popDuration;
                    const float overshoot = 1.70158f;
                    float tt = t - 1f;
                    float eased = tt * tt * ((overshoot + 1f) * tt + overshoot) + 1f;
                    float scale = Mathf.LerpUnclamped(startScale, baseTargetScale, eased);
                    _reactionLabel.transform.scale = new Vector3(scale, scale, 1f);
                }
                else if (elapsed < popDuration + holdDuration)
                {
                    _reactionLabel.transform.scale = new Vector3(baseTargetScale, baseTargetScale, 1f);
                }
                else
                {
                    float t = (elapsed - popDuration - holdDuration) / fadeDuration;
                    _reactionLabel.style.opacity = 1f - Mathf.Clamp01(t);
                    float scale = Mathf.Lerp(baseTargetScale, baseTargetScale * 0.8f, t);
                    _reactionLabel.transform.scale = new Vector3(scale, scale, 1f);
                }

                yield return null;
            }

            _reactionLabel.style.display = DisplayStyle.None;
            _reactionLabel.transform.position = Vector3.zero;
            _reactionLabel.transform.scale = Vector3.one;
            _reactionLabel.style.opacity = 1f;
            _reactionRoutine = null;
        }

        private static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        private void HandleGameOver()
        {
            if (_currentScreen != Screen.GamePlay || _gameOverPanel == null) return;

            _adManager.OnGameOver();

            _gameOverScoreLabel.text = $"Score {_gameManager.Score} / Best {_gameManager.BestScore}";
            bool continueAvailable = _adManager.IsRewardedReady && !_adManager.RewardedContinueUsedThisRun;
            _continueButton.SetEnabled(continueAvailable);
            _continueButton.text = continueAvailable ? "Continue (Watch Ad)" : "Ad not available";

            _gameOverPanel.style.display = DisplayStyle.Flex;
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
    }
}
