using UnityEngine;

namespace ChillBlocks.Core
{
    /// <summary>
    /// 音量、バイブレーション、広告削除フラグなどゲームのシステム設定を管理・ローカル保存するマネージャー。
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        private const string KEY_BGM_VOLUME = "BGM_Volume";
        private const string KEY_SE_VOLUME = "SE_Volume";
        private const string KEY_VIBE_ENABLED = "Vibe_Enabled";
        private const string KEY_ADS_REMOVED = "Ads_Removed";

        public float BgmVolume { get; private set; }
        public float SeVolume { get; private set; }
        public bool VibeEnabled { get; private set; }
        public bool IsAdsRemoved { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
        }

        private void LoadSettings()
        {
            BgmVolume = PlayerPrefs.GetFloat(KEY_BGM_VOLUME, 0.5f);
            SeVolume = PlayerPrefs.GetFloat(KEY_SE_VOLUME, 0.5f);
            VibeEnabled = PlayerPrefs.GetInt(KEY_VIBE_ENABLED, 1) == 1;
            IsAdsRemoved = PlayerPrefs.GetInt(KEY_ADS_REMOVED, 0) == 1;
        }

        public void SetBgmVolume(float vol)
        {
            BgmVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat(KEY_BGM_VOLUME, BgmVolume);
            PlayerPrefs.Save();
            
            SoundManager.Instance?.UpdateVolumes();
        }

        public void SetSeVolume(float vol)
        {
            SeVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat(KEY_SE_VOLUME, SeVolume);
            PlayerPrefs.Save();
            
            SoundManager.Instance?.UpdateVolumes();
        }

        public void SetVibeEnabled(bool enabled)
        {
            VibeEnabled = enabled;
            PlayerPrefs.SetInt(KEY_VIBE_ENABLED, VibeEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void RemoveAds()
        {
            IsAdsRemoved = true;
            PlayerPrefs.SetInt(KEY_ADS_REMOVED, 1);
            PlayerPrefs.Save();
            
            // 広告バナーを非表示にする
            Ads.AdManager.Instance?.HideBanner();
        }
        
        public void TriggerVibration()
        {
            if (VibeEnabled)
            {
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
            }
        }
    }
}
