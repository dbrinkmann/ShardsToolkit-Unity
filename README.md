# Shards Toolkit Unity

This project is the Unity toolkit used to create:

- custom maps
- custom client object libraries
- Steam Workshop mod packages

The toolkit builds a single Workshop package per mod. That package contains the client-facing assetbundles and a `mod-manifest.json` file. Server-side data such as map XML and object collision/tag XML remains separate and is written into the server mod folder.

## Requirements

- Unity 2020.3.49f1 (LTS) — see `ProjectSettings/ProjectVersion.txt`.
- [Git LFS](https://git-lfs.com/). A handful of binary assets in this
  repository (a couple of editor textures, the `ShardsXML.dll`, the
  collision-paint terrain template) are tracked via LFS. Install Git LFS
  once per machine before cloning.
- `git` on your PATH, plus network access the first time you open the project.
  One dependency (Steamworks.NET) is a git-URL Unity package, so the Package
  Manager needs both to resolve it on first open. It is cached afterwards. See
  [Uploading To Steam Workshop](#uploading-to-steam-workshop) if that ever
  fails -- it is removable, and the toolkit works without it.

```sh
# one-time install per machine
git lfs install

# then a normal clone fetches both git and LFS objects
git clone <this repo url>
```

## License

This repository is published under the [MIT License](LICENSE) — see the
`LICENSE` file at the repository root. Third-party content is listed in
[`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md): Unity built-in shader
derivatives, Unity's Post Processing Stack v1 and two Standard Assets folders
(both vendored here), the Unity Community Wiki `CopyTransform` and
`Interpolate`, and Steamworks.NET (referenced as a package, not vendored).

## What This Toolkit Produces

The toolkit produces two outputs:

1. Client package output
   - one package folder per mod
   - contains scene bundles, client object library bundles, and `mod-manifest.json`
   - this is what gets uploaded to Steam Workshop

2. Server mod data output
   - written under your `ModsPath`
   - contains map data and client object sidecar XML files
   - this is what the server reads from disk at runtime

For a client object library named `my-mod`, the toolkit writes:

```text
<ModsPath>/my-mod/assetbundles/my-mod/ObjectTagDefinitions.xml
<ModsPath>/my-mod/assetbundles/my-mod/ObjectCollisionData.xml
```

These files are not part of the Steam Workshop package.

## Setup

Open:

- `LoA Toolkit -> Build Mod Package`

The build window now contains both toolkit settings and the build/upload workflow.

Configure:

- `Base Path`
  - path to the base Aria data folder used for authoring
- `Mods Path`
  - path to the server mods folder
- `Current Mod`
  - the mod you are currently building

Example paths:

```text
Base Path: C:\LegendsOfAria\DedicatedServer\aria
Mods Path: C:\LegendsOfAria\DedicatedServer\mods
Current Mod: my-mod
```

## Creating A Mod

Use:

- `LoA Toolkit -> Create Mod`

This creates the basic mod folder structure under `ModsPath`.

## Custom Maps

### Starting a new map

Use `LoA Toolkit -> Custom Assets -> Create New Custom Map`. Pick a map type
and the wizard builds a starting scene for you:

- `Outdoor` — default outdoor lighting, flat ambient, and an outdoor camera
- `Dungeon` — a dungeon camera

Either way the wizard also creates the required `MapData` object and registers
the tags and layers the engine expects, then saves the scene.

The camera prefab it drops in already has a post-processing profile attached,
so a new map looks like the live game out of the box:

- `Outdoor` uses `Aria Post Processing Celador.asset`
- `Dungeon` uses `Aria Post Processing Dungeon.asset`

To change the look, select the camera and swap the profile. Two more ship with
the toolkit:

- `Aria Post Processing Catacombs.asset`
- `EditorTools/DefaultOutdoorPostProcessingProfile.asset`

You can also start from an existing scene instead. It just has to have a
`MapData` component on a GameObject in the scene.

### Building the map

1. Open the scene you want to use as the map.
2. Make sure the scene has a `MapData` component.
3. In the Build Mod Package window, add that scene to `Mod Scenes`.
4. Build the package.

The toolkit will:

- increment the bundle version
- save map extents into `WorldData.xml`
- build the scene assetbundle
- add the scene entry to `mod-manifest.json`

Important:

- the scene bundle name must match the world name the server will use
- if the server region world name is `TestWorld`, the manifest should contain a scene bundle named `TestWorld`

## Regions, Paths, And Seed Objects

These editors live under `LoA Toolkit -> Dynamic Editors` and operate on the
scene that is currently open. Use them to author server-side data for your own
custom map:

- `Region Editor` — named areas the server tests locations against
- `Path Editor` — waypoint paths (guard patrols, camera splines)
- `Seed Object Editor` — spawners and other templated objects
- `Prefab Editor` — permanent object placement
- `Collision Editor` — paints static collision onto your terrain

Each editor has a `Mod` dropdown. Pick your mod, not `Default` — the toolkit
refuses to write to the base ruleset. Region and path data is written to:

```text
<ModsPath>/<YourMod>/mapdata/<SceneName>/WorldData.xml
```

Notes:

- Regions are flattened to 2D footprints by the server, so a region's height
  does not affect whether a location falls inside it.
- Seed objects are markers for server templates. When no client prefab is
  available for a template's `ClientId`, the editor places a plain cube marker
  instead — placement and export still work normally.

## Custom Object Libraries

### Creating the library

Use `LoA Toolkit -> Custom Assets -> Create New Custom Object Library`. Select a
folder in the Project window first to control where the prefab is created. Set
the library's `BundleName` on the new prefab.

### Adding objects to it

Use `LoA Toolkit -> Custom Assets -> Create New Client Object`. Point it at your
model or prefab, optionally pick the library to register it in, and it creates a
client object prefab for you.

Leaving `Source Model` empty creates an empty client object instead, which is
what you want for spawners, triggers and anything else with no visible mesh. You
can add a mesh to it later.

Every entry in a library **must have a `ClientObject` component** -- the build
fails with "contains a prefab without a ClientObject component at index N" if one
does not. The wizard adds it for you, which is the main reason to use it rather
than dragging prefabs into the library inspector by hand.

Slot 0 of a library is reserved as the invalid client id and is always left
empty. Objects start at index 1.

You do not assign client ids yourself. Building the package sets each prefab's
`ClientId` from its position in the library and stamps `CustomObjectLibrary` with
the library's `BundleName`.

### Building the library

1. Create or select a `ClientObjectLibrary` prefab.
2. Set its `BundleName`.
3. Make sure the library has valid `ClientIdPrefabs` entries.
4. In the Build Mod Package window, add the library prefab to `Custom Object Libraries`.
5. Build the package.

The toolkit will:

- increment the bundle version
- update `ClientId` and `CustomObjectLibrary` on the prefabs
- generate:
  - `ObjectCollisionData.xml`
  - `ObjectTagDefinitions.xml`
- build the client object library bundle
- add the object library entry to `mod-manifest.json`

Important:

- `BundleName` is the logical object library name used by the server and client
- this same name is used:
  - in server-side XML paths
  - in permanent object `CustomAssetBundle`
  - in the package manifest

## Building The Mod Package

In the Build Mod Package window:

1. Set paths and current mod.
2. Add one or more `Mod Scenes`.
3. Add one or more `Custom Object Libraries`.
4. Optionally set `WorkshopPackageOutputPath`.
5. Click `Build Mod Package`.

If `WorkshopPackageOutputPath` is blank, the default output is:

```text
<ModsPath>/<CurrentModName>/workshop-package
```

If `WorkshopPackageOutputPath` is relative, it is resolved relative to the Unity project root.

The package contains:

```text
<PackageRoot>/mod-manifest.json
<PackageRoot>/Bundles/Scenes/...
<PackageRoot>/Bundles/Objects/...
```

## Uploading To Steam Workshop

The toolkit supports uploading the built package to Steam Workshop from the same window.

Requirements:

- Steam client running
- logged into a Steam account with access to App ID `3944440`
- Workshop enabled for the app in Steamworks

[Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET) is pulled in
automatically as a Unity package -- see `Packages/manifest.json`, which pins it
to a specific version. Unity fetches it the first time you open the project, so
there is nothing to install by hand. It ships the native Steam binaries for
Windows, macOS, Linux and Android, and is MIT licensed.

Because it is a git URL rather than a registry package, be aware of what that
costs you:

- `git` must be on your PATH. If you cloned this repository you already have it.
- The first time you open the project needs network access. After that the
  package is cached locally and opens offline.
- It is an external dependency. If that repository moves, is renamed, or the
  pinned tag is deleted, package resolution fails and Unity will not open the
  project cleanly. Unity reports this as a Package Manager error on startup.

If resolution ever fails, you do not need to wait for a fix. Remove the
`com.rlabrecque.steamworks.net` line from `Packages/manifest.json` and either:

- install Steamworks.NET manually under `Assets/Plugins/Steamworks.NET` (that
  path is gitignored, and the `STEAMWORKS_NET` define is already set, so upload
  keeps working), or
- leave it out entirely.

With it left out the toolkit still compiles. The upload code is guarded behind
the `STEAMWORKS_NET` scripting define, so clearing that define in Player
Settings disables the upload button and the Build Mod Package window explains
why. Everything else in the toolkit works without Steam.

Workflow:

1. Build the mod package.
2. In the `Steam Workshop Upload` section:
   - leave `Published File ID` blank to create a new item
   - or set it to update an existing item
3. Click `Upload to Steam Workshop`.

On success, the toolkit shows the Workshop `Published File ID`.

Use that ID as the server-side `PackageId`.

## Iterating On A Mod

**Develop against local bundles. Publish to Steam Workshop only when the mod is
ready.**

Steam does not always deliver a Workshop update to the client right away, so
after uploading you may still be running the previous version. Local bundles
avoid this: the client fetches them straight from your machine, so each rebuild
is picked up.

### Local bundles (while developing)

Serve your built package folder over HTTP from the package root:

```sh
cd <your package output folder>
python -m http.server 8000
```

Then point the server config at the bundles directly, instead of at a Workshop
package:

```xml
<Mod Name="my-mod">
  <ClientBundle Type="Scene" Name="TestDungeon">http://localhost:8000/Bundles/Scenes/TestDungeon</ClientBundle>
  <ClientBundle Type="ClientObjects" Name="TestObjectLibrary">http://localhost:8000/Bundles/Objects/TestObjectLibrary</ClientBundle>
</Mod>
```

The URLs point at the bundle files themselves, with no `.version` suffix. The
client appends that itself: it fetches `<url>.version`, reads the integer inside,
and uses it to decide whether its cached copy is stale. The toolkit writes those
sidecar files next to each bundle on every build, and the number increments each
time, so a rebuild is all it takes for the client to pick up the change.

The loop is: build the mod package, relaunch the client. No upload, no Steam.

If a bundle fails to load, the player log names which half failed --
`[VersionFile]` means the sidecar could not be fetched (usually a wrong URL or
the HTTP server is not running), `[BundleFile]` means the bundle itself did not
download.

### Steam Workshop behaviour

Workshop is the right choice for release. Update propagation is the part worth
understanding before you publish:

- **Subscribe to your own item.** Steam only tracks updates for items you are
  subscribed to. If you are not subscribed, the client downloads the package
  once and Steam then reports it as installed and up to date forever -- you will
  keep loading the first version you ever uploaded, with no error anywhere.
- **Updates can lag even when subscribed.** Steam is often slow to flag that a
  new revision exists, and the client cannot download an update Steam never
  tells it about. Unsubscribing, waiting a few seconds, and resubscribing is the
  usual remedy. This is a long-standing Steam problem reported across many
  games, not a toolkit one, and Valve has said they cannot reproduce it.
- **To force a re-download**, delete the item's folder under
  `steamapps/workshop/content/<appid>/<publishedfileid>` and restart the client.
  With the folder gone the client treats the item as not installed and fetches
  it again. The folder is pure cache -- Steam re-downloads it.
- **A successful upload does not update your local copy.** Uploading pushes your
  package to Steam's servers. The folder above is where your *subscribed* copy is
  downloaded to, and Steam refreshes it separately. Seeing "Successfully uploaded"
  in the Unity console tells you nothing about what your client will load.

### Checking which version the client actually loaded

The client logs the package it loaded on startup. In the player log, look for:

```text
[CustomBundleLibrary] Loaded mod-manifest.json for mod '<name>': ... BuildVersion=<timestamp> ...
```

Compare that `BuildVersion` against the one in your built `mod-manifest.json`.
If they differ, the client is running an old package and no amount of rebuilding
will change what you see in game until it picks up the new one.

## Dedicated Server Launcher / Cluster Config

### Steam Workshop mods

For Steam Workshop mods, use one mod entry per package:

```xml
<Mod Name="my-mod" Source="SteamWorkshop" PackageId="3697109873"></Mod>
```

Do not specify scene/object bundle paths for Steam Workshop mods. The client discovers them from `mod-manifest.json`.

### Web bundle mods

Web bundles still use explicit bundle entries:

```xml
<Mod Name="MyWebMod">
  <ClientBundle Type="Scene" Name="TestWorld">https://example.com/TestWorld</ClientBundle>
  <ClientBundle Type="ClientObjects" Name="my-mod">https://example.com/my-mod</ClientBundle>
</Mod>
```

Web delivery is still per-bundle and still expects `.version` sidecar files.

## Server Data Versus Workshop Package

This is important:

- Steam Workshop package:
  - client-only delivery
  - assetbundles + manifest

- Server mod folder:
  - server-only data
  - map XML
  - object collision/tag XML
  - scripts/templates/plugins as needed

The server does not read collision/tag XML from Steam Workshop. Those files must exist in the server mod folder under `ModsPath`.

## Multi-Mod Order

On the server, mods are applied in the order they are listed in cluster config:

- base content first
- then mods in config order
- later mods override earlier mods for layered server content

For Steam Workshop packages, the client downloads each mod package separately and reads each package manifest to discover scene and object bundles.

## Current Limitations / Notes

- Steamworks upload requires the Steam client running and access to the app's Workshop
- Steamworks.NET is pulled in as a Unity package, so first open needs `git` and network access
- Custom map world names used by the server must match the scene bundle names produced by the package manifest
- The base game's default object library is not distributed with this toolkit.
  Editors that preview base objects fall back to your own custom object
  libraries, then to placeholder markers.
- Custom object library names must remain consistent across:
  - toolkit `BundleName`
  - server-side permanent object references
  - manifest entries

## Recommended Modder Workflow

1. Create a mod.
2. Set toolkit paths and current mod.
3. Build one or more custom maps.
4. Build one or more custom object libraries.
5. Click `Build Mod Package`.
6. Verify:
   - package contents
   - `mod-manifest.json`
   - server-side XML under `ModsPath`

Then build it out against local bundles, not Workshop:

7. Serve the package folder over HTTP and point the server config at it with
   `ClientBundle` entries. See [Iterating On A Mod](#iterating-on-a-mod).
8. Rebuild and relaunch to see each change. No upload, no Steam.

Only once the mod is ready:

9. Upload to Steam Workshop.
10. Put the returned `PackageId` into the server config as a
    `Source="SteamWorkshop"` mod entry, and remove the `ClientBundle` entries.
11. Subscribe to your own Workshop item, or the client will keep loading the
    first version you uploaded.
12. Confirm the client actually picked up the package before assuming it
    published correctly -- see
    [Checking which version the client actually loaded](#checking-which-version-the-client-actually-loaded).
