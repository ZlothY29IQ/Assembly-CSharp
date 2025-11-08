using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class SIResourceDeposit : MonoBehaviour, ISIResourceDeposit
{
	public int index;

	public Text depositText;

	public Image depositImage;

	public DisableGameObjectDelayed popupScreen;

	public SuperInfection superInfection;

	public Sprite[] resourceImageSprites;

	public GameObject depositBin;

	[SerializeField]
	private Transform[] resourceDisplays;

	public SIPlayer netPlayer;

	public SIResource.ResourceType netResourceType;

	public SIResource.LimitedDepositType netLimitedDepositType;

	private bool netShowPopup;

	public List<SIUIPlayerQuestDisplay> questDisplays;

	private List<GameObject> _displayResources;

	public bool IsAuthority => SIManager.gameEntityManager.IsAuthority();

	public SuperInfectionManager SIManager => superInfection.siManager;

	private void OnEnable()
	{
		if (_displayResources != null && _displayResources.Count != 0)
		{
			return;
		}
		List<SIResource> resourcePrefabs = superInfection.ResourcePrefabs;
		if (resourcePrefabs == null || resourcePrefabs.Count <= 0)
		{
			return;
		}
		_displayResources = new List<GameObject>();
		for (int i = 0; i < Mathf.Min(resourcePrefabs.Count, resourceDisplays.Length); i++)
		{
			GameObject gameObject = resourcePrefabs[i].gameObject;
			bool activeSelf = gameObject.activeSelf;
			try
			{
				if (activeSelf)
				{
					gameObject.SetActive(value: false);
				}
				GameObject gameObject2 = Object.Instantiate(gameObject, resourceDisplays[i].transform);
				gameObject2.transform.localScale = new Vector3(0.27f, 0.27f, 0.27f);
				_displayResources.Add(gameObject2);
				MonoBehaviour[] componentsInChildren = gameObject2.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
				foreach (MonoBehaviour obj in componentsInChildren)
				{
					obj.enabled = false;
					Object.Destroy(obj);
				}
				Rigidbody component = gameObject2.GetComponent<Rigidbody>();
				if ((object)component != null)
				{
					Object.Destroy(component);
				}
				gameObject2.SetLayerRecursively(UnityLayer.Default);
				gameObject2.SetActive(value: true);
			}
			finally
			{
				if (activeSelf)
				{
					gameObject.SetActive(value: true);
				}
			}
		}
	}

	public void WriteDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		if (netPlayer != null)
		{
			stream.SendNext(netPlayer.ActorNr);
		}
		else
		{
			stream.SendNext(-1);
		}
		stream.SendNext((int)netResourceType);
		stream.SendNext((int)netLimitedDepositType);
		stream.SendNext(netShowPopup);
		netShowPopup = false;
	}

	public void ReadDataPUN(PhotonStream stream, PhotonMessageInfo info)
	{
		netPlayer = SIPlayer.Get((int)stream.ReceiveNext());
		netResourceType = (SIResource.ResourceType)(int)stream.ReceiveNext();
		netLimitedDepositType = (SIResource.LimitedDepositType)(int)stream.ReceiveNext();
		if ((bool)stream.ReceiveNext())
		{
			LocalShowPopup(netPlayer, netResourceType, netLimitedDepositType);
		}
	}

	private void LocalShowPopup(SIPlayer player, SIResource.ResourceType resourceType, SIResource.LimitedDepositType limitedDepositType)
	{
		if (limitedDepositType == SIResource.LimitedDepositType.None)
		{
			depositBin.SetActive(value: true);
		}
		popupScreen.EnableAndResetTimer();
		depositText.text = $"{player.gamePlayer.rig.Creator.SanitizedNickName} COLLECTED {resourceType.GetName()}\n(TOTAL {player.GetResourceAmount(resourceType)})";
		depositImage.sprite = ((resourceType == SIResource.ResourceType.TechPoint) ? resourceImageSprites[0] : resourceImageSprites[1]);
	}

	public void ResourceDeposited(SIResource resource)
	{
		bool flag = false;
		if (resource.lastPlayerHeld.gamePlayer.IsLocal() && !resource.localDeposited)
		{
			AuthShowPopup(resource);
			resource.HandleDepositLocal(resource.lastPlayerHeld);
			resource.lastPlayerHeld.GatherResource(resource.type, resource.limitedDepositType, 1);
			superInfection.siManager.CallRPC(SuperInfectionManager.ClientToAuthorityRPC.ResourceDepositDeposited, new object[2]
			{
				resource.myGameEntity.GetNetId(),
				index
			});
			flag = true;
		}
		if (superInfection.siManager.gameEntityManager.IsAuthority())
		{
			resource.HandleDepositAuth(resource.lastPlayerHeld);
			superInfection.siManager.gameEntityManager.RequestDestroyItem(resource.myGameEntity.id);
			AuthShowPopup(resource);
			flag = true;
		}
		if (flag)
		{
			LocalShowPopup(resource.lastPlayerHeld, resource.type, resource.limitedDepositType);
		}
	}

	private void AuthShowPopup(SIResource resource)
	{
		netPlayer = resource.lastPlayerHeld;
		netResourceType = resource.type;
		netLimitedDepositType = resource.limitedDepositType;
		netShowPopup = true;
	}
}
