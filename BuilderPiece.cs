using System;
using System.Collections.Generic;
using GorillaTagScripts;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

public class BuilderPiece : MonoBehaviour
{
	public enum State
	{
		None = -1,
		AttachedAndPlaced,
		AttachedToDropped,
		Grabbed,
		Dropped,
		OnShelf,
		Displayed,
		GrabbedLocal,
		OnConveyor,
		AttachedToArm
	}

	public const int INVALID = -1;

	public const float LIGHT_MASS = 1f;

	public const float HEAVY_MASS = 10000f;

	[Tooltip("Name for debug text")]
	public string displayName;

	[Tooltip("(Optional) scriptable object containing material swaps")]
	public BuilderMaterialOptions materialOptions;

	[Tooltip("Builder Resources used by this object\nbuilderRscBasic for simple meshes\nbuilderRscDecorative for detailed meshes\nbuilderRscFunctional for extra scripts or effects")]
	public BuilderResources cost;

	[Tooltip("Spawn Offset")]
	public Vector3 desiredShelfOffset = Vector3.zero;

	[Tooltip("Spawn Offset")]
	public Vector3 desiredShelfRotationOffset = Vector3.zero;

	[FormerlySerializedAs("vFXInfo")]
	[Tooltip("sounds for block actions. everything uses BuilderPieceEffectInfo_Default")]
	[SerializeField]
	private BuilderPieceEffectInfo fXInfo;

	private List<MeshRenderer> materialSwapTargets;

	private List<GorillaSurfaceOverride> surfaceOverrides;

	[Tooltip("parent object of everything scaled with the piece")]
	public Transform scaleRoot;

	[Tooltip("Is the block part of the room / immovable (used for the base terrain)")]
	public bool isBuiltIntoTable;

	public bool isArmShelf;

	[HideInInspector]
	public BuilderArmShelf armShelf;

	[Tooltip("Used to prevent log warnings from materials incompatible with the builder renderer\nAnything that needs text/transparency/or particles uses the normal rendering pipeline")]
	public bool suppressMaterialWarnings;

	[Tooltip("Only used by private plots")]
	private bool isPrivatePlot;

	[HideInInspector]
	public int privatePlotIndex;

	[Tooltip("Only used by private plots")]
	public BuilderPiecePrivatePlot plotComponent;

	[Tooltip("Add piece movement to player movement when touched")]
	public bool attachPlayerToPiece;

	public int pieceType;

	public int pieceId;

	public int pieceDataIndex;

	public int materialType = -1;

	public int heldByPlayerActorNumber;

	public bool heldInLeftHand;

	public Transform parentHeld;

	[HideInInspector]
	public BuilderPiece parentPiece;

	[HideInInspector]
	public BuilderPiece firstChildPiece;

	[HideInInspector]
	public BuilderPiece nextSiblingPiece;

	[HideInInspector]
	public int attachIndex;

	[HideInInspector]
	public int parentAttachIndex;

	public int shelfOwner = -1;

	[HideInInspector]
	public List<BuilderAttachGridPlane> gridPlanes;

	[HideInInspector]
	public List<Collider> colliders;

	public List<Collider> placedOnlyColliders;

	private int currentColliderLayer = BuilderTable.droppedLayer;

	[Tooltip("Components enabled when the block is snapped to the build table")]
	public List<Behaviour> onlyWhenPlacedBehaviours;

	[Tooltip("Game objects enabled when the block is snapped to the build table\nAny concave collision should be here")]
	public List<GameObject> onlyWhenPlaced;

	[Tooltip("Game objects enabled when the block is not snapped to the build table\n Convex collision should be here if there is concave collision when placed")]
	public List<GameObject> onlyWhenNotPlaced;

	public List<IBuilderPieceComponent> pieceComponents;

	public IBuilderPieceFunctional functionalPieceComponent;

	public byte functionalPieceState;

	public List<IBuilderPieceFunctional> pieceFunctionComponents;

	private bool pieceComponentsActive;

	[Tooltip("Check if any renderers are in the onlyWhenPlaced or onlyWhenNotPlaced lists")]
	public bool areMeshesToggledOnPlace;

	[NonSerialized]
	public Rigidbody rigidBody;

	[NonSerialized]
	public int activatedTimeStamp;

	[HideInInspector]
	public int preventSnapUntilMoved;

	[HideInInspector]
	public Vector3 preventSnapUntilMovedFromPos;

	[HideInInspector]
	public BuilderPiece requestedParentPiece;

	private BuilderTable tableOwner;

	public PieceFallbackInfo fallbackInfo;

	[NonSerialized]
	public bool overrideSavedPiece;

	[NonSerialized]
	public int savedPieceType = -1;

	[NonSerialized]
	public int savedMaterialType = -1;

	private float pieceScale;

	private float[] collisionEnterHistory;

	private int collisionEnterLimit = 10;

	private float collisionEnterCooldown = 2f;

	private int oldCollisionTimeIndex;

	[HideInInspector]
	public State state;

	[HideInInspector]
	public bool isStatic;

	[NonSerialized]
	private bool listeningToHandLinks;

	[HideInInspector]
	public List<MeshRenderer> renderingDirect;

	[HideInInspector]
	public List<MeshRenderer> renderingIndirect;

	[HideInInspector]
	public List<int> renderingIndirectTransformIndex;

	[HideInInspector]
	public float tint;

	private int paintingCount;

	private int potentialGrabCount;

	private int potentialGrabChildCount;

	internal bool forcedFrozen;

	private HashSet<int> collidersEntered = new HashSet<int>(128);

	private static List<MeshRenderer> tempRenderers = new List<MeshRenderer>(48);

