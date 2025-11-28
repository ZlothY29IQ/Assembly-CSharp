using ExitGames.Client.Photon;
using GorillaExtensions;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class GorillaEnemyAI : MonoBehaviourPun, IPunObservable, IInRoomCallbacks
{
	public Transform playerTransform;

	private NavMeshAgent agent;

	private Rigidbody r;

	private Vector3 targetPosition;

	private Vector3 targetRotation;

	public float lerpValue;

	private void Start()
	{
		agent = GetComponent<NavMeshAgent>();
		r = GetComponent<Rigidbody>();
		r.useGravity = true;
		if (!base.photonView.IsMine)
		{
			agent.enabled = false;
			r.isKinematic = true;
		}
	}

	void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(base.transform.position);
			stream.SendNext(base.transform.eulerAngles);
		}
		else
		{
			targetPosition.SetValueSafe((Vector3)stream.ReceiveNext());
			targetRotation.SetValueSafe((Vector3)stream.ReceiveNext());
		}
	}

	private void Update()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			FindClosestPlayer();
			if (playerTransform != null)
			{
				agent.destination = playerTransform.position;
			}
			base.transform.LookAt(new Vector3(playerTransform.transform.position.x, base.transform.position.y, playerTransform.position.z));
			r.linearVelocity *= 0.99f;
		}
		else
		{
			base.transform.position = Vector3.Lerp(base.transform.position, targetPosition, lerpValue);
			base.transform.eulerAngles = Vector3.Lerp(base.transform.eulerAngles, targetRotation, lerpValue);
		}
	}

	private void FindClosestPlayer()
	{
		VRRig[] array = Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None);
		VRRig vRRig = null;
		float num = 100000f;
		VRRig[] array2 = array;
		foreach (VRRig vRRig2 in array2)
		{
			Vector3 vector = vRRig2.transform.position - base.transform.position;
			if (vector.magnitude < num)
			{
				vRRig = vRRig2;
				num = vector.magnitude;
			}
		}
		playerTransform = vRRig.transform;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.layer == 19)
		{
			PhotonNetwork.Destroy(base.photonView);
		}
	}

	void IInRoomCallbacks.OnMasterClientSwitched(Player newMasterClient)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			agent.enabled = true;
			r.isKinematic = false;
		}
	}

	void IInRoomCallbacks.OnPlayerEnteredRoom(Player newPlayer)
	{
	}

	void IInRoomCallbacks.OnPlayerLeftRoom(Player otherPlayer)
	{
	}

	void IInRoomCallbacks.OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
	{
	}

	void IInRoomCallbacks.OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
	{
	}
}
