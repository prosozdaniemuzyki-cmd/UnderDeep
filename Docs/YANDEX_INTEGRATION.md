# Интеграция Yandex Games — чек-лист

1. Залей WebGL билд (Brotli) на ЯДиск / хостинг, укажи URL в кабинете разработчика
2. В `Plugins/WebGL/YandexSDK.jslib` уже есть вызовы `ysdk.adv.showFullscreenAdv` / `showRewardedVideo` / `getLeaderboards`
3. В Boot вызывай `GameReady()` только после загрузки UI — иначе Яша не покажет игру
4. Лидерборды: создай в кабинете `maxDepth` и `richRun`, вызывай `SetLeaderboardAsync`
5. Rewarded плейсменты: `revive` и `double_loot` — только по клику, проверяй `success`
6. Interstitial — не чаще 180с, вызывай при входе в Базу
7. Язык: `ysdk.environment.i18n.lang` → `Localization.Get`
8. Тест: локально `npm i -g serve && serve -l 8000 build/`, открой `?playtest=1`
