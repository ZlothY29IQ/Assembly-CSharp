using System;
using System.Collections;
using System.Threading.Tasks;
using Modio;
using Modio.Mods;
using Modio.Unity;
using UnityEngine;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

public class CuratedMapBoard : MonoBehaviour
{
	[Tooltip("Left shows the 1st image in the mod.io gallery, Right shows the 2nd")]
	[SerializeField]
	private CuratedDestinationsManager.CuratedDoorway doorway;

	[Tooltip("Renderer of the board face the image is drawn on")]
	[SerializeField]
	private Renderer boardRenderer;

	[SerializeField]
	private int materialIndex;

	[SerializeField]
	private string texturePropertyName = "_BaseMap";

	[Tooltip("Shown while loading and whenever the mod.io image can't be shown. Leave empty for a black board.")]
	[SerializeField]
	private Texture fallbackTexture;

	[SerializeField]
	private float refreshIntervalSeconds = 600f;

	[OnEnterPlay_SetNull]
	private static Task<Mod> sharedFetch;

	[OnEnterPlay_Set(0f)]
	private static float sharedFetchTime;

	private MaterialPropertyBlock propertyBlock;

	private Coroutine refreshCoroutine;

	private int texturePropertyId;

	private bool hasShownRemoteImage;

	private void Awake()
	{
		propertyBlock = new MaterialPropertyBlock();
		texturePropertyId = Shader.PropertyToID(texturePropertyName);
	}

	private void OnEnable()
	{
		hasShownRemoteImage = false;
		ShowFallback();
		UGCPermissionManager.SubscribeToUGCEnabled(OnUGCEnabled);
		UGCPermissionManager.SubscribeToUGCDisabled(OnUGCDisabled);
		if (!UGCPermissionManager.IsUGCDisabled)
		{
			StartRefreshing();
		}
	}

	private void OnDisable()
	{
		UGCPermissionManager.UnsubscribeFromUGCEnabled(OnUGCEnabled);
		UGCPermissionManager.UnsubscribeFromUGCDisabled(OnUGCDisabled);
		StopRefreshing();
	}

	private void OnUGCEnabled()
	{
		StartRefreshing();
	}

	private void OnUGCDisabled()
	{
		StopRefreshing();
		hasShownRemoteImage = false;
		ShowFallback();
	}

	private void StartRefreshing()
	{
		if (refreshCoroutine == null)
		{
			refreshCoroutine = StartCoroutine(RefreshLoop());
		}
	}

	private void StopRefreshing()
	{
		if (refreshCoroutine != null)
		{
			StopCoroutine(refreshCoroutine);
			refreshCoroutine = null;
		}
	}

	private IEnumerator RefreshLoop()
	{
		ModId displayModId;
		while (!ModIOManager.TryGetNewMapsModId(out displayModId))
		{
			yield return new WaitForSecondsRealtime(1f);
		}
		if (displayModId == ModId.Null)
		{
			refreshCoroutine = null;
			yield break;
		}
		while (true)
		{
			Task refresh = Refresh(displayModId);
			while (!refresh.IsCompleted)
			{
				yield return null;
			}
			yield return new WaitForSecondsRealtime(Mathf.Max(refreshIntervalSeconds, 60f));
		}
	}

	private async Task Refresh(ModId displayModId)
	{
		Mod mod = await FetchDisplayMod(displayModId, refreshIntervalSeconds);
		if (!base.isActiveAndEnabled)
		{
			return;
		}
		if (mod == null)
		{
			if (!hasShownRemoteImage)
			{
				ShowFallback();
			}
			return;
		}
		int num = (int)doorway;
		if (mod.Gallery == null || num >= mod.Gallery.Length)
		{
			hasShownRemoteImage = false;
			ShowFallback();
			return;
		}
		var (error, texture2D) = await ImageCacheTexture2D.Instance.DownloadImage(mod.Gallery[num].GetUri(Mod.GalleryResolution.X1280_Y720));
		if (!base.isActiveAndEnabled)
		{
			return;
		}
		if ((bool)error || texture2D == null)
		{
			GTDev.LogWarning($"[CuratedMapBoard::Refresh] Failed to download {doorway} board image: {error}");
			if (!hasShownRemoteImage)
			{
				ShowFallback();
			}
		}
		else
		{
			texture2D.wrapMode = TextureWrapMode.Clamp;
			hasShownRemoteImage = true;
			ShowTexture(texture2D);
		}
	}

	private static Task<Mod> FetchDisplayMod(ModId displayModId, float maxAgeSeconds)
	{
		if (sharedFetch != null && (!sharedFetch.IsCompleted || (sharedFetch.Result != null && Time.realtimeSinceStartup - sharedFetchTime < maxAgeSeconds)))
		{
			return sharedFetch;
		}
		sharedFetchTime = Time.realtimeSinceStartup;
		sharedFetch = FetchDisplayModInternal(displayModId);
		return sharedFetch;
	}

	private static async Task<Mod> FetchDisplayModInternal(ModId displayModId)
	{
		_ = 1;
		try
		{
			Error error = await ModIOManager.Initialize();
			if ((bool)error)
			{
				GTDev.LogWarning($"[CuratedMapBoard::FetchDisplayMod] mod.io not available: {error}");
				return null;
			}
			Mod result;
			(error, result) = await ModIOManager.GetMod(displayModId, forceUpdate: true);
			if ((bool)error)
			{
				GTDev.LogWarning("[CuratedMapBoard::FetchDisplayMod] Failed to get display mod " + $"{displayModId}: {error}");
				return null;
			}
			return result;
		}
		catch (Exception ex)
		{
			GTDev.LogWarning("[CuratedMapBoard::FetchDisplayMod] Failed to get display mod " + $"{displayModId}: {ex.Message}");
			return null;
		}
	}

	private void ShowFallback()
	{
		ShowTexture((fallbackTexture != null) ? fallbackTexture : Texture2D.blackTexture);
	}

	private void ShowTexture(Texture texture)
	{
		if (!(boardRenderer == null))
		{
			boardRenderer.GetPropertyBlock(propertyBlock, materialIndex);
			propertyBlock.SetTexture(texturePropertyId, texture);
			boardRenderer.SetPropertyBlock(propertyBlock, materialIndex);
		}
	}
}
