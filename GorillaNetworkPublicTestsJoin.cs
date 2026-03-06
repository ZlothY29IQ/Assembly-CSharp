using System.Collections;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

public class GorillaNetworkPublicTestsJoin : GorillaTriggerBox, ITickSystemPost
{
	public GameObject[] makeSureThisIsDisabled;

	public GameObject[] makeSureThisIsEnabled;

	public string gameModeName;

	public PhotonNetworkController photonNetworkController;

	public string componentTypeToAdd;

	public GameObject componentTarget;

	public GorillaLevelScreen[] joinScreens;

	public GorillaLevelScreen[] leaveScreens;

	private Transform tosPition;

	private Transform othsTosPosition;

	private PhotonView fotVew;

	private bool waiting;

	private Vector3 lastPosition;

	private VRRig tempRig;

	public bool PostTickRunning { get; set; }

	public void Awake()
	{
		TickSystem<object>.AddPostTickCallback(this);
	}

	public void PostTick()
	{
		try
		{
			if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.IsVisible)
			{
				if (GTPlayer.Instance.GetComponent<Rigidbody>().isKinematic && !waiting && !MonkeAgent.instance.reportedPlayers.Contains(PhotonNetwork.LocalPlayer.UserId))
				{
					StartCoroutine(GracePeriod());
				}
				if ((GTPlayer.Instance.jumpMultiplier > GorillaGameManager.instance.fastJumpMultiplier * 2f || GTPlayer.Instance.maxJumpSpeed > GorillaGameManager.instance.fastJumpLimit * 2f) && !waiting && !MonkeAgent.instance.reportedPlayers.Contains(PhotonNetwork.LocalPlayer.UserId))
				{
					StartCoroutine(GracePeriod());
				}
				_ = (GTPlayer.Instance.transform.position - lastPosition).magnitude;
				_ = 4f;
			}
			lastPosition = GTPlayer.Instance.transform.position;
		}
		catch
		{
		}
	}

	private IEnumerator GracePeriod()
	{
		waiting = true;
		yield return new WaitForSeconds(30f);
		try
		{
			if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.IsVisible)
			{
				if (GTPlayer.Instance.GetComponent<Rigidbody>().isKinematic)
				{
					MonkeAgent.instance.SendReport("gorvity bisdabled", PhotonNetwork.LocalPlayer.UserId, PhotonNetwork.LocalPlayer.NickName);
				}
				if (GTPlayer.Instance.jumpMultiplier > GorillaGameManager.instance.fastJumpMultiplier * 2f || GTPlayer.Instance.maxJumpSpeed > GorillaGameManager.instance.fastJumpLimit * 2f)
				{
					MonkeAgent.instance.SendReport("jimp 2mcuh." + GTPlayer.Instance.jumpMultiplier + "." + GTPlayer.Instance.maxJumpSpeed + ".", PhotonNetwork.LocalPlayer.UserId, PhotonNetwork.LocalPlayer.NickName);
				}
				if (GorillaTagger.Instance.sphereCastRadius > 0.04f)
				{
					MonkeAgent.instance.SendReport("wack rad. " + GorillaTagger.Instance.sphereCastRadius, PhotonNetwork.LocalPlayer.UserId, PhotonNetwork.LocalPlayer.NickName);
				}
			}
			waiting = false;
		}
		catch
		{
		}
	}
}
