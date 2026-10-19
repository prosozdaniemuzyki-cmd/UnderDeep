# Платформенный слой — Yandex Games / VK / Standalone

## Общая схема

```
Game Code → IPlatformProvider → (YandexProvider | VKProvider | StandaloneProvider)
                ↕
         Plugins/WebGL/*.jslib → JS SDK (YaGames, VK Bridge)
```

Никакого `#if UNITY_WEBGL` в геймплее. Только в `Platform/`.

## Инициализация (Boot сцена)

```csharp
var provider = PlatformFactory.Create(); // по Application.platform + js env
await provider.InitAsync();              // YaGames.init() или VK init
ServiceLocator.Register<IPlatformProvider>(provider);
var save = await provider.LoadAsync();   // MetaSave
ServiceLocator.Register(new SaveService(provider, save));
provider.GameReady();                    // критично для Яндекса
```

Порядок важен: `GameReady()` только когда сцена загружена и UI готов.

## Yandex Games

### JSLib: Plugins/WebGL/YandexSDK.jslib

```
YandexInit, YandexAuth, YandexSave, YandexLoad, YandexLeaderboardSet,
YandexShowInterstitial, YandexShowRewarded, YandexGameReady, YandexGameplayStart/Stop
```

JS вызывает `ysdk`, `player`, `ysdk.adv`.

### Лидерборды

- `maxDepth` — `SetLeaderboardAsync("maxDepth", maxDepth)`
- `lootPerRun` — `SetLeaderboardAsync("richRun", totalLoot)`

Вызывать после `Evac`/`Death`.

### Реклама

- Interstitial: `ShowInterstitialAsync()` — не чаще 1/180 сек. Вызывать при входе в Базу.
- Rewarded: `ShowRewardedAsync("revive")` и `"double_loot"` — только по клику игрока (требование Яндекса).

### I18n

`provider.Language` = `ysdk.environment.i18n.lang` (ru/en/tr...). Фолбэк `en`.

## VK Play / VK Games

- `VKBridge.jslib` — `vkBridge.send("VKWebAppInit")`, `StorageSet/Get`, `ShowNativeAds`
- Если VK SDK недоступен — фолбэк на `StandaloneProvider`.

## Standalone (Editor, itch.io без SDK)

- `PlayerPrefs` JSON для `MetaSave`
- Реклама — заглушка (сразу `true` для Rewarded в Editor, `false` в билде)
- Лидерборды — локальные (PlayerPrefs)

## Сохранения

```json
{
  "version": 1,
  "resources": { "RustMetal": 120, "Crystal": 30 },
  "purchasedUpgrades": ["drill_mk2", "battery_1"],
  "artifacts": ["artifact_1"],
  "maxDepthReached": 342
}
```

Размер < 4 КБ, подходит для Yandex Storage (лимит 10 МБ).

## Чек-лист WebGL билда

- Player Settings: WebGL, Brotli, Code Opt: Size, Exceptions: None
- Publishing: Decompression Fallback = true
- Canvas: 1920x1080, matchWebGLToCanvasSize
- Boot сцена первая в Build Settings

## Тестирование

- В Editor — всегда `StandaloneProvider` (логи в консоль)
- Для проверки Yandex — локальный сервер `http://localhost:8000` + `YandexProvider` принудительно
- Логи платформы: `Debug.Log($"[Platform:{provider.Id}] ...")`
