using UnityEngine;

public class UberCombinerAssets : ScriptableObject
{
	[SerializeField]
	private Object _rootFolder;

	[SerializeField]
	private Object _resourcesFolder;

	[SerializeField]
	private Object _materialsFolder;

	[SerializeField]
	private Object _prefabsFolder;

	[Space]
	public Object MeshBakerDefaultCustomizer;

	public Material ReferenceUberMaterial;

	public Shader TextureArrayCapableShader;

	[Space]
	public string RootFolderPath;

	public string ResourcesFolderPath;

	public string MaterialsFolderPath;

	public string PrefabsFolderPath;

	private static UberCombinerAssets gInstance;

	public static UberCombinerAssets Instance
	{
		get
		{
			_ = gInstance == null;
			return gInstance;
		}
	}

	private void OnEnable()
	{
		Setup();
	}

	private void Setup()
	{
	}

	public void ClearMaterialAssets()
	{
	}

	public void ClearPrefabAssets()
	{
	}
}
