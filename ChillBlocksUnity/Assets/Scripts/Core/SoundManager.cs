using UnityEngine;

namespace ChillBlocks.Core
{
    /// <summary>
    /// SortGemsのSoundManager.csと同じ考え方（効果音は音声アセットを使わず、その場でサイン波+指数減衰の
    /// AudioClipを合成する）を移植したもの。ChillBlocksでは配置音とライン消し音の2種類のみ用意する。
    /// BGMのクロスフェード機構はまだBGM素材が無いため今回は移植していない（必要になったらSortGems
    /// AdManager同様SortGemsのSoundManager._bgmClips/CrossFadeRoutineパターンを参照）。
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource _sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
        }

        /// <summary>ピース配置時のクリック音。</summary>
        public void PlayPlace()
        {
            PlaySynth(440f, 0.12f, 18f, 0.55f);
        }

        /// <summary>ライン消し時の明るいチャイム音（配置音より高く・長め）。</summary>
        public void PlayLineClear()
        {
            PlaySynth(880f, 0.3f, 4f, 0.6f);
        }

        private void PlaySynth(float frequency, float duration, float decay, float volume)
        {
            _sfxSource.PlayOneShot(GenerateSynthSound(frequency, duration, decay), volume);
        }

        /// <summary>サイン波 + 指数減衰エンベロープでその場に効果音を合成する（音声アセット不要）。</summary>
        private static AudioClip GenerateSynthSound(float frequency, float duration, float decay)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Exp(-decay * t);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
            }

            var clip = AudioClip.Create("SynthSfx", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
