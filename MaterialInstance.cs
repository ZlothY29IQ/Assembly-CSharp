using System.Collections.Generic;
using UnityEngine;

[HelpURL("https://docs.microsoft.com/windows/mixed-reality/mrtk-unity/features/rendering/material-instance")]
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
[AddComponentMenu("Scripts/MRTK/Core/MaterialInstance")]
public class MaterialInstance : MonoBehaviour
{
	private Renderer cachedRenderer;

	[SerializeField]
	[HideInInspector]
	private Material[] defaultMaterials;

	private Material[] instanceMaterials;

	private Material[] cachedSharedMaterials;

	private bool initialized;

	private bool materialsInstanced;

	[SerializeField]
	[Tooltip("Whether to use a cached copy of cachedRenderer.sharedMaterials or call sharedMaterials on the Renderer directly. Enabling the option will lead to better performance but you must turn it off before modifying sharedMaterials of the Renderer.")]
	private bool cacheSharedMaterialsFromRenderer;

	private readonly HashSet<Object> materialOwners = new HashSet<Object>();

	private const string instancePostfix = " (Instance)";

	public Material Material => AcquireMaterial();

	public Material[] Materials => AcquireMaterials();

	public bool CacheSharedMaterialsFromRenderer
	{
		get
		{
			return cacheSharedMaterialsFromRenderer;
		}
		set
		{
			if (cacheSharedMaterialsFromRenderer != value)
			{
				if (value)
				{
					cachedSharedMaterials = CachedRenderer.sharedMaterials;
				}
				else
				{
					cachedSharedMaterials = null;
				}
				cacheSharedMaterialsFromRenderer = value;
			}
		}
	}

	private Renderer CachedRenderer
	{
		get
		{
			if (cachedRenderer == null)
			{
				cachedRenderer = GetComponent<Renderer>();
				if (CacheSharedMaterialsFromRenderer)
				{
					cachedSharedMaterials = cachedRenderer.sharedMaterials;
				}
			}
			return cachedRenderer;
		}
	}

	private Material[] CachedRendererSharedMaterials
	{
		get
		{
			if (CacheSharedMaterialsFromRenderer)
			{
				if (cachedSharedMaterials == null)
				{
					cachedSharedMaterials = cachedRenderer.sharedMaterials;
				}
				return cachedSharedMaterials;
			}
			return cachedRenderer.sharedMaterials;
		}
		set
		{
			if (CacheSharedMaterialsFromRenderer)
			{
				cachedSharedMaterials = value;
			}
			cachedRenderer.sharedMaterials = value;
		}
	}

	public Material AcquireMaterial(Object owner = null, bool instance = true)
	{
		if (owner != null)
		{
			materialOwners.Add(owner);
		}
		if (instance)
		{
			AcquireInstances();
		}
		Material[] array = instanceMaterials;
		if (array != null && array.Length != 0)
		{
			return instanceMaterials[0];
		}
		return null;
	}

	public Material[] AcquireMaterials(Object owner = null, bool instance = true)
	{
		if (owner != null)
		{
			materialOwners.Add(owner);
		}
		if (instance)
		{
			AcquireInstances();
		}
		base.gameObject.GetComponent<Material>();
		return instanceMaterials;
	}

	public void ReleaseMaterial(Object owner, bool autoDestroy = true)
	{
		materialOwners.Remove(owner);
		if (autoDestroy && materialOwners.Count == 0)
		{
			DestroySafe(this);
			if (!base.gameObject.activeInHierarchy)
			{
				RestoreRenderer();
			}
		}
	}

	private void Awake()
	{
		Initialize();
	}

	private void OnDestroy()
	{
		RestoreRenderer();
	}

	private void RestoreRenderer()
	{
		if (CachedRenderer != null && defaultMaterials != null)
		{
			CachedRendererSharedMaterials = defaultMaterials;
		}
		DestroyMaterials(instanceMaterials);
		instanceMaterials = null;
	}

	private void Initialize()
	{
		if (!initialized && CachedRenderer != null)
		{
			if (!HasValidMaterial(defaultMaterials))
			{
				defaultMaterials = CachedRendererSharedMaterials;
			}
			else if (!materialsInstanced)
			{
				CachedRendererSharedMaterials = defaultMaterials;
			}
			initialized = true;
		}
	}

	private void AcquireInstances()
	{
		if (CachedRenderer != null && !MaterialsMatch(CachedRendererSharedMaterials, instanceMaterials))
		{
			CreateInstances();
		}
	}

	private void CreateInstances()
	{
		Initialize();
		DestroyMaterials(instanceMaterials);
		instanceMaterials = InstanceMaterials(defaultMaterials);
		if (CachedRenderer != null && instanceMaterials != null)
		{
			CachedRendererSharedMaterials = instanceMaterials;
		}
		materialsInstanced = true;
	}

	private static bool MaterialsMatch(Material[] a, Material[] b)
	{
		if (a?.Length != b?.Length)
		{
			return false;
		}
		for (int i = 0; i < a?.Length; i++)
		{
			if (a[i] != b[i])
			{
				return false;
			}
		}
		return true;
	}

	private static Material[] InstanceMaterials(Material[] source)
	{
		if (source == null)
		{
			return null;
		}
		Material[] array = new Material[source.Length];
		for (int i = 0; i < source.Length; i++)
		{
			if (source[i] != null)
			{
				if (IsInstanceMaterial(source[i]))
				{
					Debug.LogWarning("A material (" + source[i].name + ") which is already instanced was instanced multiple times.");
				}
				array[i] = new Material(source[i]);
				array[i].name += " (Instance)";
			}
		}
		return array;
	}

	private static void DestroyMaterials(Material[] materials)
	{
		if (materials != null)
		{
			for (int i = 0; i < materials.Length; i++)
			{
				DestroySafe(materials[i]);
			}
		}
	}

	private static bool IsInstanceMaterial(Material material)
	{
		if (material != null)
		{
			return material.name.Contains(" (Instance)");
		}
		return false;
	}

	private static bool HasValidMaterial(Material[] materials)
	{
		if (materials != null)
		{
			for (int i = 0; i < materials.Length; i++)
			{
				if (materials[i] != null)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static void DestroySafe(Object toDestroy)
	{
		if (toDestroy != null && Application.isPlaying)
		{
			Object.Destroy(toDestroy);
		}
	}
}
