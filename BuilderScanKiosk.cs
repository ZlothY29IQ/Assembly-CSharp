using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GorillaTagScripts;
using GorillaTagScripts.Builder;
using TMPro;
using UnityEngine;

public class BuilderScanKiosk : MonoBehaviourTick
{
	private enum ScannerState
	{
		IDLE,
		CONFIRMATION,
		SAVING
	}

	private const string MONKE_BLOCKS_SAVE_KIOSK_NO_SAVE_SLOT_KEY = "MONKE_BLOCKS_SAVE_KIOSK_NO_SAVE_SLOT";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SCAN_LABEL_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SCAN_LABEL";

	private const string MONKE_BLOCKS_SAVE_KIOSK_UPDATE_LABEL_KEY = "MONKE_BLOCKS_SAVE_KIOSK_UPDATE_LABEL";

	private const string MONKE_BLOCKS_SAVE_KIOSK_MAP_ID_LABEL_KEY = "MONKE_BLOCKS_SAVE_KIOSK_MAP_ID_LABEL";

	private const string MONKE_BLOCKS_SAVE_KIOSK_CODE_INSTRUCTIONS_KEY = "MONKE_BLOCKS_SAVE_KIOSK_CODE_INSTRUCTIONS";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SAVE_ERROR_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SAVE_ERROR";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SAVE_ERROR_BUSY_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SAVE_ERROR_BUSY";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SAVE_ERROR_BLOCKS_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SAVE_ERROR_BLOCKS";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SAVE_COOLDOWN_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SAVE_COOLDOWN";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SAVE_NO_CHANGES_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SAVE_NO_CHANGES";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SAVE_WARNING_REPLACE_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SAVE_WARNING_REPLACE";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SAVE_WARNING_CONFIRMATION_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SAVE_WARNING_CONFIRMATION";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SAVE_SAVING_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SAVE_SAVING";

	private const string MONKE_BLOCKS_SAVE_KIOSK_LOAD_INSTRUCTIONS_KEY = "MONKE_BLOCKS_SAVE_KIOSK_LOAD_INSTRUCTIONS";

	private const string MONKE_BLOCKS_SAVE_KIOSK_EMPTY_TABLE_KEY = "MONKE_BLOCKS_SAVE_KIOSK_EMPTY_TABLE";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SLOT_NONE_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SLOT_NONE";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SLOT_ONE_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SLOT_ONE";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SLOT_TWO_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SLOT_TWO";

	private const string MONKE_BLOCKS_SAVE_KIOSK_SLOT_THREE_KEY = "MONKE_BLOCKS_SAVE_KIOSK_SLOT_THREE";

	private const string MONKE_BLOCKS_SAVE_KIOSK_UPDATED_BUTTON_KEY = "MONKE_BLOCKS_SAVE_KIOSK_UPDATED_BUTTON";

	private const string MONKE_BLOCKS_SAVE_KIOSK_UPDATE_CONFIRM_BUTTON_KEY = "MONKE_BLOCKS_SAVE_KIOSK_UPDATE_CONFIRM_BUTTON";

	[SerializeField]
	private GorillaPressableButton saveButton;

	[SerializeField]
	private GorillaPressableButton noneButton;

	[SerializeField]
	private List<GorillaPressableButton> scanButtons;

	[SerializeField]
	private BuilderTable targetTable;

	[SerializeField]
	private float saveCooldownSeconds = 5f;

	[SerializeField]
	private TMP_Text screenText;

	[SerializeField]
	private SoundBankPlayer soundBank;

	[SerializeField]
	private Animation scanAnimation;

	private MeshRenderer scanTriangle;

	private bool isAnimating;

	private static string playerPrefKey = "BuilderSaveSlot";

	private static string SAVE_FOLDER = "MonkeBlocks";

	private static string SAVE_FILE = "MyBuild";

	public static int NUM_SAVE_SLOTS = 5;

	public static int DEV_SAVE_SLOT = -2;

	private Texture2D buildCaptureTexture;

	private bool isDirty;

	private bool saveError;

	private string errorMsg = string.Empty;

	private bool coolingDown;

	private double coolDownCompleteTime;

	private double scanCompleteTime;

	private ScannerState scannerState;

