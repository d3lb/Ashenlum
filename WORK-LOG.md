# Ashenlum — What We Built & Why

| Technique | Where | Why |
|---|---|---|
| **Abstract class** | `ShopGood` → Talisman / StrengthUpgrade / LumenBundle | Shop holds **one** list. Each subclass answers *cost / sold out / what happens on buy*. |
| **Abstract class** | `ActiveAbility` → `ProjectileAbility` | New ability = new subclass. The player script never changes. |
| **Abstract class** | `Interactable` → Shop / CheckPoint / Dialogue | Everything you press **E** on shares one prompt system. |
| **Interface** | `IDamageable` | Player, enemies, boss and shade all take damage. The attack doesn't care which it hit. |
| **Interface** | `IRespawnReset` | Disabling a GameObject kills its coroutines **permanently** — an enemy killed mid-swing came back with its hitbox stuck on. This undoes what the dead coroutine never finished. |
| **ScriptableObject** | Talismans, abilities, conversations | Data lives in assets. Balance changes need no code. |
| **Static registry** | `WorldReset` | A dead enemy is disabled, so a search can't find it — enemies register themselves instead. |
| **Reference counting** | `TimeManager` | Pause, inventory, shop and modal can all freeze time at once. It resumes only when the **last** one releases. |
| **DTO / model split** | `RunSave` vs `GameRunProfile` | `JsonUtility` can't write `Dictionary`, `HashSet` or asset refs — flattened to **ids** on save, resolved back on load. Also lets the runtime class be refactored without breaking old saves. |
| **Derived state** | Talisman bonuses | Computed from what's equipped, never stored. That's the only reason unequipping needs no "undo". |
| **Callbacks (`Action`)** | `ConfirmModal` | The modal knows nothing about what it's confirming — you pass it words and a function. |
| **Events** | `OnLumensChanged` | UI updates when the number changes instead of polling every frame. |