	private void Awake()
	{
		if (fXInfo == null)
		{
			Debug.LogErrorFormat("BuilderPiece {0} is missing Effect Info", base.gameObject.name);
		}
		materialType = -1;
		pieceType = -1;
		pieceId = -1;
		pieceDataIndex = -1;
		state = State.None;
		isStatic = true;
		parentPiece = null;
		firstChildPiece = null;
		nextSiblingPiece = null;
		attachIndex = -1;
		parentAttachIndex = -1;
		parentHeld = null;
		heldByPlayerActorNumber = -1;
		placedOnlyColliders = new List<Collider>(4);
		List<Collider> list = new List<Collider>(4);
		foreach (GameObject item in onlyWhenPlaced)
		{
			list.Clear();
			item.GetComponentsInChildren(list);
			for (int i = 0; i < list.Count; i++)
			{
				if (!list[i].isTrigger)
				{
					BuilderPieceCollider builderPieceCollider = list[i].GetComponent<BuilderPieceCollider>();
					if (builderPieceCollider == null)
					{
						builderPieceCollider = list[i].AddComponent<BuilderPieceCollider>();
					}
					builderPieceCollider.piece = this;
					placedOnlyColliders.Add(list[i]);
				}
			}
		}
		SetActive(onlyWhenPlaced, active: false);
		SetActive(onlyWhenNotPlaced, active: true);
		colliders = new List<Collider>(4);
		GetComponentsInChildren(colliders);
		for (int num = colliders.Count - 1; num >= 0; num--)
		{
			if (colliders[num].isTrigger)
			{
				colliders.RemoveAt(num);
			}
			else
			{
				BuilderPieceCollider builderPieceCollider2 = colliders[num].GetComponent<BuilderPieceCollider>();
				if (builderPieceCollider2 == null)
				{
					builderPieceCollider2 = colliders[num].AddComponent<BuilderPieceCollider>();
				}
				builderPieceCollider2.piece = this;
			}
		}
		gridPlanes = new List<BuilderAttachGridPlane>(8);
		GetComponentsInChildren(gridPlanes);
		pieceComponents = new List<IBuilderPieceComponent>(1);
		GetComponentsInChildren(includeInactive: true, pieceComponents);
		pieceComponentsActive = false;
		functionalPieceComponent = GetComponentInChildren<IBuilderPieceFunctional>(includeInactive: true);
		SetCollidersEnabled(colliders, enabled: false);
		SetBehavioursEnabled(onlyWhenPlacedBehaviours, enabled: false);
		preventSnapUntilMoved = 0;
		preventSnapUntilMovedFromPos = Vector3.zero;
		renderingIndirect = new List<MeshRenderer>(4);
		renderingDirect = new List<MeshRenderer>(4);
		FindActiveRenderers();
		paintingCount = 0;
		potentialGrabCount = 0;
		potentialGrabChildCount = 0;
		isPrivatePlot = plotComponent != null;
		privatePlotIndex = -1;
		ClearCollisionHistory();
	}

	public void SetTable(BuilderTable table)
	{
		tableOwner = table;
	}

	public BuilderTable GetTable()
	{
		return tableOwner;
	}

	public void OnReturnToPool()
	{
		tableOwner.builderRenderer.RemovePiece(this);
		for (int i = 0; i < pieceComponents.Count; i++)
		{
			pieceComponents[i].OnPieceDestroy();
		}
		functionalPieceState = 0;
		state = State.None;
		isStatic = true;
		materialType = -1;
		pieceType = -1;
		pieceId = -1;
		pieceDataIndex = -1;
		parentPiece = null;
		firstChildPiece = null;
		nextSiblingPiece = null;
		attachIndex = -1;
		parentAttachIndex = -1;
		overrideSavedPiece = false;
		savedMaterialType = -1;
		savedPieceType = -1;
		shelfOwner = -1;
		parentHeld = null;
		heldByPlayerActorNumber = -1;
		activatedTimeStamp = 0;
		forcedFrozen = false;
		SetActive(onlyWhenPlaced, active: false);
		SetActive(onlyWhenNotPlaced, active: true);
		SetCollidersEnabled(colliders, enabled: false);
		SetBehavioursEnabled(onlyWhenPlacedBehaviours, enabled: false);
		preventSnapUntilMoved = 0;
		preventSnapUntilMovedFromPos = Vector3.zero;
		base.transform.localScale = Vector3.one;
		if (isArmShelf)
		{
			if (armShelf != null)
			{
				armShelf.piece = null;
			}
			armShelf = null;
		}
		for (int j = 0; j < gridPlanes.Count; j++)
		{
			gridPlanes[j].OnReturnToPool(tableOwner.builderPool);
		}
	}

	public void OnCreatedByPool()
	{
		materialSwapTargets = new List<MeshRenderer>(4);
		GetComponentsInChildren(areMeshesToggledOnPlace, materialSwapTargets);
		surfaceOverrides = new List<GorillaSurfaceOverride>(4);
		GetComponentsInChildren(areMeshesToggledOnPlace, surfaceOverrides);
	}

	public void SetupPiece(float gridSize)
	{
		for (int i = 0; i < gridPlanes.Count; i++)
		{
			gridPlanes[i].Setup(this, i, gridSize);
		}
	}

	public void SetMaterial(int inMaterialType, bool force = false)
	{
		if (materialOptions == null || materialSwapTargets == null || materialSwapTargets.Count < 1 || (materialType == inMaterialType && !force))
		{
			return;
		}
		materialType = inMaterialType;
		Material material = null;
		int soundIndex = -1;
		if (inMaterialType == -1)
		{
			materialOptions.GetDefaultMaterial(out materialType, out material, out soundIndex);
		}
		else
		{
			materialOptions.GetMaterialFromType(materialType, out material, out soundIndex);
			if (material == null)
			{
				materialOptions.GetDefaultMaterial(out materialType, out material, out soundIndex);
			}
		}
		if (material == null)
		{
			Debug.LogErrorFormat("Piece {0} has no material matching Type {1}", GetPieceId(), inMaterialType);
			return;
		}
		foreach (MeshRenderer materialSwapTarget in materialSwapTargets)
		{
			if (!(materialSwapTarget == null) && materialSwapTarget.enabled)
			{
				materialSwapTarget.material = material;
			}
		}
		if (surfaceOverrides != null && soundIndex != -1)
		{
			foreach (GorillaSurfaceOverride surfaceOverride in surfaceOverrides)
			{
				surfaceOverride.overrideIndex = soundIndex;
			}
		}
		if (renderingIndirect.Count > 0)
		{
			tableOwner.builderRenderer.ChangePieceIndirectMaterial(this, materialSwapTargets, material);
		}
	}

	public int GetPieceId()
	{
		return pieceId;
	}

	public int GetParentPieceId()
	{
		if (!(parentPiece == null))
		{
			return parentPiece.pieceId;
		}
		return -1;
	}

	public int GetAttachIndex()
	{
		return attachIndex;
	}

	public int GetParentAttachIndex()
	{
		return parentAttachIndex;
	}

