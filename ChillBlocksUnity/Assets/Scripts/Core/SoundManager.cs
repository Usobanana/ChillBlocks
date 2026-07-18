using UnityEngine;

namespace ChillBlocks.Core
{
    /// <summary>
    /// 効果音およびBGMの再生を管理するクラス。
    /// 効果音はアセットを使用せずサイン波合成でリアルタイムに生成します。
    /// BGMは起動時に心地よいLo-Fiペンタトニックコード進行の音楽を動的に合成してループ再生します。
    /// Resources/BGM フォルダ内にオーディオファイルが用意された場合は、そちらを自動検知してランダム再生します。
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioSource _bgmSource;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            // AudioListenerがシーン内に存在しない場合は自動的にアタッチする
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    cam.gameObject.AddComponent<AudioListener>();
                    Debug.Log("SoundManager: Added AudioListener to Main Camera automatically.");
                }
                else
                {
                    gameObject.AddComponent<AudioListener>();
                    Debug.Log("SoundManager: Added AudioListener to SoundManager automatically (No Main Camera found).");
                }
            }
        }

        private void Start()
        {
            UpdateVolumes();
            PlayBgm();
        }

        /// <summary>
        /// ボリュームの設定を同期します。SettingsManagerから音量が変更された際に呼び出されます。
        /// </summary>
        public void UpdateVolumes()
        {
            if (SettingsManager.Instance != null)
            {
                _bgmSource.volume = SettingsManager.Instance.BgmVolume;
                _sfxSource.volume = SettingsManager.Instance.SeVolume;
            }
        }

        private void PlayBgm()
        {
            // Resources/BGM フォルダからのロードを試みる
            AudioClip[] externalBgms = Resources.LoadAll<AudioClip>("BGM");
            if (externalBgms != null && externalBgms.Length > 0)
            {
                // ランダムに選択して再生
                AudioClip selected = externalBgms[Random.Range(0, externalBgms.Length)];
                _bgmSource.clip = selected;
                _bgmSource.Play();
                Debug.Log($"[SoundManager] Playing external BGM: {selected.name}");
            }
            else
            {
                // なければ Lo-Fi BGM をコード上で動的に自動生成して再生
                AudioClip generated = GenerateLoFiChillBgm();
                _bgmSource.clip = generated;
                _bgmSource.Play();
                Debug.Log("[SoundManager] Playing generated procedural Lo-Fi BGM");
            }
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

        /// <summary>ドラッグ中、配置可能な位置にスナップした瞬間の短い「チッ」音。</summary>
        public void PlaySnapTick()
        {
            PlaySynth(1200f, 0.05f, 30f, 0.4f);
        }

        private void PlaySynth(float frequency, float duration, float decay, float volume)
        {
            if (SettingsManager.Instance != null)
            {
                // 個別SE音量を反映
                volume *= SettingsManager.Instance.SeVolume;
            }
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

        /// <summary>
        /// C#でチルなコード進行とペンタトニックのメロディラインを持つBGMのAudioClipを自動合成します。
        /// </summary>
        private AudioClip GenerateLoFiChillBgm()
        {
            const int sampleRate = 44100;
            float bpm = 80f;
            float beatDuration = 60f / bpm;
            float measureDuration = beatDuration * 4f;
            float totalDuration = measureDuration * 8f; // 8小節 (約24秒)
            
            int sampleCount = Mathf.CeilToInt(sampleRate * totalDuration);
            var samples = new float[sampleCount];
            
            // Cメジャーペンタトニックスケールの周波数
            float[] scale = { 261.63f, 293.66f, 329.63f, 392.00f, 440.00f, 523.25f, 587.33f, 659.25f };
            
            int totalSteps = 64; // 8小節 × 8ステップ
            float stepDuration = beatDuration / 2f;
            int samplesPerStep = Mathf.FloorToInt(sampleRate * stepDuration);
            
            System.Random rand = new System.Random(1234); // シード固定で心地よい繰り返しにする
            
            for (int step = 0; step < totalSteps; step++)
            {
                int startIdx = step * samplesPerStep;
                
                // コード進行（C - Am - F - G）のルート音
                float baseFreq = 130.81f; // C3 (0-15)
                if (step >= 16 && step < 32) baseFreq = 110.00f; // A2 (16-31)
                else if (step >= 32 && step < 48) baseFreq = 87.31f;  // F2 (32-47)
                else if (step >= 48) baseFreq = 98.00f;  // G2 (48-63)
                
                // メロディの選択
                bool playMelody = rand.NextDouble() < 0.6;
                float melodyFreq = scale[rand.Next(scale.Length)];
                
                for (int i = 0; i < samplesPerStep; i++)
                {
                    int idx = startIdx + i;
                    if (idx >= sampleCount) break;
                    
                    float t = idx / (float)sampleRate;
                    float stepT = i / (float)sampleRate;
                    
                    // ベース音（サイン波、低音）
                    float baseWave = Mathf.Sin(2f * Mathf.PI * baseFreq * t);
                    
                    // メロディ音（サイン波、エンベロープ減衰）
                    float melodyWave = 0f;
                    if (playMelody)
                    {
                        float env = Mathf.Exp(-6f * stepT);
                        melodyWave = Mathf.Sin(2f * Mathf.PI * melodyFreq * t) * env;
                    }
                    
                    // ミックス
                    samples[idx] = (baseWave * 0.22f + melodyWave * 0.12f);
                }
            }
            
            var clip = AudioClip.Create("GeneratedBgm", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
