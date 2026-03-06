using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GorillaGameModes;
using GorillaNetworking;
using UnityEngine;
using UnityEngine.Pool;

public class GameModeSelectorButtonLayout : MonoBehaviour
{
	private const string preLog = "[GT/GameModeSelectorButtonLayout]  ";

	private const string preErr = "ERROR!!!  ";

	[SerializeField]
	protected GorillaPressableButton superToggleButton;

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
		if (superToggleButton != null)
		{
			superToggleButton.onPressed += _OnPressedSuperToggleButton;
		}
	}

	private void OnDisable()
	{
		NetworkSystem.Instance.OnJoinedRoomEvent -= new Action(SetupButtons);
		if (superToggleButton != null)
		{
			superToggleButton.onPressed -= _OnPressedSuperToggleButton;
		}
	}

	protected virtual async void SetupButtons()
	{
		int count = 0;
		while (GorillaComputer.instance == null)
		{
			await Task.Delay(100);
		}
		bool flag = GorillaTagger.Instance.offlineVRRig.zoneEntity.currentZone != zone;
		HashSet<GameModeType> modesForZone = GameMode.GameModeZoneMapping.GetModesForZone(zone, NetworkSystem.Instance.SessionIsPrivate);
		HashSet<GameModeType> value;
		using (CollectionPool<HashSet<GameModeType>, GameModeType>.Get(out value))
		{
			bool flag2 = modesForZone.Contains(GameModeType.SuperCasual) || modesForZone.Contains(GameModeType.SuperInfect);
			superToggleButton.transform.parent.gameObject.SetActive(flag2);
			superToggleButton.isOn = flag2 && PlayerPrefFlags.Check(PlayerPrefFlags.Flag.GAME_MODE_SELECTOR_IS_SUPER);
			superToggleButton.UpdateColor();
			if (superToggleButton.isOn)
			{
				foreach (GameModeType item in modesForZone)
				{
					if (item != GameModeType.Casual && item != GameModeType.Infection)
					{
						value.Add(item);
					}
				}
			}
			else
			{
				foreach (GameModeType item2 in modesForZone)
				{
					if (item2 != GameModeType.SuperCasual && item2 != GameModeType.SuperInfect)
					{
						value.Add(item2);
					}
				}
			}
			foreach (GameModeType item3 in value)
			{
				if (count == currentButtons.Count)
				{
					currentButtons.Add(UnityEngine.Object.Instantiate(pf_button, base.transform));
				}
				ModeSelectButton modeSelectButton = currentButtons[count];
				modeSelectButton.transform.localPosition = new Vector3((float)count * -0.15f, 0f, 0f);
				modeSelectButton.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
				modeSelectButton.WarningScreen = warningScreen;
				string empty = string.Empty;
				if (NetworkSystem.Instance.SessionIsSubscription)
				{
					GameMode.GameModeZoneMapping.IsBigRoomMode(item3);
				}
				string mode = item3.ToString() + empty;
				modeSelectButton.SetInfo(mode, GameMode.GameModeZoneMapping.GetModeName(item3), GameMode.GameModeZoneMapping.IsNew(item3), GameMode.GameModeZoneMapping.GetCountdown(item3));
				modeSelectButton.gameObject.SetActive(value: true);
				count++;
				flag |= string.Equals(GorillaComputer.instance.currentGameMode.Value, item3.ToString(), StringComparison.CurrentCultureIgnoreCase);
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

	private void _OnPressedSuperToggleButton(GorillaPressableButton btn, bool isLeftHandPress)
	{
		if (GorillaComputer.instance == null)
		{
			Debug.Log("[GT/GameModeSelectorButtonLayout]  Tried pressing SUPER button but `GorillaComputer` is not ready.", this);
			return;
		}
		if (NetworkSystem.Instance == null)
		{
			Debug.Log("[GT/GameModeSelectorButtonLayout]  Tried pressing SUPER button but `NetworkSystem` is not ready.", this);
			return;
		}
		btn.isOn = !btn.isOn;
		PlayerPrefFlags.Set(PlayerPrefFlags.Flag.GAME_MODE_SELECTOR_IS_SUPER, btn.isOn);
		SetupButtons();
		HashSet<GameModeType> modesForZone = GameMode.GameModeZoneMapping.GetModesForZone(zone, NetworkSystem.Instance.SessionIsPrivate);
		GameModeType lastPressedGameModeType = GorillaComputer.instance.lastPressedGameModeType;
		GameModeType gameModeType = (((lastPressedGameModeType == GameModeType.Casual || lastPressedGameModeType == GameModeType.SuperCasual) && modesForZone.Contains(GameModeType.Casual) && modesForZone.Contains(GameModeType.SuperCasual)) ? (btn.isOn ? GameModeType.SuperCasual : GameModeType.Casual) : (((lastPressedGameModeType != GameModeType.Infection && lastPressedGameModeType != GameModeType.SuperInfect) || !modesForZone.Contains(GameModeType.Infection) || !modesForZone.Contains(GameModeType.SuperInfect)) ? lastPressedGameModeType : ((!btn.isOn) ? GameModeType.Infection : GameModeType.SuperInfect)));
		GorillaComputer.instance.OnModeSelectButtonPress(gameModeType.ToString(), isLeftHandPress);
	}
}
