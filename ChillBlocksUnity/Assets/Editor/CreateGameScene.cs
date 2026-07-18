// CreateGameScene.cs — SortGemsのAssets/Editor/CreateGameScene.csと同じ
// 「メニューアイテムから編集時にシーンを組み立てる」パターン。
// ChillBlocksは盤面もUI Toolkitで完結するため、uGUI Canvas/GridLayoutGroup/プレハブ生成は不要。

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;
using ChillBlocks.Core;
using ChillBlocks.Ads;
using ChillBlocks.UI;

namespace ChillBlocks.EditorTools
{
    public static class CreateGameScene
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string PanelSettingsPath = "Assets/UI/DefaultPanelSettings.asset";
        // Unity組み込みのデフォルトランタイムテーマ（UI Toolkitモジュール同梱、Unityバージョンが同じなら固定GUID）。
        private const string DefaultThemeGuid = "d85ad1194e8f06b4bbe6c2d3536362a3";

        [MenuItem("Tools/ChillBlocks/Create Game Scene")]
        public static void CreateScene()
        {
            EnsureGoogleMobileAdsSettings();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- Camera ----
            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(40f / 255f, 40f / 255f, 69f / 255f); // --color-bg

            // ---- EventSystem（新Input System） ----
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // ---- Managers ----
            var managersGo = new GameObject("[Managers]");
            var gameManagerGo = new GameObject("GameManager", typeof(GameManager));
            gameManagerGo.transform.SetParent(managersGo.transform);
            var adManagerGo = new GameObject("AdManager", typeof(AdManager));
            adManagerGo.transform.SetParent(managersGo.transform);
            var soundManagerGo = new GameObject("SoundManager", typeof(SoundManager));
            soundManagerGo.transform.SetParent(managersGo.transform);
            var settingsManagerGo = new GameObject("SettingsManager", typeof(SettingsManager));
            settingsManagerGo.transform.SetParent(managersGo.transform);

            var gameManager = gameManagerGo.GetComponent<GameManager>();
            var adManager = adManagerGo.GetComponent<AdManager>();

            // ---- PanelSettings ----
            var panelSettings = CreateOrLoadPanelSettings();

            // ---- UIToolkit ----
            var uiToolkitGo = new GameObject("[UIToolkit]", typeof(UIDocument), typeof(ScreenManager), typeof(SafeAreaHelper));
            var uiDocument = uiToolkitGo.GetComponent<UIDocument>();
            uiDocument.panelSettings = panelSettings;

            var titleUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/TitleScreen.uxml");
            var gamePlayUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/GamePlayScreen.uxml");
            var gameOverUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/GameOverScreen.uxml");
            var companySplashUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/CompanySplash.uxml");
            var settingsDialogUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/SettingsDialog.uxml");

            var screenManager = uiToolkitGo.GetComponent<ScreenManager>();
            var so = new SerializedObject(screenManager);
            SetRef(so, "_titleScreen", titleUxml);
            SetRef(so, "_gamePlayScreen", gamePlayUxml);
            SetRef(so, "_gameOverScreen", gameOverUxml);
            SetRef(so, "_companySplash", companySplashUxml);
            SetRef(so, "_settingsDialog", settingsDialogUxml);
            SetRef(so, "_gameManager", gameManager);
            SetRef(so, "_adManager", adManager);
            so.ApplyModifiedPropertiesWithoutUndo();

            // ---- AdOverlay（バナー/全画面広告のモック専用。ScreenManagerが_root.Clear()する対象とは
            // 別のUIDocumentにして、画面遷移でモック広告が消えてしまわないようにする） ----
            var adOverlayGo = new GameObject("[AdOverlay]", typeof(UIDocument));
            var adOverlayDocument = adOverlayGo.GetComponent<UIDocument>();
            adOverlayDocument.panelSettings = panelSettings;
            adOverlayDocument.sortingOrder = 10; // [UIToolkit]（既定0）より前面に描画

            var adSo = new SerializedObject(adManager);
            SetRef(adSo, "_overlayDocument", adOverlayDocument);
            adSo.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[ChillBlocks] Game scene created at " + ScenePath);
        }

        private static PanelSettings CreateOrLoadPanelSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (existing != null) return existing;

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1080, 1920);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0f;

            var themePath = AssetDatabase.GUIDToAssetPath(DefaultThemeGuid);
            if (!string.IsNullOrEmpty(themePath))
            {
                var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
                if (theme != null)
                {
                    panelSettings.themeStyleSheet = theme;
                }
            }

            Directory.CreateDirectory("Assets/UI");
            AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            AssetDatabase.SaveAssets();
            return panelSettings;
        }

        private static void SetRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[ChillBlocks] Field not found on ScreenManager: {fieldName}");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static void EnsureGoogleMobileAdsSettings()
        {
            const string path = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
            if (File.Exists(path)) return;

            Directory.CreateDirectory("Assets/GoogleMobileAds/Resources");
            // AdMob SDKインポート前でもAdManagerがエラーを出さないよう、最低限のダミーアセットを置く。
            // (SDK未導入の間はAdManager側の#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR ブロックが
            //  コンパイルされないため、実際にはEditor上でこのファイルが読まれることはない。SortGemsとの
            //  構成パリティのための保険。)
            File.WriteAllText(path,
                "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\n" +
                "MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_Name: GoogleMobileAdsSettings\n");
        }
    }
}
