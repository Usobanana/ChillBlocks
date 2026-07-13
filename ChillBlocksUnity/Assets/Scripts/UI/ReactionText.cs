namespace ChillBlocks.UI
{
    /// <summary>
    /// ライン消し時のリアクション文言。同時消しライン数を軸に段階分けする
    /// （実機フィードバック「消したラインの数でExcellent！みたいな反応が欲しい」に対応）。
    /// 具体的な語彙は本家の公式文言ではなく、OSSクローン RisticDjordje/BlockBlast-Game-AI-Agent の
    /// combo_names（""/"DOUBLE "/"TRIPLE "/"QUAD "/"PENTA "/"HEXA "）の段階分けを土台にしている。
    /// </summary>
    public static class ReactionText
    {
        public static string GetText(int linesCleared, int comboAfter, bool allClear)
        {
            if (linesCleared <= 0) return string.Empty;

            string tier = linesCleared switch
            {
                1 => "Nice!",
                2 => "Great!",
                3 => "Excellent!",
                _ => "Amazing!", // 4本以上
            };

            if (allClear)
            {
                tier = "PERFECT!";
            }

            // 連続クリア（コンボ）が乗っている場合は段数を併記する。
            if (comboAfter >= 2)
            {
                tier += $"\n{comboAfter} COMBO";
            }

            return tier;
        }
    }
}
