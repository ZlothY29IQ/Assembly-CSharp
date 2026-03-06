using System;
using System.Collections;
using System.Collections.Generic;
using GameObjectScheduling;
using GorillaExtensions;
using GorillaNetworking;
using GorillaNetworking.Store;
using TMPro;
using UnityEngine;

namespace FXP;

[Obsolete("CosmeticItemPrefab is deprecated, if we want to use this we need services to re-activate a webservice that was called gt-featureditem-dev.")]
public class CosmeticItemPrefab : MonoBehaviour
{
	[SerializeField]
	public enum EDisplayMode
	{
		NULL,
		HIDDEN,
		PREVIEW,
		ATTRACT,
		PURCHASE,
		POSTPURCHASE
	}

	public string PedestalID = "";

	public HeadModel HeadModel;

	public bool AffectedByStoreUpdateEvents = true;

	[SerializeField]
	private Guid? itemGUID;

	[SerializeField]
	private string itemName = string.Empty;

	[SerializeField]
	private List<Transform> sockets = new List<Transform>();

	[SerializeField]
	private int itemSocket = int.MinValue;

	[SerializeField]
	private int? hoursInPreviewMode;

	[SerializeField]
	private int? hoursInAttractMode;

	[SerializeField]
	private Mesh pedestalMesh;

	[SerializeField]
	private Mesh mannequinMesh;

	[SerializeField]
	private Mesh cosmeticMesh;

	[SerializeField]
	private AudioClip sfxPreviewMode;

	[SerializeField]
	private AudioClip sfxAttractMode;

	[SerializeField]
	private AudioClip sfxPurchaseMode;

	[SerializeField]
	private ParticleSystem vfxPreviewMode;

	[SerializeField]
	private ParticleSystem vfxAttractMode;

	[SerializeField]
	private ParticleSystem vfxPurchaseMode;

	[SerializeField]
	private GameObject goPedestal;

	[SerializeField]
	private GameObject goMannequin;

	[SerializeField]
	private GameObject goCosmeticItem;

	[SerializeField]
	private GameObject goCosmeticItemGameObject;

	[SerializeField]
	private GameObject goCosmeticItemNameplate;

	[SerializeField]
	private GameObject goClock;

	[SerializeField]
	private GameObject goPreviewMode;

	[SerializeField]
	private GameObject goAttractMode;

	[SerializeField]
	private GameObject goPurchaseMode;

	[SerializeField]
	private Mesh defaultPedestalMesh;

	[SerializeField]
	private Material defaultPedestalMaterial;

	[SerializeField]
	private Mesh defaultMannequinMesh;

	[SerializeField]
	private Material defaultMannequinMaterial;

	[SerializeField]
	private Mesh defaultCosmeticMesh;

	[SerializeField]
	private Material defaultCosmeticMaterial;

	[SerializeField]
	private string defaultItemText;

	[SerializeField]
	private int defaultHoursInPreviewMode;

	[SerializeField]
	private int defaultHoursInAttractMode;

	[SerializeField]
	private AudioClip defaultSFXPreviewMode;

	[SerializeField]
	private AudioClip defaultSFXAttractMode;

	[SerializeField]
	private AudioClip defaultSFXPurchaseMode;

	private GameObject goCosmeticItemMeshAtlas;

	public AudioSource CountdownSFX;

	private EDisplayMode currentDisplayMode;

	private bool isValid;

	private AudioSource? goPreviewModeSFX;

	private AudioSource? goAttractModeSFX;

	private AudioSource? goPurchaseModeSFX;

	private ParticleSystem? goAttractModeVFX;

	private ParticleSystem? goPurchaseModeVFX;

	private IEnumerator coroutinePreviewTimer;

	private IEnumerator coroutineAttractTimer;

	private DateTime startTime;

	private TextMeshPro clockTextMesh;

	private bool clockTextMeshIsValid;

	private StoreUpdateEvent currentUpdateEvent;

