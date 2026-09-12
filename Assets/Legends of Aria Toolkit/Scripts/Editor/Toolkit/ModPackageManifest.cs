using System;
using System.Linq;

[Serializable]
public class ModPackageManifest
{
    public string ModName;
    public string PackageNamespace;
    public string BuildVersion;
    public ModPackageBundleManifest[] SceneBundles;
    public ModPackageBundleManifest[] ClientObjectBundles;

    public string GetBundleRelativePath(string bundleName, bool isSceneBundle)
    {
        ModPackageBundleManifest[] bundles = isSceneBundle ? SceneBundles : ClientObjectBundles;
        if (bundles == null)
        {
            return null;
        }

        ModPackageBundleManifest bundle = bundles.FirstOrDefault(item => item != null && item.Name == bundleName);
        return bundle != null ? bundle.RelativePath : null;
    }
}

[Serializable]
public class ModPackageBundleManifest
{
    public string Name;
    public string RelativePath;
    public int BundleVersion;
}