	private void SetPieceActive(List<IBuilderPieceComponent> components, bool active)
	{
		if (components == null || active == pieceComponentsActive)
		{
			return;
		}
		pieceComponentsActive = active;
		for (int i = 0; i < components.Count; i++)
		{
			if (components[i] != null)
			{
				if (active)
				{
					components[i].OnPieceActivate();
				}
				else
				{
					components[i].OnPieceDeactivate();
				}
			}
		}
	}

	private void SetBehavioursEnabled<T>(List<T> components, bool enabled) where T : Behaviour
	{
		if (components == null)
		{
			return;
		}
		for (int i = 0; i < components.Count; i++)
		{
			if (components[i] != null)
			{
				components[i].enabled = enabled;
			}
		}
	}

	public void UpdateCollidersEnabled(bool _enabled)
	{
		SetCollidersEnabled(colliders, _enabled);
	}

	private void SetCollidersEnabled<T>(List<T> components, bool enabled) where T : Collider
	{
		if (components == null)
		{
			return;
		}
		for (int i = 0; i < components.Count; i++)
		{
			if (components[i] != null)
			{
				components[i].enabled = enabled;
			}
		}
	}

	public void SetColliderLayers<T>(List<T> components, int layer) where T : Collider
	{
		currentColliderLayer = layer;
		if (components == null)
		{
			return;
		}
		for (int i = 0; i < components.Count; i++)
		{
			if (components[i] != null)
			{
				components[i].gameObject.layer = layer;
			}
		}
	}

	private void SetActive(List<GameObject> gameObjects, bool active)
	{
		if (gameObjects == null)
		{
			return;
		}
		for (int i = 0; i < gameObjects.Count; i++)
		{
			if (gameObjects[i] != null)
			{
				gameObjects[i].SetActive(active);
			}
		}
	}

	public void SetFunctionalPieceState(byte fState, NetPlayer instigator, int timeStamp)
	{
		if (functionalPieceComponent == null || !functionalPieceComponent.IsStateValid(fState))
		{
			fState = 0;
		}
		functionalPieceState = fState;
		functionalPieceComponent?.OnStateChanged(fState, instigator, timeStamp);
	}

	public void SetScale(float scale)
	{
		if (scaleRoot != null)
		{
			scaleRoot.localScale = Vector3.one * scale;
		}
		pieceScale = scale;
	}

	public float GetScale()
	{
		return pieceScale;
	}

	public void PaintingTint(bool enable)
	{
		if (enable)
		{
			paintingCount++;
			if (paintingCount == 1)
			{
				RefreshTint();
			}
		}
		else
		{
			paintingCount--;
			if (paintingCount == 0)
			{
				RefreshTint();
			}
		}
	}

	public void PotentialGrab(bool enable)
	{
		if (enable)
		{
			potentialGrabCount++;
			if (potentialGrabCount == 1 && potentialGrabChildCount == 0)
			{
				RefreshTint();
			}
		}
		else
		{
			potentialGrabCount--;
			if (potentialGrabCount == 0 && potentialGrabChildCount == 0)
			{
				RefreshTint();
			}
		}
	}

	public static void PotentialGrabChildren(BuilderPiece piece, bool enable)
	{
		BuilderPiece builderPiece = piece.firstChildPiece;
		while (builderPiece != null)
		{
			if (enable)
			{
				builderPiece.potentialGrabChildCount++;
				if (builderPiece.potentialGrabChildCount == 1 && builderPiece.potentialGrabCount == 0)
				{
					builderPiece.RefreshTint();
				}
			}
			else
			{
				builderPiece.potentialGrabChildCount--;
				if (builderPiece.potentialGrabChildCount == 0 && builderPiece.potentialGrabCount == 0)
				{
					builderPiece.RefreshTint();
				}
			}
			PotentialGrabChildren(builderPiece, enable);
			builderPiece = builderPiece.nextSiblingPiece;
		}
	}

	private void RefreshTint()
	{
		if (potentialGrabCount > 0 || potentialGrabChildCount > 0)
		{
			SetTint(tableOwner.potentialGrabTint);
			return;
		}
		if (paintingCount > 0)
		{
			SetTint(tableOwner.paintingTint);
			return;
		}
		switch (state)
		{
		case State.OnShelf:
		case State.OnConveyor:
			SetTint(tableOwner.shelfTint);
			break;
		case State.Grabbed:
		case State.GrabbedLocal:
		case State.AttachedToArm:
			SetTint(tableOwner.grabbedTint);
			break;
		case State.AttachedToDropped:
		case State.Dropped:
			SetTint(tableOwner.droppedTint);
			break;
		default:
			SetTint(tableOwner.defaultTint);
			break;
		}
	}

	private void SetTint(float tint)
	{
		if (tint != this.tint)
		{
			this.tint = tint;
			tableOwner.builderRenderer.SetPieceTint(this, tint);
		}
	}

	public void SetParentPiece(int newAttachIndex, BuilderPiece newParentPiece, int newParentAttachIndex)
	{
		if (parentHeld != null)
		{
			Debug.LogErrorFormat(newParentPiece.gameObject, "Cannot attach to piece {0} while already held", (newParentPiece == null) ? null : newParentPiece.gameObject.name);
			return;
		}
		RemovePieceFromParent(this);
		attachIndex = newAttachIndex;
		parentPiece = newParentPiece;
		parentAttachIndex = newParentAttachIndex;
		AddPieceToParent(this);
		Transform parent = null;
		if (newParentPiece != null)
		{
			parent = ((newParentAttachIndex < 0) ? newParentPiece.transform : newParentPiece.gridPlanes[newParentAttachIndex].transform);
		}
		base.transform.SetParent(parent, worldPositionStays: true);
		requestedParentPiece = null;
		tableOwner.UpdatePieceData(this);
	}

	public void ClearParentPiece(bool ignoreSnaps = false)
	{
		if (parentPiece == null)
		{
			if (!ignoreSnaps)
			{
				RemoveOverlapsWithDifferentPieceRoot(this, this, tableOwner.builderPool);
			}
			return;
		}
		_ = parentPiece;
		RemovePieceFromParent(this);
		attachIndex = -1;
		parentPiece = null;
		parentAttachIndex = -1;
		base.transform.SetParent(null, worldPositionStays: true);
		requestedParentPiece = null;
		tableOwner.UpdatePieceData(this);
		if (!ignoreSnaps)
		{
			RemoveOverlapsWithDifferentPieceRoot(this, GetRootPiece(), tableOwner.builderPool);
		}
	}

