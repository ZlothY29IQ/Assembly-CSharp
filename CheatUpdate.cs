using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;

public class CheatUpdate : MonoBehaviour
{
	private void Start()
	{
		StartCoroutine(UpdateNumberOfPlayers());
	}

	public IEnumerator UpdateNumberOfPlayers()
	{
		while (true)
		{
			StartCoroutine(UpdatePlayerCount());
			yield return new WaitForSeconds(10f);
		}
	}

	private IEnumerator UpdatePlayerCount()
	{
		WWWForm wWWForm = new WWWForm();
		wWWForm.AddField("player_count", PhotonNetwork.CountOfPlayers - 1);
		wWWForm.AddField("game_version", "live");
		wWWForm.AddField("game_name", Application.productName);
		Debug.Log(PhotonNetwork.CountOfPlayers - 1);
		using UnityWebRequest www = UnityWebRequest.Post("http://ntsfranz.crabdance.com/update_monke_count", wWWForm);
		yield return www.SendWebRequest();
		if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
		{
			Debug.Log(www.error);
		}
	}
}
