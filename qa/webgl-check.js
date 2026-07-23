// ChillBlocks WebGLビルドの自動QAチェック（Playwright使用）。
// Claude Codeが「ビルドを確認して」と依頼された際に実行する想定のスクリプト。
// 実行: node qa/webgl-check.js [URL]
//   URL省略時は公開GitHub Pagesを対象にする。
//
// チェック内容:
//   1. コンソールエラー / 読み込み失敗（404等）の検出
//   2. 複数アスペクト比でのTitle画面レイアウト崩れ確認（スクリーンショット）
//   3. 標準ビューポートでの画面遷移（Title→Play→GamePlay→Settings）のスクリーンショット
//
// 出力: qa/output/<timestamp>/ 以下にスクリーンショットとissues.jsonを保存

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const TARGET_URL = process.argv[2] || 'https://usobanana.github.io/ChillBlocks/';
const LOAD_WAIT_MS = 15000;

// 検証するビューポート（幅x高さ）。UI ToolkitのPanelSettings参照解像度は1080x1920(9:16)。
// standardが基準。narrowは縦長端末、wideは横幅にゆとりのある端末を想定した負荷テスト用。
const VIEWPORTS = {
  standard: { width: 480, height: 854 },  // ~9:16、基準
  narrow: { width: 400, height: 900 },    // より縦長（9:20.25）
  wide: { width: 500, height: 760 },      // より横幅にゆとり（9:13.7）
};

const outDir = path.join(__dirname, 'output', new Date().toISOString().replace(/[:.]/g, '-'));
fs.mkdirSync(outDir, { recursive: true });

function attachDiagnostics(page, issues) {
  page.on('console', (msg) => {
    if (msg.type() === 'error') {
      issues.push({ type: 'console-error', text: msg.text() });
    }
  });
  page.on('pageerror', (err) => {
    issues.push({ type: 'page-error', text: err.message });
  });
  page.on('requestfailed', (req) => {
    issues.push({ type: 'request-failed', url: req.url(), reason: req.failure()?.errorText });
  });
  page.on('response', (res) => {
    if (res.status() >= 400) {
      issues.push({ type: 'http-error', url: res.url(), status: res.status() });
    }
  });
}

async function checkViewportLayout(browser, name, viewport, issues) {
  const page = await browser.newPage({ viewport });
  attachDiagnostics(page, issues);
  await page.goto(TARGET_URL, { waitUntil: 'load', timeout: 60000 });
  await page.waitForTimeout(LOAD_WAIT_MS);
  await page.screenshot({ path: path.join(outDir, `title_${name}_${viewport.width}x${viewport.height}.png`) });
  await page.close();
}

async function checkScreenFlow(browser, issues) {
  const viewport = VIEWPORTS.standard;
  const page = await browser.newPage({ viewport });
  attachDiagnostics(page, issues);
  await page.goto(TARGET_URL, { waitUntil: 'load', timeout: 60000 });
  await page.waitForTimeout(LOAD_WAIT_MS);
  await page.screenshot({ path: path.join(outDir, 'flow_1_title.png') });

  // Settingsボタン（画面右上）を開いて閉じる
  await page.mouse.click(viewport.width - 40, 22);
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(outDir, 'flow_2_settings.png') });
  await page.keyboard.press('Escape').catch(() => {});
  await page.waitForTimeout(500);

  // Playボタン（画面下部中央、CTA配置）
  await page.mouse.click(viewport.width / 2, Math.round(viewport.height * 0.78));
  await page.waitForTimeout(4000);
  await page.screenshot({ path: path.join(outDir, 'flow_3_gameplay.png') });

  await page.close();
}

(async () => {
  const browser = await chromium.launch();
  const issues = [];

  console.log(`Target: ${TARGET_URL}`);
  console.log('Checking Title screen layout across viewports...');
  for (const [name, viewport] of Object.entries(VIEWPORTS)) {
    await checkViewportLayout(browser, name, viewport, issues);
  }

  console.log('Walking through screen flow (Title -> Settings -> Play -> GamePlay)...');
  await checkScreenFlow(browser, issues);

  await browser.close();

  fs.writeFileSync(path.join(outDir, 'issues.json'), JSON.stringify(issues, null, 2));

  console.log(`\nDone. Output: ${outDir}`);
  console.log(`Issues detected: ${issues.length}`);
  if (issues.length > 0) {
    console.log(JSON.stringify(issues, null, 2));
  }
})();
