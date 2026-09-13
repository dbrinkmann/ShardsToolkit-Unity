# Third-Party Notices

This repository contains files derived from or interoperating with the
following third-party software. Each item is licensed by its respective
copyright holder under the terms summarized below.

## Unity Built-in Shaders

The following files are modifications of Unity Technologies' built-in
shader source, which is distributed under the MIT license
(https://unity3d.com/legal/licenses/Unity_Companion_License /
the `license.txt` shipped with Unity's shader source archive). Any
"Unity built-in shader source. Copyright (c) Unity Technologies. MIT
license" notices that appear in the original files are retained.

- `Assets/Legends of Aria Toolkit/Shaders/HFTerrainAdvancedFirstPass.shader`
- `Assets/Legends of Aria Toolkit/Shaders/HFTerrainAdvancedAddPass.shader`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/SpeedTree.shader`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/TreeCreatorBarkOptimized_Transparent.shader`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/TreeCreatorLeavesFastOptimized_Transparent.shader`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/TreeCreatorLeavesFast_Transparent.shader`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/TreeCreatorLeavesOptimized_Transparent.shader`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/TreeCreatorLeavesRendertex.shader`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/TreeCreatorLeaves_Transparent.shader`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/TerrBumpFirstPass.shader`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/TerrBumpAddPass.shader`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/Standard2Sided.shader`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/Cutout2Sided.shader`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/Tree.cginc`
- `Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/TreeVertexLit.cginc`

These derivative shaders remain subject to Unity's MIT-licensed shader
source terms.

## Unity Community Wiki: CopyTransform

The file `Assets/Legends of Aria Toolkit/Scripts/Editor/Utils/copyTransform.cs`
is adapted from
http://wiki.unity3d.com/index.php/CopyTransform and is provided under the
Creative Commons Attribution-Share Alike 3.0 Unported license
(https://creativecommons.org/licenses/by-sa/3.0/), the standard license of
the Unity Community Wiki.

## Unity Standard Assets (partially vendored)

Two folders from Unity's Standard Assets are vendored in this repository
because the toolkit depends on them directly:

- `Assets/Standard Assets/Image Effects (Pro Only)/` — supplies `GlobalFog`,
  used by the default map camera prefabs.
- `Assets/Standard Assets/Water (Pro Only)/` — supplies the `WaterBase`,
  `PlanarReflection`, `GerstnerDisplace` and `SpecularLighting` types that the
  toolkit's `Scripts/Editor/Water (Pro Only)/Water4` editor scripts compile
  against.

The remaining Standard Assets content (terrain, tree, particle and environment
art) is not required to use the toolkit and is not included. Standard Assets
are authored and licensed by Unity Technologies under Unity's own terms.

## Unity Post Processing Stack v1 (vendored)

The toolkit uses Unity's Post Processing Stack **v1**, vendored in this
repository under `Assets/PostProcessing/`. The toolkit's profile assets
(`Aria Post Processing Celador.asset`, `Aria Post Processing Dungeon.asset`,
`Aria Post Processing Catacombs.asset`,
`EditorTools/DefaultOutdoorPostProcessingProfile.asset`) are v1
`PostProcessingProfile` assets and the toolkit's camera prefabs use the v1
`PostProcessingBehaviour` component. These are not compatible with Post
Processing v2 (`com.unity.postprocessing`), which uses different types, so
the v2 package is intentionally not referenced in `Packages/manifest.json`.

The Post Processing Stack is licensed by Unity Technologies under its own
terms (Unity Companion License / MIT for the package source on GitHub).

### Bluenoise64 textures (bundled with Post Processing v1)

`Assets/PostProcessing/Resources/Bluenoise64/` contains blue noise textures
by Christoph Peters, released into the public domain under the Creative
Commons CC0 1.0 Public Domain Dedication
(https://creativecommons.org/publicdomain/zero/1.0/). See the `LICENSE.txt`
in that directory.

## Steamworks.NET (package dependency, not vendored)

Workshop upload uses Steamworks.NET
(https://github.com/rlabrecque/Steamworks.NET), MIT-licensed by Riley
Labrecque. It is referenced as a Unity package in `Packages/manifest.json`
and fetched by the Package Manager, so no Steamworks.NET source or native
binary is stored in this repository. The package bundles Valve's Steamworks
SDK redistributables, which remain subject to Valve's own terms.

## ShardsXML.dll

`Assets/Plugins/ShardsXML.dll` is a first-party compiled assembly produced
by the Emergent Worlds team and is licensed under the same MIT terms as
the rest of this repository (see `LICENSE`).

## Items deliberately excluded

The following items are NOT redistributed via this repository:

- Unity Asset Store add-ons such as BrokenVector "Favorites List". Their
  licenses do not permit redistribution. Install them locally for your own
  use; they are excluded by `.gitignore`.
- Unity Standard Assets content beyond the two folders listed above
  (terrain, tree, particle and environment art, CrossPlatformInput, and the
  other effect packages). These are not required to use the toolkit. Install
  them locally if you want them; they are excluded by `.gitignore`.
- The base game's default client object library, and any Legends of Aria
  world content. The toolkit is for building your own maps and object
  libraries; it does not ship Aria's art.