	public static bool IsSaveSlotValid(int slot)
	{
		if (slot >= 0)
		{
			return slot < NUM_SAVE_SLOTS;
		}
		return false;
	}

	private void Start()
	{
		if (saveButton != null)
		{
			saveButton.onPressButton.AddListener(OnSavePressed);
		}
		if (targetTable != null)
		{
			targetTable.OnSaveDirtyChanged.AddListener(OnSaveDirtyChanged);
			targetTable.OnSaveSuccess.AddListener(OnSaveSuccess);
			targetTable.OnSaveFailure.AddListener(OnSaveFail);
			SharedBlocksManager.OnSaveTimeUpdated += OnSaveTimeUpdated;
		}
		if (noneButton != null)
		{
			noneButton.onPressButton.AddListener(OnNoneButtonPressed);
		}
		foreach (GorillaPressableButton scanButton in scanButtons)
		{
			scanButton.onPressed += OnScanButtonPressed;
		}
		scanTriangle = scanAnimation.GetComponent<MeshRenderer>();
		scanTriangle.enabled = false;
		scannerState = ScannerState.IDLE;
		LoadPlayerPrefs();
		UpdateUI();
	}

	private new void OnEnable()
	{
		base.OnEnable();
		LocalisationManager.RegisterOnLanguageChanged(UpdateUI);
	}

	private new void OnDisable()
	{
		base.OnDisable();
		LocalisationManager.UnregisterOnLanguageChanged(UpdateUI);
	}

	private void OnDestroy()
	{
		if (saveButton != null)
		{
			saveButton.onPressButton.RemoveListener(OnSavePressed);
		}
		SharedBlocksManager.OnSaveTimeUpdated -= OnSaveTimeUpdated;
		if (targetTable != null)
		{
			targetTable.OnSaveDirtyChanged.RemoveListener(OnSaveDirtyChanged);
			targetTable.OnSaveFailure.RemoveListener(OnSaveFail);
		}
		if (noneButton != null)
		{
			noneButton.onPressButton.RemoveListener(OnNoneButtonPressed);
		}
		foreach (GorillaPressableButton scanButton in scanButtons)
		{
			if (!(scanButton == null))
			{
				scanButton.onPressed -= OnScanButtonPressed;
			}
		}
	}

	private void OnNoneButtonPressed()
	{
		if (!(targetTable == null))
		{
			if (scannerState == ScannerState.CONFIRMATION)
			{
				scannerState = ScannerState.IDLE;
			}
			if (targetTable.CurrentSaveSlot != -1)
			{
				targetTable.CurrentSaveSlot = -1;
				SavePlayerPrefs();
				UpdateUI();
			}
		}
	}

	private void OnScanButtonPressed(GorillaPressableButton button, bool isLeft)
	{
		if (targetTable == null)
		{
			return;
		}
		if (scannerState == ScannerState.CONFIRMATION)
		{
			scannerState = ScannerState.IDLE;
		}
		for (int i = 0; i < scanButtons.Count; i++)
		{
			if (button.Equals(scanButtons[i]))
			{
				if (i != targetTable.CurrentSaveSlot)
				{
					targetTable.CurrentSaveSlot = i;
					SavePlayerPrefs();
					UpdateUI();
				}
				break;
			}
		}
	}

	public void OnDevScanPressed()
	{
	}

	private void LoadPlayerPrefs()
	{
		int currentSaveSlot = PlayerPrefs.GetInt(playerPrefKey, -1);
		targetTable.CurrentSaveSlot = currentSaveSlot;
		UpdateUI();
	}

	private void SavePlayerPrefs()
	{
		PlayerPrefs.SetInt(playerPrefKey, targetTable.CurrentSaveSlot);
		PlayerPrefs.Save();
	}

	private void ToggleSaveButton(bool enabled)
	{
		if (enabled)
		{
			saveButton.enabled = true;
			saveButton.buttonRenderer.material = saveButton.unpressedMaterial;
		}
		else
		{
			saveButton.enabled = false;
			saveButton.buttonRenderer.material = saveButton.pressedMaterial;
		}
	}

	public override void Tick()
	{
		if (isAnimating)
		{
			if (scanAnimation == null)
			{
				isAnimating = false;
			}
			else if ((double)Time.time > scanCompleteTime)
			{
				scanTriangle.enabled = false;
				isAnimating = false;
			}
		}
		if (coolingDown && (double)Time.time > coolDownCompleteTime)
		{
			coolingDown = false;
			UpdateUI();
		}
	}