	public static void RemoveOverlapsWithDifferentPieceRoot(BuilderPiece piece, BuilderPiece root, BuilderPool pool)
	{
		for (int i = 0; i < piece.gridPlanes.Count; i++)
		{
			piece.gridPlanes[i].RemoveSnapsWithDifferentRoot(root, pool);
		}
		BuilderPiece builderPiece = piece.firstChildPiece;
		while (builderPiece != null)
		{
			RemoveOverlapsWithDifferentPieceRoot(builderPiece, root, pool);
			builderPiece = builderPiece.nextSiblingPiece;
		}
	}

	private void AddPieceToParent(BuilderPiece piece)
	{
		BuilderPiece builderPiece = piece.parentPiece;
		if (!(builderPiece == null))
		{
			nextSiblingPiece = builderPiece.firstChildPiece;
			builderPiece.firstChildPiece = piece;
			if (piece.parentAttachIndex >= 0 && piece.parentAttachIndex < builderPiece.gridPlanes.Count)
			{
				builderPiece.gridPlanes[piece.parentAttachIndex].ChangeChildPieceCount(1 + piece.GetChildCount());
			}
		}
	}

	private static void RemovePieceFromParent(BuilderPiece piece)
	{
		BuilderPiece builderPiece = piece.parentPiece;
		if (builderPiece == null)
		{
			return;
		}
		BuilderPiece builderPiece2 = builderPiece.firstChildPiece;
		if (builderPiece2 == null)
		{
			Debug.LogErrorFormat("Parent {0} of piece {1} doesn't have any children", builderPiece.name, piece.name);
		}
		bool flag = false;
		if (builderPiece2 == piece)
		{
			builderPiece.firstChildPiece = builderPiece2.nextSiblingPiece;
			flag = true;
		}
		else
		{
			while (builderPiece2 != null)
			{
				if (builderPiece2.nextSiblingPiece == piece)
				{
					builderPiece2.nextSiblingPiece = piece.nextSiblingPiece;
					piece.nextSiblingPiece = null;
					flag = true;
					break;
				}
				builderPiece2 = builderPiece2.nextSiblingPiece;
			}
		}
		if (!flag)
		{
			Debug.LogErrorFormat("Parent {0} of piece {1} doesn't have the piece a child", builderPiece.name, piece.name);
		}
		else if (piece.parentAttachIndex >= 0 && piece.parentAttachIndex < builderPiece.gridPlanes.Count)
		{
			builderPiece.gridPlanes[piece.parentAttachIndex].ChangeChildPieceCount(-1 * (1 + piece.GetChildCount()));
		}
	}

	public void SetParentHeld(Transform parentHeld, int heldByPlayerActorNumber, bool heldInLeftHand)
	{
		if (parentPiece != null)
		{
			Debug.LogErrorFormat(parentPiece.gameObject, "Cannot hold while already attached to piece {0}", parentPiece.gameObject.name);
			return;
		}
		this.heldByPlayerActorNumber = heldByPlayerActorNumber;
		this.parentHeld = parentHeld;
		this.heldInLeftHand = heldInLeftHand;
		base.transform.SetParent(parentHeld);
		tableOwner.UpdatePieceData(this);
		if (heldByPlayerActorNumber != -1)
		{
			OnGrabbedAsRoot();
		}
		else
		{
			OnReleasedAsRoot();
		}
	}

	public void ClearParentHeld()
	{
		if (!(parentHeld == null))
		{
			if (isArmShelf && armShelf != null)
			{
				armShelf.piece = null;
				armShelf = null;
			}
			heldByPlayerActorNumber = -1;
			parentHeld = null;
			heldInLeftHand = false;
			base.transform.SetParent(parentHeld);
			tableOwner.UpdatePieceData(this);
			OnReleasedAsRoot();
		}
	}

	public bool IsHeldLocal()
	{
		if (heldByPlayerActorNumber != -1)
		{
			return heldByPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
		}
		return false;
	}

	public bool IsHeldBy(int actorNumber)
	{
		if (actorNumber != -1)
		{
			return heldByPlayerActorNumber == actorNumber;
		}
		return false;
	}

	public bool IsHeldInLeftHand()
	{
		return heldInLeftHand;
	}

	public static bool IsDroppedState(State state)
	{
		if (state != State.Dropped && state != State.AttachedToDropped && state != State.OnShelf)
		{
			return state == State.OnConveyor;
		}
		return true;
	}

	public void SetActivateTimeStamp(int timeStamp)
	{
		activatedTimeStamp = timeStamp;
		BuilderPiece builderPiece = firstChildPiece;
		while (builderPiece != null)
		{
			builderPiece.SetActivateTimeStamp(timeStamp);
			builderPiece = builderPiece.nextSiblingPiece;
		}
	}

