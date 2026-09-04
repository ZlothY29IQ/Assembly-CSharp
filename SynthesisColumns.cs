using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class SynthesisColumns : MonoBehaviour
{
	public enum ColumnShape
	{
		Cylinder,
		Rectangle
	}

	[Serializable]
	public class SynthesisColumnLayoutPayload
	{
		public List<SynthesisColumnLayoutEntry> columns;
	}

	[Serializable]
	public class SynthesisColumnLayoutEntry
	{
		public string shape;

		public float x;

		public float y;

		public float diameter;

		public float width;

		public float depth;

		public float height;

		public string texture;

		public float rotate;
	}

	[Serializable]
	public class TextureMapping
	{
		public string key;

		public Texture2D texture;
	}

	[Serializable]
	private class ColumnVisualRuntimeData : MonoBehaviour
	{
		public float worldDiameterM;
	}

	private enum RenderPipelineType
	{
		Builtin,
		SRP_URP
	}

	[Header("Default column prefab (cylinder mesh + collider)")]
	internal GameObject defaultColumnPrefab;

	[Header("Default rectangular prefab (box mesh + collider)")]
	internal GameObject defaultRectPrefab;

	[Header("Column materials per pipeline")]
	internal Material defaultMaterialURP;

	internal Material defaultMaterialBuiltin;

	[Header("Optional known textures (can be Resources, Addressables, etc.)")]
	public List<TextureMapping> textureLibrary;

	private SynthesisColumnLayoutPayload _currentLayout;

	private Dictionary<string, Texture2D> _dynamicTextureCache = new Dictionary<string, Texture2D>();

	private Dictionary<string, List<MeshRenderer>> _pendingTextureAssignments;

	private string _lastSceneSpawnedFor;

	private string _skinsRoot;

	private readonly List<GameObject> _spawnedColumns = new List<GameObject>();

	internal MaterialPropertyBlock _mpb;

	internal static SynthesisColumnLayoutPayload ParseColumnsJson(string json)
	{
		return JsonUtility.FromJson<SynthesisColumnLayoutPayload>(json);
	}

	internal void LoadLayoutFromJson(string json)
	{
		_currentLayout = ParseColumnsJson(json);
		SpawnForActiveScene();
	}

	internal void AutoAssignFromResources()
	{
		if (defaultColumnPrefab == null)
		{
			defaultColumnPrefab = Resources.Load<GameObject>("Prefab_Column_Default");
			if (defaultColumnPrefab == null)
			{
				Debug.LogWarning("[Columns] Could not auto-load defaultColumnPrefab from Resources/Prefab_Column_Default.prefab");
			}
		}
		if (defaultRectPrefab == null)
		{
			defaultRectPrefab = Resources.Load<GameObject>("Prefab_Rect_Default");
			if (defaultRectPrefab == null)
			{
				Debug.LogWarning("[Columns] Missing Resources/Prefab_Rect_Default.prefab");
			}
		}
		if (defaultMaterialURP == null)
		{
			defaultMaterialURP = Resources.Load<Material>("Material_URP_Cylinder");
			if (defaultMaterialURP == null)
			{
				Debug.LogWarning("[Columns] Could not auto-load defaultMaterialURP from Resources/Material_URP_Cylinder.mat");
			}
		}
		if (defaultMaterialBuiltin == null)
		{
			defaultMaterialBuiltin = Resources.Load<Material>("Material_Builtin_Cylinder");
			if (defaultMaterialBuiltin == null)
			{
				Debug.LogWarning("[Columns] Could not auto-load defaultMaterialBuiltin from Resources/Material_Builtin_Cylinder.mat");
			}
		}
		if (textureLibrary == null)
		{
			textureLibrary = new List<TextureMapping>();
		}
		Texture2D texture2D = Resources.Load<Texture2D>("danger_pattern");
		if (texture2D == null)
		{
			Debug.LogWarning("[Columns] Could not auto-load danger_pattern.png from Resources/danger_pattern.png");
			return;
		}
		bool flag = false;
		for (int i = 0; i < textureLibrary.Count; i++)
		{
			if (textureLibrary[i] != null && textureLibrary[i].key == "danger_pattern")
			{
				if (textureLibrary[i].texture == null)
				{
					textureLibrary[i].texture = texture2D;
				}
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			textureLibrary.Add(new TextureMapping
			{
				key = "danger_pattern",
				texture = texture2D
			});
		}
	}

	private static ColumnShape ParseShape(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return ColumnShape.Cylinder;
		}
		switch (s.Trim().ToLowerInvariant())
		{
		case "rect":
		case "rectangle":
		case "box":
		case "quad":
			return ColumnShape.Rectangle;
		default:
			return ColumnShape.Cylinder;
		}
	}

	private RenderPipelineType GetActivePipeline()
	{
		if (GraphicsSettings.currentRenderPipeline == null)
		{
			return RenderPipelineType.Builtin;
		}
		return RenderPipelineType.SRP_URP;
	}

	private string GetSkinsRootFolder()
	{
		if (string.IsNullOrEmpty(_skinsRoot))
		{
			_skinsRoot = Path.Combine(Application.persistentDataPath, "PlayspaceSkins");
			if (!Directory.Exists(_skinsRoot))
			{
				Directory.CreateDirectory(_skinsRoot);
			}
		}
		return _skinsRoot;
	}

	internal void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (!(_lastSceneSpawnedFor == scene.name))
		{
			_lastSceneSpawnedFor = scene.name;
			SpawnForActiveScene();
		}
	}

	private void SafeClearSpawnedColumns()
	{
		if (_spawnedColumns.Count == 0)
		{
			return;
		}
		List<GameObject> list = new List<GameObject>(_spawnedColumns);
		for (int i = 0; i < list.Count; i++)
		{
			GameObject gameObject = list[i];
			if (!(gameObject == null))
			{
				Collider[] componentsInChildren = gameObject.GetComponentsInChildren<Collider>();
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					componentsInChildren[j].enabled = false;
				}
				gameObject.SetActive(value: false);
			}
		}
		_spawnedColumns.Clear();
		StartCoroutine(DestroyAfterPhysicsStep(list));
	}

	private IEnumerator DestroyAfterPhysicsStep(List<GameObject> toDestroy)
	{
		yield return new WaitForFixedUpdate();
		yield return null;
		for (int i = 0; i < toDestroy.Count; i++)
		{
			GameObject gameObject = toDestroy[i];
			if (gameObject != null)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
		}
	}

	private void SpawnForActiveScene()
	{
		SafeClearSpawnedColumns();
		if (_currentLayout == null || _currentLayout.columns == null)
		{
			Debug.LogWarning("[ColumnManager] No layout loaded, nothing to spawn.");
			return;
		}
		if (_lastSceneSpawnedFor == null || _lastSceneSpawnedFor.Length == 0)
		{
			Debug.LogWarning("[ColumnManager] No Scene loaded.");
			return;
		}
		foreach (SynthesisColumnLayoutEntry column in _currentLayout.columns)
		{
			if ((column.texture ?? "") == "" && textureLibrary.Count > 0)
			{
				column.texture = textureLibrary.First().key;
			}
			SpawnSingleColumn(column);
		}
	}

	private void SpawnSingleColumn(SynthesisColumnLayoutEntry entry)
	{
		ColumnShape columnShape = ParseShape(entry.shape);
		GameObject gameObject = ((columnShape == ColumnShape.Rectangle) ? defaultRectPrefab : defaultColumnPrefab);
		if (gameObject == null)
		{
			Debug.LogWarning("[ColumnManager] Missing prefab for shape " + columnShape);
			return;
		}
		Debug.LogWarning($"[ColumnManager][Scene={_lastSceneSpawnedFor}] Spawn new column {columnShape} ; Prefab=[{gameObject}]");
		GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject);
		gameObject2.transform.SetParent(base.transform, worldPositionStays: true);
		Vector3 position = new Vector3(entry.x, 0f, entry.y);
		Quaternion rotation = Quaternion.Euler(0f, entry.rotate, 0f);
		gameObject2.transform.SetPositionAndRotation(position, rotation);
		Vector3 localScale = gameObject2.transform.localScale;
		if (columnShape == ColumnShape.Cylinder)
		{
			localScale.z = (localScale.x = Mathf.Max(0.01f, entry.diameter));
			localScale.y = ((entry.height > 0f) ? entry.height : localScale.y);
		}
		else
		{
			float x = Mathf.Max(0.01f, entry.width);
			float z = Mathf.Max(0.01f, entry.depth);
			localScale.x = x;
			localScale.z = z;
			localScale.y = ((entry.height > 0f) ? entry.height : localScale.y);
		}
		gameObject2.transform.localScale = localScale;
		if (columnShape == ColumnShape.Cylinder)
		{
			ApplyTexture(gameObject2, entry.texture, entry.diameter);
		}
		else
		{
			Transform transform = gameObject2.transform.Find("Visual");
			MeshFilter meshFilter = (transform ? transform.GetComponent<MeshFilter>() : null);
			if (meshFilter != null)
			{
				transform.localScale = Vector3.one;
				transform.localRotation = Quaternion.identity;
				Mesh sharedMesh = BuildStripUVBox(localScale.x, localScale.z, localScale.y, 0.5f);
				meshFilter.sharedMesh = sharedMesh;
			}
			else
			{
				Debug.LogWarning("[Columns] Rectangle has no MeshFilter to swap.");
			}
			float y = meshFilter.sharedMesh.bounds.size.y;
			transform.localPosition = new Vector3(0f, y * 0.5f, 0f);
			gameObject2.transform.localScale = Vector3.one;
			BoxCollider component = gameObject2.GetComponent<BoxCollider>();
			if ((bool)component)
			{
				component.size = new Vector3(localScale.x, localScale.z, localScale.y);
				component.center = new Vector3(0f, localScale.z * 0.5f, 0f);
			}
			float worldDiameterM = 2f * (localScale.x + localScale.z) / MathF.PI;
			ApplyTexture(gameObject2, entry.texture, worldDiameterM);
		}
		_spawnedColumns.Add(gameObject2);
	}

	private void ApplyTexture(GameObject columnGO, string textureKey, float worldDiameterM)
	{
		MeshRenderer componentInChildren = columnGO.GetComponentInChildren<MeshRenderer>();
		if (componentInChildren == null)
		{
			return;
		}
		ColumnVisualRuntimeData columnVisualRuntimeData = componentInChildren.GetComponent<ColumnVisualRuntimeData>();
		if (columnVisualRuntimeData == null)
		{
			columnVisualRuntimeData = componentInChildren.gameObject.AddComponent<ColumnVisualRuntimeData>();
		}
		columnVisualRuntimeData.worldDiameterM = worldDiameterM;
		Material material = ((GetActivePipeline() == RenderPipelineType.SRP_URP) ? defaultMaterialURP : defaultMaterialBuiltin);
		if (string.IsNullOrEmpty(textureKey))
		{
			if (material != null)
			{
				componentInChildren.material = new Material(material);
			}
			return;
		}
		Texture2D texture2D = FindTexture(textureKey);
		if (texture2D != null)
		{
			Debug.Log("[Columns] ApplyTexture immediate for key=" + textureKey + " tex=" + texture2D.width + "x" + texture2D.height + " ; Diameter=" + worldDiameterM);
			ApplyTextureToRenderer(componentInChildren, texture2D, worldDiameterM);
			return;
		}
		Debug.Log("[Columns] Texture not ready yet for key=" + textureKey + " - registering pending.");
		if (material != null)
		{
			componentInChildren.material = new Material(material);
		}
		if (_pendingTextureAssignments == null)
		{
			_pendingTextureAssignments = new Dictionary<string, List<MeshRenderer>>();
		}
		if (!_pendingTextureAssignments.TryGetValue(textureKey, out var value))
		{
			value = new List<MeshRenderer>();
			_pendingTextureAssignments[textureKey] = value;
		}
		if (!value.Contains(componentInChildren))
		{
			value.Add(componentInChildren);
		}
	}

	private Texture2D FindTexture(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return null;
		}
		for (int i = 0; i < textureLibrary.Count; i++)
		{
			if (textureLibrary[i].key == key && textureLibrary[i].texture != null)
			{
				return textureLibrary[i].texture;
			}
		}
		if (_dynamicTextureCache == null)
		{
			_dynamicTextureCache = new Dictionary<string, Texture2D>();
		}
		if (_dynamicTextureCache.TryGetValue(key, out var value))
		{
			Debug.Log("[Columns] FindTexture('" + key + "') -> cached in memory");
			return value;
		}
		Texture2D texture2D = TryLoadTextureFromDisk(key);
		if (texture2D != null)
		{
			Debug.Log("[Columns] FindTexture('" + key + "') -> loaded from disk");
			_dynamicTextureCache[key] = texture2D;
			return texture2D;
		}
		if (IsHttpUrl(key))
		{
			Debug.Log("[Columns] FindTexture('" + key + "') -> not cached, starting download");
			StartCoroutine(DownloadAndCacheTextureCoroutine(key));
		}
		else
		{
			Debug.Log("[Columns] FindTexture('" + key + "') -> MISS (no file, not URL)");
		}
		return null;
	}

	private bool IsHttpUrl(string s)
	{
		if (!s.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
		{
			return s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private string GetCacheFilePathForUrl(string url)
	{
		using MD5 mD = MD5.Create();
		string text = BitConverter.ToString(mD.ComputeHash(Encoding.UTF8.GetBytes(url))).Replace("-", "").ToLowerInvariant();
		return Path.Combine(GetSkinsRootFolder(), "cache_" + text + ".png");
	}

	private Texture2D TryLoadTextureFromDisk(string keyOrUrl)
	{
		string text = ((!IsHttpUrl(keyOrUrl)) ? Path.Combine(GetSkinsRootFolder(), keyOrUrl) : GetCacheFilePathForUrl(keyOrUrl));
		if (!File.Exists(text))
		{
			return null;
		}
		try
		{
			byte[] data = File.ReadAllBytes(text);
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
			if (!texture2D.LoadImage(data))
			{
				Debug.LogWarning("[Columns] Failed to decode image at " + text);
				return null;
			}
			texture2D.wrapMode = TextureWrapMode.Repeat;
			texture2D.filterMode = FilterMode.Bilinear;
			texture2D.Apply();
			return texture2D;
		}
		catch (Exception ex)
		{
			Debug.LogError("[Columns] Error loading texture from " + text + " -> " + ex.Message);
			return null;
		}
	}

	private IEnumerator DownloadAndCacheTextureCoroutine(string url)
	{
		string cachePath = GetCacheFilePathForUrl(url);
		if (_dynamicTextureCache == null)
		{
			_dynamicTextureCache = new Dictionary<string, Texture2D>();
		}
		if (File.Exists(cachePath))
		{
			Texture2D texture2D = TryLoadTextureFromDisk(url);
			if (texture2D != null)
			{
				_dynamicTextureCache[url] = texture2D;
				ApplyDownloadedTextureToPending(url, texture2D);
			}
			yield break;
		}
		using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
		yield return req.SendWebRequest();
		if (req.result != UnityWebRequest.Result.Success)
		{
			Debug.LogWarning("[Columns] Download failed: " + req.error + " url=" + url);
			yield break;
		}
		Texture2D content = DownloadHandlerTexture.GetContent(req);
		if (content == null)
		{
			Debug.LogWarning("[Columns] Got empty texture from " + url);
			yield break;
		}
		try
		{
			byte[] bytes = content.EncodeToPNG();
			File.WriteAllBytes(cachePath, bytes);
		}
		catch (Exception ex)
		{
			Debug.LogError("[Columns] Failed saving cache PNG for " + url + " -> " + ex.Message);
		}
		_dynamicTextureCache[url] = content;
		ApplyDownloadedTextureToPending(url, content);
	}

	private void ApplyDownloadedTextureToPending(string textureKey, Texture2D tex)
	{
		if (tex == null || _pendingTextureAssignments == null || !_pendingTextureAssignments.TryGetValue(textureKey, out var value))
		{
			return;
		}
		Debug.Log("[Columns] Fulfilling pending texture key=" + textureKey + " for " + value.Count + " renderer(s)");
		for (int i = 0; i < value.Count; i++)
		{
			MeshRenderer meshRenderer = value[i];
			if (!(meshRenderer == null))
			{
				ColumnVisualRuntimeData columnVisualRuntimeData = ((meshRenderer != null) ? meshRenderer.GetComponent<ColumnVisualRuntimeData>() : null);
				float worldDiameterM = ((columnVisualRuntimeData != null) ? columnVisualRuntimeData.worldDiameterM : 0.35f);
				ApplyTextureToRenderer(meshRenderer, tex, worldDiameterM);
			}
		}
		_pendingTextureAssignments.Remove(textureKey);
	}

	private void ApplyTextureToRenderer(MeshRenderer r, Texture2D tex, float worldDiameterM)
	{
		if (r == null || tex == null)
		{
			return;
		}
		RenderPipelineType activePipeline = GetActivePipeline();
		Material material = null;
		if (activePipeline == RenderPipelineType.SRP_URP)
		{
			if (defaultMaterialURP == null)
			{
				Debug.LogError("[Columns] No URP base material assigned, cannot skin column.");
				return;
			}
			material = defaultMaterialURP;
		}
		else
		{
			if (defaultMaterialBuiltin == null)
			{
				Debug.LogError("[Columns] No Built-in base material assigned, cannot skin column.");
				return;
			}
			material = defaultMaterialBuiltin;
		}
		Material material2 = new Material(material);
		if (material2.HasProperty("_MainTex"))
		{
			material2.SetTexture("_MainTex", tex);
		}
		if (material2.HasProperty("_BaseMap"))
		{
			material2.SetTexture("_BaseMap", tex);
		}
		material2.mainTexture = tex;
		Color white = Color.white;
		white.a = 0.9f;
		if (material2.HasProperty("_BaseColor"))
		{
			material2.SetColor("_BaseColor", white);
		}
		if (material2.HasProperty("_Color"))
		{
			material2.SetColor("_Color", white);
		}
		if (activePipeline == RenderPipelineType.SRP_URP)
		{
			if (material2.HasProperty("_Surface"))
			{
				material2.SetFloat("_Surface", 1f);
			}
			if (material2.HasProperty("_AlphaClip"))
			{
				material2.SetFloat("_AlphaClip", 0f);
			}
			material2.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			material2.DisableKeyword("_SURFACE_TYPE_OPAQUE");
			material2.DisableKeyword("_ALPHATEST_ON");
		}
		float value = ComputeTileY_FromAspect(tex, 1f, 0.5f);
		value = Mathf.Clamp(value, 1f, 8f);
		float x = 1f;
		if (material2.HasProperty("_BaseMap_ST"))
		{
			Vector4 vector = material2.GetVector("_BaseMap_ST");
			vector.x = x;
			vector.y = value;
			vector.z = 0f;
			vector.w = 0f;
			material2.SetVector("_BaseMap_ST", vector);
		}
		material2.mainTextureScale = new Vector2(x, value);
		material2.mainTextureOffset = Vector2.zero;
		Material[] materials = r.materials;
		for (int i = 0; i < materials.Length; i++)
		{
			materials[i] = material2;
		}
		r.materials = materials;
		Debug.Log("[Columns] Applied runtime transparent material to " + r.name + " shader=" + material2.shader.name + " tex=" + tex.width + "x" + tex.height + " tiling=" + x + "x" + value + " slots=" + materials.Length);
	}

	private static Mesh BuildStripUVBox(float w, float d, float h, float tileMeters)
	{
		float num = w * 0.5f;
		float num2 = h * 0.5f;
		float num3 = d * 0.5f;
		float num4 = Mathf.Max(1f, Mathf.Round(w / tileMeters));
		float num5 = Mathf.Max(1f, Mathf.Round(d / tileMeters));
		float num6 = 0f;
		float num7 = num6 + num4;
		float num8 = num7 + num5;
		float num9 = num8 + num4;
		float x = num9 + num5;
		Vector3[] verts = new Vector3[24];
		Vector2[] uvs = new Vector2[24];
		int[] tris = new int[36];
		int vi = 0;
		int ti = 0;
		Face(new Vector3(0f - num, 0f - num2, num3), new Vector3(0f - num, num2, num3), new Vector3(num, num2, num3), new Vector3(num, 0f - num2, num3), new Vector2(num7, 0f), new Vector2(num7, 1f), new Vector2(num6, 1f), new Vector2(num6, 0f));
		Face(new Vector3(num, 0f - num2, num3), new Vector3(num, num2, num3), new Vector3(num, num2, 0f - num3), new Vector3(num, 0f - num2, 0f - num3), new Vector2(num8, 0f), new Vector2(num8, 1f), new Vector2(num7, 1f), new Vector2(num7, 0f));
		Face(new Vector3(num, 0f - num2, 0f - num3), new Vector3(num, num2, 0f - num3), new Vector3(0f - num, num2, 0f - num3), new Vector3(0f - num, 0f - num2, 0f - num3), new Vector2(num9, 0f), new Vector2(num9, 1f), new Vector2(num8, 1f), new Vector2(num8, 0f));
		Face(new Vector3(0f - num, 0f - num2, 0f - num3), new Vector3(0f - num, num2, 0f - num3), new Vector3(0f - num, num2, num3), new Vector3(0f - num, 0f - num2, num3), new Vector2(x, 0f), new Vector2(x, 1f), new Vector2(num9, 1f), new Vector2(num9, 0f));
		Face(new Vector3(0f - num, num2, num3), new Vector3(0f - num, num2, 0f - num3), new Vector3(num, num2, 0f - num3), new Vector3(num, num2, num3), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
		Face(new Vector3(0f - num, 0f - num2, 0f - num3), new Vector3(0f - num, 0f - num2, num3), new Vector3(num, 0f - num2, num3), new Vector3(num, 0f - num2, 0f - num3), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
		Mesh mesh = new Mesh();
		mesh.name = "StripUVBox_IntRepeats";
		mesh.vertices = verts;
		mesh.uv = uvs;
		mesh.triangles = tris;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		mesh.RecalculateTangents();
		return mesh;
		void Face(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Vector2 uvBL, Vector2 uvTL, Vector2 uvTR, Vector2 uvBR)
		{
			verts[vi] = bl;
			uvs[vi] = uvBL;
			verts[vi + 1] = tl;
			uvs[vi + 1] = uvTL;
			verts[vi + 2] = tr;
			uvs[vi + 2] = uvTR;
			verts[vi + 3] = br;
			uvs[vi + 3] = uvBR;
			tris[ti] = vi;
			tris[ti + 1] = vi + 2;
			tris[ti + 2] = vi + 1;
			tris[ti + 3] = vi;
			tris[ti + 4] = vi + 3;
			tris[ti + 5] = vi + 2;
			vi += 4;
			ti += 6;
		}
	}

	private float ComputeTileY_FromAspect(Texture2D tex, float heightMeters, float tileMetersX)
	{
		if (tex == null || tileMetersX <= 0f)
		{
			return 1f;
		}
		float b = tileMetersX * ((float)tex.height / (float)Mathf.Max(1, tex.width));
		float f = heightMeters / Mathf.Max(0.0001f, b);
		f = Mathf.Max(1f, Mathf.Round(f));
		return Mathf.Max(1f, f);
	}

	internal string readColumnsFromJsonFile(SynthesisArcadeObject _instance)
	{
		string text = _instance.ReadCommandLineArgument("-svrcolumns") ?? "{}";
		if (File.Exists(Path.Combine(Application.persistentDataPath, "Columns", text)))
		{
			try
			{
				text = File.ReadAllText(Path.Combine(Application.persistentDataPath, "Columns", text));
			}
			catch (Exception ex)
			{
				Debug.LogError("[" + _instance.TAG + "] [COLUMNS_JSON_ERROR/1] => " + ex.Message);
			}
		}
		else if (File.Exists(Path.Combine(Environment.ExpandEnvironmentVariables("%localappdata%Low\\SynthesisVR\\Columns"), text)))
		{
			try
			{
				text = File.ReadAllText(Path.Combine(Environment.ExpandEnvironmentVariables("%localappdata%Low\\SynthesisVR\\Columns"), text));
			}
			catch (Exception ex2)
			{
				Debug.LogError("[" + _instance.TAG + "] [COLUMNS_JSON_ERROR/2] => " + ex2.Message);
			}
		}
		else
		{
			Debug.Log("[" + _instance.TAG + "] Column File Does not Exists = " + text);
			text = "{}";
		}
		return text ?? "{}";
	}
}
