mergeInto(LibraryManager.library, {
  VK_Save: function(jsonPtr){ var s=UTF8ToString(jsonPtr); try{ localStorage.setItem('vk_meta', s); if(typeof vkBridge!=='undefined') vkBridge.send('VKWebAppStorageSet',{keys: s}); }catch(e){} },
  VK_Load: function(){ try{ var s=localStorage.getItem('vk_meta')||""; var len=lengthBytesUTF8(s)+1; var ptr=_malloc(len); stringToUTF8(s,ptr,len); return ptr; }catch(e){ return 0; } },
  VK_ShowRewarded: function(){ try{ if(typeof vkBridge!=='undefined') vkBridge.send('VKWebAppShowNativeAds',{ad_format:'reward'}); return true; }catch(e){ return false; } },
});
