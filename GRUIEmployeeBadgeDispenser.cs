using TMPro;
using UnityEngine;

public class GRUIEmployeeBadgeDispenser : MonoBehaviour
{
	[SerializeField]
	private TMP_Text msg;

	[SerializeField]
	private TMP_Text playerName;

	[SerializeField]
	private Transform spawnLocation;

	[SerializeField]
	private GameEntity idBadgePrefab;

	[SerializeField]
	private LayerMask badgeLayerMask;

	public int index;

	public int actorNr;

	public GRBadge idBadge;

	private GhostReactor reactor;

	private Coroutine getSpawnedBadgeCoroutine;

	private static Collider[] overlapColliders = new Collider[10];

	private bool isEmployee;

	private const string GR_DATA_KEY = "GRData";

	public void Setup(GhostReactor reactor, int employeeIndex)
	{
		this.reactor = reactor;
	}

	public void Refresh()
	{
		NetPlayer player = NetworkSystem.Instance.GetPlayer(actorNr);
		if (player != null && player.InRoom)
		{
			playerName.text = player.SanitizedNickName;
			if (idBadge != null)
			{
				idBadge.RefreshText(player);
			}
		}
		else
		{
			playerName.text = "";
		}
	}

	public void CreateBadge(NetPlayer player, GameEntityManager entityManager)
	{
		if (entityManager.IsAuthority())
		{
			entityManager.RequestCreateItem(idBadgePrefab.name.GetStaticHash(), spawnLocation.position, spawnLocation.rotation, player.ActorNumber * 100 + index);
		}
	}

	public Transform GetSpawnMarker()
	{
		return spawnLocation;
	}

	public bool IsDispenserForBadge(GRBadge badge)
	{
		return badge == idBadge;
	}

	public Vector3 GetSpawnPosition()
	{
		return spawnLocation.position;
	}

	public Quaternion GetSpawnRotation()
	{
		return spawnLocation.rotation;
	}

	public void ClearBadge()
	{
		actorNr = -1;
		idBadge = null;
	}

	public void AttachIDBadge(GRBadge linkedBadge, NetPlayer _player)
	{
		actorNr = _player?.ActorNumber ?? (-1);
		idBadge = linkedBadge;
		playerName.text = _player?.SanitizedNickName;
		idBadge.Setup(_player, index);
	}
}