	private string defaultCountdownTextTemplate = "";

	public CosmeticStand cosmeticStand;

	public string itemID = "";

	public string oldItemID = "";

	private Coroutine countdownTimerCoRoutine;

	private float updateClock = 60f;

	private float lastUpdated;

	private void Awake()
	{
		JonsAwakeCode();
	}

	private void JonsAwakeCode()
	{
		lastUpdated = 0f - updateClock;
		isValid = (bool)goPedestal && (bool)goMannequin && (bool)goCosmeticItem && (bool)goCosmeticItemNameplate && (bool)goClock && (bool)goPreviewMode && (bool)goAttractMode && (bool)goPurchaseMode;
		goPreviewModeSFX = goPreviewMode.transform.GetComponentInChildren<AudioSource>();
		goAttractModeSFX = goAttractMode.transform.FindChildRecursive("SFXAttractMode").GetComponent<AudioSource>();
		goPurchaseModeSFX = goPurchaseMode.transform.FindChildRecursive("SFXPurchaseMode").GetComponent<AudioSource>();
		goAttractModeVFX = goAttractMode.transform.FindChildRecursive("VFXAttractMode").GetComponent<ParticleSystem>();
		goPurchaseModeVFX = goPurchaseMode.transform.FindChildRecursive("VFXPurchaseMode").GetComponent<ParticleSystem>();
		clockTextMesh = goClock.GetComponent<TextMeshPro>();
		clockTextMeshIsValid = clockTextMesh != null;
		if (clockTextMeshIsValid)
		{
			defaultCountdownTextTemplate = clockTextMesh.text;
		}
		isValid = (bool)goPreviewModeSFX && (bool)goAttractModeSFX && (bool)goPurchaseModeSFX;
	}

	private void OnDisable()
	{
		if (StoreUpdater.instance != null)
		{
			countdownTimerCoRoutine = null;
			StopCountdownCoroutine();
			StoreUpdater.instance.PedestalAsleep(this);
		}
	}

	private void OnEnable()
	{
		if (goPreviewModeSFX == null)
		{
			goPreviewModeSFX = goPreviewMode.transform.GetComponentInChildren<AudioSource>();
		}
		if (goAttractModeSFX == null)
		{
			goAttractModeSFX = goAttractMode.transform.transform.GetComponentInChildren<AudioSource>();
		}
		if (goPurchaseModeSFX == null)
		{
			goPurchaseModeSFX = goPurchaseMode.transform.transform.GetComponentInChildren<AudioSource>();
		}
		isValid = (bool)goPreviewModeSFX && (bool)goAttractModeSFX && (bool)goPurchaseModeSFX;
		if (StoreUpdater.instance != null)
		{
			StoreUpdater.instance.PedestalAwakened(this);
		}
	}

