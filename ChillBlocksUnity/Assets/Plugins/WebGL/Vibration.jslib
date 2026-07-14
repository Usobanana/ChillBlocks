// Vibration.jslib — ブラウザのVibration API（navigator.vibrate）をC#から呼べるようにする。
// iOS Safariはこの API自体を実装していないため、対応ブラウザ（Android Chrome等）のみ振動する。
mergeInto(LibraryManager.library, {
  ChillBlocks_Vibrate: function (milliseconds) {
    if (window.navigator && window.navigator.vibrate) {
      window.navigator.vibrate(milliseconds);
    }
  }
});