	private void OnSavePressed()
	{
		if (targetTable == null || !isDirty || coolingDown)
		{
			return;
		}
		switch (scannerState)
		{
		case ScannerState.IDLE:
			scannerState = ScannerState.CONFIRMATION;
			UpdateUI();
			break;
		case ScannerState.CONFIRMATION:
		{
			scannerState = ScannerState.SAVING;
			if (scanAnimation != null)
			{
				scanCompleteTime = Time.time + scanAnimation.clip.length;
				scanTriangle.enabled = true;
				scanAnimation.Rewind();
				scanAnimation.Play();
			}
			if (soundBank != null)
			{
				soundBank.Play();
			}
			isAnimating = true;
			saveError = false;
			errorMsg = string.Empty;
			coolDownCompleteTime = Time.time + saveCooldownSeconds;
			coolingDown = true;
			UpdateUI();
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_SAVE_ERROR_BUSY", out var result, "BUSY");
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_SAVE_ERROR_BLOCKS", out var result2, "PLEASE REMOVE BLOCKS CONNECTED OUTSIDE OF TABLE PLATFORM");
			targetTable.SaveTableForPlayer(result, result2);
			break;
		}
		}
	}

	private string GetSavePath()
	{
		return GetSaveFolder() + Path.DirectorySeparatorChar + SAVE_FILE + "_" + targetTable.CurrentSaveSlot + ".png";
	}

	private string GetSaveFolder()
	{
		return Application.persistentDataPath + Path.DirectorySeparatorChar + SAVE_FOLDER;
	}

	private void OnSaveDirtyChanged(bool dirty)
	{
		isDirty = dirty;
		UpdateUI();
	}

	private void OnSaveTimeUpdated()
	{
		scannerState = ScannerState.IDLE;
		saveError = false;
		UpdateUI();
	}

	private void OnSaveSuccess()
	{
		scannerState = ScannerState.IDLE;
		saveError = false;
		UpdateUI();
	}

	private void OnSaveFail(string errorMsg)
	{
		scannerState = ScannerState.IDLE;
		saveError = true;
		this.errorMsg = errorMsg;
		UpdateUI();
	}

	private void UpdateUI()
	{
		screenText.text = GetTextForScreen();
		ToggleSaveButton(IsSaveSlotValid(targetTable.CurrentSaveSlot) && !coolingDown);
		noneButton.buttonRenderer.material = ((!IsSaveSlotValid(targetTable.CurrentSaveSlot)) ? noneButton.pressedMaterial : noneButton.unpressedMaterial);
		bool flag = SubscriptionManager.IsLocalSubscribed();
		for (int i = 0; i < scanButtons.Count; i++)
		{
			GorillaPressableButton gorillaPressableButton = scanButtons[i];
			if (gorillaPressableButton.isSubscriberOnlyButton && !flag)
			{
				gorillaPressableButton.buttonRenderer.material = ((gorillaPressableButton.nonSubscriberMaterial != null) ? gorillaPressableButton.nonSubscriberMaterial : gorillaPressableButton.unpressedMaterial);
			}
			else
			{
				gorillaPressableButton.buttonRenderer.material = ((targetTable.CurrentSaveSlot == i) ? gorillaPressableButton.pressedMaterial : gorillaPressableButton.unpressedMaterial);
			}
		}
		if (scannerState == ScannerState.CONFIRMATION)
		{
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_UPDATE_CONFIRM_BUTTON", out var result, "YES UPDATE SCAN");
			saveButton.myTmpText.text = result;
		}
		else
		{
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_UPDATED_BUTTON", out var result2, "UPDATE SCAN");
			saveButton.myTmpText.text = result2;
		}
	}

