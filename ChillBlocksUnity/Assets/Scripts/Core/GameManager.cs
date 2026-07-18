using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ChillBlocks.Core
{
    public enum GameState
    {
        Title,
        Playing,
        GameOver
    }

    /// <summary>
    /// ゲーム状態とロジックのみを保持し、画面遷移には関与しない。
    /// SortGemsのGameManagerと同じ一方向の依存関係：
    /// GameManager → (UnityEvent) → ScreenManager（購読側）。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private const string BestScoreKey = "ChillBlocks.BestScore";

        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Title;

        public BoardState Board { get; } = new BoardState();
        public PieceDefinitions.Definition[] Hand { get; private set; }

        private bool[] _handUsed;
        private readonly ScoreCalculator _scoreCalculator = new ScoreCalculator();
        private readonly System.Random _random = new System.Random();

        public int Score => _scoreCalculator.Score;
        public int Combo => _scoreCalculator.Combo;
        public int BestScore { get; private set; }

        public readonly UnityEvent OnScoreChanged = new UnityEvent();
        public readonly UnityEvent<int> OnLinesCleared = new UnityEvent<int>();
        public readonly UnityEvent OnHandRefilled = new UnityEvent();
        public readonly UnityEvent OnGameOver = new UnityEvent();

        /// <summary>ライン消し演出（BoardViewの消滅アニメーション）用。消える直前のマス座標・色を渡す。</summary>
        public event Action<IReadOnlyList<ClearedCell>> OnCellsCleared;
        private readonly List<ClearedCell> _clearedCellsBuffer = new List<ClearedCell>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        }

        public void StartNewGame()
        {
            Board.Reset();
            _scoreCalculator.Reset();
            Hand = HandGenerator.GenerateHand(_random);
            _handUsed = new bool[HandGenerator.HandSize];
            State = GameState.Playing;

            OnScoreChanged.Invoke();
            OnHandRefilled.Invoke();
        }

        public bool IsHandSlotUsed(int handIndex)
        {
            return _handUsed != null && handIndex >= 0 && handIndex < _handUsed.Length && _handUsed[handIndex];
        }

        /// <summary>手持ちhandIndex番目のピースを(row,col)起点で配置する。成否を返す。</summary>
        public bool TryPlacePiece(int handIndex, int row, int col)
        {
            if (State != GameState.Playing) return false;
            if (Hand == null || handIndex < 0 || handIndex >= Hand.Length) return false;
            if (_handUsed[handIndex]) return false;

            var piece = Hand[handIndex];
            if (!Board.CanPlace(piece, row, col)) return false;

            Board.Place(piece, row, col);
            _handUsed[handIndex] = true;

            int linesCleared = Board.ClearFullLines(_clearedCellsBuffer);
            _scoreCalculator.RegisterPlacement(linesCleared, Board.IsEmpty());

            if (Score > BestScore)
            {
                BestScore = Score;
                PlayerPrefs.SetInt(BestScoreKey, BestScore);
                PlayerPrefs.Save();
            }

            OnScoreChanged.Invoke();
            if (linesCleared > 0)
            {
                OnLinesCleared.Invoke(linesCleared);
                OnCellsCleared?.Invoke(_clearedCellsBuffer);
            }

            if (AllHandUsed())
            {
                Hand = HandGenerator.GenerateHand(_random);
                _handUsed = new bool[HandGenerator.HandSize];
                OnHandRefilled.Invoke();
            }

            CheckGameOver();
            return true;
        }

        private bool AllHandUsed()
        {
            foreach (var used in _handUsed)
            {
                if (!used) return false;
            }
            return true;
        }

        private void CheckGameOver()
        {
            for (int i = 0; i < Hand.Length; i++)
            {
                if (_handUsed[i]) continue;
                if (Board.HasAnyValidPlacement(Hand[i])) return;
            }

            State = GameState.GameOver;
            OnGameOver.Invoke();
        }



        public void ReturnToTitle()
        {
            State = GameState.Title;
        }
    }
}
