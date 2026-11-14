// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class UnityUpgradeTests
{
    [TestCase("Assets/Legends of Aria Toolkit/Shaders/ShardsShaders/SpeedTree.shader")]
    [TestCase("Assets/Legends of Aria Toolkit/Shaders/HFTerrainAdvancedAddPass.shader")]
    public void MigratedShadersImportWithoutCompilerErrors(string path)
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
        Assert.IsNotNull(shader, path);
        Assert.IsFalse(ShaderUtil.ShaderHasError(shader), path);
    }
}
