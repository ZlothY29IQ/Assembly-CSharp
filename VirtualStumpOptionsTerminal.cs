using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GorillaExtensions;
using GorillaTagScripts.VirtualStumpCustomMaps.UI;
using Modio;
using Modio.Customizations;
using Modio.Users;
using TMPro;
using UnityEngine;

public class VirtualStumpOptionsTerminal : MonoBehaviour, IWssAuthPrompter
{
	private enum ETerminalState
	{
		MODIO_ACCOUNT,
		ROOM_SIZE,
		NUM_STATES
	}

	[SerializeField]
	private TMP_Text optionList;

	[SerializeField]
	private TMP_Text mainScreenText;

	[SerializeField]
	private CustomMapsKeyboard keyboard;

	[SerializeField]
	private List<string> optionStrings = new List<string> { "MOD.IO", "ROOM SIZE" };

	[SerializeField]
	private string loggedInAsString = "LOGGED INTO MOD.IO AS: ";

	[SerializeField]
	private string notLoggedInString = "LOGGED OUT OF MOD.IO";

	[SerializeField]
	private string loginPromptString = "PRESS THE 'PLATFORM LOGIN' OR 'LINK MOD.IO ACCOUNT' BUTTON TO LOGIN";

	[SerializeField]
	private string loggingInString = "LOGGING IN TO MOD.IO...";

	[SerializeField]
	private string loggingOutString = "LOGGING OUT OF MOD.IO...";

	[SerializeField]
	private string linkAccountPromptString = "IF YOU HAVE AN EXISTING MOD.IO ACCOUNT, YOU CAN LINK IT BY PRESSING THE 'LINK MOD.IO ACCOUNT' BUTTON.";

	[SerializeField]
	private string alreadyLinkedAccountString = "YOU'VE ALREADY LINKED YOUR MOD.IO ACCOUNT.";

	[SerializeField]
	private string accountLinkingPromptString = "PLEASE GO TO THIS URL IN YOUR BROWSER AND LOG IN TO YOUR MOD.IO ACCOUNT. ONCE LOGGED IN, ENTER THE FOLLOWING CODE TO PROCEED: ";

	[SerializeField]
	private string urlLabelString = "URL: ";

	[SerializeField]
	private string linkCodeLabelString = "CODE: ";

	[SerializeField]
	private string roomSizeDescriptionString = "THIS SETTING WILL CHANGE THE MAXIMUM AMOUNT OF PLAYERS ALLOWED IN PRIVATE ROOMS YOU CREATE. WHEN JOINING A PUBLIC ROOM, THE MAP YOU'VE LOADED WILL CONTROL THE ROOM SIZE.";

	[SerializeField]
	private string roomSizeLabelString = "MAX PLAYERS: ";

	[SerializeField]
	private GameObject OKButton;

	[SerializeField]
	private List<GameObject> contextualButtons = new List<GameObject>();

	[SerializeField]
	private List<GameObject> buttonsToShow_MODIO = new List<GameObject>();

	[SerializeField]
	private List<GameObject> buttonsToShow_ROOMSIZE = new List<GameObject>();

	private bool processingAccountLink;

	private string cachedLinkURL = "";

	private string cachedLinkCode = "";

	private string cachedError;

	private ETerminalState currentState;

	public void Start()
	{
		optionList.gameObject.SetActive(value: true);
		mainScreenText.gameObject.SetActive(value: true);
		RefreshButtonState();
		UpdateOptionListForCurrentState();
		UpdateScreen();
		keyboard?.OnKeyPressed.AddListener(OnKeyPressed);
		ModIOManager.OnModIOLoggedIn.AddListener(OnModIOLoggedIn);
		ModIOManager.OnModIOLoginStarted.AddListener(OnModIOLoginStarted);
		ModIOManager.OnModIOLoginFailed.AddListener(OnModIOLoginFailed);
		ModIOManager.OnModIOUserChanged.AddListener(OnModIOUserChanged);
	}

	public void OnDestroy()
	{
		keyboard?.OnKeyPressed.RemoveListener(OnKeyPressed);
		ModIOManager.OnModIOLoggedIn.RemoveListener(OnModIOLoggedIn);
		ModIOManager.OnModIOLoggedOut.RemoveListener(OnModIOLoggedOut);
		ModIOManager.OnModIOLoginStarted.RemoveListener(OnModIOLoginStarted);
		ModIOManager.OnModIOLoginFailed.RemoveListener(OnModIOLoginFailed);
		ModIOManager.OnModIOUserChanged.RemoveListener(OnModIOUserChanged);
	}

	public void OnEnable()
	{
		RefreshButtonState();
		UpdateOptionListForCurrentState();
		UpdateScreen();
	}

