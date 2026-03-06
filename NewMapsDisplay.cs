using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modio;
using Modio.Errors;
using Modio.Images;
using Modio.Mods;
using Modio.Unity;
using TMPro;
using UnityEngine;

public class NewMapsDisplay : MonoBehaviour
{
	private struct NewMapData
	{
		public Texture2D image;

		public string info;
	}

	[SerializeField]
	private SpriteRenderer mapImage;

	[SerializeField]
	private TMP_Text loadingText;

	[Tooltip("DEPRECATED")]
	[SerializeField]
	private TMP_Text modNameText;

	[Tooltip("DEPRECATED")]
	[SerializeField]
	private TMP_Text modCreatorLabelText;

	[Tooltip("DEPRECATED")]
	[SerializeField]
	private TMP_Text modCreatorText;

	[SerializeField]
	private TMP_Text mapInfoTMP;

	[SerializeField]
	private float slideshowUpdateInterval = 1f;

	[SerializeField]
	private string loadingString = "LOADING...";

	[SerializeField]
	private string ugcDisabledString = "UGC DISABLED BY K-ID SETTINGS";

	private ModId newMapsModId = ModId.Null;

	private Mod newMapsModProfile;

	private List<NewMapData> newMapDatas = new List<NewMapData>();

	private bool slideshowActive;

	private int slideshowIndex;

	private float lastSlideshowUpdate;

	private bool requestingNewMapsModProfile;

	private LazyImage<Texture2D> lazyImage;

	private bool downloadingImages;

	private bool downloadingImage;

	private Texture2D lastDownloadedImage;

	private Coroutine initCoroutine;

	private Dictionary<Texture2D, Sprite> cachedTextures = new Dictionary<Texture2D, Sprite>();

	public void OnEnable()
	{
		mapImage.gameObject.SetActive(value: false);
		mapInfoTMP.text = "";
		mapInfoTMP.gameObject.SetActive(value: false);
		UGCPermissionManager.SubscribeToUGCEnabled(OnUGCEnabled);
		UGCPermissionManager.SubscribeToUGCDisabled(OnUGCDisabled);
		if (!UGCPermissionManager.IsUGCDisabled)
		{
			if (!ModIOManager.IsInitialized() || !ModIOManager.TryGetNewMapsModId(out newMapsModId))
			{
				initCoroutine = StartCoroutine(DelayedInitialize());
			}
			else
			{
				if (newMapsModId == ModId.Null)
				{
					return;
				}
				Initialize();
			}
		}
		loadingText.gameObject.SetActive(value: true);
	}

	public void OnDisable()
	{
		if (initCoroutine != null)
		{
			StopCoroutine(initCoroutine);
			initCoroutine = null;
		}
		newMapsModProfile = null;
		newMapDatas.Clear();
		slideshowActive = false;
		slideshowIndex = 0;
		lastSlideshowUpdate = 0f;
		mapImage.gameObject.SetActive(value: false);
		mapInfoTMP.text = "";
		mapInfoTMP.gameObject.SetActive(value: false);
		loadingText.text = loadingString;
		loadingText.gameObject.SetActive(value: false);
		UGCPermissionManager.UnsubscribeFromUGCEnabled(OnUGCEnabled);
		UGCPermissionManager.UnsubscribeFromUGCDisabled(OnUGCDisabled);
	}

	private void OnUGCEnabled()
	{
		if (newMapDatas.IsNullOrEmpty())
		{
			if (!ModIOManager.IsInitialized() || !ModIOManager.TryGetNewMapsModId(out newMapsModId))
			{
				initCoroutine = StartCoroutine(DelayedInitialize());
			}
			else if (!(newMapsModId == ModId.Null))
			{
				Initialize();
			}
		}
	}

	private void OnUGCDisabled()
	{
		mapImage.gameObject.SetActive(value: false);
		mapInfoTMP.text = "";
		mapInfoTMP.gameObject.SetActive(value: false);
		loadingText.text = ugcDisabledString;
		loadingText.gameObject.SetActive(value: true);
	}

