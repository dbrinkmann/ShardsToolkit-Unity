# Custom creatures and mounts

This guide covers taking a rigged model into the game as a **creature** (a mob that wanders, fights and dies) or a **mount** (a creature summoned from a statue and ridden), with its own sounds if you like.

Some features need a recent game client or server. They are marked **(new client)** or **(new server)**. Everything else works with any client.

## Quick start

1. **Library.** Create a client object library (`LoA Toolkit → Custom Assets → Create New Custom Object Library`) and register it under `LoA Toolkit → Settings`.
2. **Sounds (optional).** Put your sound files in a `Sounds` folder next to the library prefab (see [Sounds](#sounds)).
3. **Prefab.** In the Project window select the folder for the creature, then open `LoA Toolkit → Custom Assets → Create Creature`:
   - Pick the model. Clips are guessed from their names; check them.
   - Set the walk and run clip speeds.
   - Pick a sound set.
   - For a mount, tick **Is Mount** and choose the spine bone the rider sits on.
   - Pick the library and press **Create Creature**.
4. **Server templates.** Open `LoA Toolkit → Custom Assets → Create Creature Templates`, pick the new prefab and a base game creature to copy AI, stats and loot from, and press **Create Templates**. A mount also gets a statue template.
5. **Build.** Run `LoA Toolkit → Build Mod Package`. It checks every creature first (see [Checks](#checks-and-errors)), and writes the bundle's `Mobile` and `Mount` tags for you.
6. **Test in game:**
   - Spawn the creature with `/create <template>`, or for a mount `/create <template>_statue` and use the statue.
   - Mobs ignore gods: type `/testmortal`, then spawn a fresh mob.
   - Press **F9** with the creature targeted to see its animator and sound state **(new client)**.

The wizard's result works as is. The rest of this guide explains what it builds, so you can tune it or build by hand.

## What a creature prefab contains

```
<Name>                    Mobile; FMODPlayEnvironmentSound (ambient sound); mounts: EquipmentObject
├── Model                 the model: Animator (controller, Apply Root Motion off), FootstepHandler
│   └── … bones …
│       ├── Head          empty under the head bone: effects aimed at "Head" attach here
│       └── MountPoint    mounts: the rider's feet go here
├── OverheadAnchor        where the name plate appears
├── EffectHitPoint        spells and hits attach here (chest height)
├── PickerBounds          collider on layer 19 "PickerCollider": what the mouse can click
└── MountedOverheadAnchor mounts: the rider's name plate while riding
```

- **`Model`** must be a direct child of the root and named exactly that.
- **Scale `Model`, not the root.** The server sets the root's scale and overwrites yours.
- **Don't add a child named `Collider`.** Creatures collide using the template's `BodyOffset` (their body radius in metres).
- **Layers** of renderers are set by the client; you don't need to set them.

## The animator controller

| State | Played by | Notes |
|---|---|---|
| `idle` (default) | client | |
| `walk`, `run` | your transitions on `Speed` | Must be directly in the first layer, and that layer must be named `Base` |
| `attack` | server, on every attack | With several attack clips: `RandomAnimIndexBehaviour` (`numAnims` = N) on this state, then transitions to `attack1`…`attackN` on `AnimIndex` |
| `was_hit` | server, on damage | |
| `die` | client, on death | Must not loop, and must end lying down |
| `die_pose` | client, for corpses coming into view **(new client)** | Same clip, state speed 0 |
| `combat_idle` | your transition on `Combat` | Optional |
| `roar` | server, for some AIs | Optional |

**Parameters:**

| Parameter | Type | Meaning |
|---|---|---|
| `Speed` | float | Movement speed in metres per second: 0 standing, about 1 wandering, 3–4.5 chasing; a ridden mount about 2.25 walking and 6.75 running |
| `IsDead` | bool | `die` and `die_pose` must return to `idle` when it turns false (resurrection) |
| `Combat` | bool | In combat |
| `AnimIndex` | int | Set by `RandomAnimIndexBehaviour` |
| `WalkCycle`, `RunCycle` | float | Footsteps, see below |

- Use a walk to run switch at `Speed` 2.
- Don't use *Any State* transitions to `die`; the client plays `die` itself.
- Keep to one layer. The client sets the weights of layers 1 and 2 itself.

### Animation speed

While a creature is in `walk` or `run`, the client sets the animation speed to **movement speed × factor**. The factors are `Mobile.WalkAnimSpeed` (used up to 2 m/s) and `Mobile.RunAnimSpeed` (above 2 m/s).

Set each factor to **1 ÷ the ground speed the clip was made for**. The wizard does this from the clip speeds you enter. The game's horse uses walk 0.2 and run 0.1, tuned for riding speeds.

If walking looks like sliding, raise the factor. If the legs race, lower it.

### Footsteps

`FootstepHandler` plays a step when a float curve on the clip crosses zero:
- **Walk clip:** a curve `WalkCycle` that goes down through zero once per step.
- **Run clip:** a curve `RunCycle` that crosses zero once per step.

To add a curve to a clip inside an FBX: select the FBX, open the **Animation** tab, select the clip, and add a curve under **Curves**.

You can also add `PlayFootstep` animation events instead, with the string `Walk` or `Run`.

## Mounts

The client puts the **rider's feet** on the transform named in `EquipmentObject.MountBone`, and copies its rotation. So `MountPoint`:
- sits under a spine bone, so the rider moves with the animation
- is about one hip height (≈ 1 m) below the top of the back
- has its Y axis up and Z axis toward the head

The wizard places it this way. To fine-tune, ride the mount and move `MountPoint` in steps of about 0.1 m.

`MountedOverheadAnchor` places the **rider's** name plate while riding. The mount's own name plate is hidden while ridden.

**Statue.** The statue template is an item with `ResourceType` = `MountStatue` and `StatueMountTemplate` = the creature template. The template wizard writes it.

**Riding speed (new server).** Riding speed is a server setting shared by all mounts. To make one mount faster or slower, add this to its template's `ObjectVariableComponent`:

```xml
<DoubleVariable Name="MountSpeedMultiplier">1.2</DoubleVariable>
```

## Sounds

A creature's sounds come from `Mobile.FMODAudioPath`, a prefix the game adds suffixes to:

| Suffix | When it plays |
|---|---|
| `_attack` | Attacking |
| `_pain` | Damage above 3 |
| `_death` | Dying |
| `_footsteps` | Footsteps (or `OptionalFMODFootstepPath` if set) |

The ambient loop is `FMODPlayEnvironmentSound.FMODPath`, which by convention is `<prefix>_environment`.

### Sounds from the game

Use a prefix like `event:/animals/wolf/wolf`. The wizard lists every sound set in the game, read from `FMODAudioReference.txt` in the base game folder.

### Your own sounds (new client)

1. Create a folder named **`Sounds`** next to your library prefab.
2. Put `.ogg` (recommended), `.wav`, `.mp3`, `.flac` or `.aif` files in it, named by the suffix convention:

   ```
   Sounds/
     hound_attack_1.ogg   ← numbered files are variants, picked at random
     hound_attack_2.ogg
     hound_pain.ogg
     hound_death.ogg
     hound_footsteps.ogg
     hound_footsteps_run.ogg   ← optional: used for running steps
     hound_environment.ogg     ← ambient loop
     tavern_music.ogg          ← names containing "music" go to the music volume
   ```

3. Use the prefix **`mod:/<BundleName>/hound`**. The wizard lists your library's sound sets first.
4. Build. The sounds go into the bundle; the build writes a `GeneratedSounds` folder next to the library, which you can ignore.

**Settings per sound.** After a build, the library prefab has a `CustomSoundLibrary` component with one entry per sound: volume, pitch range, 3D distances, looping and volume group (`sfx`, `ambience` or `music`). Your changes there are kept on later builds.

**Behaviour in game:**
- Sounds play through the game's volume sliders and are positioned on the creature.
- Mod footsteps don't vary with the ground surface.

**Where `mod:/` paths work:** anywhere an FMOD path is accepted:
- `FMODAudioPath`
- `OptionalFMODFootstepPath`
- `FMODPlayEnvironmentSound`
- `FMODAudioZone` `EventPath` and `MusicEventPath`, for an area with its own ambience or music
- Lua `PlayObjectSound("mod:/<bundle>/<name>", false)`

**Bundle size:** players download the bundle, so keep files small. Short `.ogg` effects and a moderate bitrate for music are best.

## Materials

- **Bodies:** use Unity's **Standard** shader. URP, HDRP and Shader Graph materials don't render in the game.
- **Colour:** put it in the textures, not in the material colour. The client overwrites the material colour with the colour the server sends (white unless the template sets `<Color>`).
- **Hues:** a creature that will be hued (template `<Hue>`) should use `Custom/Diffuse-Hue` with a `_Mask` texture (in `Shaders/ShardsShaders`).
- **Particles:** use **Legacy Shaders/Particles/Additive** or **Alpha Blended**. `Particles/Standard Unlit` shows as dark squares.

## Effects

- Spells and hits attach to a child of the root named `EffectHitPoint`. Without one, they appear 1 m above the ground.
- Server effects aimed at a bone find it by a case-sensitive **part of its name**, searching from the root: `Head`, `L_Hand`, `R_Hand`, `Spine2`, `Ground`, and `Bone_Proj_Source` for where projectiles start.
- Add empties with those names under the right bones. A bone called `Bip001 L Hand` does not match `L_Hand`.

## Server data

**Tags.** The build keeps the bundle's `ObjectTagDefinitions.xml` up to date:
- a `Mobile` tag with every creature's ClientId and all the shared state the game's creatures have
- a `Mount` tag for mounts

Other tags you add, and values you change, are kept.

**Templates.** The template wizard sets:
- `ClientId` and `CustomAssetBundle`
- the name
- `BodyOffset`: the body radius in metres, used for collision and reach; check it
- no `ScaleModifier`, since size comes from the prefab

`MobileComponent/BaseRunSpeed` scales how fast the creature moves. `AI-RandomizeScale` has no effect in the current game.

**Reloading:** after changing templates or tags, reload templates or restart the server. On an older server, tag changes need a restart.

## Checks and errors

- **Before building:** `LoA Toolkit → Validate Creatures` checks every creature in your libraries, and Build Mod Package runs the same checks first. **Errors** stop the build; **warnings** go to the Console. Each message says what goes wrong in game and how to fix it.
- **In game (new client):** problems the client finds are logged once per creature in `Player.log` as `[Prefab check] ClientId N (bundle '…', …): …`.
  - `Player.log` is in `%USERPROFILE%/AppData/LocalLow/Emergent Worlds/Legends of Aria/`.
  - It also shows `Loading bundle <BundleName> v<N>`, so you can check which build the client loaded.
- **Debug overlay (new client):** for staff, **F9** opens an overlay for the targeted creature, showing:
  - movement speed, animation speed factors and `Animator.speed`
  - each layer's state and clip
  - all parameters
  - the last animation requested and whether it existed
  - whether each sound resolves

  Its **Reload mod bundles** button loads your rebuilt bundle without logging out. Creatures update in place; mounts being ridden update when you next mount them.
- **Without the new client:** log out to the login screen and back in to load a rebuilt bundle.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Creature doesn't appear | Check the Validate Creatures window and `Player.log` |
| Creature doesn't appear (older client) | The root has no `Mobile`, or its ClientId is above 2879 |
| Walk or run plays far too fast or slow | `WalkAnimSpeed` / `RunAnimSpeed` (see [Animation speed](#animation-speed)), or the first layer isn't named `Base` |
| Always the same attack | `RandomAnimIndexBehaviour` missing on `attack` |
| Corpse gets up, or dies again when seen | `die` loops, or `die`/`die_pose` has no transition back on `IsDead` false |
| Colours washed out | Colour is in the material colour instead of the texture |
| Rider floats or sinks | Move `MountPoint` |
| Name plate at the wrong height | Move `OverheadAnchor` (`MountedOverheadAnchor` while riding) |
| Effects at the creature's feet | Add `EffectHitPoint` at chest height and a `Head` under the head bone |
| Server log: "Template attempts to override a non existent property" | The bundle's `Mobile` tag is incomplete. Rebuild, and the build adds it |
| No mod sounds | Prefix must be `mod:/<BundleName>/<name>` with the library's exact `BundleName`; files must be in `Sounds` next to the library; needs the new client |
