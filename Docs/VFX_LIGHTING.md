# VFX и освещение Deep Lone

## Свет (URP 2D)

- **Global Light 2D** — интенсивность 0.7, цвет `fogColor` из `BiomeDef` (поверхность — дневной, глубже — темнее/синее).
- **Point Light 2D на дроне** (`PlayerLight`):
  - Радиус 5 (с радаром 7), интенсивность 1, flicker 0.08 через Perlin
  - Цвет: от тёплого (поверхность) к холодному на глубине, меркнет при sanity >75
  - Влияет на sanity: `bright` снижает накопление безумия ×0.6
- **Фонарные столбы** в металлическом биоме — `Freeform Light` вдоль труб

Настройка URP: `Renderer2D` → Shadows enabled, blending.

## Эффекты бурения

- `VFXManager` слушает `BlockDamaged` → `drillParticles` (искры, пыль по цвету блока)
- `BlockMined` → `breakParticles` + `ScreenShake` 0.05
- Звук: `drillLoop` (loop while drilling), `drillEnd`, `pickup`

## Ловушки

- **Газовая жила**: при `BlockMined(GasVein)` спавн `GasPoison` (тик-урон 0.5с ×3с), зелёный оверлей, звук шипения
- **Провода**: при добыче `Wire` в радиусе 1.2 — `TakeDamage(1)` + стан 0.6с, искры
- **Обвал**: `CaveInSystem` проверяет опору над выкопанным блоком, шанс 12% → спавн `FallingRock` (6 юнитов/с), урон при контакте

## Безумие (Sanity 0-100)

| Стадия | Эффект |
|--------|--------|
| 25% — LightDistortion | vignette 10%, лёгкая хроматика камеры, шёпот |
| 50% — FakeResources | спавн 6 фейк-кристаллов (исчезают при бурении) |
| 75% — FakeEnemies | спавн Hallucination сквозь стены, vignette 30%, свет ×0.5 |
| 100% — Blackout | потеря управления 5с, sanity →70 |

Шёпоты: UI `whispersText` раз в 12с при sanity>50. Светлый биом и био-гель снижают sanity.

## Враги

- **RustWorm** — `MoveSpeed 2`, уничтожает блоки, поворот при Bedrock
- **Firefly** — орбита 3 вокруг игрока, knockback 5, +8 sanity
- **CrystalSpider** — hideAlpha 0.3, detect 4, рывок 1.2, cooldown 3с
- **Hallucination** — сквозь стены, lifetime 8с, +15 sanity, мерцание

## Артефакты

5 частей памяти (1 на 20м глубины). При `BlockMined(ArtifactChunk)` → `ArtifactManager` даёт перк:
- 1 — +10 топлива
- 2-5 — заглушки для будущих (устойчивость к яду, радар v2 и т.д.)

При 5/5 открывается 2-й финал (побег) и бесконечный режим. Ядро на 98м — `EndingManager` ловит `DepthChanged`.