	public void SwitchDisplayMode(EDisplayMode NewDisplayMode)
	{
		if (isValid && !NewDisplayMode.Equals(EDisplayMode.NULL) && NewDisplayMode != currentDisplayMode)
		{
			switch (NewDisplayMode)
			{
			case EDisplayMode.HIDDEN:
				goPedestal.SetActive(value: false);
				goMannequin.SetActive(value: false);
				goCosmeticItem.SetActive(value: false);
				goCosmeticItemNameplate.SetActive(value: false);
				goClock.SetActive(value: false);
				goPreviewMode.SetActive(value: false);
				goPreviewModeSFX?.GTStop();
				goAttractMode.SetActive(value: false);
				goAttractModeSFX?.GTStop();
				goPurchaseMode.SetActive(value: false);
				goPurchaseModeSFX?.GTStop();
				StopPreviewTimer();
				StopAttractTimer();
				break;
			case EDisplayMode.PREVIEW:
				goPedestal.SetActive(value: true);
				goMannequin.SetActive(value: true);
				goCosmeticItem.SetActive(value: true);
				goCosmeticItemNameplate.SetActive(value: false);
				goClock.SetActive(value: true);
				goAttractMode.SetActive(value: false);
				goAttractModeSFX.GTStop();
				goPurchaseMode.SetActive(value: false);
				goPurchaseModeSFX.GTStop();
				goPreviewMode.SetActive(value: true);
				goPreviewModeSFX.GTPlay();
				StopPreviewTimer();
				StartPreviewTimer();
				break;
			case EDisplayMode.ATTRACT:
				goPedestal.SetActive(value: true);
				goMannequin.SetActive(value: true);
				goCosmeticItem.SetActive(value: true);
				goCosmeticItemNameplate.SetActive(value: true);
				goClock.SetActive(value: true);
				goPreviewMode.SetActive(value: false);
				goPreviewModeSFX.GTStop();
				goPurchaseMode.SetActive(value: false);
				goPurchaseModeSFX.GTStop();
				goAttractMode.SetActive(value: true);
				goAttractModeSFX.GTPlay();
				StopPreviewTimer();
				StartAttractTimer();
				break;
			case EDisplayMode.PURCHASE:
				goPedestal.SetActive(value: true);
				goMannequin.SetActive(value: true);
				goCosmeticItem.SetActive(value: true);
				goCosmeticItemNameplate.SetActive(value: true);
				goClock.SetActive(value: false);
				goPreviewMode.SetActive(value: false);
				goPreviewModeSFX.GTStop();
				goAttractMode.SetActive(value: false);
				goAttractModeSFX.GTStop();
				goPurchaseMode.SetActive(value: true);
				goPurchaseModeSFX.GTPlay();
				goCosmeticItemNameplate.GetComponent<TextMesh>().text = "Purchased!";
				StopPreviewTimer();
				break;
			case EDisplayMode.POSTPURCHASE:
				goPedestal.SetActive(value: true);
				goMannequin.SetActive(value: true);
				goCosmeticItem.SetActive(value: true);
				goCosmeticItemNameplate.SetActive(value: false);
				goClock.SetActive(value: false);
				goPreviewMode.SetActive(value: false);
				goPreviewModeSFX.GTStop();
				goAttractMode.SetActive(value: false);
				goAttractModeSFX.GTStop();
				goPurchaseMode.SetActive(value: false);
				goPurchaseModeSFX.GTStop();
				StopPreviewTimer();
				break;
			}
			currentDisplayMode = NewDisplayMode;
		}
	}

	private void Update()
	{
		if (Time.time > lastUpdated + updateClock)
		{
			lastUpdated = Time.time;
			UpdateClock();
		}
	}

	private void UpdateClock()
	{
		if (currentUpdateEvent != null && clockTextMeshIsValid && clockTextMesh.isActiveAndEnabled)
		{
			TimeSpan ts = currentUpdateEvent.EndTimeUTC.ToUniversalTime() - StoreUpdater.instance.DateTimeNowServerAdjusted;
			clockTextMesh.text = CountdownText.GetTimeDisplay(ts, defaultCountdownTextTemplate);
		}
	}

	public void SetDefaultProperties()
	{
		if (isValid)
		{
			goPedestal.GetComponent<MeshFilter>().sharedMesh = defaultPedestalMesh;
			goPedestal.GetComponent<MeshRenderer>().sharedMaterial = defaultPedestalMaterial;
			goMannequin.GetComponent<MeshFilter>().sharedMesh = defaultMannequinMesh;
			goMannequin.GetComponent<MeshRenderer>().sharedMaterial = defaultMannequinMaterial;
			goCosmeticItem.GetComponent<MeshFilter>().sharedMesh = defaultCosmeticMesh;
			goCosmeticItem.GetComponent<MeshRenderer>().sharedMaterial = defaultCosmeticMaterial;
			goCosmeticItemNameplate.GetComponent<TextMesh>().text = defaultItemText;
			goPreviewModeSFX.clip = defaultSFXPreviewMode;
			goAttractModeSFX.clip = defaultSFXAttractMode;
			goPurchaseModeSFX.clip = defaultSFXPurchaseMode;
		}
	}

