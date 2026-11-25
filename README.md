# Chronicles of the Lost Dungeon

First-person sci-fi action game made in **Unity 6.5 (URP)** for **Windows, WebGL and Android**.

## Game overview

You are a scavenger who breaks into **the Lost Dungeon**, an abandoned warehouse vault still guarded
by its security robots. The vault is split into sealed sectors. Each level is one sector, and
clearing it opens the way to the next, until you escape across the rooftop.

**Core loop:** explore → fight → solve the sector's objective → reach the exit → results screen →
next sector. You fight with a **blaster** (overheats instead of using ammo) and a **melee bash**,
plus two **abilities**: **Dash** (quick invulnerable burst) and **Shield** (blocks damage).

**Enemies:** three robot types share one AI "brain" with different attack strategies:
- **Warriors** chase you and hit you up close.
- **Archers** keep their distance and shoot.
- **Guardians** carry a front shield that blocks shots. You must bash them to break their guard.

Enemies patrol, **hear** gunshots and explosions, investigate, chase and attack.

### How the five levels connect

| # | Sector | Objective | What it adds | Leads into |
|---|---|---|---|---|
| 1 | **Loading Bay** | Destroy 3 security bots, reach the exit | Combat basics. **Completing it unlocks the Shield ability** for the rest of the game | You enter the storage area |
| 2 | **Storage Racks** | Recover the red keycard, open the bay door | Exploration, **inventory**, keycard doors | The keycard opens the way deeper in |
| 3 | **Catwalks** | Activate the three power switches | Vertical level, **switch puzzle** (all 3 open one door) | Restoring power **triggers the vault's lockdown** |
| 4 | **Ducts & Corridors** | Enter the reactor hall and survive the lockdown for 60 s | **Waves of enemies** from spawners, survival timer | When the doors reopen, the route to the roof is clear |
| 5 | **Rooftop (Reactor Core)** | Clear every enemy on the roof | Open-air finale. **Falling off the roof kills you**, and Guardians can **bash you over the edge** | **Game Complete** |

**What carries over between levels** (saved as JSON):
- **Progression:** finishing a level unlocks the next one in the level select.
- **Abilities:** Shield, once earned in Level 1, stays unlocked.
- **Persistent items** (e.g. credits); level-only keys reset.
- **Best results:** stars (based on par time), score and time per level.
- **Campaign score:** the sum of your best level scores. It is uploaded to the online leaderboard
  after every level, so the whole run is one competition.

The difficulty rises with each sector: more enemy types at once, Guardians' shields, waves, and
finally a level where positioning matters as much as aim.

## Controls

| Action | Keyboard / mouse | Gamepad | Touch |
|---|---|---|---|
| Move / look | WASD + mouse | Sticks | Left joystick / drag right side |
| Fire / melee | Left / right mouse (or F) | RT / RB | FIRE / BASH |
| Jump / interact | Space / E | A / Y | JUMP / USE |
| Dash / Shield | Q / R | LB / LT | Ability buttons |
| Pause | Esc | Start | II |

## Running and building

1. Open the project in **Unity 6000.5.10f1** and open `Assets/Scenes/MainMenu.unity` → Play.
   Managers, UI and the leaderboard create themselves.
2. **Build:** File → Build Profiles → **Windows / WebGL / Android** → Build. Addressables content is
   built automatically with the player.
3. **Online leaderboard (optional):** Tools → The Lost Dungeon → **Create Leaderboard Config**, then
   paste a LootLocker Game API key. Without a key the game uses a local leaderboard. The config file
   is git-ignored, so the key is not published.

## Architecture

Systems communicate through a central **event hub** (`GameEvents`) and **interfaces** instead of
direct references. The UI, audio, objectives, save system and leaderboard all *listen*, so gameplay
code never calls them.

| Pattern | Used for |
|---|---|
| **Singleton** | Persistent managers: Game, Level, Save, Audio, UI, Leaderboard |
| **Observer** | `GameEvents` (about 20 events: `EnemyDefeated`, `LevelCompleted`, `ObjectiveUpdated`…) |
| **Strategy** | Weapons (`IWeapon`), enemy attacks (`IEnemyAttack`), abilities (`IAbility`), save storage, leaderboard service |
| **State** | Enemy AI: Patrol → Investigate → Chase → Attack → Stagger → Dead |
| **Object pooling** | Projectiles, effects and spawned enemies (no runtime Instantiate/Destroy) |

**Interfaces (15):** `IWeapon`, `IAbility`, `IEnemyAttack`, `IEnemyState`, `IDamageable`,
`IInteractable`, `IActivatable`, `ICollectable`, `IPoolable`, `ITargetable`, `ISaveable`,
`ISaveStorage`, `ISceneContentLoader`, `ILeaderboardService`, `IUIScreen`.

