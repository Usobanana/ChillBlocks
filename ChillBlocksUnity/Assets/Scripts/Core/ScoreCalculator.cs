namespace ChillBlocks.Core
{
    /// <summary>
    /// GS3で採用したristic方式のスコア計算。
    /// ライン消し: lines × 10 × (combo+1)。3ライン以上は ×(lines-1)。全消しは+300。
    /// コンボは「ライン消し無しの配置が3回連続」でリセット（2つの独立実装が一致した値）。
    /// </summary>
    public class ScoreCalculator
    {
        private const int MaxPlacementsWithoutClear = 3;
        private const int AllClearBonus = 300;

        public int Score { get; private set; }
        public int Combo { get; private set; }

        private int _placementsWithoutClear;

        /// <summary>配置1回ぶんの結果を登録し、今回加算されたスコアを返す。</summary>
        public int RegisterPlacement(int linesCleared, bool boardNowEmpty)
        {
            if (linesCleared <= 0)
            {
                _placementsWithoutClear++;
                if (_placementsWithoutClear >= MaxPlacementsWithoutClear)
                {
                    Combo = 0;
                    _placementsWithoutClear = 0;
                }
                return 0;
            }

            int bonus = linesCleared * 10 * (Combo + 1);
            if (linesCleared > 2)
            {
                bonus *= linesCleared - 1;
            }
            if (boardNowEmpty)
            {
                bonus += AllClearBonus;
            }

            Combo += linesCleared;
            _placementsWithoutClear = 0;
            Score += bonus;
            return bonus;
        }

        public void Reset()
        {
            Score = 0;
            Combo = 0;
            _placementsWithoutClear = 0;
        }
    }
}