	private IEnumerator DelayedInitialize()
	{
		while (!ModIOManager.TryGetNewMapsModId(out newMapsModId))
		{
			yield return new WaitForSecondsRealtime(1f);
		}
		initCoroutine = null;
		if (!(newMapsModId == ModId.Null))
		{
			Initialize();
		}
	}

	private async Task<Error> Initialize()
	{
		if (!requestingNewMapsModProfile && !downloadingImages)
		{
			requestingNewMapsModProfile = true;
			loadingText.text = loadingString;
			Error error = await ModIOManager.Initialize();
			if ((bool)error)
			{
				return error;
			}
			if (!base.isActiveAndEnabled)
			{
				return Error.None;
			}
			(error, newMapsModProfile) = await ModIOManager.GetMod(newMapsModId);
			if ((bool)error)
			{
				GTDev.LogWarning("[NewMapsDisplay::OnGetNewMapsModProfile] Failed to get NewMaps ModProfile " + $"from mod.io: {error}");
				return error;
			}
			newMapDatas.Clear();
			string[] array = newMapsModProfile.MetadataBlob.Split(';');
			string text = "";
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				if (text2.StartsWith("mapInfo:"))
				{
					text = text2.Substring(8);
					break;
				}
			}
			string[] mapInfoList = (text.IsNullOrEmpty() ? null : text.Split(','));
			lazyImage = new LazyImage<Texture2D>(ImageCacheTexture2D.Instance, delegate(Texture2D loadedImage)
			{
				downloadingImage = false;
				lastDownloadedImage = loadedImage;
			});
			downloadingImages = true;
			int i;
			for (int i2 = 0; i2 < newMapsModProfile.Gallery.Length; i2 = i)
			{
				downloadingImage = true;
				lazyImage.SetImage(newMapsModProfile.Gallery[i2], Mod.GalleryResolution.X320_Y180);
				while (downloadingImage)
				{
					await Task.Yield();
				}
				string info = ((mapInfoList != null && mapInfoList.Length > i2) ? mapInfoList[i2] : "");
				NewMapData item = new NewMapData
				{
					image = lastDownloadedImage,
					info = info
				};
				newMapDatas.Add(item);
				lastDownloadedImage = null;
				i = i2 + 1;
			}
			downloadingImages = false;
			if (!base.isActiveAndEnabled)
			{
				return Error.None;
			}
			StartSlideshow();
			requestingNewMapsModProfile = false;
			return Error.None;
		}
		return new Error(ErrorCode.UNKNOWN, "Initialization already in progress.");
	}

	private void StartSlideshow()
	{
		if (!newMapDatas.IsNullOrEmpty())
		{
			slideshowIndex = 0;
			slideshowActive = true;
			UpdateSlideshow();
		}
	}

	public void Update()
	{
		if (slideshowActive && !(Time.time - lastSlideshowUpdate < slideshowUpdateInterval))
		{
			UpdateSlideshow();
		}
	}

	private void UpdateSlideshow()
	{
		loadingText.gameObject.SetActive(value: false);
		lastSlideshowUpdate = Time.time;
		Texture2D image = newMapDatas[slideshowIndex].image;
		if (image != null)
		{
			if (!cachedTextures.TryGetValue(image, out var value))
			{
				value = Sprite.Create(image, new Rect(0f, 0f, image.width, image.height), new Vector2(0.5f, 0.5f));
				cachedTextures.Add(image, value);
			}
			mapImage.sprite = value;
			mapImage.gameObject.SetActive(value: true);
		}
		else
		{
			mapImage.gameObject.SetActive(value: false);
		}
		mapInfoTMP.text = newMapDatas[slideshowIndex].info;
		mapInfoTMP.gameObject.SetActive(value: true);
		slideshowIndex++;
		if (slideshowIndex >= newMapDatas.Count)
		{
			slideshowIndex = 0;
		}
	}
}
