using System.Collections.Generic;
using UnityEngine;

namespace ChillBlocks.Core
{
    /// <summary>8×8固定盤面。GS1準拠（ステージ／レベルの概念なし）。
    /// 配置したピースの色は表示用に_cellColorsへ保持する（判定ロジック自体は_cellsのbool値のみ使用）。</summary>
    public readonly struct ClearedCell
    {
        public readonly int Row;
        public readonly int Col;
        public readonly Color Color;

        public ClearedCell(int row, int col, Color color)
        {
            Row = row;
            Col = col;
            Color = color;
        }
    }

    public class BoardState
    {
        public const int Size = 8;

        private readonly bool[,] _cells = new bool[Size, Size];
        private readonly Color[,] _cellColors = new Color[Size, Size];

        public bool IsFilled(int row, int col) => _cells[row, col];

        public Color GetCellColor(int row, int col) => _cellColors[row, col];

        public bool CanPlace(PieceDefinitions.Definition piece, int originRow, int originCol)
        {
            int rows = piece.Rows;
            int cols = piece.Cols;

            if (originRow < 0 || originCol < 0 || originRow + rows > Size || originCol + cols > Size)
            {
                return false;
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (piece.Cells[r, c] && _cells[originRow + r, originCol + c])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// targetRow/targetColにそのまま置ければそこを返す。置けなければ、そこから近い順に
        /// リング状（Chebyshev距離1, 2, ...）に周囲を探索し、最初に見つかった配置可能なマスを返す。
        /// 見つからなければfalse（実機フィードバック：「置けない位置ではゴーストを出さず、
        /// 近くに置ける場所があればそこにスナップしてほしい」に対応）。
        /// </summary>
        public bool TryFindNearbyValidPlacement(PieceDefinitions.Definition piece, int targetRow, int targetCol, int maxRadius, out int foundRow, out int foundCol)
        {
            if (CanPlace(piece, targetRow, targetCol))
            {
                foundRow = targetRow;
                foundCol = targetCol;
                return true;
            }

            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int dr = -radius; dr <= radius; dr++)
                {
                    for (int dc = -radius; dc <= radius; dc++)
                    {
                        if (Mathf.Max(Mathf.Abs(dr), Mathf.Abs(dc)) != radius) continue; // このリング上のマスのみ（内側は探索済み）

                        int r = targetRow + dr;
                        int c = targetCol + dc;
                        if (CanPlace(piece, r, c))
                        {
                            foundRow = r;
                            foundCol = c;
                            return true;
                        }
                    }
                }
            }

            foundRow = -1;
            foundCol = -1;
            return false;
        }

        public bool HasAnyValidPlacement(PieceDefinitions.Definition piece)
        {
            int rows = piece.Rows;
            int cols = piece.Cols;

            for (int r = 0; r <= Size - rows; r++)
            {
                for (int c = 0; c <= Size - cols; c++)
                {
                    if (CanPlace(piece, r, c))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Place(PieceDefinitions.Definition piece, int originRow, int originCol)
        {
            int rows = piece.Rows;
            int cols = piece.Cols;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (piece.Cells[r, c])
                    {
                        _cells[originRow + r, originCol + c] = true;
                        _cellColors[originRow + r, originCol + c] = piece.Color;
                    }
                }
            }
        }

        /// <summary>揃った行・列を消し、消した本数を返す。clearedCellsOutを渡すと、消える直前の
        /// マス（座標・色）を演出用に書き出す（呼び出し側の任意、nullなら何もしない）。</summary>
        public int ClearFullLines(List<ClearedCell> clearedCellsOut = null)
        {
            var rowsToClear = new List<int>();
            var colsToClear = new List<int>();

            for (int r = 0; r < Size; r++)
            {
                bool full = true;
                for (int c = 0; c < Size; c++)
                {
                    if (!_cells[r, c]) { full = false; break; }
                }
                if (full) rowsToClear.Add(r);
            }

            for (int c = 0; c < Size; c++)
            {
                bool full = true;
                for (int r = 0; r < Size; r++)
                {
                    if (!_cells[r, c]) { full = false; break; }
                }
                if (full) colsToClear.Add(c);
            }

            if (clearedCellsOut != null)
            {
                clearedCellsOut.Clear();
                var seen = new HashSet<(int, int)>();
                foreach (var r in rowsToClear)
                {
                    for (int c = 0; c < Size; c++)
                    {
                        if (seen.Add((r, c))) clearedCellsOut.Add(new ClearedCell(r, c, _cellColors[r, c]));
                    }
                }
                foreach (var c in colsToClear)
                {
                    for (int r = 0; r < Size; r++)
                    {
                        if (seen.Add((r, c))) clearedCellsOut.Add(new ClearedCell(r, c, _cellColors[r, c]));
                    }
                }
            }

            foreach (var r in rowsToClear)
            {
                for (int c = 0; c < Size; c++) _cells[r, c] = false;
            }
            foreach (var c in colsToClear)
            {
                for (int r = 0; r < Size; r++) _cells[r, c] = false;
            }

            return rowsToClear.Count + colsToClear.Count;
        }

        public bool IsEmpty()
        {
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    if (_cells[r, c]) return false;
                }
            }
            return true;
        }

        public void Reset()
        {
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    _cells[r, c] = false;
                }
            }
        }
    }
}
