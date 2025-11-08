using System;
using Modio;
using Modio.Errors;
using Modio.Mods;
using Modio.Unity;
using TMPro;
using UnityEngine;

public class CustomMapsModTile : CustomMapsScreenTouchPoint
{
	[SerializeField]
	private TMP_Text ratingsText;

	[SerializeField]
	private TMP_Text mapNameText;

	[SerializeField]
	private GameObject thumsbUp;

	[SerializeField]
	private GameObject highlight;

	private const float LOGO_WIDTH = 320f;

	private const float LOGO_HEIGHT = 180f;

	private Mod currentMod;

	private Sprite defaultLogo;

	private bool isDownloadingThumbnail;

	private bool newDownloadRequest;

	private bool isActive;

	public Mod CurrentMod => currentMod;

	protected override void Awake()
	{
		base.Awake();
		defaultLogo = touchPointRenderer.sprite;
		highlight.SetActive(value: false);
	}

	public void ShowTileText(bool show, bool useMapName)
	{
		if (!show)
		{
			ratingsText.gameObject.SetActive(value: false);
			mapNameText.gameObject.SetActive(value: false);
			thumsbUp.SetActive(value: false);
		}
		else if (useMapName)
		{
			mapNameText.gameObject.SetActive(value: true);
			ratingsText.gameObject.SetActive(value: false);
			thumsbUp.SetActive(value: false);
		}
		else
		{
			ratingsText.gameObject.SetActive(value: true);
			thumsbUp.SetActive(value: true);
			mapNameText.gameObject.SetActive(value: false);
		}
	}

	public void ActivateTile(bool useMapName)
	{
		isActive = true;
		base.gameObject.SetActive(value: true);
		ShowTileText(show: true, useMapName);
		CustomMapsScreenTouchPoint.pressTime = Time.time;
	}

	public void DeactivateTile()
	{
		isActive = false;
		base.gameObject.SetActive(value: false);
		highlight.SetActive(value: false);
		ShowTileText(show: false, useMapName: false);
		ResetLogo();
	}

	public override void PressButtonColourUpdate()
	{
	}

	protected override void OnButtonPressedEvent()
	{
	}

	public async void SetMod(Mod mod, bool useMapName)
	{
		ActivateTile(useMapName);
		touchPointRenderer.sprite = defaultLogo;
		highlight.SetActive(value: false);
		currentMod = mod;
		if (IsCurrentModHidden())
		{
			mapNameText.text = "HIDDEN MAP";
			ratingsText.text = "0%";
			return;
		}
		mapNameText.text = currentMod.Name;
		long num = currentMod.Stats.RatingsNegative + currentMod.Stats.RatingsPositive;
		string text;
		if (num < 1000)
		{
			text = $"({num})";
		}
		else if (num < 1000000)
		{
			num = Mathf.FloorToInt(num / 100);
			text = $"({num / 10}K)";
		}
		else
		{
			num = Mathf.FloorToInt(num / 100);
			text = $"({num / 10000}mil)";
		}
		ratingsText.text = currentMod.Stats.RatingsPercent + "% " + text;
		if (isDownloadingThumbnail)
		{
			newDownloadRequest = true;
			return;
		}
		isDownloadingThumbnail = true;
		Error error = new Error(ErrorCode.NONE);
		Texture2D tex = new Texture2D(320, 180);
		try
		{
			(error, tex) = await mod.Logo.DownloadAsTexture2D(Mod.LogoResolution.X320_Y180);
		}
		catch (Exception arg)
		{
			GTDev.Log($"CustomMapsModTile::DownloadThumbnail error {arg}");
		}
		isDownloadingThumbnail = false;
		if (newDownloadRequest)
		{
			newDownloadRequest = false;
			SetMod(currentMod, useMapName);
		}
		else if ((bool)error)
		{
			GTDev.LogError($"CustomMapsListScreen::DownloadThumbnail {error}");
		}
		else
		{
			touchPointRenderer.sprite = Sprite.Create(tex, new Rect(0f, 0f, 320f, 180f), new Vector2(0.5f, 0.5f));
		}
	}

	public void ResetLogo()
	{
		touchPointRenderer.sprite = defaultLogo;
	}

	public void ShowDetails()
	{
		CustomMapsTerminal.ShowDetailsScreen(currentMod);
	}

	public void HighlightTile()
	{
		highlight.SetActive(value: true);
	}

	public bool IsCurrentModHidden()
	{
		if (!(currentMod.Creator == null))
		{
			if (!ModIOManager.IsLoggedIn())
			{
				return currentMod.IsHidden();
			}
			return false;
		}
		return true;
	}
}