	private void ClearCosmeticMesh()
	{
		UnityEngine.Object.Destroy(goCosmeticItemGameObject);
	}

	private void ClearCosmeticAtlas()
	{
		if (goCosmeticItemMeshAtlas.IsNotNull())
		{
			UnityEngine.Object.Destroy(goCosmeticItemMeshAtlas);
		}
	}

	public void SetCosmeticItemFromCosmeticController(CosmeticsController.CosmeticItem item)
	{
		if (isValid)
		{
			ClearCosmeticAtlas();
			ClearCosmeticMesh();
			oldItemID = itemID;
			itemID = item.itemName;
			itemName = item.displayName;
			if (item.overrideDisplayName != string.Empty)
			{
				itemName = item.overrideDisplayName;
			}
			HeadModel.SetCosmeticActive(itemID);
			SetCosmeticStand();
		}
	}

	public void SetCosmeticStand()
	{
		cosmeticStand.thisCosmeticName = itemID;
		cosmeticStand.InitializeCosmetic();
		if (oldItemID.Length > 0)
		{
			if (oldItemID != itemID)
			{
				cosmeticStand.isOn = false;
			}
			cosmeticStand.UpdateColor();
		}
	}

	public void SetStoreUpdateEvent(StoreUpdateEvent storeUpdateEvent, bool playFX)
	{
		if (isValid && AffectedByStoreUpdateEvents)
		{
			if (playFX)
			{
				goAttractMode.SetActive(value: true);
				goAttractModeVFX.Play();
			}
			currentUpdateEvent = storeUpdateEvent;
			SetCosmeticItemFromCosmeticController(CosmeticsController.instance.GetItemFromDict(storeUpdateEvent.ItemName));
			if (base.isActiveAndEnabled)
			{
				countdownTimerCoRoutine = StartCoroutine(PlayCountdownTimer());
			}
			UpdateClock();
		}
	}

	private IEnumerator PlayCountdownTimer()
	{
		yield return new WaitForSeconds(Mathf.Clamp((float)((currentUpdateEvent.EndTimeUTC.ToUniversalTime() - StoreUpdater.instance.DateTimeNowServerAdjusted).TotalSeconds - 10.0), 0f, float.MaxValue));
		PlaySFX();
	}

	public void StopCountdownCoroutine()
	{
		CountdownSFX.GTStop();
		goAttractModeVFX.Stop();
		if (countdownTimerCoRoutine != null)
		{
			StopCoroutine(countdownTimerCoRoutine);
			countdownTimerCoRoutine = null;
		}
	}

	private void PlaySFX()
	{
		if (currentUpdateEvent != null)
		{
			TimeSpan timeSpan = currentUpdateEvent.EndTimeUTC.ToUniversalTime() - StoreUpdater.instance.DateTimeNowServerAdjusted;
			if (timeSpan.TotalSeconds >= 10.0)
			{
				CountdownSFX.time = 0f;
				CountdownSFX.GTPlay();
			}
			else
			{
				CountdownSFX.time = 10f - (float)timeSpan.TotalSeconds;
				CountdownSFX.GTPlay();
			}
		}
	}

	public void SetCosmeticItemProperties(string WhichGUID, string Name, List<Transform> SocketsList, int Socket, string PedestalMesh = null, string MannequinMesh = null)
	{
		if (isValid && Guid.TryParse(WhichGUID, out var _))
		{
			itemName = Name;
			itemSocket = Socket;
			if (pedestalMesh != null)
			{
				goPedestal.GetComponent<MeshFilter>().sharedMesh = pedestalMesh;
			}
		}
	}

