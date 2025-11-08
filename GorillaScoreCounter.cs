using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class GorillaScoreCounter : MonoBehaviour
{
	public bool isRedTeam;

	public Text text;

	public string attribute;

	private void Awake()
	{
		text = base.gameObject.GetComponent<Text>();
		if (isRedTeam)
		{
			attribute = "redScore";
		}
		else
		{
			attribute = "blueScore";
		}
	}

	private void Update()
	{
		if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties[attribute] != null)
		{
			text.text = ((int)PhotonNetwork.CurrentRoom.CustomProperties[attribute]).ToString();
		}
	}
}
