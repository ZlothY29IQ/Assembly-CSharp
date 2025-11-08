using System;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaTagScripts;

public class BuilderItem : TransferrableObject
{
	private enum BuilderItemState
	{
		isHeld = 1,
		dropped = 2,
		placed = 4,
		unused0 = 8,
		none = 0x10
	}

	public BuilderItemReliableState reliableState;

	public string builtItemPath;

	public GameObject itemRoot;

	private bool enableCollidersWhenReady;

	private float handsFreeOfCollidersTime;

	[NonSerialized]
	public BuilderPiece attachedPiece;

	public List<Behaviour> onlyWhenPlacedBehaviours;

	[NonSerialized]
	public BuilderItem parentItem;

	public List<BuilderAttachGridPlane> gridPlanes;

	public List<BuilderAttachEdge> edges;

	private List<Collider> colliders;

	private Transform parent;

	private Vector3 initialPosition;

	private Quaternion initialRotation;

	private Vector3 initialGrabInteractorScale;

	private BuilderTable currTable;

	[SerializeField]
	private AudioSource audioSource;

	public AudioClip snapAudio;

	public AudioClip placeAudio;

	public GameObject placeVFX;

	private new BuilderItemState previousItemState = BuilderItemState.dropped;

	public override bool ShouldBeKinematic()
	{
		if (itemState != ItemStates.State2 && itemState != ItemStates.State4)
		{
			return base.ShouldBeKinematic();
		}
		return true;
	}

	protected override void Awake()
	{
		base.Awake();
		parent = base.transform.parent;
		currTable = null;
		initialPosition = base.transform.position;
		initialRotation = base.transform.rotation;
		initialGrabInteractorScale = gripInteractor.transform.localScale;
	}

	internal override void OnEnable()
	{
		base.OnEnable();
	}

	internal override void OnDisable()
	{
		base.OnDisable();
	}

	protected override void Start()
	{
		base.Start();
		itemState = ItemStates.State4;
		currentState = PositionState.Dropped;
	}

	public void AttachPiece(BuilderPiece piece)
	{
		base.transform.SetPositionAndRotation(piece.transform.position, piece.transform.rotation);
		piece.transform.localScale = Vector3.one;
		piece.transform.SetParent(itemRoot.transform);
		Debug.LogFormat(piece.gameObject, "Attach Piece {0} to container {1}", piece.gameObject.GetInstanceID(), base.gameObject.GetInstanceID());
		attachedPiece = piece;
	}

	public void DetachPiece(BuilderPiece piece)
	{
		if (piece != attachedPiece)
		{
			Debug.LogErrorFormat("Trying to detach piece {0} from a container containing {1}", piece.pieceId, attachedPiece.pieceId);
		}
		else
		{
			piece.transform.SetParent(null);
			Debug.LogFormat(attachedPiece.gameObject, "Detach Piece {0} from container {1}", attachedPiece.gameObject.GetInstanceID(), base.gameObject.GetInstanceID());
			attachedPiece = null;
		}
	}

	private new void OnStateChanged()
	{
		if (itemState == ItemStates.State2)
		{
			enableCollidersWhenReady = true;
			gripInteractor.transform.localScale = initialGrabInteractorScale * 2f;
			handsFreeOfCollidersTime = 0f;
		}
		else
		{
			enableCollidersWhenReady = false;
			gripInteractor.transform.localScale = initialGrabInteractorScale;
			handsFreeOfCollidersTime = 0f;
		}
	}

	public override Matrix4x4 GetDefaultTransformationMatrix()
	{
		if (reliableState.dirty)
		{
			SetupHandMatrix(reliableState.leftHandAttachPos, reliableState.leftHandAttachRot, reliableState.rightHandAttachPos, reliableState.rightHandAttachRot);
			reliableState.dirty = false;
		}
		return base.GetDefaultTransformationMatrix();
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		if (InHand())
		{
			itemState = ItemStates.State0;
		}
		BuilderItemState builderItemState = (BuilderItemState)itemState;
		if (builderItemState != previousItemState)
		{
			OnStateChanged();
		}
		previousItemState = builderItemState;
		if (enableCollidersWhenReady)
		{
			bool flag = IsOverlapping(EquipmentInteractor.instance.overlapInteractionPointsRight) || IsOverlapping(EquipmentInteractor.instance.overlapInteractionPointsLeft);
			handsFreeOfCollidersTime += (flag ? 0f : Time.deltaTime);
			if (handsFreeOfCollidersTime > 0.1f)
			{
				gripInteractor.transform.localScale = initialGrabInteractorScale;
				enableCollidersWhenReady = false;
			}
		}
	}

	private bool IsOverlapping(List<InteractionPoint> interactionPoints)
	{
		if (interactionPoints == null)
		{
			return false;
		}
		for (int i = 0; i < interactionPoints.Count; i++)
		{
			if (interactionPoints[i] == gripInteractor)
			{
				return true;
			}
		}
		return false;
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
	}

	public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
	{
		if (!(GorillaTagger.Instance.offlineVRRig.scaleFactor < 1f))
		{
			base.OnGrab(pointGrabbed, grabbingHand);
			itemState = ItemStates.State0;
		}
	}

	public override bool OnRelease(DropZone zoneReleased, GameObject releasingHand)
	{
		if (!base.OnRelease(zoneReleased, releasingHand))
		{
			return false;
		}
		itemState = ItemStates.State1;
		Reparent(null);
		parentItem = null;
		gripInteractor.transform.localScale = initialGrabInteractorScale;
		return true;
	}

	public void OnHoverOverTableStart(BuilderTable table)
	{
		currTable = table;
	}

	public void OnHoverOverTableEnd(BuilderTable table)
	{
		currTable = null;
	}

	public override void OnJoinedRoom()
	{
		base.OnJoinedRoom();
	}

	public override void OnLeftRoom()
	{
		base.OnLeftRoom();
		base.transform.position = initialPosition;
		base.transform.rotation = initialRotation;
		if (worldShareableInstance != null)
		{
			worldShareableInstance.transform.position = initialPosition;
			worldShareableInstance.transform.rotation = initialRotation;
		}
		itemState = ItemStates.State4;
		currentState = PositionState.Dropped;
	}

	private void PlayVFX(GameObject vfx)
	{
		ObjectPools.instance.Instantiate(vfx, base.transform.position);
	}

	private bool Reparent(Transform _transform)
	{
		if (!allowReparenting)
		{
			return false;
		}
		if ((bool)parent)
		{
			parent.SetParent(_transform);
			base.transform.SetParent(parent);
			return true;
		}
		return false;
	}

	private bool ShouldPlayFX()
	{
		if (previousItemState == BuilderItemState.isHeld || previousItemState == BuilderItemState.dropped)
		{
			return true;
		}
		return false;
	}

	public static GameObject BuildEnvItem(int prefabHash, Vector3 position, Quaternion rotation)
	{
		GameObject obj = ObjectPools.instance.Instantiate(prefabHash);
		obj.transform.SetPositionAndRotation(position, rotation);
		return obj;
	}

	protected override void OnHandMatrixUpdate(Vector3 localPosition, Quaternion localRotation, bool leftHand)
	{
		if (leftHand)
		{
			reliableState.leftHandAttachPos = localPosition;
			reliableState.leftHandAttachRot = localRotation;
		}
		else
		{
			reliableState.rightHandAttachPos = localPosition;
			reliableState.rightHandAttachRot = localRotation;
		}
		reliableState.dirty = true;
	}

	public int GetPhotonViewId()
	{
		if (worldShareableInstance == null)
		{
			return -1;
		}
		return worldShareableInstance.ViewID;
	}
}
