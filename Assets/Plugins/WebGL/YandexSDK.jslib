mergeInto(LibraryManager.library, {
  Save: function(jsonPtr) {
    var json = UTF8ToString(jsonPtr);
    try {
      if (typeof player !== 'undefined' && player) player.setData({ meta: json }).catch(()=>localStorage.setItem('meta_save', json));
      else localStorage.setItem('meta_save', json);
    } catch(e) { console.warn('[YandexSDK] Save fail', e); }
  },
  Load: function() {
    try {
      var s = localStorage.getItem('meta_save') || "";
      var len = lengthBytesUTF8(s)+1;
      var ptr = _malloc(len);
      stringToUTF8(s, ptr, len);
      return ptr;
    } catch(e) { return 0; }
  },
  ShowInterstitial: function() {
    try { if (typeof ysdk !== 'undefined' && ysdk.adv) ysdk.adv.showFullscreenAdv({ callbacks:{} }); } catch(e){}
  },
  ShowRewarded: function(placementPtr) {
    var placement = UTF8ToString(placementPtr);
    try {
      if (typeof ysdk !== 'undefined' && ysdk.adv) {
        ysdk.adv.showRewardedVideo({
          callbacks: {
            onRewarded: function(){ SendMessage('PLATFORM', 'OnRewardedSuccess', placement); },
            onClose: function(wasRewarded){ if(!wasRewarded) SendMessage('PLATFORM', 'OnRewardedClose', placement); }
          }
        });
        return true;
      }
    } catch(e){}
    return false;
  },
  SetLeaderboard: function(boardPtr, score) {
    var board = UTF8ToString(boardPtr);
    try { if (typeof ysdk !== 'undefined' && ysdk.getLeaderboards) ysdk.getLeaderboards().then(lb=>lb.setLeaderboardScore(board, score)); } catch(e){}
  },
  GameReady: function() { try { if(typeof ysdk!=='undefined' && ysdk.features) ysdk.features.LoadingAPI && ysdk.features.LoadingAPI.ready(); } catch(e){} console.log('[Yandex] GameReady'); },
  GameplayStart: function() { try { if(typeof ysdk!=='undefined' && ysdk.features) ysdk.features.GameplayAPI && ysdk.features.GameplayAPI.start(); } catch(e){} },
  GameplayStop: function() { try { if(typeof ysdk!=='undefined' && ysdk.features) ysdk.features.GameplayAPI && ysdk.features.GameplayAPI.stop(); } catch(e){} },
});
