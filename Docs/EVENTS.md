# События EventBus

Все события — `readonly struct`, публикуются через `EventBus.Publish(evt)`.

## World

```csharp
readonly struct BlockMined { Vector2Int pos; BlockType type; int yield; ResourceType resource; }
readonly struct BlockDamaged { Vector2Int pos; BlockType type; float progress; }
readonly struct BiomeEntered { BiomeId biome; int depth; }
readonly struct TrapTriggered { Vector2Int pos; TrapType type; } // Gas / Wire / GasTick / CaveIn; публикуют Utils/TrapSystem, Utils/CaveInSystem
```

## Player

```csharp
readonly struct FuelChanged { float current; float max; float delta; }
readonly struct HeatChanged { float heat; bool overheated; }
readonly struct HealthChanged { int hp; int maxHp; }
readonly struct SanityChanged { float sanity; SanityStage stage; }
readonly struct DepthChanged { int depth; int maxDepthThisRun; }
readonly struct PlayerMoved { Vector2Int from; Vector2Int to; }
readonly struct DrillStarted { Vector2Int target; }
readonly struct DrillEnded { Vector2Int target; bool success; }
```

## Inventory / Progression

```csharp
readonly struct ResourceCollected { ResourceType type; int amount; int totalRun; }
readonly struct RunEnded { RunEndReason reason; int depth; Dictionary<ResourceType,int> loot; } // reason: Death/Evac/EmergencyEvac/CoreActivate/Escape
readonly struct UpgradePurchased { string id; }
readonly struct ArtifactFound { string artifactId; int count; }
readonly struct EvacRequested { } // Player (DrillSystem, хоткей B) → Progression (RunResultHandler.Evacuate)
```

## Platform / GameState

```csharp
readonly struct GameStateChanged { GameState from; GameState to; }
readonly struct RewardedResult { string placement; bool success; }
```

## Подписка

```csharp
EventBus.Subscribe<BlockMined>(OnMined);
...
EventBus.Unsubscribe<BlockMined>(OnMined);
```

Все подписчики обязаны отписываться в `OnDestroy`.