	private void OnKeyPressed(CustomMapKeyboardBinding pressedButton)
	{
		if (!cachedError.IsNullOrEmpty())
		{
			cachedError = null;
			RefreshButtonState();
			UpdateScreen();
			return;
		}
		switch (pressedButton)
		{
		case CustomMapKeyboardBinding.up:
		{
			int num2 = (int)(currentState - 1);
			if (num2 < 0)
			{
				num2 = 1;
			}
			ChangeState((ETerminalState)num2);
			UpdateOptionListForCurrentState();
			UpdateScreen();
			break;
		}
		case CustomMapKeyboardBinding.down:
		{
			int num = (int)(currentState + 1);
			if (num >= 2)
			{
				num = 0;
			}
			ChangeState((ETerminalState)num);
			UpdateOptionListForCurrentState();
			UpdateScreen();
			break;
		}
		default:
			switch (currentState)
			{
			case ETerminalState.MODIO_ACCOUNT:
				OnKeyPressed_ModIOAccount(pressedButton);
				break;
			case ETerminalState.ROOM_SIZE:
				OnKeyPressed_RoomSize(pressedButton);
				break;
			}
			break;
		}
	}

	private void ChangeState(ETerminalState newState)
	{
		if (newState != currentState)
		{
			currentState = newState;
			RefreshButtonState();
		}
	}

	private void RefreshButtonState()
	{
		for (int i = 0; i < contextualButtons.Count; i++)
		{
			if (contextualButtons[i].IsNotNull())
			{
				contextualButtons[i].SetActive(value: false);
			}
		}
		if (!cachedError.IsNullOrEmpty())
		{
			OKButton.SetActive(value: true);
			return;
		}
		switch (currentState)
		{
		case ETerminalState.MODIO_ACCOUNT:
		{
			for (int k = 0; k < buttonsToShow_MODIO.Count; k++)
			{
				if (buttonsToShow_MODIO[k].IsNotNull())
				{
					buttonsToShow_MODIO[k].SetActive(value: true);
				}
			}
			break;
		}
		case ETerminalState.ROOM_SIZE:
		{
			for (int j = 0; j < buttonsToShow_ROOMSIZE.Count; j++)
			{
				if (buttonsToShow_ROOMSIZE[j].IsNotNull())
				{
					buttonsToShow_ROOMSIZE[j].SetActive(value: true);
				}
			}
			break;
		}
		}
	}

