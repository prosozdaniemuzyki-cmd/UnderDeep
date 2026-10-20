# Геймплей v2 — ответ на 4 запроса (авто-бурение, верх-низ, анимации/топливо, геймпад)

## 1. Управление: авто-бурение в направлении движения

**Было:** отдельная кнопка `Space` / клик `ЛКМ` для бурения.  
**Стало:** удержание направления = движение + бурение.

```
Вход: Vector2 input (WASD/стрелки/левый стик геймпада, мёртвая зона 0.2)
  ↓
Квантование в 8 направлений (N, NE, E, SE, S, SW, W, NW)
  ↓
Проверка тайла в этом направлении от GridPos
  → если Air: двигаться (со скольжением по осям как раньше)
  → если Drillable: начать TickDrill() автоматически, движение блокируется до разрушения
  → если SurfacеCrust/Bedrock: стоп + анимация упора
```

- Убираем `Space`/`ЛКМ` как обязательные. Клик остаётся как альтернатива для тача/мыши (тап по соседнему блоку).
- Бурение непрерывно пока зажат ввод. `Overheat` и `FuelConsumption` тикают как раньше.
- Анимация `Drill` триггерится при `DrillStarted`.

## 2. Появление сверху и гравитация

**Лор:** дрон стартует на поверхности `y=height-1` у базы, рядом начатая шахта 2×6 вниз.

**Генерация WorldGrid (фикс: поверхность — верх карты):**
- `y=height-1..height-2` — `SurfaceCrust` (небуримый, `canDrill=false`) — корка поверхности наверху.
- `y<=1` — `Bedrock` внизу (дно карты).
- Шахта: `sx = width/2`, `Air` от `y=height-1` до `y=height-6` (вертикальный шурф 2×6 вниз).
- Дрон спавнится в `GridPos = (sx, height-2)` внутри шахты (depth=1).
- Глубина `Depth = (height-1)-y` растёт вниз, `GetBiome(depth)`/`RollBlock(depth)` используют её.
- Камера `CameraFollow` `minY=0 maxY=height-3` покрывает весь столб.

**Гравитация/проваливание:**
- Каждый `FixedUpdate` проверяем `Get(GridPos+down)`. Если `IsAir` — падаем.
- Падение — плавное с ускорением: `fallSpeed += gravity(12) * dt`, `MovePosition(down * fallSpeed * dt)`.
- При падении бурение вниз ускорено: если игрок жмёт вниз + блок под ним, бурим быстрее (коэф 1.2).
- После разрушения блока под ногами дрон автоматически проваливается — не нужно отпускать вниз.
- Влево/вправо — только через бурение боковых стенок шахты (ширина шахты растёт игроком).

## 3. Анимации + топливо-блоки (выбор лута)

**Убрали:** пополнение топлива за run-кристаллы `R` — ломало выбор.

**Добавили блоки:**
| BlockType | Hardness | Drop | Особое |
|---|---|---|---|
| `SurfaceCrust` | ∞ | — | `canDrill=false` |
| `FuelCanister` | 0.5s | +25 топлива | не даёт мета-ресурс, спавнится кластерами в биомах Metal/Crystal (шанс 2-3%) |
| `EnergyCell` | 1.0s | +15 топлива + 0.5с щита | редкий, 0.8% |

Генерация: кластер `PerlinNoise > 0.82` в Metal/Crystal → FuelCanister.

**База под анимации** — новый компонент `DroneAnimator` на LONE-1:
```csharp
interface IDroneAnimator {
  void Play(string state); // Idle, Move, DrillDown, DrillSide, Fall, Damage, Death, Evac
  void TriggerDrill(Vector2 dir);
  void TriggerDamage();
  void TriggerDeath();
  void TriggerEvac();
}
```
Реализация v1 — заглушка на `SpriteRenderer`/`Animator`: меняет `sortingOrder`, включает `ParticleSystem` при `Drill`, дергает `eyeDisplay`. Позже заменится на `Animator Controller` с клипами.

Вызывается из:
- `DroneController` (движение/падение)
- `DrillSystem` (бурение)
- `HealthSystem` (урон/смерть)
- `RunResultHandler` (эвак)

## 4. Геймпад

**Input:** `InputSystem_Actions.inputactions` уже есть. Добавили карту `Gameplay`:
- `Move` — `Gamepad/leftStick` + `Dpad` + `Keyboard/WASD`
- `Dash` — `Gamepad/buttonEast (B/Circle)` + `Keyboard/Shift` — рывок 2 тайла в направлении, стоимость 8 топлива + 25 тепла, кд 1.5с, i-frame 0.3с
- `Shield` — удержание `Gamepad/leftTrigger` + `Keyboard/Q` — щит тратит 6 топлива/с + 15 тепла/с, блокирует урон

Слои:
- `InputReader.cs` в `Utils` — обёртка над `PlayerInput`, отдаёт `Vector2 move`, `bool dashPressed`, `bool shieldHeld`.
- `DroneController` читает `InputReader.Move` вместо `Keyboard.current`.
- `AbilitySystem` слушает `dash/shield`, публикует `AbilityTriggered`.

## 5. Предложенные варианты взаимодействия (выбор)

Пользователь просил: «если жать влево-вправо то дрон будет бурить соседние блоки и двигаться в лево право (предложи варианты)».

| Вариант | Описание | Плюсы | Минусы |
|---|---|---|---|
| **A — Dash/рывок** (реализуем) | `Shift/B` → бросок на 2 тайла, ломает слабые блоки на пути, неуязвим 0.3с | Быстро уворачиваться от червя, динамично | Требует топлива |
| **B — Прыжок** | `Space/A` → прыжок вверх на 1.5 тайла, если над головой Air | Обход проводов/газа сверху | Нужна гравитация, может ломать шахту |
| **C — Щит** (реализуем) | Удержание `Q/LT` → купол, тратит энергию/греет | Защита от галлюцинаций, тактический выбор | Скучнее без движения |

**Рекомендация:** A+C — даёт и мобильность, и защиту без усложнения физики прыжка. Прыжок отложим до полноценной платформерной шахты.

---
*Реализация: WorldGrid, DroneController, DrillSystem, FuelSystem, DroneAnimator, AbilitySystem, InputReader, VerticalSliceBootstrap, EventBus, BALANCE — обновлены.*
