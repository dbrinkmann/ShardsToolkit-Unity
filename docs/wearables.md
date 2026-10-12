# Wearables

How to make clothing that player characters (the base game's human male and female bodies) can wear, dye and drop: **pants** (slot `Legs`), **shirts** (`Chest`), **hats** (`Head`) and **cloaks** (`Cloak`).

There is no wizard or validator for wearables yet; everything here is done by hand. The skeleton and bind-pose data is in [human-rig.md](human-rig.md).

## How the game shows a worn item

An item has two parts.

**The item prefab (library slot N):**
- What lies on the ground and sits in the backpack.
- Its `EquipmentObject` says which slot it goes in, and which **worn mesh** to put on each kind of body.

**The worn mesh prefab:**
- A separate prefab, not in the library, referenced from the item. It only needs to be in the same bundle.
- When the item is equipped, the game:
  1. instantiates it under the wearer's `Model`;
  2. binds each skinned renderer to the wearer's bones (`BoneMap`);
  3. hides the body parts it covers (`MeshReplacement`);
  4. applies the item's hue.

The worn mesh is chosen by the **wearer's** ClientId:

| Key | Body |
|---|---|
| 1 | Human male (players and NPCs) |
| 208 | Human female |
| 269 | Skeleton. A resurrected player's corpse turns into one and keeps its items; use the male mesh. |

If no key matches, `DefaultMeshPrefab` is used. Leave it empty unless one mesh really fits every body.

**Make separate male and female meshes.** The male body is bound in a T-pose and the female in an A-pose, and a few bone names differ, so one mesh can't fit both.

## The body and what to hide

Each body is four skinned renderers under `Model`, without blend shapes:

| Renderer (male / female) | Covers |
|---|---|
| `Male_NudeBody_body` / `Female_NudeBody_body` | torso and upper arms |
| `Male_NudeBody_AM` / `Female_NudeBody_AM` | forearms and hands |
| `Male_NudeBody_LG` / `Female_NudeBody_LG` | hips, legs **and feet** |
| `Male_NudeBody_head` / `Female_NudeBody_head` | head and neck |

**Hair and beards are items too:**
- Hair is a renderer named `Male_NudeBody_hair` / `Female_NudeBody_hair`.
- Beards are named `Beard`.

**`MeshReplacement.ReplaceMeshes` lists the names to hide** while your item is worn. Any renderer under `Model` whose name *contains* an entry is hidden.

| Slot | Usually hides |
|---|---|
| Pants | `…_NudeBody_LG`. **Pants therefore include the feet or shoes**; there is no boots slot. |
| Shirt | `…_NudeBody_body`; add `…_NudeBody_AM` if the sleeves cover the hands |
| Hat | `…_NudeBody_hair` to hide the hair (leave it out to keep hair); `Beard` to hide beards |
| Cloak | nothing; a hood can hide `…_NudeBody_hair` |

Body shape doesn't change in character creation, so clothing only has to fit these two bodies.

## Authoring the mesh

**Skeleton:**
- Skin to the body's skeleton, **in its bind pose**: male T-pose, female A-pose. See [human-rig.md](human-rig.md).
- Bone names must match exactly. Biped names: `Bip001 Pelvis`, `Bip001 Spine1`, `Bip001 L Thigh`, …
- The female's front loincloth bones are `Bone_F_Lcloth_01…04`; the male's are `Bone_Lcloth_F_01…04`.

**Skinning:**
- At most **4 bone influences** per vertex.
- No twist bones, cape bones or scaled bones exist. Clothing follows the body in every animation, including riding.

**Size:**
- One material per renderer.
- About 1,000–2,000 vertices per renderer.
- Split large items into 1–3 renderers, e.g. torso / sleeves / skirt.

**Fit:** leave a few millimetres of clearance over the body. Items never hide each other except through `MeshReplacement`.

## Recipes

These use bundle `my_mod`, ClientIds 12–15 and `ModShirt` as the example. Pants, hat and cloak follow the same pattern.

### The item prefab (in the library)

```
ModShirt                   layer 0
├── ModShirt_Folded        layer 0   MeshFilter + MeshRenderer: ground and backpack model
├── PickerBounds           layer 19  BoxCollider around the folded model (required to pick it up)
└── ObjectBounds           layer 0   BoxCollider, same box (optional; helps drops into containers)
```

**Root components:**
- **`ClientObject`:** `Category` = `ClothingArmor`.
- **`EquipmentObject`:**

| Field | Value |
|---|---|
| `EquipmentSlot` | `Chest` (pants `Legs`, hat `Head`, cloak `Cloak`). This must equal the server tag's `EquipSlot`. |
| `EquipmentMeshPrefabs` | 1 → `ModShirt_Male`, 208 → `ModShirt_Female`, 269 → `ModShirt_Male` |
| `DefaultMeshPrefab` | empty |
| `EquipmentMaterial` | empty for cloth; `leather`, `chain mail` or `plate` change the wearer's footstep sound |

- **`InventoryObject`** (backpack pose; adjust by eye):

| Item | `InventoryRotation` | `InventoryScale` |
|---|---|---|
| Shirt | (0, 90, −90) | 0.6 |
| Pants | (0, −90, 90) | 0.8 |
| Hat | (0, 0, 0) | 0.7 |
| Cloak | (−90, 0, 0) | 1 |

### The worn mesh prefabs (one per body)

**Shirt (`ModShirt_Male`):**

```
ModShirt_Male                         no components
├── ModShirt_Torso                    SkinnedMeshRenderer + BoneMap + MeshReplacement [Male_NudeBody_body]
└── ModShirt_Sleeves   (optional)     SkinnedMeshRenderer + BoneMap
```

**Pants (`ModPants_Male`):**

```
ModPants_Male
└── ModPants_Legs                     SkinnedMeshRenderer + BoneMap + MeshReplacement [Male_NudeBody_LG]
```

**Hat (`ModHat_Male`):**

```
ModHat_Male
└── ModHat_Mesh                       SkinnedMeshRenderer + BoneMap (RootBone and Bones = "Bip001 Head")
                                      + MeshReplacement [Male_NudeBody_hair]
```

A rigid alternative is a `MeshRenderer` hat with `ManualBoneMap` on the root: `ManualEntry`, `RootBone` = `Bip001 Head`, `ObjectsToReassign` = the hat object. Its offset is kept relative to the head bone, whose X axis points up.

**Cloak (`ModCloak_Male`):**

```
ModCloak_Male
└── ModCloak_Mesh                     SkinnedMeshRenderer + BoneMap
```

- The bones to use: `Bip001 Spine`/`Spine1`/`Spine2`, `Bip001 Neck`, `Bip001 Head`, `Bone_Spad_L`, `Bone_Spad_R`.
- **Start with a plain skinned cloak.** Cloth comes later (see [Cloaks with cloth](#cloaks-with-cloth)).

**Female meshes** are the same, built on the female skeleton, with `Female_NudeBody_…` names in `MeshReplacement`.

### Filling in `BoneMap`

Each `BoneMap` needs two fields:
- **`RootBone`:** one bone near the middle of the piece, e.g. `Bip001` for pants and shirts, `Bip001 Head` for hats, `Bip001 Spine` for cloaks.
- **`Bones`:** the names of the mesh's bones, **in the same order as the imported `SkinnedMeshRenderer.bones`**. The game assigns the wearer's bones in that order; a wrong order twists the mesh.

The workflow:
1. Import your FBX with its skeleton.
2. Copy the bone names into `Bones` in order.
3. Delete the skeleton objects from the worn prefab, which needs only the renderers.

If any listed bone doesn't exist on the wearer, that renderer is switched off in game.

## Materials

Use **`Custom/Diffuse-Hue`** (in `Shaders/ShardsShaders`) so the item can be dyed:

| Slot | Contents |
|---|---|
| `_MainTex` | Colour. Keep alpha opaque: texels below about 12% alpha are cut away. Paint a light, fairly neutral base: dye recolours by brightness. |
| `_Mask` | Red channel = how much dye applies (white = all, black = none). Paint buttons, buckles and hat bands black. |
| `_BumpMap` | Optional normal map |
| `_Cutoff` | 0.5 |

- **Back faces:** `Custom/Diffuse-Hue` draws front faces only. For cloak insides, hat brims and open sleeves either:
  - model the back faces, or
  - use **`Custom/Diffuse-Hue-Normal`** (double-sided, dyeable) and assign `_HueTable` = `HueTableAll` in that material yourself.
- **Not dyeable:** `Transparent/Cutout/Cutout2Sided` and Standard. Standard is turned into a plain Lambert the first time it's dyed.
- **Texture sizes:** 1024² for shirts, pants and cloaks; 512² for hats.
- **Un-dyed parts:** besides the mask, a renderer can carry `HueSettings` with `DoNotColor` on.

The worn item uses **its own** hue and colour, not the wearer's.

## Server data

**1. Tag.** Add one `ObjectTag` per slot to `<ModsPath>/<mod>/assetbundles/<BundleName>/ObjectTagDefinitions.xml`. The build doesn't write these yet.

```xml
<ObjectTag>
    <Name>Chest</Name>
    <SharedStateEntry name="EquipSlot" type="string" value="Chest" restriction="readonly"/>
    <ClientId>12</ClientId>
</ObjectTag>
```

Do the same with `Legs`, `Head` and `Cloak`.

**2. Template.** One per item:

```xml
<ObjectTemplate>
    <ClientId>12</ClientId>
    <CustomAssetBundle>my_mod</CustomAssetBundle>
    <Name>[978161]Linen Shirt[-]</Name>
    <Hue>139</Hue>
    <SharedStateEntry name="Weight" type="int" value="1"/>
    <ObjectVariableComponent>
        <BoolVariable Name="CanDye">true</BoolVariable>
    </ObjectVariableComponent>
</ObjectTemplate>
```

- `CanDye` lets dye bottles recolour it; dyeing sets the item's `Hue`.
- Add `<StringVariable Name="ArmorType">Linen</StringVariable>` and `<DoubleVariable Name="MaxDurability">30</DoubleVariable>` for armour stats.
- Hue 139 is the base game's undyed cloth tone.

## Cloaks with cloth

Once the plain cloak works:

1. **Add Unity `Cloth`** to `ModCloak_Mesh`. Pin the neck and shoulder vertices (max distance 0); let the rest move up to 0.15–0.35 m.
2. **Settings the game's cloaks use:**

| Setting | Value |
|---|---|
| Stretching stiffness | 0.2 |
| Bending stiffness | 0.5 |
| Tethers | on |
| Gravity | on |
| Damping | 0.35 |
| External acceleration | (0, −2, 0) |
| World velocity / acceleration scale | 0 |
| Friction | 0 |
| Collision mass scale | 0 |
| Continuous collision | on |
| Virtual particles | on |
| Solver frequency | 40 |
| Sleep threshold | 0.1 |

3. **Add `EquippableClothSimComponent`** to the worn prefab's root, with `clothSim` → the `Cloth`. The game then gives the cloth the wearer's body colliders and those of a mount being ridden.

There is no separate hood support; a hood is just part of the cloak mesh.

## Checking it in game

- [ ] It builds, and the bundle's tag file has the `EquipSlot` tags.
- [ ] `/create <template>`: the folded model lies on the ground in its hue and can be picked up.
- [ ] It looks right in the backpack. (The 2D icon in list view is the base game's item with the same ClientId; known limitation.)
- [ ] It equips on a male and a female character and follows idle, walking, running, combat, sitting, emotes and riding.
- [ ] No body pokes through. The hat hides the hair if it should.
- [ ] The character window shows it and its slot button finds it.
- [ ] A dye bottle recolours it on the body, on the ground and in the character window; masked parts keep their colour.
- [ ] On death the corpse wears it; after resurrection the skeleton does.
- [ ] Unequipped, the body is complete again.

## Troubleshooting

| What you see | Likely cause |
|---|---|
| Nothing on the body | No `EquipmentObject` on the item root; no key for that body and no `DefaultMeshPrefab`; or the mesh prefab isn't referenced |
| The mesh sits frozen around the feet | No `BoneMap` on the renderer |
| One part is missing | A name in `BoneMap` doesn't exist on that body (for example male-only loincloth or `Nub` bones on the female mesh) |
| Twisted or exploded mesh | `Bones` in the wrong order |
| Offset or stretched mesh | Authored in the wrong bind pose (prefab pose, or the other gender's pose) |
| Body pokes through | Missing or misspelled `MeshReplacement` entry |
| Equips, but the character-window slot is empty | `EquipmentSlot` differs from the tag's `EquipSlot` |
| "You cannot equip that." | No `EquipSlot` tag for the ClientId |
| Can't pick it up | No `PickerBounds` on layer 19 |
| Inside of the cloak invisible | Single-sided shader; see [Materials](#materials) |
| Dye shows black or wrong | `Custom/Diffuse-Hue-Normal` without `_HueTable` |