	private void StartPreviewTimer()
	{
		if (isValid)
		{
			if (coroutinePreviewTimer != null)
			{
				StopCoroutine(coroutinePreviewTimer);
				coroutinePreviewTimer = null;
			}
			coroutinePreviewTimer = DoPreviewTimer(DateTime.UtcNow + TimeSpan.FromSeconds((hoursInPreviewMode ?? defaultHoursInPreviewMode) * 60 * 60));
			StartCoroutine(coroutinePreviewTimer);
		}
	}

	private void StopPreviewTimer()
	{
		if (isValid)
		{
			if (coroutinePreviewTimer != null)
			{
				StopCoroutine(coroutinePreviewTimer);
				coroutinePreviewTimer = null;
			}
			clockTextMesh.text = "Clock";
		}
	}

	private IEnumerator DoPreviewTimer(DateTime ReleaseTime)
	{
		if (!isValid)
		{
			yield break;
		}
		bool timerDone = false;
		TimeSpan remainingTime = ReleaseTime - DateTime.UtcNow;
		while (!timerDone)
		{
			int delayTime;
			string text;
			if (remainingTime.TotalSeconds <= 59.0)
			{
				text = remainingTime.Seconds + "s";
				delayTime = 1;
			}
			else
			{
				delayTime = 60;
				text = string.Empty;
				if (remainingTime.Days > 0)
				{
					text = text + remainingTime.Days + "d ";
				}
				if (remainingTime.Hours > 0)
				{
					text = text + remainingTime.Hours + "h ";
				}
				if (remainingTime.Minutes > 0)
				{
					text = text + remainingTime.Minutes + "m ";
				}
				text = text.TrimEnd();
			}
			clockTextMesh.text = text;
			yield return new WaitForSecondsRealtime(delayTime);
			remainingTime = remainingTime.Subtract(TimeSpan.FromSeconds(delayTime));
			if (remainingTime.TotalSeconds <= 0.0)
			{
				timerDone = true;
			}
		}
		SwitchDisplayMode(EDisplayMode.ATTRACT);
		yield return null;
	}

	public void StartAttractTimer()
	{
		if (isValid)
		{
			if (coroutineAttractTimer != null)
			{
				StopCoroutine(coroutineAttractTimer);
				coroutineAttractTimer = null;
			}
			coroutineAttractTimer = DoAttractTimer(DateTime.UtcNow + TimeSpan.FromSeconds((hoursInAttractMode ?? defaultHoursInAttractMode) * 60 * 60));
			StartCoroutine(coroutineAttractTimer);
		}
	}

	private void StopAttractTimer()
	{
		if (isValid)
		{
			if (coroutineAttractTimer != null)
			{
				StopCoroutine(coroutineAttractTimer);
				coroutineAttractTimer = null;
			}
			goClock.GetComponent<TextMesh>().text = "Clock";
		}
	}

	private IEnumerator DoAttractTimer(DateTime ReleaseTime)
	{
		if (!isValid)
		{
			yield break;
		}
		bool timerDone = false;
		TimeSpan remainingTime = ReleaseTime - DateTime.UtcNow;
		while (!timerDone)
		{
			int delayTime;
			string text;
			if (remainingTime.TotalSeconds <= 59.0)
			{
				text = remainingTime.Seconds + "s";
				delayTime = 1;
			}
			else
			{
				delayTime = 60;
				text = string.Empty;
				if (remainingTime.Days > 0)
				{
					text = text + remainingTime.Days + "d ";
				}
				if (remainingTime.Hours > 0)
				{
					text = text + remainingTime.Hours + "h ";
				}
				if (remainingTime.Minutes > 0)
				{
					text = text + remainingTime.Minutes + "m ";
				}
				text = text.TrimEnd();
			}
			goClock.GetComponent<TextMesh>().text = text;
			yield return new WaitForSecondsRealtime(delayTime);
			remainingTime = remainingTime.Subtract(TimeSpan.FromSeconds(delayTime));
			if (remainingTime.TotalSeconds <= 0.0)
			{
				timerDone = true;
			}
		}
		SwitchDisplayMode(EDisplayMode.HIDDEN);
		yield return null;
	}
}
