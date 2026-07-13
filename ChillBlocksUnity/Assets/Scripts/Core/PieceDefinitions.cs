// PieceDefinitions.cs — ピース形状・出現重み・形状ごとの色の定義
// 形状/重みの出典: tokaa1/blockerino (constants/Piece.tsx) から移植。
// 重みは design-spec.html の GS2「採用する重み付け」表と一致させること。
// 色はSortGemsの「Cozy Lo-Fi Pixel Palette」8色（gem-pink/coral/orange/lemon/mint/sky/lavender/purple）を
// 形状ファミリー単位で割り当て（実機フィードバックで「形ごとに色を変えたい」との要望に対応）。

using UnityEngine;

namespace ChillBlocks.Core
{
    public static class PieceDefinitions
    {
        public struct Definition
        {
            public bool[,] Cells;
            public float Weight;
            public Color Color;

            public int Rows => Cells.GetLength(0);
            public int Cols => Cells.GetLength(1);
        }

        // Cozy Lo-Fi Pixel Palette（gem colors）
        private static readonly Color GemPink = FromHex(0xFC, 0xAC, 0xC9);
        private static readonly Color GemCoral = FromHex(0xFE, 0xB9, 0x8B);
        private static readonly Color GemOrange = FromHex(0xFE, 0xAB, 0x67);
        private static readonly Color GemLemon = FromHex(0xFE, 0xE0, 0x77);
        private static readonly Color GemMint = FromHex(0xA4, 0xEF, 0xDF);
        private static readonly Color GemSky = FromHex(0x96, 0xDE, 0xFD);
        private static readonly Color GemLavender = FromHex(0xB7, 0xAD, 0xFB);
        private static readonly Color GemPurple = FromHex(0x95, 0x7F, 0xE7);

        public static readonly Definition[] All = BuildDefinitions();

        private static Definition[] BuildDefinitions()
        {
            return new[]
            {
                // L字（8バリエーション、重み2） — coral
                Def(2f, GemCoral, new[,] { { 1, 0, 0 }, { 1, 1, 1 } }),
                Def(2f, GemCoral, new[,] { { 1, 1 }, { 1, 0 }, { 1, 0 } }),
                Def(2f, GemCoral, new[,] { { 1, 1, 1 }, { 0, 0, 1 } }),
                Def(2f, GemCoral, new[,] { { 0, 1 }, { 0, 1 }, { 1, 1 } }),
                Def(2f, GemCoral, new[,] { { 0, 0, 1 }, { 1, 1, 1 } }),
                Def(2f, GemCoral, new[,] { { 1, 0 }, { 1, 0 }, { 1, 1 } }),
                Def(2f, GemCoral, new[,] { { 1, 1, 1 }, { 1, 0, 0 } }),
                Def(2f, GemCoral, new[,] { { 1, 1 }, { 0, 1 }, { 0, 1 } }),

                // T字/三角形（4バリエーション、重み1.5） — lemon
                Def(1.5f, GemLemon, new[,] { { 1, 1, 1 }, { 0, 1, 0 } }),
                Def(1.5f, GemLemon, new[,] { { 1, 0 }, { 1, 1 }, { 1, 0 } }),
                Def(1.5f, GemLemon, new[,] { { 0, 1, 0 }, { 1, 1, 1 } }),
                Def(1.5f, GemLemon, new[,] { { 0, 1 }, { 1, 1 }, { 0, 1 } }),

                // S字/Z字（4バリエーション、重み1） — sky
                Def(1f, GemSky, new[,] { { 0, 1, 1 }, { 1, 1, 0 } }),
                Def(1f, GemSky, new[,] { { 1, 0 }, { 1, 1 }, { 0, 1 } }),
                Def(1f, GemSky, new[,] { { 1, 1, 0 }, { 0, 1, 1 } }),
                Def(1f, GemSky, new[,] { { 0, 1 }, { 1, 1 }, { 1, 0 } }),

                // 3×3 正方形（重み3） — purple
                Def(3f, GemPurple, new[,] { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } }),

                // 2×2 正方形（重み6、最も出やすい） — pink
                Def(6f, GemPink, new[,] { { 1, 1 }, { 1, 1 } }),

                // 4マス直線（縦横、重み2） — mint
                Def(2f, GemMint, new[,] { { 1 }, { 1 }, { 1 }, { 1 } }),
                Def(2f, GemMint, new[,] { { 1, 1, 1, 1 } }),

                // 3マス直線（縦横、重み4） — lavender
                Def(4f, GemLavender, new[,] { { 1 }, { 1 }, { 1 } }),
                Def(4f, GemLavender, new[,] { { 1, 1, 1 } }),

                // 2マス直線（縦横、重み2） — orange
                Def(2f, GemOrange, new[,] { { 1 }, { 1 } }),
                Def(2f, GemOrange, new[,] { { 1, 1 } }),
            };
        }

        private static Definition Def(float weight, Color color, int[,] pattern)
        {
            int rows = pattern.GetLength(0);
            int cols = pattern.GetLength(1);
            var cells = new bool[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    cells[r, c] = pattern[r, c] != 0;
                }
            }
            return new Definition { Cells = cells, Weight = weight, Color = color };
        }

        private static Color FromHex(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }
    }
}