	private void UpdateOptionListForCurrentState()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < 2; i++)
		{
			stringBuilder.Append(optionStrings[i]);
			if (i == (int)currentState)
			{
				stringBuilder.Append(" <-");
			}
			stringBuilder.Append("\n");
		}
		optionList.text = stringBuilder.ToString();
	}

	private void UpdateScreen()
	{
		mainScreenText.text = "";
		if (!cachedError.IsNullOrEmpty())
		{
			RefreshButtonState();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(cachedError);
			mainScreenText.text = "<color=\"red\">" + stringBuilder;
			return;
		}
		switch (currentState)
		{
		case ETerminalState.MODIO_ACCOUNT:
			mainScreenText.text = UpdateScreen_ModIOAccount();
			break;
		case ETerminalState.ROOM_SIZE:
			mainScreenText.text = UpdateScreen_RoomSize();
			break;
		}
	}

	private void OnModIOLoginStarted()
	{
		UpdateScreen();
	}

	private void OnModIOLoggedIn()
	{
		ModIOManager.OnModIOLoggedOut.RemoveListener(OnModIOLoggedOut);
		ModIOManager.OnModIOLoggedOut.AddListener(OnModIOLoggedOut);
		processingAccountLink = false;
		UpdateScreen();
		StartCoroutine(ModIOManager.AssociateMothershipAndModIOAccounts(new AssociateMotherhsipAndModIOAccountsRequest
		{
			ModIOId = ModIOManager.GetCurrentUserId(),
			ModIOToken = ModIOManager.GetCurrentAuthToken(),
			MothershipEnvId = MothershipClientApiUnity.EnvironmentId,
			MothershipPlayerId = MothershipClientContext.MothershipId,
			MothershipToken = MothershipClientContext.Token
		}, delegate
		{
		}));
	}

	private void OnModIOLoggedOut()
	{
		ModIOManager.OnModIOLoggedOut.RemoveListener(OnModIOLoggedOut);
		processingAccountLink = false;
		UpdateScreen();
	}

	private void OnModIOLoginFailed(string error)
	{
		processingAccountLink = false;
		cachedError = error;
		UpdateScreen();
	}

	private void OnModIOUserChanged(User user)
	{
		UpdateScreen();
	}

	private void OnKeyPressed_ModIOAccount(CustomMapKeyboardBinding pressedButton)
	{
		if (pressedButton == CustomMapKeyboardBinding.option1)
		{
			StartAccountLinkingProcess();
		}
		if (pressedButton == CustomMapKeyboardBinding.option2)
		{
			GTDev.Log($"[VirtualStumpOptionsTerminal::OnKeyPressed_ModIOAccount] logout {ModIOManager.IsLoggedIn()}");
			if (ModIOManager.IsLoggedIn())
			{
				ModIOManager.LogoutFromModIO();
			}
		}
		if (pressedButton != CustomMapKeyboardBinding.option3)
		{
			return;
		}
		GTDev.Log($"[VirtualStumpOptionsTerminal::OnKeyPressed_ModIOAccount] login {ModIOManager.IsLoggedIn()}");
		if (!ModIOManager.IsLoggedIn())
		{
			ModIOManager.CancelExternalAuthentication();
			try
			{
				ModIOManager.RequestPlatformLogin();
			}
			catch (Exception arg)
			{
				GTDev.Log($"VirtualStumpOptionsTerminal::OnKeyPressed_ModIOAccount platform login error: {arg}");
				throw;
			}
		}
	}

	private async Task StartAccountLinkingProcess()
	{
		if (!processingAccountLink)
		{
			processingAccountLink = true;
			if (ModIOManager.IsAuthenticated())
			{
				if (ModIOManager.GetLastAuthMethod() == ModIOManager.ModIOAuthMethod.LinkedAccount)
				{
					ModIOManager.OnModIOLoggedIn.RemoveListener(OnModIOLoggedIn);
					ModIOManager.OnModIOLoggedIn.AddListener(OnModIOLoggedIn);
					ModIOManager.OnModIOUserChanged.RemoveListener(OnModIOUserChanged);
					ModIOManager.OnModIOUserChanged.AddListener(OnModIOUserChanged);
					processingAccountLink = false;
					return;
				}
				ModIOManager.OnModIOLoggedOut.RemoveListener(OnModIOLoggedOut);
				ModIOManager.LogoutFromModIO();
				ModIOManager.OnModIOLoggedIn.RemoveListener(OnModIOLoggedIn);
				ModIOManager.OnModIOLoggedIn.AddListener(OnModIOLoggedIn);
				ModIOManager.OnModIOUserChanged.RemoveListener(OnModIOUserChanged);
				ModIOManager.OnModIOUserChanged.AddListener(OnModIOUserChanged);
			}
			ModIOManager.SetAccountLinkPrompter(this);
			Error error = await ModIOManager.RequestAccountLinkCode();
			if ((bool)error)
			{
				Debug.LogError("[ModIOAccountLinkingTerminal::StartAccountLinkingProcess] Failed to log in to mod.io: " + error.GetMessage());
				cachedError = error.GetMessage() + "\n\nPRESS THE 'LINK MOD.IO ACCOUNT' BUTTON TO RETRY.";
				processingAccountLink = false;
				UpdateScreen();
			}
		}
		else
		{
			UpdateScreen();
		}
	}

	public void ShowPrompt(string url, string code)
	{
		cachedLinkURL = url;
		cachedLinkCode = code;
		UpdateScreen();
	}

	private string UpdateScreen_ModIOAccount()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (ModIOManager.IsLoggedIn())
		{
			stringBuilder.Append(loggedInAsString + "\n");
			stringBuilder.Append("   " + ModIOManager.GetCurrentUsername() + "\n\n");
			if (ModIOManager.GetLastAuthMethod() != ModIOManager.ModIOAuthMethod.LinkedAccount)
			{
				stringBuilder.Append(linkAccountPromptString + "\n");
			}
			else
			{
				stringBuilder.Append(alreadyLinkedAccountString + "\n");
			}
		}
		else if (ModIOManager.IsLoggingIn() && !processingAccountLink)
		{
			stringBuilder.Append(loggingInString);
		}
		else if (ModIOManager.IsLoggingOut())
		{
			stringBuilder.Append(loggingOutString);
		}
		else if (processingAccountLink)
		{
			stringBuilder.Append(linkAccountPromptString + "\n\n");
			stringBuilder.Append(urlLabelString + cachedLinkURL + "\n");
			stringBuilder.Append(linkCodeLabelString + cachedLinkCode + "\n");
		}
		else
		{
			stringBuilder.Append(notLoggedInString + "\n\n");
			stringBuilder.Append(loginPromptString);
		}
		return stringBuilder.ToString();
	}

	private void OnKeyPressed_RoomSize(CustomMapKeyboardBinding pressedButton)
	{
		if (pressedButton == CustomMapKeyboardBinding.left)
		{
			DecrementRoomSize();
		}
		if (pressedButton == CustomMapKeyboardBinding.right)
		{
			IncrementRoomSize();
		}
		UpdateScreen();
	}

	private void DecrementRoomSize()
	{
		RoomSystem.OverrideRoomSize((byte)(RoomSystem.GetOverridenRoomSize() - 1));
		UpdateScreen();
	}

	private void IncrementRoomSize()
	{
		RoomSystem.OverrideRoomSize((byte)(RoomSystem.GetOverridenRoomSize() + 1));
		UpdateScreen();
	}

	private string UpdateScreen_RoomSize()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(roomSizeDescriptionString + "\n\n");
		stringBuilder.Append(roomSizeLabelString + RoomSystem.GetOverridenRoomSize());
		return stringBuilder.ToString();
	}
}