	public void SetState(State newState, bool force = false)
	{
		if (newState == this.state && !force)
		{
			if (newState == State.Grabbed)
			{
				int expectedGrabCollisionLayer = GetExpectedGrabCollisionLayer();
				if (currentColliderLayer != expectedGrabCollisionLayer)
				{
					SetColliderLayers(colliders, expectedGrabCollisionLayer);
					SetChildrenCollisionLayer(expectedGrabCollisionLayer);
				}
			}
			return;
		}
		if (newState == State.Dropped && this.state != State.Dropped)
		{
			tableOwner.AddPieceToDropList(this);
		}
		else if (this.state == State.Dropped && newState != State.Dropped)
		{
			tableOwner.RemovePieceFromDropList(this);
		}
		State state = this.state;
		this.state = newState;
		if (pieceDataIndex >= 0)
		{
			tableOwner.UpdatePieceData(this);
		}
		switch (this.state)
		{
		case State.AttachedAndPlaced:
			SetCollidersEnabled(colliders, enabled: true);
			SetBehavioursEnabled(onlyWhenPlacedBehaviours, enabled: true);
			SetActive(onlyWhenPlaced, active: true);
			SetActive(onlyWhenNotPlaced, active: false);
			SetKinematic(kinematic: true);
			SetColliderLayers(colliders, BuilderTable.placedLayer);
			SetChildrenState(State.AttachedAndPlaced, force);
			SetStatic(isStatic: false, force || areMeshesToggledOnPlace);
			SetPieceActive(pieceComponents, active: true);
			RefreshTint();
			break;
		case State.AttachedToDropped:
			SetCollidersEnabled(colliders, enabled: true);
			SetBehavioursEnabled(onlyWhenPlacedBehaviours, enabled: false);
			SetActive(onlyWhenPlaced, active: false);
			SetActive(onlyWhenNotPlaced, active: true);
			SetKinematic(kinematic: true);
			SetColliderLayers(colliders, BuilderTable.droppedLayer);
			SetChildrenState(State.AttachedToDropped, force);
			SetStatic(isStatic: false, force);
			SetPieceActive(pieceComponents, active: false);
			RefreshTint();
			break;
		case State.Grabbed:
		{
			SetCollidersEnabled(colliders, enabled: true);
			SetBehavioursEnabled(onlyWhenPlacedBehaviours, enabled: false);
			SetActive(onlyWhenPlaced, active: false);
			SetActive(onlyWhenNotPlaced, active: true);
			SetKinematic(kinematic: true);
			int expectedGrabCollisionLayer2 = GetExpectedGrabCollisionLayer();
			SetColliderLayers(colliders, expectedGrabCollisionLayer2);
			SetChildrenState(State.Grabbed, force);
			SetStatic(isStatic: false, force || (areMeshesToggledOnPlace && state == State.AttachedAndPlaced));
			SetPieceActive(pieceComponents, active: false);
			SetActivateTimeStamp(0);
			RefreshTint();
			forcedFrozen = false;
			break;
		}
		case State.GrabbedLocal:
			SetCollidersEnabled(colliders, enabled: true);
			SetBehavioursEnabled(onlyWhenPlacedBehaviours, enabled: false);
			SetActive(onlyWhenPlaced, active: false);
			SetActive(onlyWhenNotPlaced, active: true);
			SetKinematic(kinematic: true);
			SetColliderLayers(colliders, BuilderTable.heldLayerLocal);
			SetChildrenState(State.GrabbedLocal, force);
			SetStatic(isStatic: false, force || (areMeshesToggledOnPlace && state == State.AttachedAndPlaced));
			SetPieceActive(pieceComponents, active: false);
			SetActivateTimeStamp(0);
			RefreshTint();
			forcedFrozen = false;
			break;
		case State.Dropped:
			ClearCollisionHistory();
			SetCollidersEnabled(colliders, enabled: true);
			SetBehavioursEnabled(onlyWhenPlacedBehaviours, enabled: false);
			SetActive(onlyWhenPlaced, active: false);
			SetActive(onlyWhenNotPlaced, active: true);
			SetKinematic(kinematic: false);
			SetColliderLayers(colliders, BuilderTable.droppedLayer);
			SetChildrenState(State.AttachedToDropped, force);
			SetStatic(isStatic: false, force);
			SetPieceActive(pieceComponents, active: false);
			RefreshTint();
			break;
		case State.OnShelf:
			SetCollidersEnabled(colliders, enabled: true);
			SetBehavioursEnabled(onlyWhenPlacedBehaviours, enabled: false);
			SetActive(onlyWhenPlaced, active: false);
			SetActive(onlyWhenNotPlaced, active: true);
			SetKinematic(kinematic: true);
			SetColliderLayers(colliders, BuilderTable.droppedLayer);
			SetChildrenState(State.OnShelf, force);
			SetStatic(isStatic: true, force);
			SetPieceActive(pieceComponents, active: false);
			RefreshTint();
			break;
		case State.Displayed:
			SetCollidersEnabled(colliders, enabled: false);
			SetBehavioursEnabled(onlyWhenPlacedBehaviours, enabled: false);
			SetActive(onlyWhenPlaced, active: false);
			SetActive(onlyWhenNotPlaced, active: true);
			SetKinematic(kinematic: true);
			SetChildrenState(State.Displayed, force);
			SetStatic(isStatic: false, force);
			SetPieceActive(pieceComponents, active: false);
			RefreshTint();
			break;
		case State.OnConveyor:
			SetCollidersEnabled(colliders, enabled: true);
			SetBehavioursEnabled(onlyWhenPlacedBehaviours, enabled: false);
			SetActive(onlyWhenPlaced, active: false);
			SetActive(onlyWhenNotPlaced, active: true);
			SetKinematic(kinematic: true);
			SetColliderLayers(colliders, BuilderTable.droppedLayer);
			SetChildrenState(State.OnConveyor, force);
			SetStatic(isStatic: false, force);
			SetPieceActive(pieceComponents, active: false);
			RefreshTint();
			break;
		case State.AttachedToArm:
			SetCollidersEnabled(colliders, enabled: true);
			SetBehavioursEnabled(onlyWhenPlacedBehaviours, enabled: false);
			SetActive(onlyWhenPlaced, active: false);
			SetActive(onlyWhenNotPlaced, active: true);
			SetKinematic(kinematic: true);
			SetColliderLayers(colliders, BuilderTable.heldLayerLocal);
			SetChildrenState(State.AttachedToArm, force);
			SetStatic(isStatic: false, force);
			SetPieceActive(pieceComponents, active: false);
			RefreshTint();
			break;
		case State.None:
			SetCollidersEnabled(colliders, enabled: false);
			SetBehavioursEnabled(onlyWhenPlacedBehaviours, enabled: false);
			SetActive(onlyWhenPlaced, active: false);
			SetActive(onlyWhenNotPlaced, active: true);
			SetKinematic(kinematic: true, destroyImmediate: false);
			SetColliderLayers(colliders, BuilderTable.droppedLayer);
			SetChildrenState(State.None, force);
			tableOwner.builderRenderer.RemovePiece(this);
			isStatic = true;
			SetPieceActive(pieceComponents, active: false);
			RefreshTint();
			break;
		}
	}

	public void OnGrabbedAsRoot()
	{
		if (!isArmShelf && heldByPlayerActorNumber != NetworkSystem.Instance.LocalPlayer.ActorNumber && !listeningToHandLinks)
		{
			TakeMyHand_HandLink.OnHandLinkChanged = (Action)Delegate.Combine(TakeMyHand_HandLink.OnHandLinkChanged, new Action(UpdateGrabbedPieceCollisionLayer));
			listeningToHandLinks = true;
		}
	}

