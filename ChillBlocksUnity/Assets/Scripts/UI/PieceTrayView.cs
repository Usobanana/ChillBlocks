using System;
using UnityEngine;
using UnityEngine.UIElements;
using ChillBlocks.Core;

namespace ChillBlocks.UI
{
    /// <summary>
    /// 3ピースのトレイ描画とドラッグ入力の起点。実際の当たり判定・配置判定はScreenManager側が行う
    /// （BoardViewは盤面のヒットテストのみ、GameManagerは配置可否の判定のみを持つ）。
    /// </summary>
    public class PieceTrayView
    {
        private readonly VisualElement _container;
        private readonly VisualElement _dragLayer;
        private readonly VisualElement[] _slots = new VisualElement[HandGenerator.HandSize];

        private PieceDefinitions.Definition[] _hand;
        private VisualElement _ghost;
        private int _draggingHandIndex = -1;
        private float _ghostHalfWidth;
        private float _ghostHalfHeight;

        /// <summary>ドラッグ開始（handIndex, パネル座標）。</summary>
        public event Action<int, Vector2> PieceDragStarted;
        /// <summary>ドラッグ中の移動（パネル座標）。</summary>
        public event Action<Vector2> PieceDragMoved;
        /// <summary>ドラッグ終了（パネル座標）。</summary>
        public event Action<Vector2> PieceDragEnded;

        public PieceTrayView(VisualElement container, VisualElement dragLayer)
        {
            _container = container;
            _dragLayer = dragLayer;
        }

        public void Build(PieceDefinitions.Definition[] hand, Func<int, bool> isUsed, Func<int, bool> isPlaceable)
        {
            _hand = hand;
            _container.Clear();

            for (int i = 0; i < hand.Length; i++)
            {
                int handIndex = i;
                bool used = isUsed(handIndex);
                bool unplaceable = !used && !isPlaceable(handIndex);

                var slot = new VisualElement();
                slot.AddToClassList("tray-slot");
                slot.EnableInClassList("tray-slot-used", used);
                slot.EnableInClassList("tray-slot-unplaceable", unplaceable);
                slot.pickingMode = used ? PickingMode.Ignore : PickingMode.Position;

                if (!used)
                {
                    slot.Add(BuildPieceElement(hand[handIndex], PieceVisual.TrayCellSize));
                    slot.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt, handIndex, slot));
                    slot.RegisterCallback<PointerMoveEvent>(evt => OnPointerMove(evt, handIndex));
                    slot.RegisterCallback<PointerUpEvent>(evt => OnPointerUp(evt, handIndex, slot));
                }

                _container.Add(slot);
                _slots[i] = slot;
            }
        }

        private VisualElement BuildPieceElement(PieceDefinitions.Definition piece, float cellSize)
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.style.flexDirection = FlexDirection.Column;

            for (int r = 0; r < piece.Rows; r++)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.style.flexDirection = FlexDirection.Row;

                for (int c = 0; c < piece.Cols; c++)
                {
                    var cell = new VisualElement { pickingMode = PickingMode.Ignore };
                    cell.style.width = cellSize;
                    cell.style.height = cellSize;
                    // 空きマスにも同じmarginを付けないと、埋まっているマスだけ余白ぶん
                    // レイアウト上のサイズが変わり、行内でマスがズレて見える（実機フィードバックで発覚）。
                    cell.style.marginLeft = 2;
                    cell.style.marginRight = 2;
                    cell.style.marginTop = 2;
                    cell.style.marginBottom = 2;
                    if (piece.Cells[r, c])
                    {
                        cell.AddToClassList("piece-cell-filled");
                        cell.style.backgroundColor = piece.Color;
                    }
                    row.Add(cell);
                }

                root.Add(row);
            }

            return root;
        }

        private void OnPointerDown(PointerDownEvent evt, int handIndex, VisualElement slot)
        {
            slot.CapturePointer(evt.pointerId);
            _draggingHandIndex = handIndex;

            var piece = _hand[handIndex];
            const float cellFootprint = PieceVisual.CellSize + 4f; // セルの見た目サイズ + 左右(上下)のmargin2pxずつ
            _ghostHalfWidth = piece.Cols * cellFootprint / 2f;
            _ghostHalfHeight = piece.Rows * cellFootprint / 2f;

            _ghost?.RemoveFromHierarchy();
            _ghost = BuildPieceElement(piece, PieceVisual.CellSize);
            _ghost.AddToClassList("drag-ghost");
            _dragLayer.Add(_ghost);
            MoveGhost(evt.position);

            PieceDragStarted?.Invoke(handIndex, evt.position);
        }

        private void OnPointerMove(PointerMoveEvent evt, int handIndex)
        {
            if (_draggingHandIndex != handIndex) return;
            MoveGhost(evt.position);
            PieceDragMoved?.Invoke(evt.position);
        }

        private void OnPointerUp(PointerUpEvent evt, int handIndex, VisualElement slot)
        {
            if (_draggingHandIndex != handIndex) return;
            slot.ReleasePointer(evt.pointerId);
            HideGhost();
            _draggingHandIndex = -1;
            PieceDragEnded?.Invoke(evt.position);
        }

        private void MoveGhost(Vector2 panelPosition)
        {
            if (_ghost == null) return;
            // ピースの中央がポインターの位置に来るようにする（先端がポインターに来ると操作しづらい、との実機フィードバック対応）。
            _ghost.style.left = panelPosition.x - _ghostHalfWidth;
            _ghost.style.top = panelPosition.y - _ghostHalfHeight;
        }

        private void HideGhost()
        {
            _ghost?.RemoveFromHierarchy();
            _ghost = null;
        }
    }
}
