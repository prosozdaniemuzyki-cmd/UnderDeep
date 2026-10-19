# WebGL Оптимизация под Яндекс/VK

## Требования площадки
- Макс 30 МБ Brotli (идеал 15-20)
- Загрузка <15 сек на 3G
- 60 FPS на mid-phone

## Настройки (уже в WebGLBuilder)
- `Compression: Brotli`, `Decompression Fallback: true` (обязательно для Яндекса)
- `Code Optimization: Size`, `Exceptions: None`, `Data Caching: true`
- `Memory Size: 32 MB`, `Initial 32 → Max 2048`, `Growth: Geometric`
- `WebAssemblyBigInt: false`, `ThreadsSupport: false` (иначе не пропустит модерация)
- `Template: YandexGames` (кастомный index.html с ysdk, прогресс-баром)

## Текстуры
- Тайлы 32×32: `Max Size 1024`, `Compression: DXT5` (ASTC для мобил)
- Спрайт-атлас: включи `Sprite Atlas` для всех тайлов/врагов
- Иконки: 512, 256, 120 — уже есть в `Assets/_Project/Art/`

## Код
- Не использовать `Update()` с `new`, LINQ-аллокациями, `FindObjectOfType` в рантайме
- EventBus struct-события (уже zero-GC), ObjectPool для частиц/врагов
- Нет потоков, нет синхронного WebRequest

## Проверка размера
После билда: `Build/Yandex/Build/*.data` + `*.wasm` — смотри лог PostBuildHook. Если >30 МБ:
- Удали пакеты `com.unity.timeline`, `visualscripting` (если не нужны)
- Сожми аудио `Vorbis 70%`, текстуры `Crunch 50%`
- Включи `Strip Engine Code`

## Локальный тест
```bash
npx serve Build/Yandex -l 3000
# открой http://localhost:3000 — проверь бурение, рекламу, сохранения
```

## CI (опционально)
- GitHub Actions: `unity-builder` + `yagames-publish` API
- Секрет `YANDEX_OAUTH_TOKEN` → автозаливка черновика