	public void OnReleasedAsRoot()
	{
		if (!isArmShelf && listeningToHandLinks)
		{
			TakeMyHand_HandLink.OnHandLinkChanged = (Action)Delegate.Remove(TakeMyHand_HandLink.OnHandLinkChanged, new Action(UpdateGrabbedPieceCollisionLayer));
			listeningToHandLinks = false;
		}
	}

	public void SetKinematic(bool kinematic, bool destroyImmediate = true)
	{
		if (kinematic && rigidBody != null)
		{
			if (destroyImmediate)
			{
				UnityEngine.Object.DestroyImmediate(rigidBody);
				rigidBody = null;
			}
			else
			{
				UnityEngine.Object.Destroy(rigidBody);
				rigidBody = null;
			}
		}
		else if (!kinematic && rigidBody == null)
		{
			rigidBody = base.gameObject.GetComponent<Rigidbody>();
			if (rigidBody != null)
			{
				Debug.LogErrorFormat("We should never already have a rigid body here {0} {1}", pieceId, pieceType);
			}
			if (rigidBody == null)
			{
				rigidBody = base.gameObject.AddComponent<Rigidbody>();
			}
			if (rigidBody != null)
			{
				rigidBody.isKinematic = kinematic;
			}
		}
		if (rigidBody != null)
		{
			rigidBody.mass = 1f;
		}
	}

	public void ClearCollisionHistory()
	{
		if (collisionEnterHistory == null)
		{
			collisionEnterHistory = new float[collisionEnterLimit];
		}
		for (int i = 0; i < collisionEnterLimit; i++)
		{
			collisionEnterHistory[i] = float.MinValue;
		}
		collidersEntered.Clear();
		oldCollisionTimeIndex = 0;
		forcedFrozen = false;
	}

	private void OnCollisionEnter(Collision other)
	{
		if (state != State.Dropped || forcedFrozen)
		{
			return;
		}
		BuilderPieceCollider component = other.collider.GetComponent<BuilderPieceCollider>();
		if (!(component != null))
		{
			return;
		}
		BuilderPiece piece = component.piece;
		if ((piece.state == State.AttachedAndPlaced || piece.forcedFrozen) && !collidersEntered.Add(other.collider.GetInstanceID()))
		{
			if (collisionEnterHistory[oldCollisionTimeIndex] > Time.time)
			{
				tableOwner.FreezeDroppedPiece(this);
				return;
			}
			collisionEnterHistory[oldCollisionTimeIndex] = Time.time + collisionEnterCooldown;
			oldCollisionTimeIndex = ++oldCollisionTimeIndex % collisionEnterLimit;
		}
	}

	public int GetExpectedGrabCollisionLayer()
	{
		if (heldByPlayerActorNumber != -1)
		{
			if (!GorillaTagger.Instance.offlineVRRig.IsInHandHoldChainWithOtherPlayer(heldByPlayerActorNumber))
			{
				return BuilderTable.heldLayer;
			}
			return BuilderTable.heldLayerLocal;
		}
		if (parentPiece != null)
		{
			return parentPiece.currentColliderLayer;
		}
		return BuilderTable.heldLayer;
	}

	public void UpdateGrabbedPieceCollisionLayer()
	{
		int expectedGrabCollisionLayer = GetExpectedGrabCollisionLayer();
		if (currentColliderLayer != expectedGrabCollisionLayer)
		{
			SetColliderLayers(colliders, expectedGrabCollisionLayer);
			SetChildrenCollisionLayer(expectedGrabCollisionLayer);
		}
	}

	private void SetChildrenCollisionLayer(int layer)
	{
		BuilderPiece builderPiece = firstChildPiece;
		while (builderPiece != null)
		{
			builderPiece.SetColliderLayers(builderPiece.colliders, layer);
			builderPiece.SetChildrenCollisionLayer(layer);
			builderPiece = builderPiece.nextSiblingPiece;
		}
	}

	public void SetStatic(bool isStatic, bool force = false)
	{
		isStatic = true;
		if (this.isStatic != isStatic || force)
		{
			SetDirectRenderersVisible(visible: true);
			tableOwner.builderRenderer.RemovePiece(this);
			this.isStatic = isStatic;
			if (areMeshesToggledOnPlace)
			{
				FindActiveRenderers();
			}
			tableOwner.builderRenderer.AddPiece(this);
			SetDirectRenderersVisible(tableOwner.IsInBuilderZone());
		}
	}

	private void FindActiveRenderers()
	{
		if (renderingDirect.Count > 0)
		{
			foreach (MeshRenderer item in renderingDirect)
			{
				item.enabled = true;
			}
		}
		renderingDirect.Clear();
		tempRenderers.Clear();
		GetComponentsInChildren(includeInactive: false, tempRenderers);
		foreach (MeshRenderer tempRenderer in tempRenderers)
		{
			if (tempRenderer.enabled)
			{
				renderingDirect.Add(tempRenderer);
			}
		}
	}

	public void SetDirectRenderersVisible(bool visible)
	{
		if (renderingDirect == null || renderingDirect.Count <= 0)
		{
			return;
		}
		foreach (MeshRenderer item in renderingDirect)
		{
			item.enabled = visible;
		}
	}

	private void SetChildrenState(State newState, bool force)
	{
		BuilderPiece builderPiece = firstChildPiece;
		while (builderPiece != null)
		{
			builderPiece.SetState(newState, force);
			builderPiece = builderPiece.nextSiblingPiece;
		}
	}

	public void OnCreate()
	{
		for (int i = 0; i < pieceComponents.Count; i++)
		{
			pieceComponents[i].OnPieceCreate(pieceType, pieceId);
		}
	}

	public void OnPlacementDeserialized()
	{
		for (int i = 0; i < pieceComponents.Count; i++)
		{
			pieceComponents[i].OnPiecePlacementDeserialized();
		}
	}

	public void PlayPlacementFx()
	{
		PlayFX(fXInfo.placeVFX);
	}

	public void PlayDisconnectFx()
	{
		PlayFX(fXInfo.disconnectVFX);
	}

	public void PlayGrabbedFx()
	{
		PlayFX(fXInfo.grabbedVFX);
	}

	public void PlayTooHeavyFx()
	{
		PlayFX(fXInfo.tooHeavyVFX);
	}

