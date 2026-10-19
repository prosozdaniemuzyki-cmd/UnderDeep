# Чек-лист публикации на Яндекс Игры

## 1. Подготовка билда

1. В Unity: `UnderDeep → Build → Настроить PlayerSettings для WebGL (Yandex/VK)` — один раз
2. `UnderDeep → Build → Выставить сцены Boot/Base/Run` — один раз
3. `UnderDeep → Build → Билд — Yandex Games` — соберёт в `Build/Yandex/` (~15-25 МБ Brotli)
4. Проверь локально: `npx serve Build/Yandex` → открой `http://localhost:3000` → клик/бурение работает

## 2. Кабинет разработчика (https://yandex.ru/dev/games/)

- Создай новую игру → укажи название **Deep Lone**, жанр Аркада/Рогалик
- Иконка: `Assets/_Project/Art/icon_512.png` (512×512, <1 МБ)
- Обложка: `Assets/_Project/Art/promo_banner.png` (1280×720)
- Залей ZIP из `Build/Yandex/*` (иконку можно отдельно)
- Лидерборды: создай `maxDepth` (max, int) и `richRun` (max)
- Реклама: включи `Fullscreen` (межстраничка) и `Rewarded` (2 плейсмента — revive/double_loot в коде уже)

## 3. Тестирование на площадке

- Во вкладке `Тестирование` открой черновик → проверь `ysdk.adv.showFullscreenAdv` срабатывает при входе в Базу
- Rewarded: кнопка Воскресить → должно открыть видео, после колбэка даёт HP
- Сохранения: выкопай → эвак → перезапусти → ресурсы остались (Yandex Storage)
- `GameReady` вызывается в `Boot` после загрузки — без него игра не появится в каталоге!

## 4. VK Play

- Аналогично: `Билд — VK Play` → `Build/VK/` → залей в https://dev.vk.com
- Шаблон `VKPlay` уже подключает `vk-bridge`, Storage fallback на PlayerPrefs

## 5. Размер — критично!

- Цель ≤ 30 МБ Brotli. Если больше: `Edit → Project Settings → Player → WebGL → Code Optimization: Size`, сожми текстуры до 1024, включи `Strip Engine Code`, убери неиспользуемые пакеты (Timeline, VisualScripting опционально).

## 6. Локализация

`IPlatformProvider.Language` уже тянет `ysdk.environment.i18n.lang` → `Localization.Get(key)` вернёт ru/en

## 7. Обновление

Перезалей ZIP → версия инкрементится автоматом, MetaSave мигрирует (version=1)
