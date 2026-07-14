using UnityEngine;
using System.Runtime.InteropServices;

namespace ChillBlocks.Core
{
    /// <summary>
    /// 短い振動フィードバック。WebGLビルドではブラウザのVibration API（Assets/Plugins/WebGL/Vibration.jslib）
    /// を呼び出す。iOS Safariはこの API自体を未実装のため反応しない（既知の制約、Android Chrome等では動作）。
    /// </summary>
    public static class Haptics
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ChillBlocks_Vibrate(int milliseconds);
#endif

        public static void Tick()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ChillBlocks_Vibrate(20);
#elif UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