	public void PlayLocationLockFx()
	{
		PlayFX(fXInfo.locationLockVFX);
	}

	public void PlayRecycleFx()
	{
		PlayFX(fXInfo.recycleVFX);
	}

	private void PlayFX(GameObject fx)
	{
		ObjectPools.instance.Instantiate(fx, base.transform.position);
	}

	public static BuilderPiece GetBuilderPieceFromCollider(Collider collider)
	{
		if (collider == null)
		{
			return null;
		}
		BuilderPieceCollider component = collider.GetComponent<BuilderPieceCollider>();
		if (!(component == null))
		{
			return component.piece;
		}
		return null;
	}

	public static BuilderPiece GetBuilderPieceFromTransform(Transform transform)
	{
		while (transform != null)
		{
			BuilderPiece component = transform.GetComponent<BuilderPiece>();
			if (component != null)
			{
				return component;
			}
			transform = transform.parent;
		}
		return null;
	}

	public static void MakePieceRoot(BuilderPiece piece)
	{
		if (!(piece == null) && !(piece.parentPiece == null) && !piece.parentPiece.isBuiltIntoTable)
		{
			MakePieceRoot(piece.parentPiece);
			int newAttachIndex = piece.parentAttachIndex;
			int newParentAttachIndex = piece.attachIndex;
			BuilderPiece builderPiece = piece.parentPiece;
			bool ignoreSnaps = true;
			piece.ClearParentPiece(ignoreSnaps);
			builderPiece.SetParentPiece(newAttachIndex, piece, newParentAttachIndex);
		}
	}

	public BuilderPiece GetRootPiece()
	{
		BuilderPiece builderPiece = this;
		while (builderPiece.parentPiece != null && !builderPiece.parentPiece.isBuiltIntoTable)
		{
			builderPiece = builderPiece.parentPiece;
		}
		return builderPiece;
	}

	public bool IsPrivatePlot()
	{
		return isPrivatePlot;
	}

	public bool TryGetPlotComponent(out BuilderPiecePrivatePlot plot)
	{
		plot = plotComponent;
		if (!isPrivatePlot)
		{
			return false;
		}
		return true;
	}

	public static bool CanPlayerAttachPieceToPiece(int playerActorNumber, BuilderPiece attachingPiece, BuilderPiece attachToPiece)
	{
		if (attachToPiece.state != State.AttachedAndPlaced && !attachToPiece.IsPrivatePlot() && attachToPiece.state != State.AttachedToArm)
		{
			return true;
		}
		BuilderPiece attachedBuiltInPiece = attachToPiece.GetAttachedBuiltInPiece();
		if (attachedBuiltInPiece == null || (!attachedBuiltInPiece.isPrivatePlot && !attachedBuiltInPiece.isArmShelf))
		{
			return true;
		}
		if (attachedBuiltInPiece.isArmShelf)
		{
			if (attachedBuiltInPiece.heldByPlayerActorNumber == playerActorNumber && attachedBuiltInPiece.armShelf != null)
			{
				return attachedBuiltInPiece.armShelf.CanAttachToArmPiece();
			}
			return false;
		}
		if (attachedBuiltInPiece.TryGetPlotComponent(out var plot))
		{
			if (plot.CanPlayerAttachToPlot(playerActorNumber))
			{
				return plot.IsChainUnderCapacity(attachingPiece);
			}
			return false;
		}
		return true;
	}

	public bool CanPlayerGrabPiece(int actorNumber, Vector3 worldPosition)
	{
		if (state != State.AttachedAndPlaced && !isPrivatePlot)
		{
			return true;
		}
		BuilderPiece attachedBuiltInPiece = GetAttachedBuiltInPiece();
		if (attachedBuiltInPiece == null || !attachedBuiltInPiece.isPrivatePlot)
		{
			return true;
		}
		if (attachedBuiltInPiece.TryGetPlotComponent(out var plot))
		{
			if (plot.CanPlayerGrabFromPlot(actorNumber, worldPosition))
			{
				return true;
			}
			return tableOwner.IsLocationWithinSharedBuildArea(worldPosition);
		}
		return true;
	}

	public bool IsPieceMoving()
	{
		if (state != State.AttachedAndPlaced)
		{
			return false;
		}
		if (attachPlayerToPiece)
		{
			return true;
		}
		if (attachIndex < 0 || attachIndex >= gridPlanes.Count)
		{
			return false;
		}
		if (gridPlanes[attachIndex].IsAttachedToMovingGrid())
		{
			return true;
		}
		foreach (BuilderAttachGridPlane gridPlane in gridPlanes)
		{
			if (gridPlane.isMoving)
			{
				return true;
			}
		}
		return false;
	}

	public BuilderPiece GetAttachedBuiltInPiece()
	{
		if (isBuiltIntoTable)
		{
			return this;
		}
		if (state != State.AttachedAndPlaced)
		{
			return null;
		}
		BuilderPiece rootPiece = GetRootPiece();
		if (rootPiece.parentPiece != null)
		{
			rootPiece = rootPiece.parentPiece;
		}
		if (rootPiece.isBuiltIntoTable)
		{
			return rootPiece;
		}
		return null;
	}

	public int GetChainCostAndCount(int[] costArray)
	{
		for (int i = 0; i < costArray.Length; i++)
		{
			costArray[i] = 0;
		}
		foreach (BuilderResourceQuantity quantity in cost.quantities)
		{
			if (quantity.type >= BuilderResourceType.Basic && quantity.type < BuilderResourceType.Count)
			{
				costArray[(int)quantity.type] += quantity.count;
			}
		}
		return 1 + GetChildCountAndCost(costArray);
	}

	public int GetChildCountAndCost(int[] costArray)
	{
		int num = 0;
		BuilderPiece builderPiece = firstChildPiece;
		while (builderPiece != null)
		{
			num++;
			foreach (BuilderResourceQuantity quantity in builderPiece.cost.quantities)
			{
				if (quantity.type >= BuilderResourceType.Basic && quantity.type < BuilderResourceType.Count)
				{
					costArray[(int)quantity.type] += quantity.count;
				}
			}
			num += builderPiece.GetChildCountAndCost(costArray);
			builderPiece = builderPiece.nextSiblingPiece;
		}
		return num;
	}

	public int GetChildCount()
	{
		int num = 0;
		foreach (BuilderAttachGridPlane gridPlane in gridPlanes)
		{
			num += gridPlane.GetChildCount();
		}
		return num;
	}

