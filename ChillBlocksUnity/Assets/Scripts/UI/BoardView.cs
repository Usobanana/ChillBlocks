using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ChillBlocks.Core;

namespace ChillBlocks.UI
{
    /// <summary>
    /// 8×8盤面の描画・ヒットテストのみを担当する（ドラッグ入力自体は持たない）。
    /// ChillBlocksは盤面もUI Toolkitで完結させる（SortGemsのuGUI+UIToolkitハイブリッドは踏襲しない、design-spec.html参照）。
    /// </summary>
    public class BoardView
    {
        // ライン消しエフェクト用の演出パラメータ（SortGems GemCellView.CompletedScaleRoutineのタイミングを踏襲）。
        private const float PopDuration = 0.1f;
        private const float VanishDuration = 0.12f;
        private const float PopScale = 1.3f;
        private const float StaggerPerCell = 0.03f;

        private readonly VisualElement _container;
        private readonly VisualElement[,] _cells = new VisualElement[BoardState.Size, BoardState.Size];
        private readonly MonoBehaviour _host;

        public VisualElement Container => _container;

        /// <param name="host">ライン消し演出のコルーチンを回すためのMonoBehaviour（通常はScreenManager自身）。</param>
        public BoardView(VisualElement container, MonoBehaviour host)
        {
            _container = container;
            _host = host;
            Build();
        }

        private void Build()
        {
            _container.Clear();
            _container.style.flexDirection = FlexDirection.Column;

            for (int r = 0; r < BoardState.Size; r++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.pickingMode = PickingMode.Ignore;

                for (int c = 0; c < BoardState.Size; c++)
                {
                    var cell = new VisualElement();
                    cell.AddToClassList("board-cell");
                    cell.style.width = PieceVisual.CellSize;
                    cell.style.height = PieceVisual.CellSize;
                    cell.pickingMode = PickingMode.Ignore;
                    row.Add(cell);
                    _cells[r, c] = cell;
                }

                _container.Add(row);
            }
        }

        public void Refresh(BoardState board)
        {
            for (int r = 0; r < BoardState.Size; r++)
            {
                for (int c = 0; c < BoardState.Size; c++)
                {
                    var cell = _cells[r, c];
                    bool filled = board.IsFilled(r, c);
                    cell.EnableInClassList("board-cell-filled", filled);
                    if (filled)
                    {
                        cell.style.backgroundColor = board.GetCellColor(r, c);
                    }
                    else
                    {
                        cell.style.backgroundColor = StyleKeyword.Null;
                    }
                }
            }
        }

        /// <summary>盤面コンテナのローカル座標から対応する行・列を求める。範囲外はfalse。</summary>
        public bool TryGetCellAtLocalPoint(Vector2 localPoint, out int row, out int col)
        {
            row = col = -1;
            var rect = _container.contentRect;
            if (rect.width <= 0 || rect.height <= 0) return false;
            if (!rect.Contains(localPoint)) return false;

            float cellW = rect.width / BoardState.Size;
            float cellH = rect.height / BoardState.Size;
            col = Mathf.Clamp(Mathf.FloorToInt(localPoint.x / cellW), 0, BoardState.Size - 1);
            row = Mathf.Clamp(Mathf.FloorToInt(localPoint.y / cellH), 0, BoardState.Size - 1);
            return true;
        }

        public void ClearPreview()
        {
            for (int r = 0; r < BoardState.Size; r++)
            {
                for (int c = 0; c < BoardState.Size; c++)
                {
                    _cells[r, c].RemoveFromClassList("board-cell-preview-ok");
                }
            }
        }

        /// <summary>
        /// 配置可能な位置のプレビュー表示。実機フィードバックにより、置けない位置は
        /// そもそも呼び出し側（ScreenManager）でスナップ探索して除外するため、常に
        /// 「配置可能」の見た目（緑）のみを表示する。
        /// </summary>
        public void ShowPreview(PieceDefinitions.Definition piece, int originRow, int originCol)
        {
            ClearPreview();

            for (int r = 0; r < piece.Rows; r++)
            {
                for (int c = 0; c < piece.Cols; c++)
                {
                    if (!piece.Cells[r, c]) continue;
                    int rr = originRow + r;
                    int cc = originCol + c;
                    if (rr < 0 || cc < 0 || rr >= BoardState.Size || cc >= BoardState.Size) continue;
                    _cells[rr, cc].AddToClassList("board-cell-preview-ok");
                }
            }
        }

        /// <summary>
        /// ライン消し演出。SortGemsのGridView.HandleStageCleared→GemCellView.PlayCompletedEffectと同じ
        /// 「(row+col)*0.03sずつ遅延させてEaseOutBackで拡大」パターンを踏襲するが、SortGemsは消えずに
        /// 100%へ戻って留まるのに対し、ChillBlocksはライン消しでマス自体が消えるため、拡大後はそのまま
        /// 縮小・フェードアウトして空きマスへ戻す。
        /// </summary>
        public void PlayClearEffect(IReadOnlyList<ClearedCell> cells)
        {
            if (_host == null) return;

            foreach (var cell in cells)
            {
                var element = _cells[cell.Row, cell.Col];
                float delay = (cell.Row + cell.Col) * StaggerPerCell;
                _host.StartCoroutine(ClearCellRoutine(element, cell.Color, delay));
            }
        }

        private IEnumerator ClearCellRoutine(VisualElement cell, Color color, float delay)
        {
            // Refresh()が先に呼ばれて空欄色になっていても、演出の間だけは消える瞬間の色を再現する。
            cell.style.backgroundColor = color;
            cell.transform.scale = Vector3.one;
            cell.style.opacity = 1f;

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            yield return ScalePopRoutine(cell);
            yield return ScaleFadeOutRoutine(cell);

            cell.style.backgroundColor = StyleKeyword.Null;
            cell.transform.scale = Vector3.one;
            cell.style.opacity = 1f;
        }

        private static IEnumerator ScalePopRoutine(VisualElement element)
        {
            float elapsed = 0f;
            while (elapsed < PopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / PopDuration);
                float scale = Mathf.LerpUnclamped(1f, PopScale, EaseOutBack(t));
                element.transform.scale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            element.transform.scale = new Vector3(PopScale, PopScale, 1f);
        }

        private static IEnumerator ScaleFadeOutRoutine(VisualElement element)
        {
            float elapsed = 0f;
            while (elapsed < VanishDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / VanishDuration);
                float scale = Mathf.Lerp(PopScale, 0f, t);
                element.transform.scale = new Vector3(scale, scale, 1f);
                element.style.opacity = 1f - t;
                yield return null;
            }
        }

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float tt = t - 1f;
            return tt * tt * ((overshoot + 1f) * tt + overshoot) + 1f;
        }
    }
}
