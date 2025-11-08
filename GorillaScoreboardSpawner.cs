using System;
using System.Collections;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.UI;

public class GorillaScoreboardSpawner : MonoBehaviour
{
	public string gameType;

	public bool includeMMR;

	public GameObject scoreboardPrefab;

	public GameObject notInRoomText;

	public GameObject controllingParentGameObject;

	public bool isActive = true;

	public GorillaScoreBoard currentScoreboard;

	public bool lastVisible;

	public bool forOverlay;

	public void Awake()
	{
		StartCoroutine(UpdateBoard());
	}

	private void Start()
	{
		RoomSystem.JoinedRoomEvent += new Action(OnJoinedRoom);
		RoomSystem.LeftRoomEvent += new Action(OnLeftRoom);
	}

	public bool IsCurrentScoreboard()
	{
		return base.gameObject.activeInHierarchy;
	}

	public void OnJoinedRoom()
	{
		Debug.Log("SCOREBOARD JOIN ROOM");
		if (IsCurrentScoreboard())
		{
			notInRoomText.SetActive(value: false);
			currentScoreboard = UnityEngine.Object.Instantiate(scoreboardPrefab, base.transform).GetComponent<GorillaScoreBoard>();
			currentScoreboard.transform.rotation = base.transform.rotation;
			if (includeMMR)
			{
				currentScoreboard.GetComponent<GorillaScoreBoard>().includeMMR = true;
				currentScoreboard.GetComponent<Text>().text = "Player                     Color         Level        MMR";
			}
		}
	}

	public bool IsVisible()
	{
		if (!forOverlay)
		{
			return controllingParentGameObject.activeSelf;
		}
		return GTPlayer.Instance.inOverlay;
	}

	private IEnumerator UpdateBoard()
	{
		while (true)
		{
			try
			{
				if (currentScoreboard != null)
				{
					bool flag = IsVisible();
					foreach (GorillaPlayerScoreboardLine line in currentScoreboard.lines)
					{
						if (flag != line.lastVisible)
						{
							line.lastVisible = flag;
						}
					}
					if (currentScoreboard.boardText.enabled != flag)
					{
						currentScoreboard.boardText.enabled = flag;
					}
					if (currentScoreboard.buttonText.enabled != flag)
					{
						currentScoreboard.buttonText.enabled = flag;
					}
				}
			}
			catch
			{
			}
			yield return new WaitForSeconds(1f);
		}
	}

	public void OnLeftRoom()
	{
		Cleanup();
		if ((bool)notInRoomText)
		{
			notInRoomText.SetActive(value: true);
		}
	}

	public void Cleanup()
	{
		if (currentScoreboard != null)
		{
			UnityEngine.Object.Destroy(currentScoreboard.gameObject);
			currentScoreboard = null;
		}
	}
}