	public void GetChainCost(int[] costArray)
	{
		for (int i = 0; i < costArray.Length; i++)
		{
			costArray[i] = 0;
		}
		foreach (BuilderResourceQuantity quantity in cost.quantities)
		{
			if (quantity.type >= BuilderResourceType.Basic && quantity.type < BuilderResourceType.Count)
			{
				costArray[(int)quantity.type] += quantity.count;
			}
		}
		AddChildCost(costArray);
	}

	public void AddChildCost(int[] costArray)
	{
		int num = 0;
		BuilderPiece builderPiece = firstChildPiece;
		while (builderPiece != null)
		{
			num++;
			foreach (BuilderResourceQuantity quantity in builderPiece.cost.quantities)
			{
				if (quantity.type >= BuilderResourceType.Basic && quantity.type < BuilderResourceType.Count)
				{
					costArray[(int)quantity.type] += quantity.count;
				}
			}
			builderPiece.AddChildCost(costArray);
			builderPiece = builderPiece.nextSiblingPiece;
		}
	}

	public void BumpTwistToPositionRotation(byte twist, sbyte xOffset, sbyte zOffset, int potentialAttachIndex, BuilderAttachGridPlane potentialParentGridPlane, out Vector3 localPosition, out Quaternion localRotation, out Vector3 worldPosition, out Quaternion worldRotation)
	{
		float gridSize = tableOwner.gridSize;
		BuilderAttachGridPlane builderAttachGridPlane = gridPlanes[potentialAttachIndex];
		bool num = (long)(twist % 2) == 1;
		Transform center = potentialParentGridPlane.center;
		Vector3 position = center.position;
		Quaternion rotation = center.rotation;
		float num2 = (num ? builderAttachGridPlane.lengthOffset : builderAttachGridPlane.widthOffset);
		float num3 = (num ? builderAttachGridPlane.widthOffset : builderAttachGridPlane.lengthOffset);
		float num4 = num2 - potentialParentGridPlane.widthOffset;
		float num5 = num3 - potentialParentGridPlane.lengthOffset;
		Quaternion quaternion = Quaternion.Euler(0f, (float)(int)twist * 90f, 0f);
		Quaternion quaternion2 = rotation * quaternion;
		float x = (float)xOffset * gridSize + num4;
		float z = (float)zOffset * gridSize + num5;
		Vector3 vector = new Vector3(x, 0f, z);
		Vector3 vector2 = position + rotation * vector;
		Transform center2 = builderAttachGridPlane.center;
		Quaternion quaternion3 = quaternion2 * Quaternion.Inverse(center2.localRotation);
		Vector3 vector3 = base.transform.InverseTransformPoint(center2.position);
		Vector3 vector4 = vector2 - quaternion3 * vector3;
		localPosition = potentialParentGridPlane.transform.InverseTransformPoint(vector4);
		localRotation = quaternion * Quaternion.Inverse(center2.localRotation);
		worldPosition = vector4;
		worldRotation = quaternion3;
	}

	public Quaternion TwistToLocalRotation(byte twist, int potentialAttachIndex)
	{
		float y = 90f * (float)(int)twist;
		Quaternion quaternion = Quaternion.Euler(0f, y, 0f);
		if (potentialAttachIndex < 0 || potentialAttachIndex >= gridPlanes.Count)
		{
			return quaternion;
		}
		BuilderAttachGridPlane builderAttachGridPlane = gridPlanes[potentialAttachIndex];
		Transform transform = ((builderAttachGridPlane.center != null) ? builderAttachGridPlane.center : builderAttachGridPlane.transform);
		return quaternion * Quaternion.Inverse(transform.localRotation);
	}

	public int GetPiecePlacement()
	{
		byte pieceTwist = GetPieceTwist();
		GetPieceBumpOffset(pieceTwist, out var xOffset, out var zOffset);
		return BuilderTable.PackPiecePlacement(pieceTwist, xOffset, zOffset);
	}

	public byte GetPieceTwist()
	{
		if (attachIndex == -1)
		{
			return 0;
		}
		Quaternion localRotation = base.transform.localRotation;
		BuilderAttachGridPlane builderAttachGridPlane = gridPlanes[attachIndex];
		Quaternion quaternion = localRotation * builderAttachGridPlane.transform.localRotation;
		float num = 0.866f;
		Vector3 lhs = quaternion * Vector3.forward;
		float num2 = Vector3.Dot(lhs, Vector3.forward);
		float num3 = Vector3.Dot(lhs, Vector3.right);
		bool flag = Mathf.Abs(num2) > num;
		bool flag2 = Mathf.Abs(num3) > num;
		if (!(flag || flag2))
		{
			return 0;
		}
		uint num4 = 0u;
		num4 = ((!flag) ? ((num3 > 0f) ? 1u : 3u) : ((!(num2 > 0f)) ? 2u : 0u));
		return (byte)num4;
	}

	public void GetPieceBumpOffset(byte twist, out sbyte xOffset, out sbyte zOffset)
	{
		if (attachIndex == -1 || parentPiece == null)
		{
			xOffset = 0;
			zOffset = 0;
			return;
		}
		float gridSize = tableOwner.gridSize;
		BuilderAttachGridPlane builderAttachGridPlane = gridPlanes[attachIndex];
		BuilderAttachGridPlane builderAttachGridPlane2 = parentPiece.gridPlanes[parentAttachIndex];
		bool num = (long)(twist % 2) == 1;
		float num2 = (num ? builderAttachGridPlane.lengthOffset : builderAttachGridPlane.widthOffset);
		float num3 = (num ? builderAttachGridPlane.widthOffset : builderAttachGridPlane.lengthOffset);
		float num4 = num2 - builderAttachGridPlane2.widthOffset;
		float num5 = num3 - builderAttachGridPlane2.lengthOffset;
		Vector3 position = builderAttachGridPlane.center.position;
		Vector3 position2 = builderAttachGridPlane2.center.position;
		Vector3 vector = Quaternion.Inverse(builderAttachGridPlane2.center.rotation) * (position - position2);
		xOffset = (sbyte)Mathf.RoundToInt((vector.x - num4) / gridSize);
		zOffset = (sbyte)Mathf.RoundToInt((vector.z - num5) / gridSize);
	}
}
