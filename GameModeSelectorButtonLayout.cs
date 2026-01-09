using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaGameModes;
using GorillaNetworking;
using UnityEngine;

public class GameModeSelectorButtonLayout : MonoBehaviour
{
	[SerializeField]
	protected ModeSelectButton pf_button;

	[SerializeField]
	protected GTZone zone;

	[SerializeField]
	protected PartyGameModeWarning warningScreen;

	protected List<ModeSelectButton> currentButtons = new List<ModeSelectButton>();

	private void OnEnable()
	{
		SetupButtons();
		NetworkSystem.Instance.OnJoinedRoomEvent += new Action(SetupButtons);
	}

	private void OnDisable()
	{
		NetworkSystem.Instance.OnJoinedRoomEvent -= new Action(SetupButtons);
	}

	public virtual async void SetupButtons()
	{
		int count = 0;
		while (GorillaComputer.instance == null)
		{
			await Task.Delay(100);
		}
		bool flag = GorillaTagger.Instance.offlineVRRig.zoneEntity.currentZone != zone;
		foreach (GameModeType item in GameMode.GameModeZoneMapping.GetModesForZone(zone, NetworkSystem.Instance.SessionIsPrivate))
		{
			if (count == currentButtons.Count)
			{
				currentButtons.Add(UnityEngine.Object.Instantiate(pf_button, base.transform));
			}
			ModeSelectButton modeSelectButton = currentButtons[count];
			modeSelectButton.transform.localPosition = new Vector3((float)count * -0.15f, 0f, 0f);
			modeSelectButton.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
			modeSelectButton.WarningScreen = warningScreen;
			modeSelectButton.SetInfo(item.ToString(), GameMode.GameModeZoneMapping.GetModeName(item), GameMode.GameModeZoneMapping.IsNew(item), GameMode.GameModeZoneMapping.GetCountdown(item));
			modeSelectButton.gameObject.SetActive(value: true);
			count++;
			flag |= string.Equals(GorillaComputer.instance.currentGameMode.Value, item.ToString(), StringComparison.CurrentCultureIgnoreCase);
		}
		for (int i = count; i < currentButtons.Count; i++)
		{
			currentButtons[i].gameObject.SetActive(value: false);
		}
		if (!flag)
		{
			GorillaComputer.instance.SetGameModeWithoutButton(currentButtons[0].gameMode);
		}
	}
}
