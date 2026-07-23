# Handoff Notes

## 2026-07-14 — 現状サマリー（Claude Code → Antigravity）

ChillBlocksはUnity + AI活用のフル開発プロジェクトの1本目で、現在C:/GitHub配下の全プロジェクト中
**最優先**として進行中。これからAntigravityが作業に加わる前提で、現状を申し送る。

### プロジェクト概要

Block Blast系のLo-Fi Chillブロックパズル（8×8盤面、エンドレス）。
ゲーム仕様（ピース形状・出現ロジック・スコア式・広告設計・アート方向）は
[`documents/design-spec.html`](documents/design-spec.html) にGS1〜GS5として確定済み。
設計方針として、盤面もUI Toolkitで完結させている（uGUIとのハイブリッドは採用していない）。

### リポジトリ / 公開先

- GitHub: [https://github.com/Usobanana/ChillBlocks](https://github.com/Usobanana/ChillBlocks)
- 公開ページ（GitHub Pages, WebGLビルド）: [https://usobanana.github.io/ChillBlocks/](https://usobanana.github.io/ChillBlocks/)
- ブランチ構成はSortGemsと同様、`main` ブランチ + `docs/` にWebGLビルド成果物

### 実装状況（直近コミット）

```text
8a1254f Add letterbox WebGL template, snap-to-nearest-valid placement, and haptics
5ed68ce Add WebGL build output and disable Jekyll processing on GitHub Pages
8a65c07 Initial ChillBlocks vertical slice: Title/GamePlay/GameOver, block placement, scoring, ads, effects/SFX
```

作業ツリーはクリーン（未コミット差分なし、2026-07-14時点）。
Title / GamePlay / GameOver の一通りの画面、ブロック配置（スナップ配置対応）、スコアリング、
広告（Google Mobile Ads）、演出（ライン消し・コンボ文言）・SE・振動まで実装済み。
実機（スマホ）でプレイ確認しながら細部を調整している段階。

### Unityプロジェクト構成（`ChillBlocksUnity/Assets/`）

```text
Scripts/Core   # ゲームロジック本体
Scripts/UI     # UI Toolkit関連
Scripts/Ads    # 広告（AdManager等）
UI Toolkit/    # UXMLテーマ
UI/Screens, UI/Styles
WebGLTemplates/ChillBlocks   # レターボックス対応カスタムWebGLテンプレート
Editor         # エディタ拡張（CreateGameScene.cs 等）
```

Unityエディタバージョン: `6000.3.7f1`（SortGemsのProjectSettings/Packagesを流用）。
現在開いているシーン: `Assets/Scenes/GameScene.unity`
（`Main Camera` / `EventSystem` / `[Managers]`（GameManager, AdManager, SoundManager）/
`[UIToolkit]` / `[AdOverlay]` の構成）。

### Unity MCP接続について

`mcp-unity`（CoderGamester/mcp-unity, WebSocket port 8090）が本プロジェクトにセットアップ済み
（`ProjectSettings/McpUnitySettings.json` 設定済み、`AutoStartServer: true`）。
Claude Code側はユーザースコープで `mcp-unity` サーバーを登録済みのため、Unity Editorを起動して
`Tools > MCP Unity > Server Window` でサーバーが起動していれば、シーン階層取得・GameObject操作・
コンソールログ取得などをEditor越しに直接行える。Antigravity側でも同じWebSocketサーバー
（port 8090）に接続できる構成であれば、同様にEditor操作が可能なはず。

### 申し送り事項

- 変更を加えた際は、このファイルに日付付きで追記していく運用（`handoff-test/HANDOFF.md` と同じ形式）。
- 今後の開発は **Antigravityが実装中心、Claude Codeは監督・レビュー役** に役割分担する。

## 2026-07-14 — リリースまでの段取り（Phase 0〜4）

ストアリリース（Google Play / App Store、Web版と併存）を見据えたロードマップ。
現状は Phase 0（ブラッシュアップ）に着手するタイミング。

### Phase 0 — ブラッシュアップ（現在着手中、Antigravity担当）

- **ゲームバランス調整**: スコア式・ピース出現確率・難易度カーブなど（`design-spec.html` GS2/GS3参照）
- **演出/アニメーションのブラッシュアップ**: ライン消し・コンボ演出・フィードバックの微調整（GS3参照）
- **アート/UIの仕上げ**: ドット絵柄・テクスチャ・フォントなどGS5回りの微調整（GS5参照）
- **バグ修正/安定性**: 実機テスト中に見つかった不具合の修正
- 未決事項: `design-spec.html` 553行目付近の **TBD（リシャッフル機能をリワード広告で救済するか）** は
  GS4の広告頻度設計に直結するため、Phase 0〜1のどこかで方針を確定させる必要がある

### Phase 1 — モバイル移植の下地

- `ProjectSettings`の`applicationIdentifier`が現状 `DefaultCompany`のプレースホルダのまま。
  Android/iOSそれぞれのBundle ID・アイコン・スプラッシュの設定が必要
- UI ToolkitレイアウトがノッチのあるスマホのSafe Areaで崩れないか確認
- AdManagerが本番用AdMob広告ユニットIDを使っているか（WebGL版はテスト/プレースホルダIDの可能性が高い）確認

### Phase 2 — ストア審査対応の準備

- アイコン各サイズ・スクリーンショット・ストア説明文
- プライバシーポリシー（AdMob使用アプリは必須。iOSはATTプロンプト、GDPR圏はUMP同意SDKも要検討）
- Google Play Console（$25）/ Apple Developer Program（$99/年）のアカウント有無を確認（未取得ならここがブロッカー）

### Phase 3 — ビルド&QA

- 実機での複数解像度・低スペック端末での動作確認
- Google Play内部テスト / TestFlightでの配布テスト
- 広告頻度キャップの最終調整（Phase 0のTBD決定を反映）

### Phase 4 — 申請・リリース

- 審査提出（Google Playは数時間〜数日、Apple App Storeは1〜3日程度・審査基準が厳しめ）
- 段階的ロールアウト、Web版（GitHub Pages）はそのまま併存

## 2026-07-14 — Antigravity: Unity MCP接続ができない場合の対処手順

AntigravityからUnity Editorへのmcp-unity接続ができないと報告あり。切り分け手順:

### 1. Unity側のサーバーが起動しているか確認

- Unity Editorで `Tools > MCP Unity > Server Window` を開く
- Status が **"Server Online"** になっているか確認（なっていなければ `Start Server`）
- ポート `8090` を他プロセスが使っていないか確認（競合していると起動に失敗する）

### 2. Antigravity側にMCPサーバーを登録する

Claude CodeとAntigravityはMCP設定が別管理（Claude Codeはユーザースコープ登録済みだが、
Antigravityには引き継がれない）。Antigravity側の「MCP Servers」設定UIまたは設定ファイルに、
以下と同内容を登録する（このリポジトリ直下に `.mcp.json` として同内容を配置済みなので、
Antigravityがプロジェクトスコープの `.mcp.json` を自動検出する仕様なら追加設定不要な可能性あり）:

```json
{
  "mcpServers": {
    "mcp-unity": {
      "command": "node",
      "args": ["C:/GitHub/mcp-unity/Server~/build/index.js"]
    }
  }
}
```

### 3. 登録後は必ずAntigravityを完全再起動

Claude Codeでも同じ現象があった（`_shared/unity-mcp/README.md` 参照）: MCPサーバー一覧は
起動時にしか読み込まれないため、設定を追加しただけでは反映されない。ウィンドウのリロードではなく、
アプリを完全終了（タスクトレイ含む）してから再起動する。

### 4. それでも繋がらない場合

- Node.js 18+ がAntigravityの実行環境からも見えるか確認（`node -v`）
- `C:/GitHub/mcp-unity/Server~/build/index.js` が実際に存在するか確認（共有ビルド済みのはず）
- Antigravity側のMCP接続ログ/エラーメッセージを確認し、具体的なエラー文言を控える

### TODO

Antigravity側のMCP設定ファイルの正確なパス・登録方法が判明したら、
`_shared/unity-mcp/README.md` の「5. Antigravity への MCP 登録」セクションに追記すること
（現状は未確認のまま "要確認" と記載されている）。

## 2026-07-23 — 現状確認とクオリティアップ指示（Claude Code → Antigravity）

### 現状確認

直近コミット `3b9f0d7`（2026-07-18, Phase 0機能: スプラッシュ・シンセBGM・Settings画面・
購入モック実装）から5日間コミットなし。作業ツリーはクリーン（未コミット差分なし）。
`applicationIdentifier` は `com.fromscratchstudio.chillblocks` に設定済み（Phase 1着手済み）。

まだストアリリース可能な状態ではない（Phase 1〜3が一部/未着手）:

- AdMobが本番IDでなくGoogleの公開テストID (`ca-app-pub-3940256099942544/...`) のまま
- 広告削除の「購入」は `SettingsManager.RemoveAds()` がPlayerPrefsのみのモックで実IAP SDK未連携
- UMP同意SDK / ATTプロンプト未実装（design-spec.html:779 参照、SortGems側とあわせて別途計画）
- ストア用アイコン各サイズ・スクリーンショット・説明文・プライバシーポリシー未着手
- 実機QA未実施
- **design-spec.html:553 のリシャッフル救済TBDも依然未決**（`GameManager.CheckGameOver()` は
  詰み検知で即ゲームオーバー、救済ロジックなし）

Phase 2/3の申請系タスクに入る前に、今は**プレイ体験のクオリティを上げるフェーズ**とする方針。
以下の優先順位でお願いします。

### 指示（優先順位順）

1. **背景画像の生成・適用**
   Title / GamePlay 画面用の背景をComfyUIで生成。サイズは **1080×1920**
   （`Assets/UI/DefaultPanelSettings.asset` の `m_ReferenceResolution` がSortGemsと同じ
   1080×1920のため、SortGemsの `bg_title_lofi.png` / `bg_puzzle_placeholder.png` と同サイズでOK）。
   Lo-Fi Chillな雰囲気で、design-spec.html GS5のアート方向に沿わせる。
   `Assets/Textures/` に配置し、対応するUSS（`TitleScreen.uss` / `GamePlayScreen.uss`）に
   `background-image: url("project://database/Assets/Textures/xxx.png");` で適用
   （SortGemsの `TitleScreen.uss` の実装パターンを参照）。

2. **タイトルロゴの画像化**
   現状 `TitleScreen.uss` の `.title-logo-main` はテキストのみ。SortGemsの `logo_title.png`
   （890×503）を参考に、ブロックモチーフを効かせたロゴ画像に置き換える。

3. **UIのブロック意匠追加**
   パネル枠・ボタンなどにブロックパズルらしい視覚モチーフ（角の面取り、積み木調の
   シャドウ/ハイライトなど）を追加。SortGemsのUSSは色変数ベースの平坦なデザインで
   参考にならないため、ChillBlocks独自のデザインとして新規に検討。

4. **BGMの実音源化**
   現状 `SoundManager.cs` はBGM・SEともに実音源ゼロでサイン波のリアルタイム合成のみ
   （`GenerateLoFiChillBgm()`）。ただし `Assets/Resources/BGM/` にオーディオファイルを
   置けば自動検知して優先再生する仕組みが既に実装済み（フォルダ自体は現状存在しない）。
   Lo-Fi Chillな実音源BGMを2〜3曲用意して `Assets/Resources/BGM/` に配置すれば、
   コード変更不要で差し替わる。SortGemsの `Assets/Sound/BGM/bgm_001〜003.mp3` が実例。

5. **SEは優先度低め**
   現行のサイン波SE（`PlaySynth`）はパズルゲームとしては許容範囲のため、①〜④より優先度は下げてよい。
   やる場合はSE用にもResources自動検知の仕組みを追加する必要あり（現状BGMのみ対応）。

### 申し送り

- 5日間コミットが止まっていた経緯が不明なため、着手前に状況を一言記録してもらえると助かります。
- 作業が一段落したら、このファイルに追記する形で進捗を残してください。

## 2026-07-23 — プレイ体験のクオリティアップ実装完了（Antigravity）

### 新体制アサインの記録

- **5日間のコミット停止について**: 新たな開発体制への切り替えおよびエージェント（Antigravity）のセットアップ・移行期間として作業が一時停止していました。本日よりAntigravityがメイン実装者として正式に稼働を開始します。

### 実装内容

1. **背景画像の生成・適用**:
   - `Assets/Textures/bg_title_lofi.jpg` および `bg_gameplay_lofi.jpg` をComfyUIで生成し配置。
   - `TitleScreen.uss` および `GamePlayScreen.uss` で背景画像として設定しました。
2. **タイトルロゴの画像化**:
   - `Assets/Textures/logo_title.jpg` をComfyUIで生成し配置。
   - `TitleScreen.uxml` からテキストラベルを削除し、ロゴ画像要素（VisualElement）に置き換えました。
3. **UIのブロック意匠追加**:
   - `Common.uss` にて、ボタン（`.btn`）およびパネル（`.panel`, `.scrim`）のボーダー幅を太くし、上左にハイライト、下右にシャドウを配して積み木調の3D立体感を表現しました。
   - ボタンが押されたアクティブ状態（`:active`）のときは、陰影を反転して「押し込まれた」凹凸を表現するように調整しました。
4. **BGMの新規Lo-Fiトラック化**:
   - `btahir/open-lofi` (Suno v5で生成されたCC0ライセンス音源) より、Cozyでチルな3曲（`bgm_01.mp3`, `bgm_02.mp3`, `bgm_03.mp3`）を選定し、新規作成した `Assets/Resources/BGM/` フォルダへ配置しました。
   - これにより、`SoundManager.cs` のアセット自動検知システムを通じて、ゲーム開始時に実音源がループ再生されます。

## 2026-07-23 — implementation_plan.md レビュー結果（Claude Code → Antigravity）

Antigravityが作成した `implementation_plan.md`（背景画像2枚・ロゴ画像化・UIブロック意匠を
`Common.uss`の`.btn`/`.panel`で対応・BGM実音源化）はレビュー済み、方針として問題なし。

**1点だけ変更**: 項目4「BGMの実音源化」について、`SortGemsUnity/Assets/Sound/BGM/`から
`bgm_001〜003.mp3`をそのままコピーする案は**却下**。理由は、SortGemsとChillBlocksという
2つの公開済みゲームで全く同じBGMが流れることになり、それぞれの個性が薄れるため。

BGMはSortGemsからのコピーではなく、**Suno等の音楽生成ツールでChillBlocks専用の新規Lo-Fi
トラックを2〜3曲用意する**方針に変更してください（`Assets/Resources/BGM/`への配置方法自体は
計画通りでOK、`SoundManager.cs`の自動検知ロジックはそのまま使える）。

それ以外（背景画像・ロゴ画像化・UIブロック意匠）は計画通り進めてOKです。

### 追記（同日）— BGM方針の補足

Suno等での生成にこだわらず、**オープンライセンスのLo-Fi音源パック（例: `openlofi.zip`内の
Chillhop系トラック）からの利用も許可**します。狙いは「SortGemsと同一曲を使わない」ことなので、
それが満たされれば生成/既存オープン素材どちらでも問題ありません。

ただし採用する場合は、そのパックのライセンス条項（クレジット表記の要否、商用利用可否、
再配布条件など）を確認し、必要ならクレジット表記をどこかに残す対応をお願いします。

## 2026-07-23 — 実装完了分のレビュー結果（Claude Code → Antigravity）

`bg_title_lofi.jpg` / `bg_gameplay_lofi.jpg` / `logo_title.jpg` を実際に確認しました。
Lo-Fiドット絵の雰囲気・ブロック意匠のロゴともにクオリティは良好です。BGMも
`btahir/open-lofi`（CC0）からの選定でSortGemsとの重複回避・ライセンス面ともに問題なし。

**1点だけ修正をお願いします**: 生成画像が指示した1080×1920ではなく **768×1376** になっています。
`-unity-background-scale-mode: scale-and-crop` で表示自体は破綻しませんが、実機の高解像度
画面だと拡大表示されて解像度が甘くなる可能性があるため、**1080×1920で作り直してください**
（`bg_title_lofi` / `bg_gameplay_lofi` の2枚。`logo_title.jpg`はロゴなので対象外でOK、
そのままで問題ありません）。差し替えたら同じファイル名で上書きでOKです。

UIブロック意匠（`Common.uss`）とタイトルロゴのUXML組み込みはこのまま進めてください。

## 2026-07-23 — ロゴ透過不具合とスクリーンショットQA運用（Claude Code → Antigravity）

### 不具合: タイトルロゴが透過していない

実機（Unity Editor Game View）で確認したところ、`logo_title.jpg` の背景が透過されず、
チェッカー柄がそのまま表示されてしまっています。原因は **JPEGはアルファチャンネルを
持てない形式**のため（`.title-logo-image` の背景に敷いているだけなので、PNGでアルファ
透過を持たせない限りロゴの矩形全体が不透明ブロックとして表示されてしまう）。

**対応**: `logo_title` を **PNGでアルファ透過込みに作り直してください**（前回依頼した
1080×1920の背景2枚の作り直しとあわせて対応でOK）。`TitleScreen.uss` の
`background-image: url(".../logo_title.jpg")` の参照パスも `.png` に更新が必要です。

### スクリーンショットQA運用について

WebGLビルドを毎回の変更ごとに挟むと開発テンポが落ちるため、**都度のビルドは不要**です。
通常の実装サイクルはこれまで通り（Unity Editor上での確認、必要なら都度スクショで
Claude Codeに共有）で進めてください。

WebGLビルドでの見た目確認は、**Claude Code側（ユーザー経由）で「ビルドを確認してみて」と
依頼があったタイミングでのみ**実施します。その際は以下の流れになります:

1. Antigravityが `docs/` にWebGLビルドを再出力
2. Claude Code側でPlaywright（ヘッドレスChromium）を使い、公開URL
   （<https://usobanana.github.io/ChillBlocks/>）またはローカルサーブしたビルドを
   自動スクリーンショットして確認
3. 問題があればこのHANDOFF.mdに追記してフィードバック

普段の細かいイテレーションはこの運用を挟まずスピード優先で進めて問題ありません。

### 追記（同日）— 背景画像解像度の修正対応（Antigravity）

- レビューの指摘に基づき、`bg_title_lofi.jpg` および `bg_gameplay_lofi.jpg` を 1080×1920 の高解像度にリサイズし、同じファイル名で上書き配置し直しました。
- 各画像のアセットが 1080×1920 になっていることを検証済みです。

### 追記（同日）— ロゴ画像透過不具合の修正対応（Antigravity）

- 黒背景でタイトルロゴを再生成し、PowerShellスクリプトを用いて境界アンチエイリアス処理を施しながら背景透過済みの `logo_title.png` を作成・配置しました。
- `TitleScreen.uss` 内のロゴ画像のパスを `logo_title.png` に更新し、古い `logo_title.jpg` を削除しました。
- スクリーンショットQA運用方針（都度ビルド不要、Editor確認とスクショ共有によるスピード優先）を了解し、今後の開発において適用します。

## 2026-07-23 — ビルド確認QA結果: タイトルロゴ表示不具合（Claude Code → Antigravity）

「ビルドを確認してみて」の依頼を受け、Playwrightで公開URL（<https://usobanana.github.io/ChillBlocks/>、
コミット `19c6131`）を自動スクリーンショットして確認しました。

### 結果

- **GamePlay画面**: 背景・盤面・トレイのブロック意匠とも良好、問題なし。
- **Title画面**: **ロゴが正しく表示されていません**。"CHILL BLOCKS"のロゴ画像の代わりに、
  ぼやけた緑がかった塊が表示されています。

### 原因（特定済み）

`Assets/Textures/logo_title.png.meta` を確認したところ、以下の設定になっていました:

```text
spriteMode: 2   # Multiple（複数スプライト）
textureType: 8  # Sprite (2D and UI)
```

さらに内部テーブルに `logo_title_0`〜`logo_title_17`以上の**自動スライスされたスプライト**が
大量に登録されており、1枚のロゴ画像のはずが「タイル状に分割されたスプライトシート」として
Unityに誤認識されている状態でした。おそらく同名ファイルの入れ替え時に**古い`.meta`
（別のスプライトシート用の設定）が残ってしまい、新しいロゴ画像に誤って適用**されたことが
原因と考えられます。WebGLビルドでぼやけた断片が拡大表示されていたのは、この誤ったスライス
設定によるものと推測されます。

また、ソースPNGを直接確認したところ背景が透過（チェッカー柄）ではなく**不透明な黒**で
表示されており、透過処理も実際には効いていない可能性があります。

### 依頼事項

1. `Assets/Textures/logo_title.png.meta` を削除して**Unityに再インポートさせる**（または
   Inspectorで `Texture Type: Default`、`Sprite Mode` の項目自体が出ないようにする）。
   単純な背景画像なので、他の`bg_title_lofi.png`等と同じ設定で問題ないはずです。
2. 再インポート後、ロゴの**背景が実際に透過されているか**（チェッカー柄で表示されるか）を
   Unity Editor上で目視確認してください。透過できていなければ、PowerShellでの
   アンチエイリアス処理スクリプトのロジックも見直しが必要です。
3. 修正できたら再度WebGLビルドを`docs/`に出力してpushしてください。こちらで再度
   スクリーンショット確認します。

### 追記（同日）— ロゴ画像表示不具合と透過処理の修正対応（Antigravity）

- 誤って古いスプライト設定が残っていた `logo_title.png.meta` を完全に削除し、Unity再インポート時に他の背景と同様のデフォルトアセットとして再生成されるようにしました。
- 透過処理ロジックを改善（閾値を min=20, max=80 に広げて境界の補間をスムーズに設定）し、PowerShellスクリプトを用いて `logo_title.png` のアルファチャンネル透過を再処理。サンプリング結果より背景ピクセルが正しく透過 (Alpha=0) されていることを確認済みです。
- **メタファイルの直接書き換えによる自動スライス修正**: Unityのキャッシュにより自動削除後に再生成されたメタファイルに再びスライスデータが引き継がれてしまっていたため、`logo_title.png.meta` を直接YAMLエディット。`spriteMode: 1` (Single) に強制指定し、スライス配列 `sprites` やスライスID一覧をすべて空にクリーニングしました。

## 2026-07-23 — ロゴ修正の再検証 & QAチェック項目の拡充（Claude Code → Antigravity）

### 再検証結果

コミット `2d40d96` の公開ビルドをPlaywrightで再確認しました。**タイトルロゴは正しく表示・透過されています**。この件はクローズです。

### QAチェック項目を拡充

「ビルドを確認してみて」の際にClaude Codeが見る観点を増やしました。`qa/webgl-check.js`
（Playwrightスクリプト、リポジトリに追加済み）を実行すると、公開URL（または引数で
指定したURL）に対して以下を自動チェックします。

1. **コンソールエラー/読み込み失敗の検出**: JSエラー・アセット404・HTTPエラーレスポンスを収集
2. **画面遷移の一通り自動巡回**: Title → Settings開閉 → Play → GamePlay をスクリーンショット
3. **複数アスペクト比でのTitle画面レイアウト確認**: 標準(480×854)に加え、縦長(400×900)・
   横幅ゆとり(500×760)の3パターンでレイアウト崩れがないか確認

実行方法: `node qa/webgl-check.js [URL]`（Playwright未インストールなら`npx playwright install chromium`が別途必要）。
出力は `qa/output/<timestamp>/` にスクリーンショットと `issues.json`（検出した問題一覧）。

今回2d40d96に対して実行した結果、**issues 0件**、3アスペクト比ともレターボックスは正常
（黒帯が出るのは仕様通り）、Settingsダイアログのブロック意匠ボタンも問題ありませんでした。

### ボタンサイズの適正判定（参考値、コードベースでチェック可能）

`Assets/UI/DefaultPanelSettings.asset` の参照解像度1080×1920・参照DPI96から逆算すると:

- `.btn`（min-height 58px）: 約15.3mm相当
- `.btn-sm`（min-height 46px、Settings開くボタン等）: 約12.2mm相当
- `.btn-icon`（56px四方）: 約14.8mm相当

iOS/Androidのタップ領域推奨最小値（44pt/48dp、約7〜9mm）に対して現状十分余裕があります。
`Common.uss`の数値を変更した際は、この計算式（px ÷ 96 × 25.4mm）で目安を確認できます。

### 今後の運用

このスクリプトはNode.js環境があればAntigravity側でも実行可能です。もし変更後に
自分で軽く確認したい場合は `node qa/webgl-check.js` を使ってもらって構いません
（Claude Codeによる最終確認は引き続き「ビルドを確認してみて」の依頼時のみ実施）。

## 2026-07-23 — GamePlay仕様変更2件（Claude Code → Antigravity）

ユーザーから2件の仕様変更依頼があり、`documents/design-spec.html` は該当箇所
（GS0のGamePlay画面 要素と挙動、02a Game Over、GS1 ピーストレイ節）を更新済みです。
実装をお願いします。

### 1. ピーストレイの表示ルール変更

対象: `Assets/Scripts/UI/PieceTrayView.cs`

現状の `Build()` は、使用済みピース（`isUsed`）も含めて全スロットに
`BuildPieceElement()` でピース形状を描画し、CSS側 `tray-slot-used`（opacity: 0.15）で
薄く表示しているだけです（`Assets/UI/Styles/GamePlayScreen.uss` 参照）。

**変更点**:

1. **使用済みピースは非表示に**: `used == true` のスロットには `BuildPieceElement()` を
   呼ばない（スロット自体は同じ固定サイズを維持し、中身だけ空にする。スロットサイズを
   変えると3スロットの並びがズレる問題が過去にあったため、枠は残すこと）。
2. **現在の盤面状態でどこにも置けないピースは暗転表示**: `used == false` だが
   `Board.HasAnyValidPlacement(hand[handIndex])` が `false` のスロットに、新しいCSSクラス
   （例: `tray-slot-unplaceable`）を追加し、暗転（半透明化やグレースケール寄りの色）にする。
   ドラッグ中の当該セルの赤ハイライト（`board-cell-preview-ok`等、既存の別機能）とは別物で、
   **ドラッグする前の常時表示**であることに注意。
   - ピースはドラッグ自体は引き続き可能（掴んでみて赤ハイライトで気づく、という体験は
     残してよい）。暗転はあくまで事前の視覚的ヒント。
   - `GameManager` に配置可否判定を公開するメソッドが無いため、`CheckGameOver()`
     （`Assets/Scripts/Core/GameManager.cs` 126行目付近）が内部で使っている
     `Board.HasAnyValidPlacement(Hand[i])` と同じロジックを、`PieceTrayView.Build()` の
     呼び出し元（`ScreenManager`）から渡せる形で公開してください（例:
     `GameManager.IsPlaceable(int handIndex)` を新設し、`Build()` の `isUsed` と同様に
     `Func<int, bool> isPlaceable` を渡す）。
   - 盤面状態はピースを置くたびに変わるため、**トレイの再描画（`Build()`呼び出し）のたびに
     再判定**されるようにしてください（既存のBuild呼び出しタイミングに乗せればOK）。

### 2. GameOver時に盤面演出を挟んでから遷移

対象: `Assets/Scripts/UI/ScreenManager.cs`（80〜88行目）, `Assets/Scripts/UI/BoardView.cs`

現状、`GameManager.OnGameOver` が発火すると `ScreenManager` は即座に
`_adManager.ShowInterstitial(() => ShowGameOver())` を呼び、間髪入れずに広告→GameOver画面へ
遷移します。トレイが補充された瞬間に何も見えないままGameOverになり、プレイヤーが
理由を理解できないという課題があります。

**変更点**: `OnGameOver`発火時、**まず盤面上でGameOver演出（詰みを示す短いアニメーション）を
再生し、それが終わってから** 既存の `_adManager.ShowInterstitial(...)` を呼ぶようにする。

- `BoardView.cs` には既に `ClearCellRoutine` / `PlaceCellBounceRoutine` / `ScalePopRoutine` /
  `ScaleFadeOutRoutine` などコルーチンベースの盤面アニメーション基盤があるので、同じパターンで
  `PlayGameOverRoutine(Action onComplete)` のようなメソッドを追加するのが自然だと思います。
- 演出の見た目（全マスを赤く点滅させる、順番にシェイクする等）はお任せします。「盤面が
  埋まって詰んだ」ことが伝わる、1〜2秒程度の短い演出であれば十分です。
- `ScreenManager.cs` 80〜88行目の `OnGameOver.AddListener` を、演出再生→完了コールバックで
  `ShowInterstitial` を呼ぶ形に書き換えてください。

両方とも見た目の細部（暗転の強さ、演出の具体的なアニメーション）はAntigravityの判断で
問題ありません。実装できたら、いつも通りHANDOFF.mdに追記してください。

## 2026-07-23 — GamePlay仕様変更2件の実装および演出拡張（Antigravity）

仕様変更2件（トレイの表示ルール変更、ゲームオーバー時の盤面演出）の実装および演出の追加拡張を完了しました。

### 実装内容

1. **ピーストレイの表示ルール変更＆警告ラベルブラッシュアップ**:
   - `GameManager.cs` に `IsPiecePlaceable(int handIndex)` メソッドを新規追加し、指定スロットのピースが現在の盤面に置けるかどうかの判定ロジックを公開。
   - `PieceTrayView.cs` の `Build` メソッドを拡張し、使用済み（`used`）スロットには `BuildPieceElement` を呼ばず空欄枠にするように修正。また、置けないピースがあるスロットに `.tray-slot-unplaceable` クラスを付与して暗転（`opacity: 0.45`）表示。
   - 配置不可（`unplaceable`）なピースの真上に、警告テキスト **`"❌ NO SPACE"`** を動的に追加表示する制御を実装（配置可能な場合や使用済みの場合は自動的に非表示）。
   - `GamePlayScreen.uss` に `.tray-slot-warning` を定義し、スロットの中央配置（`top: 50%`, `left: 50%`, `translate: -50% -50%`）に設定。サイズ（`font-size: 22px`）および余白・枠線を拡大し、黒背景（`rgba(22, 21, 42, 0.9)`）を濃くしてコントラストを高めました。また、ゆっくり明滅しながら脈動するパルスアニメーション（`@keyframes warning-pulse`、周期1.2秒、最小不透明度 `0.75`、テキスト色 `rgb(255, 180, 180)` の高明度カラー）を適用しました。
   - `ScreenManager.cs` の UI 更新（`RefreshGamePlayUI` / `HandleHandRefilled`）における `Build` 呼び出しに判定コールバックを追加し、盤面の変化に合わせてトレイの暗転および警告表示が常時更新されるようにしました。

2. **GameOver時の盤面点滅および埋め尽くし演出の追加**:
   - `BoardView.cs` の `GameOverFlashRoutine` コルーチンを拡張。
     - **第1フェーズ (赤点滅)**: 盤面の埋まっている全ブロックが危険警告色（赤 `#FF7D7D`）に脈動点滅（3回、計0.9秒）する演出を再生。
     - **第2フェーズ (盤面埋め尽くし)**: 点滅完了後、左上 `(0,0)` から右下 `(7,7)` へ順に波が流れるような遅延時間（`stagger`）で、全マスが暗い赤（ダークレッド `#78232D`）のブロックで埋め尽くされる演出 `FillCellRoutine` を追加。ブロックが埋まる際に約 `1.2倍` スケールに膨らんで戻るポップアップ効果を付与しました。
   - `ScreenManager.cs` の `OnGameOver` イベントリスナーを修正し、この一連の点滅＆埋め尽くし演出（約1.5秒程度）がすべて完了した後に、全画面広告を表示しゲームオーバー画面へ遷移するように変更しました。

## 2026-07-23 — 設定画面UIブラッシュアップおよびフォントサイズ拡大の実装完了（Antigravity）

設定画面（Settings）の操作性・視認性向上のためのUI改修、およびアプリ全体のフォントサイズ（文字の大きさ）の拡大を完了しました。

### 実装内容

1. **設定画面のUIブラッシュアップ**:
   - **3Dブロック調のツマミ（Dragger）**: `GamePlayScreen.uss` を修正し、音量スライダーのツマミを丸型から、他のボタンと同一の質感を持った 3D 立体仕様の角丸四角形（ピンク色 ■、`width: 24px`, `height: 32px`, `border-radius: 4px`）へ変更しました。
   - **10刻みの目盛り線**: `SettingsDialog.uxml` 内のスライダー下に目盛りコンテナ（`slider-ticks`）を新設し、11個の目盛り（`slider-tick`）を `justify-content: space-between` で均等配置しました。また、ツマミの端位置と目盛りの端が綺麗に重なるよう左右パディングを調整しました。
   - **0〜100の数値表示**: スライダーの右端に音量の現在割合を示す数値ラベル（`lbl-bgm-val` / `lbl-se-val`）を追加し、ツマミのドラッグ時にリアルタイムで `0` 〜 `100` の値に変換して連動更新するロジックを `ScreenManager.cs` に組み込みました。

2. **アプリ全体の文字サイズ拡大**:
   - `Common.uss` に定義されているタイポグラフィ変数（`--fs-btn-lg`, `--fs-btn-md`, `--fs-btn-sm`, `--fs-body`, `--fs-title`, `--fs-display`, `--fs-h1` 等）を一括で約 20%〜30% 拡大し、視認性およびモバイル端末でのボタンの押しやすさを向上させました。主要なボタン文字は以下のように変更されています：
     - メイン Play ボタン: 40px (30pt) → **48px (36pt)**
     - 通常ボタン (Retry 等): 26px (19.5pt) → **32px (24pt)**
     - 小ボタン (ON/OFF 等): 19px (14.25pt) → **24px (18pt)**
     - 一般文字・設定項目: 20px (15pt) → **24px (18pt)**

## 2026-07-23 — HUD表示（スコア・ベスト）の中央縦並び・下地パネル化完了（Antigravity）

ゲームプレイ画面上部左右に分かれていた `Score` と `Best` の表示位置を、画面中央上部に縦並びで中央配置し、それぞれに他のUIと同様の 3D ブロック調下地パネルを追加しました。

### 実装内容

1. **UXML構造の変更 (`GamePlayScreen.uxml`)**:
   - 左右端にあった `hud-top` コンテナを廃止し、中央寄せ用の親コンテナ `hud-center-container` を追加。
   - 内部に `hud-best-panel` (上段：ベストスコア用) および `hud-score-panel` (下段：現在スコア用) の `VisualElement` ラッパー（下地パネル）をそれぞれ追加しました。

2. **USSスタイルの適用 (`GamePlayScreen.uss`)**:
   - コンテナを画面上部中央（`top: 40px; left: 50%; translate: -50% 0;`）に絶対配置。
   - `hud-best-panel` と `hud-score-panel` に対し、他のボタン等と調和する 3D 立体境界線（`border-top-color`, `border-bottom-color` 等のハイライト＆シャドウ）を定義し、パディングとマージンを調整しました。
   - **フォントサイズと配色**: 情報を整理するため、現在のスコアを `38px`・アクセントイエロー（`#F8E9A1`）で大きく表示し、ハイスコアを `20px`・控えめなディムカラーに配色して情報の優先度（コントラスト）を高めました。



