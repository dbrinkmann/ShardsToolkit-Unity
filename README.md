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

```sh
# one-time install per machine
git lfs install

# then a normal clone fetches both git and LFS objects
git clone <this repo url>
```

## License

This repository is published under the [MIT License](LICENSE) — see the
`LICENSE` file at the repository root. Third-party files included or
referenced (Unity built-in shader derivatives, the Unity Post Processing
package, the Unity Community Wiki `CopyTransform`, Steamworks.NET when
installed locally) are listed in [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).

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

To build a custom object library:

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
- Steamworks.NET SDK installed locally in the Unity project

The public repo is expected to ignore the Steamworks.NET SDK. Install it locally before using upload features.

Workflow:

1. Build the mod package.
2. In the `Steam Workshop Upload` section:
   - leave `Published File ID` blank to create a new item
   - or set it to update an existing item
3. Click `Upload to Steam Workshop`.

On success, the toolkit shows the Workshop `Published File ID`.

Use that ID as the server-side `PackageId`.

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

- Steamworks upload requires a local Steamworks.NET SDK install and Steam client access
- The Steamworks.NET SDK is intentionally not committed to this repository (see `.gitignore` and the install instructions above)
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
7. Upload to Steam Workshop.
8. Put the returned `PackageId` into the server config.
