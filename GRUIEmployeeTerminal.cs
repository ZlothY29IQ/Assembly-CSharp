using System.Collections.Generic;
using GorillaNetworking;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;

public class GRUIEmployeeTerminal : MonoBehaviour
{
	[SerializeField]
	private GorillaPressableButton signupButton;

	[SerializeField]
	private TMP_Text signupButtonText;

	[SerializeField]
	private Transform spawnMarker;

	[SerializeField]
	private GRUIStationEmployeeBadges badgeStation;

	private int entityTypeId;

	private bool isEmployee;

	private bool isSigningUp;

	private const string GR_DATA_KEY = "GRData";

	public void Setup()
	{
		signupButton.onPressButton.AddListener(OnSignup);
		PlayFab.ClientModels.GetUserDataRequest request = new PlayFab.ClientModels.GetUserDataRequest
		{
			PlayFabId = PlayFabAuthenticator.instance.GetPlayFabPlayerId(),
			Keys = new List<string> { "GRData" }
		};
		isSigningUp = true;
		PlayFabClientAPI.GetUserData(request, OnGetUserDataInitialState, OnGetUserDataInitialStateFail);
		Refresh();
	}

	public void OnSignup()
	{
		if (isSigningUp || isEmployee)
		{
			return;
		}
		UpdateUserDataRequest request = new UpdateUserDataRequest
		{
			Data = new Dictionary<string, string> { { "GRData", "Now we have data" } }
		};
		if (!PlayFabClientAPI.IsClientLoggedIn())
		{
			if (PlayFabAuthenticator.instance != null)
			{
				PlayFabAuthenticator.instance.AuthenticateWithPlayFab();
			}
		}
		else
		{
			isSigningUp = true;
			PlayFabClientAPI.UpdateUserData(request, OnSaveTableSuccess, OnSaveTableFailure);
		}
	}

	public Transform GetSpawnMarker()
	{
		return spawnMarker;
	}

	public void Refresh()
	{
		if (isSigningUp)
		{
			signupButtonText.text = "APPLYING";
		}
		else if (isEmployee)
		{
			signupButtonText.text = "HIRED";
		}
		else
		{
			signupButtonText.text = "APPLY";
		}
	}

	private void OnGetUserDataInitialState(GetUserDataResult result)
	{
		if (result.Data.TryGetValue("GRData", out var value))
		{
			_ = value.Value;
			isEmployee = true;
		}
		else
		{
			isEmployee = false;
		}
		isSigningUp = false;
		Refresh();
	}

	private void OnGetUserDataInitialStateFail(PlayFabError error)
	{
		isEmployee = false;
		isSigningUp = false;
		Refresh();
	}

	private void OnSaveTableSuccess(UpdateUserDataResult result)
	{
		isEmployee = true;
		isSigningUp = false;
		Refresh();
	}

	private void OnSaveTableFailure(PlayFabError error)
	{
		isEmployee = false;
		isSigningUp = false;
		Refresh();
	}
}
