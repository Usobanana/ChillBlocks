using System;

namespace ChillBlocks.Core
{
    /// <summary>
    /// 手持ち3ピースの生成。GS2で採用した「盤面を見ない重み付きランダム」方式
    /// （tokaa1/blockerino の getRandomPiece() と同じルーレット選択）。
    /// </summary>
    public static class HandGenerator
    {
        public const int HandSize = 3;

        public static PieceDefinitions.Definition[] GenerateHand(Random random)
        {
            var hand = new PieceDefinitions.Definition[HandSize];
            for (int i = 0; i < HandSize; i++)
            {
                hand[i] = PickWeightedRandom(random);
            }
            return hand;
        }

        private static PieceDefinitions.Definition PickWeightedRandom(Random random)
        {
            var all = PieceDefinitions.All;

            float total = 0f;
            foreach (var def in all) total += def.Weight;

            double roll = random.NextDouble() * total;
            foreach (var def in all)
            {
                roll -= def.Weight;
                if (roll < 0)
                {
                    return def;
                }
            }

            return all[all.Length - 1];
        }
    }
}