	private string GetTextForScreen()
	{
		if (targetTable == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		string result = "";
		int currentSaveSlot = targetTable.CurrentSaveSlot;
		if (!IsSaveSlotValid(currentSaveSlot))
		{
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_NO_SAVE_SLOT", out result, "<b><color=red>NONE</color></b>");
			stringBuilder.Append(result);
		}
		else if (currentSaveSlot == DEV_SAVE_SLOT)
		{
			stringBuilder.Append("<b><color=red>DEV SCAN</color></b>");
		}
		else
		{
			stringBuilder.Append("<b><color=red>");
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_SCAN_LABEL", out result, "SCAN ");
			stringBuilder.Append(result);
			stringBuilder.Append(currentSaveSlot + 1);
			stringBuilder.Append("</color></b>");
			SharedBlocksManager.LocalPublishInfo publishInfoForSlot = SharedBlocksManager.GetPublishInfoForSlot(currentSaveSlot);
			DateTime dateTime = DateTime.FromBinary(publishInfoForSlot.publishTime);
			if (dateTime > DateTime.MinValue)
			{
				LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_UPDATE_LABEL", out result, "UPDATED ");
				stringBuilder.Append(": ");
				stringBuilder.Append(result);
				stringBuilder.Append(dateTime.ToString());
				stringBuilder.Append("\n");
			}
			if (SharedBlocksManager.IsMapIDValid(publishInfoForSlot.mapID))
			{
				LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_MAP_ID_LABEL", out result, "MAP ID: ");
				stringBuilder.Append(result);
				stringBuilder.Append(publishInfoForSlot.mapID.Substring(0, 4));
				stringBuilder.Append("-");
				stringBuilder.Append(publishInfoForSlot.mapID.Substring(4));
				LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_CODE_INSTRUCTIONS", out result, "\nUSE THIS CODE IN THE SHARE MY BLOCKS ROOM");
				stringBuilder.Append(result);
			}
		}
		stringBuilder.Append("\n");
		switch (scannerState)
		{
		case ScannerState.IDLE:
			if (saveError)
			{
				LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_SAVE_ERROR", out result, "ERROR WHILE SCANNING: ");
				stringBuilder.Append(result);
				stringBuilder.Append(errorMsg);
			}
			else if (coolingDown)
			{
				LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_SAVE_COOLDOWN", out result, "COOLING DOWN...");
				stringBuilder.Append(result);
			}
			else if (!isDirty)
			{
				LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_SAVE_NO_CHANGES", out result, "NO UNSAVED CHANGES");
				stringBuilder.Append(result);
			}
			break;
		case ScannerState.CONFIRMATION:
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_SAVE_WARNING_REPLACE", out result, "YOU ARE ABOUT TO REPLACE ");
			if (currentSaveSlot == DEV_SAVE_SLOT)
			{
				stringBuilder.Append(result);
				stringBuilder.Append("<b><color=red>DEV SCAN</color></b>");
			}
			else
			{
				stringBuilder.Append(result);
				stringBuilder.Append("<b><color=red>");
				LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_SCAN_LABEL", out result, "SCAN ");
				stringBuilder.Append(result);
				stringBuilder.Append(currentSaveSlot + 1);
				stringBuilder.Append("</color></b>");
			}
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_SAVE_WARNING_CONFIRMATION", out result, " ARE YOU SURE YOU WANT TO SCAN?");
			stringBuilder.Append(result);
			break;
		case ScannerState.SAVING:
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_SAVE_SAVING", out result, "SCANNING BUILD...");
			stringBuilder.Append(result);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		stringBuilder.Append("\n\n\n");
		LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_LOAD_INSTRUCTIONS", out result, "CREATE A <b><color=red>NEW</color></b> PRIVATE ROOM TO LOAD ");
		stringBuilder.Append(result);
		if (!IsSaveSlotValid(currentSaveSlot))
		{
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_EMPTY_TABLE", out result, "<b><color=red>AN EMPTY TABLE</color></b>");
			stringBuilder.Append(result);
		}
		else if (currentSaveSlot == DEV_SAVE_SLOT)
		{
			stringBuilder.Append("<b><color=red>DEV SCAN</color></b>");
		}
		else
		{
			LocalisationManager.TryGetKeyForCurrentLocale("MONKE_BLOCKS_SAVE_KIOSK_SCAN_LABEL", out result, "SCAN ");
			stringBuilder.Append("<b><color=red>");
			stringBuilder.Append(result);
			stringBuilder.Append(currentSaveSlot + 1);
			stringBuilder.Append("</color></b>");
		}
		return stringBuilder.ToString();
	}
}
