# CI/CD — автобилд и деплой в Yandex Games

## Что делает `deploy-yandex.yml`

1. На каждый `push` в `main` / `arena/**` собирает `Build/Yandex` через `game-ci/unity-builder` (Unity 6000.3.7f1, WebGL, Brotli)
2. Вызывает наш метод `UnderDeep.EditorTools.WebGLBuilder.BuildYandex` (тот же что в меню)
3. Проверяет размер, сохраняет `yandex-manifest.json`, льёт артефакт `DeepLone-Yandex-<sha>` (7 дней)
4. Если ветка `main` — зипует `DeepLone-Yandex.zip` и, если заданы секреты, заливает черновик в консоль Яндекса через `api.games.yandex.net`

## Секреты (Settings → Secrets → Actions)

| Секрет | Где взять | Обязат. |
|--------|-----------|---------|
| `UNITY_LICENSE` | `Unity → Manual activation` → файл `.ulf` → `base64` | Да для билда |
| `UNITY_EMAIL` / `UNITY_PASSWORD` | Твой Unity ID | Альтернатива лицензии |
| `YANDEX_OAUTH_TOKEN` | https://oauth.yandex.ru/authorize?response_type=token&client_id=... с правами `yagames:publish` | Для автозаливки |
| `YANDEX_GAME_ID` | Цифры из URL `yandex.ru/dev/games/<ID>` | Для автозаливки |

Без токена workflow всё равно соберёт ZIP — скачай его из вкладки **Actions → Artifacts** и залей руками.

## Локальный аналог

```bash
# То же что CI делает:
# 1) Configure + SetScenes + BuildYandex
# В Unity: UnderDeep → Build → Билд — Yandex Games
# 2) Проверка
ls -lh Build/Yandex/Build/*.data
cat Build/Yandex/yandex-manifest.json
```

## VK / itch

Запусти `workflow_dispatch` → выбери `build-vk` — соберёт `Build/VK` без автодеплоя.

## Отладка

- Логи билда — в `Actions → Build WebGL → game-ci` 
- Если `Library` велик — включи LFS для `Assets/_Project/Art/`
- Лицензия падает: проверь `UNITY_LICENSE` (годна 30 дней, обнови через `unity-activate`)
