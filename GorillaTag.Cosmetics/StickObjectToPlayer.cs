using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class StickObjectToPlayer : MonoBehaviour, ITickSystemTick
{
	private enum SpawnLocation
	{
		Head,
		RightHand,
		LeftHand
	}

	[Header("Shared Settings")]
	[Tooltip("Must be in the global object pool and have a tag.")]
	[SerializeField]
	private GameObject objectToSpawn;

	[Tooltip("Optional: how many objects can be active at once")]
	[SerializeField]
	private int maxActiveStickies = 1;

	[SerializeField]
	private SpawnLocation spawnLocation;

	[SerializeField]
	private float stickRadius = 0.5f;

	[SerializeField]
	private bool alignToHitNormal = true;

	[SerializeField]
	private Rigidbody spawnerRigidbody;

	[SerializeField]
	private string parentTag = "GorillaHead";

	[SerializeField]
	private float cooldown;

	[Header("Third Person View")]
	[Tooltip("If you are only interested in the FPV, don't check this box so that others don't see it.")]
	[SerializeField]
	private bool thirdPersonView = true;

	[SerializeField]
	private Vector3 positionOffset = new Vector3(0f, 0.02f, 0.17f);

	[Tooltip("Local rotation to apply to the spawned object (Euler angles, degrees)")]
	[SerializeField]
	private Vector3 localEulerAngles = Vector3.zero;

	[Header("First Person View")]
	[SerializeField]
	private bool firstPersonView;

	[SerializeField]
	private Vector3 FPVOffset = new Vector3(0f, 0.02f, 0.17f);

	[Tooltip("Local rotation to apply to the spawned object (Euler angles, degrees)")]
	[SerializeField]
	private Vector3 FPVlocalEulerAngles = Vector3.zero;

	[Header("Events")]
	public UnityEvent OnStickShared;

	private GameObject stickyObject;

	private float lastSpawnedTime;

	private bool canSpawn = true;

	private NetPlayer ownerPlayer;

	public bool TickRunning { get; set; }

	public void Tick()
	{
		if (!canSpawn && Time.time - lastSpawnedTime >= cooldown)
		{
			canSpawn = true;
		}
	}

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
		canSpawn = true;
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void SetOwner(NetPlayer player)
	{
		ownerPlayer = player;
	}

	private Transform MakeOrGetStickyContainer(Transform parent)
	{
		Transform transform = parent;
		Transform[] componentsInChildren = parent.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform2 in componentsInChildren)
		{
			if (!firstPersonView && transform2.CompareTag(parentTag))
			{
				transform = transform2;
				break;
			}
		}
		string n = "StickyObjects_" + objectToSpawn.name;
		Transform transform3 = transform.Find(n);
		if (transform3 != null)
		{
			return transform3;
		}
		GameObject obj = new GameObject(n);
		obj.transform.SetParent(transform, worldPositionStays: false);
		return obj.transform;
	}

	public void Stick(bool leftHand, Collider other)
	{
		if (!canSpawn || other == null || !base.enabled)
		{
			return;
		}
		VRRig componentInParent = other.GetComponentInParent<VRRig>();
		if (!componentInParent || (ownerPlayer != null && componentInParent.creator == ownerPlayer))
		{
			return;
		}
		Vector3 vector = ((spawnerRigidbody != null) ? spawnerRigidbody.linearVelocity : Vector3.zero);
		Vector3 vector2 = Time.fixedDeltaTime * 2f * vector;
		Vector3 vector3 = vector2.normalized;
		if (vector3 == Vector3.zero)
		{
			vector3 = base.transform.forward;
			vector2 = vector3 * 0.01f;
		}
		Vector3 vector4 = base.transform.position - vector2;
		Vector3 vector5;
		if (alignToHitNormal)
		{
			float magnitude = vector2.magnitude;
			vector5 = ((!other.Raycast(new Ray(vector4, vector3), out var hitInfo, 2f * magnitude)) ? other.ClosestPoint(vector4) : hitInfo.point);
		}
		else
		{
			vector5 = other.ClosestPoint(vector4);
		}
		Vector3 vector6 = GetSpawnPosition(spawnLocation, componentInParent).TransformPoint(positionOffset);
		if (!((vector5 - vector6).magnitude <= stickRadius * componentInParent.scaleFactor))
		{
			return;
		}
		if (NetworkSystem.Instance.LocalPlayer == componentInParent.creator)
		{
			if (firstPersonView && spawnLocation == SpawnLocation.Head)
			{
				StickFirstPersonView();
			}
		}
		else
		{
			if (!thirdPersonView)
			{
				return;
			}
			Transform parent = MakeOrGetStickyContainer(componentInParent.transform);
			StickTo(parent, vector6, localEulerAngles);
		}
		OnStickShared?.Invoke();
	}

	private void StickFirstPersonView()
	{
		Transform cosmeticsHeadTarget = GTPlayer.Instance.CosmeticsHeadTarget;
		Vector3 position = cosmeticsHeadTarget.TransformPoint(FPVOffset);
		Transform parent = MakeOrGetStickyContainer(cosmeticsHeadTarget);
		StickTo(parent, position, FPVlocalEulerAngles);
	}

	private void StickTo(Transform parent, Vector3 position, Vector3 eulerAngle)
	{
		int num = 0;
		for (int i = 0; i < parent.childCount; i++)
		{
			if (parent.GetChild(i).gameObject.activeInHierarchy)
			{
				num++;
			}
		}
		if (num < maxActiveStickies)
		{
			stickyObject = ObjectPools.instance.Instantiate(objectToSpawn);
			if (!(stickyObject == null))
			{
				stickyObject.transform.SetParent(parent, worldPositionStays: false);
				stickyObject.transform.position = position;
				stickyObject.transform.localEulerAngles = eulerAngle;
				lastSpawnedTime = Time.time;
				canSpawn = false;
			}
		}
	}

	private Transform GetSpawnPosition(SpawnLocation spawnType, VRRig hitRig)
	{
		return spawnType switch
		{
			SpawnLocation.Head => hitRig.head.rigTarget.transform, 
			SpawnLocation.LeftHand => hitRig.leftHand.rigTarget.transform, 
			SpawnLocation.RightHand => hitRig.rightHand.rigTarget.transform, 
			_ => null, 
		};
	}

	public void Debug_StickToLocalPlayer()
	{
		Vector3 position = GetSpawnPosition(spawnLocation, VRRig.LocalRig).TransformPoint(positionOffset);
		StickTo(VRRig.LocalRig.transform, position, localEulerAngles);
	}

	public void Debug_StickToLocalPlayerFPV()
	{
		StickFirstPersonView();
	}
}
