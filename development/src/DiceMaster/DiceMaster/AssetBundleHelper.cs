using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace DiceMaster;

public static class AssetBundleHelper
{
	public static bool LoadAsset<T>(string assetName, string bundlePath, out T asset, bool useFile = false) where T : Object
	{
		AssetBundle assetBundle = (useFile ? LoadAssetBundleFromFile(bundlePath) : LoadAssetBundleFromEmbedded(bundlePath));
		asset = ((assetBundle != null) ? assetBundle.LoadAsset<T>(assetName) : null);
		return asset != null;
	}

	private static AssetBundle LoadAssetBundleFromFile(string bundlePath)
	{
		string path = Paths.PluginPath + bundlePath;
		if (File.Exists(path))
		{
			return AssetBundle.LoadFromFile(path);
		}
		return null;
	}

	private static AssetBundle LoadAssetBundleFromEmbedded(string bundlePath)
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		using Stream stream = executingAssembly.GetManifestResourceStream(executingAssembly.GetName().Name + "." + bundlePath);
		return (stream != null) ? AssetBundle.LoadFromStream(stream) : null;
	}
}
