using System;
using System.Collections.Generic;
using GorillaTagScripts;
using Photon.Pun;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.XR;

public class BuilderPieceInteractor : MonoBehaviour
{
	public enum HandType
	{
		Invalid = -1,
		Left,
		Right
	}

	public enum HandState
	{
		Invalid = -1,
		Empty,
		Grabbed,
		PotentialGrabbed,
		WaitForGrabbed,
		WaitingForSnap,
		WaitingForUnSnap
	}

	[OnEnterPlay_SetNull]
	public static volatile BuilderPieceInteractor instance;

	[OnEnterPlay_Set(false)]
	public static bool hasInstance;

	public EquipmentInteractor equipmentInteractor;

	private const int NUM_HANDS = 2;

	public GorillaVelocityEstimator velocityEstimatorLeft;

	public GorillaVelocityEstimator velocityEstimatorRight;

	public BuilderLaserSight laserSightLeft;

	public BuilderLaserSight laserSightRight;

	public int maxHoldablePieceStackCount = 50;

	public List<GorillaVelocityEstimator> velocityEstimator;

	public List<HandState> handState;

	public List<BuilderPiece> heldPiece;

	public List<BuilderPiece> potentialHeldPiece;

	public List<float> potentialGrabbedOffsetDist;

	public List<Quaternion> heldInitialRot;

	public List<Quaternion> heldCurrentRot;

	public List<Vector3> heldInitialPos;

	public List<Vector3> heldCurrentPos;

	public List<BuilderPotentialPlacement> delayedPotentialPlacement;

	public List<float> delayedPlacementTime;

	public List<BuilderPotentialPlacement> prevPotentialPlacement;

	public List<BuilderLaserSight> laserSight;

	public int[] heldChainLength;

	public List<int[]> heldChainCost;

	private static List<BuilderPotentialPlacement>[] allPotentialPlacements;

	private static NativeList<BuilderGridPlaneData>[] handGridPlaneData;

	private static NativeList<BuilderPieceData>[] handPieceData;

	private static NativeList<BuilderGridPlaneData>[] localAttachableGridPlaneData;

	private static NativeList<BuilderPieceData>[] localAttachablePieceData;

	private JobHandle findNearbyJobHandle;

	public List<GameObject> collisionDisabledPiecesLeft = new List<GameObject>();

	public List<GameObject> collisionDisabledPiecesRight = new List<GameObject>();

	public const int MAX_SPHERE_CHECK_RESULTS = 1024;

	public NativeArray<OverlapSphereCommand> checkPiecesInSphere;

	public NativeArray<ColliderHit> checkPiecesInSphereResults;

	public JobHandle checkNearbyPiecesHandle;

	public const float GRAB_CAST_RADIUS = 0.0375f;

	public const int MAX_GRAB_CAST_RESULTS = 64;

	public NativeArray<SpherecastCommand> grabSphereCast;

	public NativeArray<RaycastHit> grabSphereCastResults;

	public JobHandle findPiecesToGrab;

	private RaycastHit emptyRaycastHit;

	public BuilderBumpGlow glowBumpPrefab;

	public List<List<BuilderBumpGlow>> glowBumps;

	private const int MAX_GRID_PLANES = 8192;

	private bool isRigSmall;

	private BuilderTable currentTable;

	private static HashSet<BuilderPiece> tempPieceSet = new HashSet<BuilderPiece>(512);

	private static RaycastHit[] tempHitResults = new RaycastHit[64];

	private const float PIECE_DISTANCE_DISABLE = 0.15f;

	private const float PIECE_DISTANCE_ENABLE = 0.2f;

