namespace ChillBlocks.UI
{
    /// <summary>
    /// 盤面・トレイ・ドラッグ中ゴーストで共通して使うセルの見た目サイズ。
    /// 3箇所すべてこの定数を参照することで、トレイのピースやドラッグ中の見た目が
    /// 盤面のマスと同じ大きさになるようにする（実機フィードバックで修正）。
    /// </summary>
    public static class PieceVisual
    {
        /// <summary>盤面1マスの見た目サイズ（px）。board-container 960x960・padding 8px・8x8・cell margin 2pxから逆算。
        /// ドラッグ中のゴーストにも使う（配置時と同じ大きさで持ち上がるように、との実機フィードバックで採用）。
        /// Block Blast!の実機スクショ（盤面が画面幅の9割程度）に合わせて720→960に拡大。</summary>
        public const float CellSize = 114f;

        /// <summary>トレイ（手持ち3ピース）表示用のセルサイズ。「選択しづらい」との実機フィードバックで元の18pxから3倍に拡大。</summary>
        public const float TrayCellSize = 54f;
    }
}
