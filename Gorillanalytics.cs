using System;
using System.Collections;
using System.Linq;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using PlayFab;
using UnityEngine;

public class Gorillanalytics : MonoBehaviour
{
	private class UploadData
	{
		public string version;

		public double upload_chance;

		public string map;

		public string mode;

		public string queue;

		public int player_count;

		public float pos_x;

		public float pos_y;

		public float pos_z;

		public float vel_x;

		public float vel_y;

		public float vel_z;

		public string cosmetics_owned;

		public string cosmetics_worn;
	}

	public float interval = 60f;

	public double oneOverChance = 4320.0;

	public PhotonNetworkController photonNetworkController;

	public GameModeZoneMapping gameModeData;

	private readonly UploadData uploadData = new UploadData();

	public const string GORILLANALYTICS_EVENT_NAME = "periodic_player_state";

	private IEnumerator Start()
	{
		PlayFabTitleDataCache.Instance.GetTitleData("GorillanalyticsChance", delegate(string s)
		{
			if (double.TryParse(s, out var result))
			{
				oneOverChance = result;
			}
		}, delegate
		{
		});
		while (true)
		{
			yield return new WaitForSecondsRealtime(interval);
			if ((double)UnityEngine.Random.Range(0f, 1f) < 1.0 / oneOverChance && PlayFabClientAPI.IsClientLoggedIn())
			{
				UploadGorillanalytics();
			}
		}
	}

	private void UploadGorillanalytics()
	{
		try
		{
			GetMapModeQueue(out var map, out var mode, out var queue);
			Vector3 position = GTPlayer.Instance.headCollider.transform.position;
			Vector3 averagedVelocity = GTPlayer.Instance.AveragedVelocity;
			uploadData.version = NetworkSystemConfig.AppVersion;
			uploadData.upload_chance = oneOverChance;
			uploadData.map = map;
			uploadData.mode = mode;
			uploadData.queue = queue;
			uploadData.player_count = (PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 0);
			uploadData.pos_x = position.x;
			uploadData.pos_y = position.y;
			uploadData.pos_z = position.z;
			uploadData.vel_x = averagedVelocity.x;
			uploadData.vel_y = averagedVelocity.y;
			uploadData.vel_z = averagedVelocity.z;
			uploadData.cosmetics_owned = string.Join(";", CosmeticsController.instance.unlockedCosmetics.Select((CosmeticsController.CosmeticItem c) => c.itemName));
			uploadData.cosmetics_worn = string.Join(";", CosmeticsController.instance.currentWornSet.items.Select((CosmeticsController.CosmeticItem c) => c.itemName));
			GorillaServer.Instance.UploadGorillanalytics(uploadData);
			GorillaTelemetry.EnqueueTelemetryEvent("periodic_player_state", uploadData);
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
	}

	private void GetMapModeQueue(out string map, out string mode, out string queue)
	{
		if (!PhotonNetwork.InRoom)
		{
			map = "none";
			mode = "none";
			queue = "none";
			return;
		}
		object value = null;
		PhotonNetwork.CurrentRoom?.CustomProperties.TryGetValue("gameMode", out value);
		GameModeString gameModeString = GameModeString.FromString(value?.ToString() ?? "");
		GTZone gTZone = GorillaTagger.Instance.offlineVRRig.zoneEntity.currentZone;
		if (gTZone == GTZone.cityNoBuildings || gTZone == GTZone.cityWithSkyJungle || gTZone == GTZone.mall)
		{
			gTZone = GTZone.city;
		}
		if (gTZone == GTZone.tutorial)
		{
			gTZone = GTZone.forest;
		}
		if (gTZone == GTZone.ghostReactorTunnel)
		{
			gTZone = GTZone.ghostReactor;
		}
		map = gTZone.ToString().ToLower();
		if (NetworkSystem.Instance.SessionIsPrivate)
		{
			map += "private";
		}
		mode = gameModeString?.gameType.ToUpper();
		if (mode.IsNullOrEmpty())
		{
			mode = "none";
		}
		queue = gameModeString?.queue.ToUpper();
		if (queue.IsNullOrEmpty())
		{
			queue = "none";
		}
	}
}