## Algorithms

### 1. Merge sort: leaderboard ranking (`Algorithms/MergeSort.cs`)
- **Purpose:** order leaderboard entries by score (highest first) before displaying them.
- **Approach:** recursive divide and conquer. Split the list in half, sort each half, then merge the
  two sorted halves by repeatedly taking the smaller front item. On ties it takes from the left half,
  which keeps equal items in their original order. O(n log n).
- **Why chosen:** it is **stable**, so when two players have the same score, whoever submitted
  first stays above. C#'s `List.Sort` is not stable. It is also O(n log n) in every case, unlike
  quicksort's O(n²) worst case.

### 2. Nearest target in a cone: aim assist (`Algorithms/TargetFinder.cs`)
- **Purpose:** on mobile, bend blaster shots toward the enemy closest to the crosshair.
- **Approach:** a linear search over registered targets. Skip targets out of range, or outside the
  aiming cone using a **dot product** against the cone's cosine. Compare **squared distances** and
  keep the nearest. O(n), no memory allocations.
- **Why chosen:** with only a few enemies a simple linear search is fastest. The dot-product and
  squared-distance tricks avoid expensive angle and square-root maths every shot.

### 3. Weighted random selection: loot drops (`Algorithms/WeightedRandom.cs`)
- **Purpose:** decide what an enemy or chest drops, with rare items less likely than common ones.
- **Approach:** "roulette wheel". Add up all the weights, pick a random number below the total,
  then walk the list subtracting weights until the number lands inside an item's slice. O(n).
- **Why chosen:** designers set chances as simple weights in a `LootTable` asset (e.g. 70 / 25 / 5)
  without the percentages having to add up to 100. It is simple and easy to test.

### 4. Recursive depth-first search: barrel chain reactions (`Algorithms/ChainReaction.cs`)
- **Purpose:** when a barrel explodes, find every barrel that will be set off, and in what order.
- **Approach:** start at the first barrel and **recursively** visit each barrel within blast radius
  that hasn't been visited yet. A visited set prevents infinite loops. The visit order is the
  detonation order.
- **Why chosen:** a chain reaction is naturally a graph search (barrels are nodes, "in range" is an
  edge), and recursion expresses "explode, then explode your neighbours" directly.

All four are covered by unit tests.

## Asset and content management (Addressables)

| Content | Loaded | Released |
|---|---|---|
| **5 level scenes** | When a level starts (`Addressables.LoadSceneAsync`) | When the scene unloads (`ReleaseSceneWhenSceneUnloaded`) + explicit release of the previous handle |
| **Spawner enemies** | Preloaded into the spawner's pool at level start (`LoadAssetAsync`) | When the spawner is destroyed at level exit |
| **Level music** (optional) | On level start | When the next track loads |

- **Loading feedback:** a loading screen with the level name, objective, progress bar and percentage.
- **Failure handling:** a failed load shows a **"Load Failed"** popup with the reason and
  **Retry / Main Menu**, instead of freezing. A missing enemy type is skipped and the level still runs.
- **Platforms:** bundles are built for the active target with each player build (Windows, WebGL,
  Android) using local paths, so no server is needed.
- Loading sits behind the `ISceneContentLoader` interface, so `LevelManager` doesn't depend on Addressables.

## REST API: online leaderboard

`UnityWebRequest` + JSON calls to the **LootLocker** game API (`Online/LootLockerLeaderboardService.cs`):

| Call | Endpoint |
|---|---|
| Guest login | `POST /game/v2/session/guest` → session token (sent as the `x-session-token` header afterwards) |
| Submit campaign score | `POST /game/leaderboards/{key}/submit` (with name, levels and stars as metadata) |
| Top 10 | `GET /game/leaderboards/{key}/list?count=10` |

**Error handling:** timeouts, no connection and HTTP errors become readable messages. An expired
session is renewed automatically. Failed submissions are **queued offline** and re-sent later. The
last scores are **cached** for offline viewing. The UI shows *Online / Offline / Local only* and
*Uploaded / Will upload later*.

## Tests


- 14 NUnit EditMode test files (Window → General → Test Runner): progression, save,
  damage, health, pooling, inventory, AI state machine, enemy maths and all four algorithms.

## Credits

- Environment: **Sci-Fi Construction Kit (Modular)**, Sickhead Games (Unity Asset Store).
- Enemies, props, weapon: **Quaternius** sci-fi packs (CC0).
- Leaderboard backend: **LootLocker**.
- Developed by **Emmanuella Ikirezi**. 
