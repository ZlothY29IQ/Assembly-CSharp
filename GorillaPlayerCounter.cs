using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class GorillaPlayerCounter : MonoBehaviour
{
	public bool isRedTeam;

	public Text text;

	public string attribute;

	private void Awake()
	{
		text = base.gameObject.GetComponent<Text>();
	}

	private void Update()
	{
		if (PhotonNetwork.CurrentRoom == null)
		{
			return;
		}
		int num = 0;
		foreach (KeyValuePair<int, Player> player in PhotonNetwork.CurrentRoom.Players)
		{
			if ((bool)player.Value.CustomProperties["isRedTeam"] == isRedTeam)
			{
				num++;
			}
		}
		text.text = num.ToString();
	}
}
