# Quickstart — 5 минут до первого забега

1. **Открыть проект** Unity 6000.3.7f1
2. **Создать данные**: меню `UnderDeep → Create Default Upgrades` и `Create Default Biomes`
3. **Поставить сцены** в Build Settings: Boot, Base, Run
4. **Настроить WebGL**: `Edit → Project Settings → Player → WebGL`:
   - Publishing: `Decompression Fallback ✓`, `Compression Brotli`, `Code Optimization Size`
   - Resolution: `960x600` canvas
5. **Нажать Play** на Boot — попадёшь в Базу. Нажми "Тонуть вглубь!" → шахта.
6. **Бурение**: ЛКМ по тайлу рядом или Space в сторону взгляда. WASD движение.
7. **Эвакуация**: кнопка в HUD (стоит 10 топлива). Сохраняет лут. Смерть — теряет.

Для Yandex: выложи билд на `yandex.net` и подключи `YaGames.init()` в `YandexSDK.jslib` (уже есть заглушка).
