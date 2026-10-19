# Build — инструкция

Папка `Build/` не коммитится (в .gitignore). Собирается через меню:

- **UnderDeep → Build → Настроить PlayerSettings** — один раз
- **Выставить сцены Boot/Base/Run** — один раз
- **Билд — Yandex Games** → `Build/Yandex/` — для yandex.ru/games
- **Билд — VK Play** → `Build/VK/` — для vk.com/vkplay
- **Билд — Standalone** → `Build/Standalone/` — для itch.io/crazygames

После билда:
1. `Build/*/index.html` — уже кастомный шаблон с SDK
2. `Build/*/Build/` — .data/.wasm (Brotli)
3. `thumbnail.png` — иконка (копируется автоматом)
4. `yandex-manifest.json` — мета для проверки
5. `README_BUILD.txt` — краткий лог

Локальный прогон: `npx serve Build/Yandex`

Подробнее: `YANDEX_CHECKLIST.md`, `WEBGL_OPTIMIZATION.md`
