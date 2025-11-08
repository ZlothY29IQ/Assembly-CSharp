using System.Collections.Generic;
using System.Text;
using GorillaGameModes;
using GorillaNetworking;
using UnityEngine;
using UnityEngine.UI;

public class GameModePages : BasePageHandler
{
	private int currentButtonIndex;

	[SerializeField]
	private Text gameModeText;

	[SerializeField]
	private GameModeSelectButton[] buttons;

	private bool initialized;

	private static int sharedSelectedIndex = 0;

	private static StringBuilder textBuilder = new StringBuilder(50);

	[OnEnterPlay_Clear]
	private static List<GameModePages> gameModeSelectorInstances = new List<GameModePages>(7);

	protected override int pageSize => buttons.Length;

	protected override int entriesCount => GameMode.gameModeNames.Count;

	private void Awake()
	{
		gameModeSelectorInstances.Add(this);
		buttons = GetComponentsInChildren<GameModeSelectButton>();
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i].buttonIndex = i;
			buttons[i].selector = this;
		}
	}

	protected override void Start()
	{
		base.Start();
		SelectEntryFromIndex(sharedSelectedIndex);
		initialized = true;
	}

	private void OnEnable()
	{
		if (initialized)
		{
			SelectEntryFromIndex(sharedSelectedIndex);
		}
	}

	private void OnDestroy()
	{
		gameModeSelectorInstances.Remove(this);
	}

	protected override void ShowPage(int selectedPage, int startIndex, int endIndex)
	{
		textBuilder.Clear();
		for (int i = startIndex; i < endIndex; i++)
		{
			textBuilder.AppendLine(GameMode.gameModeNames[i]);
		}
		gameModeText.text = textBuilder.ToString();
		if (base.selectedIndex >= startIndex && base.selectedIndex <= endIndex)
		{
			UpdateAllButtons(currentButtonIndex);
		}
		else
		{
			UpdateAllButtons(-1);
		}
		int buttonsMissing = ((selectedPage == base.pages - 1 && base.maxEntires > endIndex) ? (base.maxEntires - endIndex) : 0);
		EnableEntryButtons(buttonsMissing);
	}

	protected override void PageEntrySelected(int pageEntry, int selectionIndex)
	{
		if (selectionIndex < entriesCount)
		{
			sharedSelectedIndex = selectionIndex;
			UpdateAllButtons(pageEntry);
			currentButtonIndex = pageEntry;
			GorillaComputer.instance.OnModeSelectButtonPress(GameMode.gameModeNames[selectionIndex], leftHand: false);
		}
	}

	private void UpdateAllButtons(int onButton)
	{
		for (int i = 0; i < buttons.Length; i++)
		{
			if (i == onButton)
			{
				buttons[onButton].isOn = true;
				buttons[onButton].UpdateColor();
			}
			else if (buttons[i].isOn)
			{
				buttons[i].isOn = false;
				buttons[i].UpdateColor();
			}
		}
	}

	private void EnableEntryButtons(int buttonsMissing)
	{
		int num = buttons.Length - buttonsMissing;
		int i;
		for (i = 0; i < num; i++)
		{
			buttons[i].gameObject.SetActive(value: true);
		}
		for (; i < buttons.Length; i++)
		{
			buttons[i].gameObject.SetActive(value: false);
		}
	}

	public static void SetSelectedGameModeShared(string gameMode)
	{
		sharedSelectedIndex = GameMode.gameModeNames.IndexOf(gameMode);
		if (sharedSelectedIndex >= 0)
		{
			for (int i = 0; i < gameModeSelectorInstances.Count; i++)
			{
				gameModeSelectorInstances[i].SelectEntryFromIndex(sharedSelectedIndex);
			}
		}
	}
}