	private static Collider[] tempDisableColliders = new Collider[128];

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			hasInstance = true;
		}
		else if (instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		velocityEstimator = new List<GorillaVelocityEstimator>(2) { velocityEstimatorLeft, velocityEstimatorRight };
		laserSight = new List<BuilderLaserSight>(2) { laserSightLeft, laserSightRight };
		handState = new List<HandState>(2);
		heldPiece = new List<BuilderPiece>(2);
		potentialHeldPiece = new List<BuilderPiece>(2);
		potentialGrabbedOffsetDist = new List<float>(2);
		heldInitialRot = new List<Quaternion>(2);
		heldCurrentRot = new List<Quaternion>(2);
		heldInitialPos = new List<Vector3>(2);
		heldCurrentPos = new List<Vector3>(2);
		heldChainLength = new int[2];
		heldChainLength[0] = 0;
		heldChainLength[1] = 0;
		heldChainCost = new List<int[]>(2);
		for (int i = 0; i < 2; i++)
		{
			heldChainCost.Add(new int[3]);
		}
		allPotentialPlacements = new List<BuilderPotentialPlacement>[2];
		delayedPotentialPlacement = new List<BuilderPotentialPlacement>(2);
		delayedPlacementTime = new List<float>(2);
		prevPotentialPlacement = new List<BuilderPotentialPlacement>(2);
		glowBumps = new List<List<BuilderBumpGlow>>(2);
		for (int j = 0; j < 2; j++)
		{
			handState.Add(HandState.Empty);
			heldPiece.Add(null);
			potentialHeldPiece.Add(null);
			potentialGrabbedOffsetDist.Add(0f);
			heldInitialRot.Add(Quaternion.identity);
			heldCurrentRot.Add(Quaternion.identity);
			heldInitialPos.Add(Vector3.zero);
			heldCurrentPos.Add(Vector3.zero);
			delayedPotentialPlacement.Add(default(BuilderPotentialPlacement));
			delayedPlacementTime.Add(-1f);
			prevPotentialPlacement.Add(default(BuilderPotentialPlacement));
			allPotentialPlacements[j] = new List<BuilderPotentialPlacement>(128);
			glowBumps.Add(new List<BuilderBumpGlow>(1024));
		}
		checkPiecesInSphere = new NativeArray<OverlapSphereCommand>(2, Allocator.Persistent);
		checkPiecesInSphereResults = new NativeArray<ColliderHit>(2048, Allocator.Persistent);
		grabSphereCast = new NativeArray<SpherecastCommand>(2, Allocator.Persistent);
		grabSphereCastResults = new NativeArray<RaycastHit>(128, Allocator.Persistent);
		handGridPlaneData = new NativeList<BuilderGridPlaneData>[2];
		handPieceData = new NativeList<BuilderPieceData>[2];
		localAttachableGridPlaneData = new NativeList<BuilderGridPlaneData>[2];
		localAttachablePieceData = new NativeList<BuilderPieceData>[2];
		for (int k = 0; k < 2; k++)
		{
			handGridPlaneData[k] = new NativeList<BuilderGridPlaneData>(512, Allocator.Persistent);
			handPieceData[k] = new NativeList<BuilderPieceData>(512, Allocator.Persistent);
			localAttachableGridPlaneData[k] = new NativeList<BuilderGridPlaneData>(10240, Allocator.Persistent);
			localAttachablePieceData[k] = new NativeList<BuilderPieceData>(2560, Allocator.Persistent);
		}
	}

	public bool GetIsHolding(XRNode node)
	{
		if (heldPiece == null)
		{
			return false;
		}
		if (node == XRNode.LeftHand)
		{
			return heldPiece[0] != null;
		}
		return heldPiece[1] != null;
	}

	public void PreInteract()
	{
	}

	public void StartFindNearbyPieces()
	{
		VRRig offlineVRRig = GorillaTagger.Instance.offlineVRRig;
		if (BuilderTable.TryGetBuilderTableForZone(offlineVRRig.zoneEntity.currentZone, out var table) && table.isTableMutable)
		{
			QueryParameters queryParameters = new QueryParameters
			{
				layerMask = table.allPiecesMask
			};
			checkPiecesInSphere[0] = new OverlapSphereCommand(offlineVRRig.leftHand.overrideTarget.position, (handState[0] == HandState.Empty) ? 0.0375f : 1f, queryParameters);
			checkPiecesInSphere[1] = new OverlapSphereCommand(offlineVRRig.rightHand.overrideTarget.position, (handState[1] == HandState.Empty) ? 0.0375f : 1f, queryParameters);
			checkNearbyPiecesHandle = OverlapSphereCommand.ScheduleBatch(checkPiecesInSphere, checkPiecesInSphereResults, 1, 1024);
			for (int i = 0; i < 64; i++)
			{
				grabSphereCastResults[i] = emptyRaycastHit;
			}
			grabSphereCast[0] = new SpherecastCommand(offlineVRRig.leftHand.overrideTarget.position, 0.0375f, offlineVRRig.leftHand.overrideTarget.rotation * Vector3.right, queryParameters, 0.15f);
			grabSphereCast[1] = new SpherecastCommand(offlineVRRig.rightHand.overrideTarget.position, 0.0375f, offlineVRRig.rightHand.overrideTarget.rotation * -Vector3.right, queryParameters, 0.15f);
			findPiecesToGrab = SpherecastCommand.ScheduleBatch(grabSphereCast, grabSphereCastResults, 1, 64);
			JobHandle.ScheduleBatchedJobs();
		}
	}

	private void CalcLocalGridPlanes()
	{
		checkNearbyPiecesHandle.Complete();
		for (int i = 0; i < 2; i++)
		{
			if (handState[i] != HandState.Grabbed)
			{
				continue;
			}
			localAttachableGridPlaneData[i].Clear();
			localAttachablePieceData[i].Clear();
			tempPieceSet.Clear();
			if (!currentTable.IsInBuilderZone())
			{
				continue;
			}
			for (int j = 0; j < 1024; j++)
			{
				int index = i * 1024 + j;
				if (checkPiecesInSphereResults[index].instanceID == 0)
				{
					break;
				}
				BuilderPiece pieceInHand = heldPiece[i];
				BuilderPiece builderPieceFromCollider = BuilderPiece.GetBuilderPieceFromCollider(checkPiecesInSphereResults[index].collider);
				if (!(builderPieceFromCollider != null) || tempPieceSet.Contains(builderPieceFromCollider))
				{
					continue;
				}
				tempPieceSet.Add(builderPieceFromCollider);
				if (currentTable.CanPiecesPotentiallySnap(pieceInHand, builderPieceFromCollider))
				{
					int length = localAttachablePieceData[i].Length;
					localAttachablePieceData[i].Add(new BuilderPieceData(builderPieceFromCollider));
					for (int k = 0; k < builderPieceFromCollider.gridPlanes.Count; k++)
					{
						localAttachableGridPlaneData[i].Add(new BuilderGridPlaneData(builderPieceFromCollider.gridPlanes[k], length));
					}
				}
			}
		}
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			hasInstance = false;
			instance = null;
		}
		if (checkPiecesInSphere.IsCreated)
		{
			checkPiecesInSphere.Dispose();
		}
		if (checkPiecesInSphereResults.IsCreated)
		{
			checkPiecesInSphereResults.Dispose();
		}
		if (grabSphereCast.IsCreated)
		{
			grabSphereCast.Dispose();
		}
		if (grabSphereCastResults.IsCreated)
		{
			grabSphereCastResults.Dispose();
		}
		for (int i = 0; i < 2; i++)
		{
			if (handGridPlaneData[i].IsCreated)
			{
				handGridPlaneData[i].Dispose();
			}
			if (handPieceData[i].IsCreated)
			{
				handPieceData[i].Dispose();
			}
			if (localAttachableGridPlaneData[i].IsCreated)
			{
				localAttachableGridPlaneData[i].Dispose();
			}
			if (localAttachablePieceData[i].IsCreated)
			{
				localAttachablePieceData[i].Dispose();
			}
		}
	}

	public bool BlockSnowballCreation()
	{
		if (GorillaTagger.Instance == null)
		{
			return false;
		}
		if (!BuilderTable.TryGetBuilderTableForZone(GorillaTagger.Instance.offlineVRRig.zoneEntity.currentZone, out var table))
		{
			return false;
		}
		if (table.IsInBuilderZone() && table.isTableMutable && GorillaTagger.Instance.offlineVRRig.scaleFactor >= 0.99f)
		{
			return true;
		}
		return false;
	}

	public void OnLateUpdate()
	{
		VRRig offlineVRRig = GorillaTagger.Instance.offlineVRRig;
		if (!BuilderTable.TryGetBuilderTableForZone(offlineVRRig.zoneEntity.currentZone, out var table) || !table.isTableMutable)
		{
			return;
		}
		currentTable = table;
		CalcLocalGridPlanes();
		BodyDockPositions myBodyDockPositions = offlineVRRig.myBodyDockPositions;
		findPiecesToGrab.Complete();
		UpdateHandState(HandType.Left, offlineVRRig.leftHand.overrideTarget, Vector3.right, myBodyDockPositions.leftHandTransform, equipmentInteractor.isLeftGrabbing, equipmentInteractor.wasLeftGrabPressed, equipmentInteractor.leftHandHeldEquipment, equipmentInteractor.disableLeftGrab);
		UpdateHandState(HandType.Right, offlineVRRig.rightHand.overrideTarget, -Vector3.right, myBodyDockPositions.rightHandTransform, equipmentInteractor.isRightGrabbing, equipmentInteractor.wasRightGrabPressed, equipmentInteractor.rightHandHeldEquipment, equipmentInteractor.disableRightGrab);
		UpdatePieceDisables();
		if (!(offlineVRRig != null))
		{
			return;
		}
		bool flag = offlineVRRig.scaleFactor < 1f;
		if (flag && !isRigSmall)
		{
			if (offlineVRRig.builderArmShelfLeft != null)
			{
				offlineVRRig.builderArmShelfLeft.DropAttachedPieces();
				if (offlineVRRig.builderArmShelfLeft.piece != null)
				{
					foreach (Collider collider in offlineVRRig.builderArmShelfLeft.piece.colliders)
					{
						collider.enabled = false;
					}
				}
			}
			if (offlineVRRig.builderArmShelfRight != null)
			{
				offlineVRRig.builderArmShelfRight.DropAttachedPieces();
				if (offlineVRRig.builderArmShelfRight.piece != null)
				{
					foreach (Collider collider2 in offlineVRRig.builderArmShelfRight.piece.colliders)
					{
						collider2.enabled = false;
					}
				}
			}
		}
		else if (!flag && isRigSmall)
		{
			if (offlineVRRig.builderArmShelfLeft != null && offlineVRRig.builderArmShelfLeft.piece != null)
			{
				foreach (Collider collider3 in offlineVRRig.builderArmShelfLeft.piece.colliders)
				{
					collider3.enabled = true;
				}
			}
			if (offlineVRRig.builderArmShelfRight != null && offlineVRRig.builderArmShelfRight.piece != null)
			{
				foreach (Collider collider4 in offlineVRRig.builderArmShelfRight.piece.colliders)
				{
					collider4.enabled = true;
				}
			}
		}
		isRigSmall = flag;
	}

	private void SetHandState(int handIndex, HandState newState)
	{
		if (handState[handIndex] == HandState.Empty && potentialHeldPiece[handIndex] != null)
		{
			potentialHeldPiece[handIndex].PotentialGrab(enable: false);
			potentialHeldPiece[handIndex] = null;
		}
		handState[handIndex] = newState;
		switch (handState[handIndex])
		{
		case HandState.Grabbed:
			heldChainLength[handIndex] = heldPiece[handIndex].GetChildCount() + 1;
			heldPiece[handIndex].GetChainCost(heldChainCost[handIndex]);
			break;
		case HandState.PotentialGrabbed:
		{
			heldChainLength[handIndex] = 0;
			for (int j = 0; j < heldChainCost[handIndex].Length; j++)
			{
				heldChainCost[handIndex][j] = 0;
			}
			break;
		}
		case HandState.Empty:
		{
			heldChainLength[handIndex] = 0;
			for (int i = 0; i < heldChainCost[handIndex].Length; i++)
			{
				heldChainCost[handIndex][i] = 0;
			}
			break;
		}
		case HandState.WaitForGrabbed:
			break;
		}
	}

	public void OnCountChangedForRoot(BuilderPiece piece)
	{
		if (!(piece == null))
		{
			if (heldPiece[0] != null && heldPiece[0].Equals(piece))
			{
				heldChainLength[0] = heldPiece[0].GetChainCostAndCount(heldChainCost[0]);
			}
			else if (heldPiece[1] != null && heldPiece[1].Equals(piece))
			{
				heldChainLength[1] = heldPiece[1].GetChainCostAndCount(heldChainCost[1]);
			}
		}
	}

	private void UpdateHandState(HandType handType, Transform handTransform, Vector3 palmForwardLocal, Transform handAttachPoint, bool isGrabbing, bool wasGrabPressed, IHoldableObject heldEquipment, bool grabDisabled)
	{
		int index = (int)(handType + 1) % 2;
		bool flag = GorillaTagger.Instance.offlineVRRig.scaleFactor < 1f;
		bool flag2 = isGrabbing && !wasGrabPressed;
		bool flag3 = heldPiece[(int)handType] != null && (!isGrabbing || flag);
		bool num = heldEquipment != null;
		bool flag4 = heldPiece[(int)handType] != null;
		bool flag5 = !num && !flag4 && !grabDisabled && !flag && currentTable.IsInBuilderZone();
		BuilderPiece builderPiece = null;
		_ = handTransform.position;
		_ = handTransform.rotation * palmForwardLocal;
		_ = Vector3.zero;
		switch (this.handState[(int)handType])
		{
		case HandState.Empty:
			if (!flag)
			{
				float num7 = float.MaxValue;
				for (int j = 0; j < 1024; j++)
				{
					int index3 = (int)handType * 1024 + j;
					ColliderHit colliderHit = checkPiecesInSphereResults[index3];
					if (colliderHit.instanceID == 0)
					{
						break;
					}
					BuilderPiece builderPieceFromCollider = BuilderPiece.GetBuilderPieceFromCollider(colliderHit.collider);
					if (builderPieceFromCollider != null && !builderPieceFromCollider.isBuiltIntoTable)
					{
						float num8 = Vector3.SqrMagnitude(colliderHit.collider.transform.position - handTransform.position);
						if ((builderPiece == null || num8 < num7) && builderPieceFromCollider.CanPlayerGrabPiece(PhotonNetwork.LocalPlayer.ActorNumber, builderPieceFromCollider.transform.position))
						{
							builderPiece = builderPieceFromCollider;
							num7 = num8;
						}
					}
				}
				if (builderPiece == null)
				{
					for (int k = 0; k < 64; k++)
					{
						int index4 = (int)handType * 64 + k;
						RaycastHit raycastHit = grabSphereCastResults[index4];
						if (raycastHit.colliderInstanceID == 0)
						{
							break;
						}
						BuilderPiece builderPieceFromCollider2 = BuilderPiece.GetBuilderPieceFromCollider(raycastHit.collider);
						if (builderPieceFromCollider2 != null && !builderPieceFromCollider2.isBuiltIntoTable && (builderPiece == null || raycastHit.distance < num7) && builderPieceFromCollider2.CanPlayerGrabPiece(PhotonNetwork.LocalPlayer.ActorNumber, builderPieceFromCollider2.transform.position))
						{
							builderPiece = builderPieceFromCollider2;
							num7 = raycastHit.distance;
						}
					}
				}
			}
			if (potentialHeldPiece[(int)handType] != builderPiece)
			{
				if (potentialHeldPiece[(int)handType] != null)
				{
					potentialHeldPiece[(int)handType].PotentialGrab(enable: false);
				}
				potentialHeldPiece[(int)handType] = builderPiece;
				if (potentialHeldPiece[(int)handType] != null)
				{
					potentialHeldPiece[(int)handType].PotentialGrab(enable: true);
				}
			}
			if (flag5 && flag2 && builderPiece != null)
			{
				CalcPieceLocalPosAndRot(builderPiece.transform.position, builderPiece.transform.rotation, handAttachPoint, out var localPosition3, out var localRotation3);
				if (BuilderPiece.IsDroppedState(builderPiece.state) || heldPiece[index] == builderPiece)
				{
					builderPiece.PlayGrabbedFx();
					currentTable.RequestGrabPiece(builderPiece, handType == HandType.Left, localPosition3, localRotation3);
					break;
				}
				builderPiece.PlayGrabbedFx();
				SetHandState((int)handType, HandState.PotentialGrabbed);
				potentialGrabbedOffsetDist[(int)handType] = 0f;
				heldPiece[(int)handType] = builderPiece;
				heldInitialRot[(int)handType] = localRotation3;
				heldCurrentRot[(int)handType] = localRotation3;
				heldInitialPos[(int)handType] = localPosition3;
				heldCurrentPos[(int)handType] = localPosition3;
			}
			break;
		case HandState.Grabbed:
		{
			if (flag3)
			{
				Vector3 velocity = velocityEstimator[(int)handType].linearVelocity;
				if (flag)
				{
					Vector3 vector6 = currentTable.roomCenter.position - velocityEstimator[(int)handType].handPos;
					vector6.Normalize();
					Vector3 vector7 = Quaternion.Euler(0f, 180f, 0f) * vector6;
					velocity = BuilderTable.DROP_ZONE_REPEL * vector7;
				}
				else if (prevPotentialPlacement[(int)handType].attachPiece == heldPiece[(int)handType] && prevPotentialPlacement[(int)handType].parentPiece != null)
				{
					Vector3 vector8 = prevPotentialPlacement[(int)handType].parentPiece.gridPlanes[prevPotentialPlacement[(int)handType].parentAttachIndex].transform.TransformDirection(prevPotentialPlacement[(int)handType].attachPlaneNormal);
					velocity += vector8 * 1f;
				}
				currentTable.TryDropPiece(handType == HandType.Left, heldPiece[(int)handType], velocity, velocityEstimator[(int)handType].angularVelocity);
				break;
			}
			BuilderPiece builderPiece6 = heldPiece[(int)handType];
			if (!(builderPiece6 != null))
			{
				break;
			}
			builderPiece6.transform.localRotation = heldInitialRot[(int)handType];
			builderPiece6.transform.localPosition = heldInitialPos[(int)handType];
			Quaternion quaternion4 = heldCurrentRot[(int)handType];
			Vector3 vector9 = heldCurrentPos[(int)handType];
			_ = ref localAttachableGridPlaneData[(int)handType];
			handPieceData[(int)handType].Clear();
			handGridPlaneData[(int)handType].Clear();
			BuilderTableJobs.BuildTestPieceListForJob(builderPiece6, handPieceData[(int)handType], handGridPlaneData[(int)handType]);
			allPotentialPlacements[(int)handType].Clear();
			BuilderPotentialPlacement potentialPlacement;
			bool flag8 = currentTable.TryPlacePieceOnTableNoDropJobs(handGridPlaneData[(int)handType], handPieceData[(int)handType], localAttachableGridPlaneData[(int)handType], localAttachablePieceData[(int)handType], out potentialPlacement, allPotentialPlacements[(int)handType]);
			if (flag8)
			{
				BuilderPiece.State state = potentialPlacement.attachPiece.state;
				BuilderPiece.State state2 = ((potentialPlacement.parentPiece == null) ? BuilderPiece.State.None : potentialPlacement.parentPiece.state);
				bool num4 = state == BuilderPiece.State.Grabbed || state == BuilderPiece.State.GrabbedLocal;
				bool flag9 = state2 == BuilderPiece.State.Grabbed || state2 == BuilderPiece.State.GrabbedLocal;
				bool flag10 = num4 && flag9;
				int index2 = (int)(handType + 1) % 2;
				HandState handState = this.handState[index2];
				if (flag10 && potentialPlacement.attachPiece.gridPlanes[potentialPlacement.attachIndex].male)
				{
					flag8 = false;
				}
				else if (flag10 && handState == HandState.WaitingForSnap)
				{
					flag8 = false;
				}
			}
			if (flag8)
			{
				for (int i = 0; i < allPotentialPlacements[(int)handType].Count; i++)
				{
					BuilderPotentialPlacement builderPotentialPlacement2 = allPotentialPlacements[(int)handType][i];
					BuilderAttachGridPlane builderAttachGridPlane4 = builderPotentialPlacement2.attachPiece.gridPlanes[builderPotentialPlacement2.attachIndex];
					BuilderAttachGridPlane builderAttachGridPlane5 = ((builderPotentialPlacement2.parentPiece == null) ? null : builderPotentialPlacement2.parentPiece.gridPlanes[builderPotentialPlacement2.parentAttachIndex]);
					bool flag11 = builderAttachGridPlane4.IsConnected(builderPotentialPlacement2.attachBounds);
					if (!flag11)
					{
						flag11 = builderAttachGridPlane5.IsConnected(builderPotentialPlacement2.parentAttachBounds);
					}
					if (flag11)
					{
						flag8 = false;
						break;
					}
				}
			}
			if (flag8)
			{
				Vector3 position2 = potentialPlacement.localPosition;
				Quaternion quaternion5 = potentialPlacement.localRotation;
				Vector3 vector10 = potentialPlacement.attachPlaneNormal;
				if (potentialPlacement.parentPiece != null)
				{
					BuilderAttachGridPlane builderAttachGridPlane6 = potentialPlacement.parentPiece.gridPlanes[potentialPlacement.parentAttachIndex];
					position2 = builderAttachGridPlane6.transform.TransformPoint(potentialPlacement.localPosition);
					quaternion5 = builderAttachGridPlane6.transform.rotation * potentialPlacement.localRotation;
					vector10 = builderAttachGridPlane6.transform.TransformDirection(potentialPlacement.attachPlaneNormal);
				}
				Vector3 vector11 = handAttachPoint.transform.InverseTransformPoint(position2);
				Quaternion a = Quaternion.Inverse(handAttachPoint.transform.rotation) * quaternion5;
				float attachDistance = potentialPlacement.attachDistance;
				float value = Mathf.InverseLerp(currentTable.pushAndEaseParams.snapDelayOffsetDist, currentTable.pushAndEaseParams.maxOffsetY, attachDistance);
				value = Mathf.Clamp(value, 0f, 1f);
				bool num5 = potentialPlacement.attachPiece == builderPiece6;
				bool flag12 = potentialPlacement.attachPiece == builderPiece6;
				if (num5)
				{
					Quaternion b = heldInitialRot[(int)handType];
					Quaternion b2 = Quaternion.Slerp(a, b, value);
					quaternion4 = Quaternion.Slerp(quaternion4, b2, 0.1f);
				}
				if (flag12)
				{
					Vector3 vector12 = heldInitialPos[(int)handType];
					Vector3 vector13 = handAttachPoint.transform.InverseTransformDirection(vector10);
					Vector3 vector14 = vector11 + vector13 * currentTable.pushAndEaseParams.snapDelayOffsetDist - vector12;
					float b3 = Vector3.Dot(vector14, vector13);
					b3 = Mathf.Min(0f, b3);
					Vector3 vector15 = vector13 * b3;
					Vector3 a2 = vector14 - vector15;
					Vector3 b4 = vector12 + Vector3.Lerp(a2, Vector3.zero, value);
					vector9 = Vector3.Lerp(vector9, b4, 0.5f);
				}
				heldCurrentRot[(int)handType] = quaternion4;
				heldCurrentPos[(int)handType] = vector9;
				builderPiece6.transform.localRotation = quaternion4;
				builderPiece6.transform.localPosition = vector9;
				bool flag13 = Vector3.Dot(velocityEstimator[(int)handType].linearVelocity, vector10) > 0f;
				float snapAttachDistance = currentTable.pushAndEaseParams.snapAttachDistance;
				if (potentialPlacement.attachDistance < snapAttachDistance && !flag13 && BuilderPiece.CanPlayerAttachPieceToPiece(PhotonNetwork.LocalPlayer.ActorNumber, builderPiece6, potentialPlacement.parentPiece))
				{
					GorillaTagger.Instance.StartVibration(handType == HandType.Left, GorillaTagger.Instance.tapHapticStrength, currentTable.pushAndEaseParams.snapDelayTime * 2f);
					if (((potentialPlacement.parentPiece == null) ? BuilderPiece.State.None : potentialPlacement.parentPiece.state) == BuilderPiece.State.GrabbedLocal)
					{
						GorillaTagger.Instance.StartVibration(handType != HandType.Left, GorillaTagger.Instance.tapHapticStrength, currentTable.pushAndEaseParams.snapDelayTime * 2f);
					}
					delayedPotentialPlacement[(int)handType] = potentialPlacement;
					delayedPlacementTime[(int)handType] = 0f;
					SetHandState((int)handType, HandState.WaitingForSnap);
				}
				else
				{
					float num6 = currentTable.gridSize * 0.5f * (currentTable.gridSize * 0.5f);
					if (prevPotentialPlacement[(int)handType].attachPiece != potentialPlacement.attachPiece || prevPotentialPlacement[(int)handType].parentPiece != potentialPlacement.parentPiece || prevPotentialPlacement[(int)handType].attachIndex != potentialPlacement.attachIndex || prevPotentialPlacement[(int)handType].parentAttachIndex != potentialPlacement.parentAttachIndex || Vector3.SqrMagnitude(prevPotentialPlacement[(int)handType].localPosition - potentialPlacement.localPosition) > num6)
					{
						GorillaTagger.Instance.StartVibration(handType == HandType.Left, GorillaTagger.Instance.tapHapticStrength * 0.15f, currentTable.pushAndEaseParams.snapDelayTime);
						try
						{
							ClearGlowBumps((int)handType);
							AddGlowBumps((int)handType, allPotentialPlacements[(int)handType]);
						}
						catch (Exception ex)
						{
							Debug.LogErrorFormat("Error adding glow bumps {0}", ex.ToString());
						}
					}
				}
				UpdateGlowBumps((int)handType, 1f - value);
				prevPotentialPlacement[(int)handType] = potentialPlacement;
			}
			else
			{
				ClearGlowBumps((int)handType);
				Quaternion b5 = heldInitialRot[(int)handType];
				quaternion4 = Quaternion.Slerp(quaternion4, b5, 0.1f);
				Vector3 b6 = heldInitialPos[(int)handType];
				vector9 = Vector3.Lerp(vector9, b6, 0.1f);
				heldCurrentRot[(int)handType] = quaternion4;
				heldCurrentPos[(int)handType] = vector9;
				builderPiece6.transform.localRotation = quaternion4;
				builderPiece6.transform.localPosition = vector9;
				prevPotentialPlacement[(int)handType] = default(BuilderPotentialPlacement);
			}
			break;
		}
		case HandState.PotentialGrabbed:
		{
			if (flag3)
			{
				BuilderPiece builderPiece4 = heldPiece[(int)handType];
				ClearUnSnapOffset((int)handType, builderPiece4);
				RemovePieceFromHand(builderPiece4, (int)handType);
				heldPiece[(int)handType] = null;
				SetHandState((int)handType, HandState.Empty);
				break;
			}
			BuilderPiece builderPiece5 = heldPiece[(int)handType];
			CalcPieceLocalPosAndRot(builderPiece5.transform.position, builderPiece5.transform.rotation, handAttachPoint, out var localPosition2, out var _);
			if (BuilderPiece.IsDroppedState(builderPiece5.state))
			{
				currentTable.RequestGrabPiece(builderPiece5, handType == HandType.Left, heldInitialPos[(int)handType], heldInitialRot[(int)handType]);
				break;
			}
			Vector3 vector5 = heldInitialPos[(int)handType] - localPosition2;
			UpdatePullApartOffset((int)handType, builderPiece5, handAttachPoint.TransformVector(vector5));
			float num3 = currentTable.pushAndEaseParams.unSnapDelayDist * currentTable.pushAndEaseParams.unSnapDelayDist;
			if (vector5.sqrMagnitude > num3)
			{
				GorillaTagger.Instance.StartVibration(handType == HandType.Left, GorillaTagger.Instance.tapHapticStrength * 0.15f, currentTable.pushAndEaseParams.unSnapDelayTime * 2f);
				if (((builderPiece5 == null) ? BuilderPiece.State.None : builderPiece5.state) == BuilderPiece.State.GrabbedLocal)
				{
					GorillaTagger.Instance.StartVibration(handType != HandType.Left, GorillaTagger.Instance.tapHapticStrength * 0.15f, currentTable.pushAndEaseParams.unSnapDelayTime * 2f);
				}
				SetHandState((int)handType, HandState.WaitingForUnSnap);
				delayedPlacementTime[(int)handType] = 0f;
			}
			break;
		}
		case HandState.WaitingForUnSnap:
		{
			BuilderPiece builderPiece3 = heldPiece[(int)handType];
			if (BuilderPiece.IsDroppedState(builderPiece3.state))
			{
				currentTable.RequestGrabPiece(builderPiece3, handType == HandType.Left, heldInitialPos[(int)handType], heldInitialRot[(int)handType]);
			}
			else if (delayedPlacementTime[(int)handType] > currentTable.pushAndEaseParams.unSnapDelayTime)
			{
				if (builderPiece3.GetChildCount() > maxHoldablePieceStackCount)
				{
					builderPiece3.PlayTooHeavyFx();
					ClearUnSnapOffset((int)handType, builderPiece3);
					RemovePieceFromHand(builderPiece3, (int)handType);
				}
				else
				{
					currentTable.RequestGrabPiece(builderPiece3, handType == HandType.Left, heldInitialPos[(int)handType], heldInitialRot[(int)handType]);
				}
			}
			else
			{
				CalcPieceLocalPosAndRot(builderPiece3.transform.position, builderPiece3.transform.rotation, handAttachPoint, out var localPosition, out var _);
				Vector3 vector4 = heldInitialPos[(int)handType] - localPosition;
				UpdatePullApartOffset((int)handType, builderPiece3, handAttachPoint.TransformVector(vector4));
				delayedPlacementTime[(int)handType] = delayedPlacementTime[(int)handType] + Time.deltaTime;
			}
			break;
		}
		case HandState.WaitingForSnap:
		{
			BuilderPiece builderPiece2 = heldPiece[(int)handType];
			if (!(builderPiece2 != null))
			{
				break;
			}
			builderPiece2.transform.localRotation = heldInitialRot[(int)handType];
			builderPiece2.transform.localPosition = heldInitialPos[(int)handType];
			Quaternion quaternion = heldCurrentRot[(int)handType];
			Vector3 vector = heldCurrentPos[(int)handType];
			if (!(delayedPlacementTime[(int)handType] >= 0f))
			{
				break;
			}
			BuilderPotentialPlacement builderPotentialPlacement = delayedPotentialPlacement[(int)handType];
			if (delayedPlacementTime[(int)handType] > currentTable.pushAndEaseParams.snapDelayTime)
			{
				BuilderAttachGridPlane builderAttachGridPlane = builderPotentialPlacement.attachPiece.gridPlanes[builderPotentialPlacement.attachIndex];
				BuilderAttachGridPlane builderAttachGridPlane2 = ((builderPotentialPlacement.parentPiece == null) ? null : builderPotentialPlacement.parentPiece.gridPlanes[builderPotentialPlacement.parentAttachIndex]);
				bool flag6 = builderAttachGridPlane.IsConnected(builderPotentialPlacement.attachBounds);
				if (!flag6)
				{
					flag6 = builderAttachGridPlane2.IsConnected(builderPotentialPlacement.parentAttachBounds);
				}
				if (flag6)
				{
					Debug.LogError("Snap Overlapping Why are we doing this!!??");
				}
				if (!BuilderPiece.CanPlayerAttachPieceToPiece(PhotonNetwork.LocalPlayer.ActorNumber, builderPiece2, builderPotentialPlacement.parentPiece))
				{
					SetHandState((int)handType, HandState.Grabbed);
				}
				currentTable.RequestPlacePiece(builderPiece2, builderPotentialPlacement.attachPiece, builderPotentialPlacement.bumpOffsetX, builderPotentialPlacement.bumpOffsetZ, builderPotentialPlacement.twist, builderPotentialPlacement.parentPiece, builderPotentialPlacement.attachIndex, builderPotentialPlacement.parentAttachIndex);
				break;
			}
			delayedPlacementTime[(int)handType] = delayedPlacementTime[(int)handType] + Time.deltaTime;
			Transform parent = builderPiece2.transform.parent;
			Vector3 position = builderPotentialPlacement.localPosition;
			Quaternion quaternion2 = builderPotentialPlacement.localRotation;
			Vector3 direction = builderPotentialPlacement.attachPlaneNormal;
			if (builderPotentialPlacement.parentPiece != null)
			{
				BuilderAttachGridPlane builderAttachGridPlane3 = builderPotentialPlacement.parentPiece.gridPlanes[builderPotentialPlacement.parentAttachIndex];
				position = builderAttachGridPlane3.transform.TransformPoint(builderPotentialPlacement.localPosition);
				quaternion2 = builderAttachGridPlane3.transform.rotation * builderPotentialPlacement.localRotation;
				direction = builderAttachGridPlane3.transform.TransformDirection(builderPotentialPlacement.attachPlaneNormal);
			}
			Vector3 vector2 = parent.transform.InverseTransformPoint(position);
			Quaternion quaternion3 = Quaternion.Inverse(parent.transform.rotation) * quaternion2;
			bool num2 = builderPotentialPlacement.attachPiece == builderPiece2;
			bool flag7 = builderPotentialPlacement.attachPiece == builderPiece2;
			if (num2)
			{
				quaternion = quaternion3;
			}
			if (flag7)
			{
				Vector3 vector3 = parent.transform.InverseTransformDirection(direction);
				vector = vector2 + vector3 * currentTable.pushAndEaseParams.snapDelayOffsetDist;
			}
			heldCurrentRot[(int)handType] = quaternion;
			heldCurrentPos[(int)handType] = vector;
			builderPiece2.transform.localRotation = quaternion;
			builderPiece2.transform.localPosition = vector;
			break;
		}
		case HandState.WaitForGrabbed:
			break;
		}
	}

	private void ClearGlowBumps(int handIndex)
	{
		if (!BuilderTable.TryGetBuilderTableForZone(GorillaTagger.Instance.offlineVRRig.zoneEntity.currentZone, out var table))
		{
			return;
		}
		BuilderPool builderPool = table.builderPool;
		List<BuilderBumpGlow> list = glowBumps[handIndex];
		if (builderPool != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				builderPool.DestroyBumpGlow(list[i]);
			}
		}
		else
		{
			Debug.LogError("BuilderPieceInteractor could not find Builderpool");
		}
		list.Clear();
	}

	private void AddGlowBumps(int handIndex, List<BuilderPotentialPlacement> allPotentialPlacements)
	{
		ClearGlowBumps(handIndex);
		if (allPotentialPlacements == null)
		{
			Debug.LogError("How is allPotentialPlacements null");
			return;
		}
		BuilderPool builderPool = currentTable.builderPool;
		if (builderPool == null)
		{
			Debug.LogError("How is the pool null?");
			return;
		}
		float gridSize = currentTable.gridSize;
		for (int i = 0; i < allPotentialPlacements.Count; i++)
		{
			BuilderPotentialPlacement builderPotentialPlacement = allPotentialPlacements[i];
			if (!(builderPotentialPlacement.parentPiece != null) || !(builderPotentialPlacement.attachPiece != null) || builderPotentialPlacement.attachPiece.gridPlanes == null || builderPotentialPlacement.parentPiece.gridPlanes == null)
			{
				continue;
			}
			BuilderAttachGridPlane builderAttachGridPlane = builderPotentialPlacement.parentPiece.gridPlanes[builderPotentialPlacement.parentAttachIndex];
			if (builderAttachGridPlane != null)
			{
				Vector2Int min = builderPotentialPlacement.parentAttachBounds.min;
				Vector2Int max = builderPotentialPlacement.parentAttachBounds.max;
				for (int j = min.x; j <= max.x; j++)
				{
					for (int k = min.y; k <= max.y; k++)
					{
						Vector3 gridPosition = builderAttachGridPlane.GetGridPosition(j, k, gridSize);
						BuilderBumpGlow builderBumpGlow = builderPool.CreateGlowBump();
						if (builderBumpGlow != null)
						{
							builderBumpGlow.transform.SetPositionAndRotation(gridPosition, builderAttachGridPlane.transform.rotation);
							builderBumpGlow.transform.SetParent(builderAttachGridPlane.transform, worldPositionStays: true);
							builderBumpGlow.transform.localScale = Vector3.one;
							builderBumpGlow.gameObject.SetActive(value: true);
							glowBumps[handIndex].Add(builderBumpGlow);
						}
					}
				}
			}
			BuilderAttachGridPlane builderAttachGridPlane2 = builderPotentialPlacement.attachPiece.gridPlanes[builderPotentialPlacement.attachIndex];
			if (!(builderAttachGridPlane2 != null))
			{
				continue;
			}
			Vector2Int min2 = builderPotentialPlacement.attachBounds.min;
			Vector2Int max2 = builderPotentialPlacement.attachBounds.max;
			for (int l = min2.x; l <= max2.x; l++)
			{
				for (int m = min2.y; m <= max2.y; m++)
				{
					Vector3 gridPosition2 = builderAttachGridPlane2.GetGridPosition(l, m, gridSize);
					BuilderBumpGlow builderBumpGlow2 = builderPool.CreateGlowBump();
					if (builderBumpGlow2 != null)
					{
						Quaternion quaternion = Quaternion.Euler(180f, 0f, 0f);
						builderBumpGlow2.transform.SetPositionAndRotation(gridPosition2, builderAttachGridPlane2.transform.rotation * quaternion);
						builderBumpGlow2.transform.SetParent(builderAttachGridPlane2.transform, worldPositionStays: true);
						builderBumpGlow2.transform.localScale = Vector3.one;
						builderBumpGlow2.gameObject.SetActive(value: true);
						glowBumps[handIndex].Add(builderBumpGlow2);
					}
				}
			}
		}
	}

	private void UpdateGlowBumps(int handIndex, float intensity)
	{
		List<BuilderBumpGlow> list = glowBumps[handIndex];
		for (int i = 0; i < list.Count; i++)
		{
			list[i].SetIntensity(intensity);
		}
	}

	private void UpdatePullApartOffset(int handIndex, BuilderPiece potentialGrabPiece, Vector3 pullApartDiff)
	{
		BuilderPiece parentPiece = potentialGrabPiece.parentPiece;
		BuilderAttachGridPlane builderAttachGridPlane = null;
		if (parentPiece != null)
		{
			builderAttachGridPlane = parentPiece.gridPlanes[potentialGrabPiece.parentAttachIndex];
		}
		Vector3 vector = Vector3.up;
		if (builderAttachGridPlane != null)
		{
			vector = builderAttachGridPlane.transform.TransformDirection(vector);
			if (!builderAttachGridPlane.male)
			{
				vector *= -1f;
			}
		}
		float a = Vector3.Dot(pullApartDiff, vector);
		a = Mathf.Max(a, 0f);
		float num = 0.0025f;
		float num2 = a / num;
		num2 = 1f - 1f / (1f + num2);
		a = num2 * num;
		Vector3 vector2 = vector * potentialGrabbedOffsetDist[handIndex];
		potentialGrabbedOffsetDist[handIndex] = a;
		Vector3 vector3 = vector * potentialGrabbedOffsetDist[handIndex];
		if (builderAttachGridPlane != null)
		{
			vector2 = builderAttachGridPlane.transform.InverseTransformVector(vector2);
			vector3 = builderAttachGridPlane.transform.InverseTransformVector(vector3);
		}
		Vector3 vector4 = potentialGrabPiece.transform.localPosition - vector2;
		potentialGrabPiece.transform.localPosition = vector4 + vector3;
	}

	private void ClearUnSnapOffset(int handIndex, BuilderPiece potentialGrabPiece)
	{
		UpdatePullApartOffset(handIndex, potentialGrabPiece, Vector3.zero);
	}

	public void AddPieceToHeld(BuilderPiece piece, bool isLeft, Vector3 localPosition, Quaternion localRotation)
	{
		int num = ((!isLeft) ? 1 : 0);
		AddPieceToHand(piece, num, localPosition, localRotation);
		int num2 = (num + 1) % 2;
		if (heldPiece[num2] == piece)
		{
			RemovePieceFromHand(piece, num2);
		}
	}

	public void RemovePieceFromHeld(BuilderPiece piece)
	{
		for (int i = 0; i < 2; i++)
		{
			if (heldPiece[i] == piece)
			{
				RemovePieceFromHand(piece, i);
			}
		}
	}

	private void AddPieceToHand(BuilderPiece piece, int handIndex, Vector3 localPosition, Quaternion localRotation)
	{
		heldPiece[handIndex] = piece;
		delayedPlacementTime[handIndex] = -1f;
		SetHandState(handIndex, HandState.Grabbed);
		heldInitialRot[handIndex] = localRotation;
		heldCurrentRot[handIndex] = localRotation;
		heldInitialPos[handIndex] = localPosition;
		heldCurrentPos[handIndex] = localPosition;
	}

	private void RemovePieceFromHand(BuilderPiece piece, int handIndex)
	{
		heldPiece[handIndex] = null;
		delayedPlacementTime[handIndex] = -1f;
		SetHandState(handIndex, HandState.Empty);
		ClearGlowBumps(handIndex);
	}

	public void RemovePiecesFromHands()
	{
		for (int i = 0; i < 2; i++)
		{
			heldPiece[i] = null;
			delayedPlacementTime[i] = -1f;
			SetHandState(i, HandState.Empty);
			ClearGlowBumps(i);
		}
	}

	private void CalcPieceLocalPosAndRot(Vector3 worldPosition, Quaternion worldRotation, Transform attachPoint, out Vector3 localPosition, out Quaternion localRotation)
	{
		Quaternion rotation = attachPoint.transform.rotation;
		Vector3 position = attachPoint.transform.position;
		localRotation = Quaternion.Inverse(rotation) * worldRotation;
		localPosition = Quaternion.Inverse(rotation) * (worldPosition - position);
	}

	public void DisableCollisionsWithHands()
	{
		DisableCollisionsWithHand(leftHand: true);
		DisableCollisionsWithHand(leftHand: false);
	}

	private void DisableCollisionsWithHand(bool leftHand)
	{
		VRRig offlineVRRig = GorillaTagger.Instance.offlineVRRig;
		if (!BuilderTable.TryGetBuilderTableForZone(offlineVRRig.zoneEntity.currentZone, out var table))
		{
			return;
		}
		Transform obj = (leftHand ? offlineVRRig.leftHand.overrideTarget : offlineVRRig.rightHand.overrideTarget);
		List<GameObject> list = (leftHand ? collisionDisabledPiecesLeft : collisionDisabledPiecesRight);
		int num = Physics.OverlapSphereNonAlloc(obj.position, 0.15f, tempDisableColliders, table.allPiecesMask);
		for (int i = 0; i < num; i++)
		{
			GameObject gameObject = tempDisableColliders[i].gameObject;
			BuilderPiece builderPieceFromCollider = BuilderPiece.GetBuilderPieceFromCollider(tempDisableColliders[i]);
			if (builderPieceFromCollider != null && builderPieceFromCollider.state == BuilderPiece.State.AttachedAndPlaced && !list.Contains(gameObject))
			{
				gameObject.layer = BuilderTable.heldLayer;
				list.Add(gameObject);
			}
		}
	}

	public void UpdatePieceDisables()
	{
		UpdatePieceDisablesForHand(leftHand: true);
		UpdatePieceDisablesForHand(leftHand: false);
	}

	public void UpdatePieceDisablesForHand(bool leftHand)
	{
		VRRig offlineVRRig = GorillaTagger.Instance.offlineVRRig;
		Transform obj = (leftHand ? offlineVRRig.leftHand.overrideTarget : offlineVRRig.rightHand.overrideTarget);
		List<GameObject> list = (leftHand ? collisionDisabledPiecesLeft : collisionDisabledPiecesRight);
		List<GameObject> list2 = ((!leftHand) ? collisionDisabledPiecesLeft : collisionDisabledPiecesRight);
		Vector3 position = obj.position;
		float num = 0.040000003f;
		for (int i = 0; i < list.Count; i++)
		{
			GameObject gameObject = list[i];
			if (gameObject == null)
			{
				list.RemoveAt(i);
				i--;
			}
			else if ((gameObject.transform.position - position).sqrMagnitude > num)
			{
				BuilderPiece builderPieceFromTransform = BuilderPiece.GetBuilderPieceFromTransform(gameObject.transform);
				if (builderPieceFromTransform.state == BuilderPiece.State.AttachedAndPlaced && !list2.Contains(gameObject))
				{
					builderPieceFromTransform.SetColliderLayers(builderPieceFromTransform.colliders, BuilderTable.placedLayer);
				}
				list.RemoveAt(i);
				i--;
			}
		}
	}
}
