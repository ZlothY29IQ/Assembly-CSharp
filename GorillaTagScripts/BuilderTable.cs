using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BoingKit;
using CjLib;
using GorillaExtensions;
using GorillaNetworking;
using GorillaTag;
using GorillaTagScripts.Builder;
using Ionic.Zlib;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using Unity.Collections;
using Unity.Jobs;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;

namespace GorillaTagScripts;

public class BuilderTable : MonoBehaviour, ITickSystemTick
{
	private struct BoxCheckParams
	{
		public Vector3 center;

		public Vector3 halfExtents;

		public Quaternion rotation;
	}

	[Serializable]
	public class BuildPieceSpawn
	{
		public GameObject buildPiecePrefab;

		public int count = 1;
	}

	public enum BuilderCommandType
	{
		Create,
		Place,
		Grab,
		Drop,
		Remove,
		Paint,
		Recycle,
		ClaimPlot,
		FreePlot,
		CreateArmShelf,
		PlayerLeftRoom,
		FunctionalStateChange,
		SetSelection,
		Repel
	}

	public enum TableState
	{
		WaitingForZoneAndRoom,
		WaitingForInitalBuild,
		ReceivingInitialBuild,
		WaitForInitialBuildMaster,
		WaitForMasterResync,
		ReceivingMasterResync,
		InitialBuild,
		ExecuteQueuedCommands,
		Ready,
		BadData,
		WaitingForSharedMapLoad
	}

	public enum DroppedPieceState
	{
		None = -1,
		Light,
		Heavy,
		Frozen
	}

	private struct DroppedPieceData
	{
		public DroppedPieceState droppedState;

		public float speedThreshCrossedTime;

		public float filteredSpeed;
	}

	public struct BuilderCommand
	{
		public BuilderCommandType type;

		public int pieceType;

		public int pieceId;

		public int attachPieceId;

		public int parentPieceId;

		public int parentAttachIndex;

		public int attachIndex;

		public Vector3 localPosition;

		public Quaternion localRotation;

		public byte twist;

		public sbyte bumpOffsetX;

		public sbyte bumpOffsetZ;

		public Vector3 velocity;

		public Vector3 angVelocity;

		public bool isLeft;

		public int materialType;

		public NetPlayer player;

		public BuilderPiece.State state;

		public bool isQueued;

		public bool canRollback;

		public int localCommandId;

		public int serverTimeStamp;
	}

	[Serializable]
	public struct SnapParams
	{
		public float minOffsetY;

		public float maxOffsetY;

		public float maxUpDotProduct;

		public float maxTwistDotProduct;

		public float snapAttachDistance;

		public float snapDelayTime;

		public float snapDelayOffsetDist;

		public float unSnapDelayTime;

		public float unSnapDelayDist;

		public float maxBlockSnapDist;
	}

	private struct SnapOverlapKey
	{
		public long piece;

		public long otherPiece;

		public override int GetHashCode()
		{
			return HashCode.Combine(piece.GetHashCode(), otherPiece.GetHashCode());
		}

		public bool Equals(SnapOverlapKey other)
		{
			if (piece == other.piece)
			{
				return otherPiece == other.otherPiece;
			}
			return false;
		}

		public override bool Equals(object o)
		{
			if (!(o is SnapOverlapKey))
			{
				return false;
			}
			return Equals((SnapOverlapKey)o);
		}
	}

	public const GTZone BUILDER_ZONE = GTZone.monkeBlocks;

	private const int INITIAL_BUILTIN_PIECE_ID = 5;

	private const int INITIAL_CREATED_PIECE_ID = 10000;

	public static float MAX_DROP_VELOCITY = 20f;

	public static float MAX_DROP_ANG_VELOCITY = 50f;

	private const float MAX_DISTANCE_FROM_CENTER = 217f;

	private const float MAX_LOCAL_MAGNITUDE = 80f;

	public const float MAX_DISTANCE_FROM_HAND = 2.5f;

	public static float DROP_ZONE_REPEL = 2.25f;

	public static int placedLayer;

	public static int heldLayer;

	public static int heldLayerLocal;

	public static int droppedLayer;

	private float acceptableSqrDistFromCenter = 47089f;

	public float pieceScale = 0.04f;

	public GTZone tableZone = GTZone.monkeBlocks;

	[SerializeField]
	private string SharedMapConfigTitleDataKey = "SharedBlocksStartingMapConfig";

	public BuilderTableNetworking builderNetworking;

	public BuilderRenderer builderRenderer;

	[HideInInspector]
	public BuilderPool builderPool;

	public Transform tableCenter;

	public Transform roomCenter;

	public Transform worldCenter;

	public GameObject noBlocksArea;

	public List<GameObject> builtInPieceRoots;

	[Tooltip("Optional terminal to control loaded blocks")]
	public SharedBlocksTerminal linkedTerminal;

	[Tooltip("Can Blocks Be Placed and Grabbed")]
	public bool isTableMutable;

	public GameObject shelvesRoot;

	public GameObject dropZoneRoot;

	public List<GameObject> recyclerRoot;

	public List<GameObject> allShelvesRoot;

	[NonSerialized]
	public List<BuilderConveyor> conveyors = new List<BuilderConveyor>();

	[NonSerialized]
	public List<BuilderDispenserShelf> dispenserShelves = new List<BuilderDispenserShelf>();

	public BuilderConveyorManager conveyorManager;

	public List<BuilderResourceMeter> resourceMeters;

	public GameObject sharedBuildArea;

	private BoxCollider[] sharedBuildAreas;

	public BuilderPiece armShelfPieceType;

	[NonSerialized]
	public List<BuilderRecycler> recyclers;

	[NonSerialized]
	public List<BuilderDropZone> dropZones;

	private int shelfSliceUpdateIndex;

	public static int SHELF_SLICE_BUCKETS = 6;

	public float defaultTint = 1f;

	public float droppedTint = 0.75f;

	public float grabbedTint = 0.75f;

	public float shelfTint = 1f;

	public float potentialGrabTint = 0.75f;

	public float paintingTint = 0.6f;

	private List<BoxCheckParams> noBlocksAreas;

	private Collider[] noBlocksCheckResults = new Collider[64];

	public LayerMask allPiecesMask;

	public bool useSnapRotation;

	public BuilderPlacementStyle usePlacementStyle;

	public BuilderOptionButton buttonSnapRotation;

	public BuilderOptionButton buttonSnapPosition;

	public BuilderOptionButton buttonSaveLayout;

	public BuilderOptionButton buttonClearLayout;

	[HideInInspector]
	public List<BuilderAttachGridPlane> baseGridPlanes;

	private List<BuilderPiece> basePieces;

	[HideInInspector]
	public List<BuilderPiecePrivatePlot> allPrivatePlots;

	private int nextPieceId;

	[HideInInspector]
	public List<BuildPieceSpawn> buildPieceSpawns;

	[HideInInspector]
	public List<BuilderShelf> shelves;

	[NonSerialized]
	public List<BuilderPiece> pieces = new List<BuilderPiece>(1024);

	private Dictionary<int, int> pieceIDToIndexCache = new Dictionary<int, int>(1024);

	[HideInInspector]
	public Dictionary<int, int> plotOwners;

	private bool doesLocalPlayerOwnPlot;

	public Dictionary<int, int> playerToArmShelfLeft;

	public Dictionary<int, int> playerToArmShelfRight;

	private HashSet<int> builderPiecesVisited = new HashSet<int>(128);

	public BuilderResources totalResources;

	[Tooltip("Resources reserved for conveyors and dispensers")]
	public BuilderResources totalReservedResources;

	public BuilderResources resourcesPerPrivatePlot;

	[NonSerialized]
	public int[] maxResources;

	private int[] plotMaxResources;

	[NonSerialized]
	public int[] usedResources;

	[NonSerialized]
	public int[] reservedResources;

	private List<int> playersInBuilder;

	private List<IBuilderPieceFunctional> activeFunctionalComponents = new List<IBuilderPieceFunctional>(128);

	private List<IBuilderPieceFunctional> funcComponentsToRegister = new List<IBuilderPieceFunctional>(10);

	private List<IBuilderPieceFunctional> funcComponentsToUnregister = new List<IBuilderPieceFunctional>(10);

	private List<IBuilderPieceFunctional> fixedUpdateFunctionalComponents = new List<IBuilderPieceFunctional>(128);

	private List<IBuilderPieceFunctional> funcComponentsToRegisterFixed = new List<IBuilderPieceFunctional>(10);

	private List<IBuilderPieceFunctional> funcComponentsToUnregisterFixed = new List<IBuilderPieceFunctional>(10);

	private const int MAX_SPHERE_CHECK_RESULTS = 1024;

	private NativeList<BuilderGridPlaneData> gridPlaneData;

	private NativeList<BuilderGridPlaneData> checkGridPlaneData;

	private NativeArray<ColliderHit> nearbyPiecesResults;

	private NativeArray<OverlapSphereCommand> nearbyPiecesCommands;

	private List<BuilderPotentialPlacement> allPotentialPlacements;

	private static HashSet<BuilderPiece> tempPieceSet = new HashSet<BuilderPiece>(512);

	private TableState tableState;

	private bool inRoom;

	private bool inBuilderZone;

	private static int DROPPED_PIECE_LIMIT = 100;

	public static string nextUpdateOverride = string.Empty;

	private List<BuilderPiece> droppedPieces;

	private List<DroppedPieceData> droppedPieceData;

	private HashSet<int>[] repelledPieceRoots;

	private int repelHistoryLength = 3;

	private int repelHistoryIndex;

	private bool hasRequestedConfig;

	private bool isDirty;

	private bool saveInProgress;

	private int currentSaveSlot = -1;

	[HideInInspector]
	public UnityEvent OnSaveTimeUpdated;

	[HideInInspector]
	public UnityEvent<bool> OnSaveDirtyChanged;

	[HideInInspector]
	public UnityEvent OnSaveSuccess;

	[HideInInspector]
	public UnityEvent<string> OnSaveFailure;

	[HideInInspector]
	public UnityEvent OnTableConfigurationUpdated;

	[HideInInspector]
	public UnityEvent<bool> OnLocalPlayerClaimedPlot;

	[HideInInspector]
	public UnityEvent OnMapCleared;

	[HideInInspector]
	public UnityEvent<string> OnMapLoaded;

	[HideInInspector]
	public UnityEvent<string> OnMapLoadFailed;

	private List<BuilderCommand> queuedBuildCommands;

	private List<BuilderAction> rollBackActions;

	private List<BuilderCommand> rollBackBufferedCommands;

	private List<BuilderCommand> rollForwardCommands;

	[OnEnterPlay_Clear]
	private static Dictionary<GTZone, BuilderTable> zoneToInstance;

	private bool isSetup;

	public SnapParams pushAndEaseParams;

	public SnapParams overlapParams;

	private SnapParams currSnapParams;

	public int maxPlacementChildDepth = 5;

	public List<SimpleAABB> m_areaBounds = new List<SimpleAABB>();

	private static List<BuilderPiece> tempPieces = new List<BuilderPiece>(256);

	private static List<BuilderConveyor> tempConveyors = new List<BuilderConveyor>(256);

	private static List<BuilderDispenserShelf> tempDispensers = new List<BuilderDispenserShelf>(256);

	private static List<BuilderRecycler> tempRecyclers = new List<BuilderRecycler>(5);

	private static List<BuilderCommand> tempRollForwardCommands = new List<BuilderCommand>(128);

	private static List<BuilderPiece> tempDeletePieces = new List<BuilderPiece>(1024);

	public const int MAX_PIECE_DATA = 2560;

	public const int MAX_GRID_PLANE_DATA = 10240;

	public const int MAX_PRIVATE_PLOT_DATA = 64;

	public const int MAX_PLAYER_DATA = 64;

	private BuilderTableData tableData;

	private int fetchConfigurationAttempts;

	private int maxRetries = 3;

	private SharedBlocksManager.SharedBlocksMap sharedBlocksMap;

	private string pendingMapID;

	private SharedBlocksManager.StartingMapConfig startingMapConfig = new SharedBlocksManager.StartingMapConfig
	{
		pageNumber = 0,
		pageSize = 50,
		sortMethod = SharedBlocksManager.MapSortMethod.Top.ToString(),
		useMapID = false,
		mapID = null
	};

	private List<SharedBlocksManager.SharedBlocksMap> startingMapList = new List<SharedBlocksManager.SharedBlocksMap>();

	private SharedBlocksManager.SharedBlocksMap startingMap;

	private bool hasStartingMap;

	private double startingMapCacheTime = double.MinValue;

	private bool getStartingMapInProgress;

	private bool hasCachedTopMaps;

	private double lastGetTopMapsTime = double.MinValue;

	private static string personalBuildKey = "MyBuild";

	private static HashSet<SnapOverlapKey> tempDuplicateOverlaps = new HashSet<SnapOverlapKey>(16384);

	private static List<BuilderPiece> childPieces = new List<BuilderPiece>(4096);

	private static List<BuilderPiece> rootPieces = new List<BuilderPiece>(4096);

	private static List<int> overlapPieces = new List<int>(4096);

	private static List<int> overlapOtherPieces = new List<int>(4096);

	private static List<long> overlapPacked = new List<long>(4096);

	private static char[] mapIDBuffer = new char[8];

	private static Dictionary<long, int> snapOverlapSanity = new Dictionary<long, int>(16384);

	private static List<int> tempPeiceIds = new List<int>(4096);

	private static List<int> tempParentPeiceIds = new List<int>(4096);

	private static List<int> tempAttachIndexes = new List<int>(4096);

	private static List<int> tempParentAttachIndexes = new List<int>(4096);

	private static List<int> tempParentActorNumbers = new List<int>(4096);

	private static List<bool> tempInLeftHand = new List<bool>(4096);

	private static List<int> tempPiecePlacement = new List<int>(4096);

	public bool TickRunning { get; set; }

	[HideInInspector]
	public float gridSize => pieceScale / 2f;

	public int CurrentSaveSlot
	{
		get
		{
			return currentSaveSlot;
		}
		set
		{
			if (!saveInProgress)
			{
				if (!BuilderScanKiosk.IsSaveSlotValid(value))
				{
					currentSaveSlot = -1;
				}
				if (currentSaveSlot != value)
				{
					SetIsDirty(dirty: true);
				}
				currentSaveSlot = value;
			}
		}
	}

	private void ExecuteAction(BuilderAction action)
	{
		if (!isTableMutable)
		{
			return;
		}
		BuilderPiece piece = GetPiece(action.pieceId);
		BuilderPiece piece2 = GetPiece(action.parentPieceId);
		int playerActorNumber = action.playerActorNumber;
		bool flag = PhotonNetwork.LocalPlayer.ActorNumber == action.playerActorNumber;
		switch (action.type)
		{
		case BuilderActionType.AttachToPlayer:
		{
			piece.ClearParentHeld();
			piece.ClearParentPiece();
			piece.transform.localScale = Vector3.one;
			if (!VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.GetPlayer(playerActorNumber), out var playerRig))
			{
				_ = $"Execute Builder Action {action.localCommandId} {action.type} {action.pieceId} {action.playerActorNumber} {action.isLeftHand}";
				break;
			}
			BodyDockPositions myBodyDockPositions = playerRig.Rig.myBodyDockPositions;
			Transform parentHeld = (action.isLeftHand ? myBodyDockPositions.leftHandTransform : myBodyDockPositions.rightHandTransform);
			piece.SetParentHeld(parentHeld, playerActorNumber, action.isLeftHand);
			piece.transform.SetLocalPositionAndRotation(action.localPosition, action.localRotation);
			BuilderPiece.State newState = (flag ? BuilderPiece.State.GrabbedLocal : BuilderPiece.State.Grabbed);
			piece.SetState(newState);
			if (!flag)
			{
				BuilderPieceInteractor.instance.RemovePieceFromHeld(piece);
			}
			if (flag)
			{
				BuilderPieceInteractor.instance.AddPieceToHeld(piece, action.isLeftHand, action.localPosition, action.localRotation);
			}
			break;
		}
		case BuilderActionType.DetachFromPlayer:
			if (flag)
			{
				BuilderPieceInteractor.instance.RemovePieceFromHeld(piece);
			}
			piece.ClearParentHeld();
			piece.ClearParentPiece();
			piece.transform.localScale = Vector3.one;
			break;
		case BuilderActionType.AttachToPiece:
		{
			piece.ClearParentHeld();
			piece.ClearParentPiece();
			piece.transform.localScale = Vector3.one;
			Quaternion localRotation = Quaternion.identity;
			Vector3 localPosition = Vector3.zero;
			Vector3 worldPosition = piece.transform.position;
			Quaternion worldRotation = piece.transform.rotation;
			if (piece2 != null)
			{
				piece.BumpTwistToPositionRotation(action.twist, action.bumpOffsetx, action.bumpOffsetz, action.attachIndex, piece2.gridPlanes[action.parentAttachIndex], out localPosition, out localRotation, out worldPosition, out worldRotation);
			}
			piece.transform.SetPositionAndRotation(worldPosition, worldRotation);
			BuilderPiece.State state = BuilderPiece.State.AttachedToDropped;
			state = ((!(piece2 == null)) ? ((!piece2.isArmShelf && piece2.state != BuilderPiece.State.AttachedToArm) ? ((!piece2.isBuiltIntoTable && piece2.state != BuilderPiece.State.AttachedAndPlaced) ? ((piece2.state == BuilderPiece.State.Grabbed) ? BuilderPiece.State.Grabbed : ((piece2.state != BuilderPiece.State.GrabbedLocal) ? BuilderPiece.State.AttachedToDropped : BuilderPiece.State.GrabbedLocal)) : BuilderPiece.State.AttachedAndPlaced) : BuilderPiece.State.AttachedToArm) : BuilderPiece.State.AttachedAndPlaced);
			BuilderPiece rootPiece = piece2.GetRootPiece();
			gridPlaneData.Clear();
			checkGridPlaneData.Clear();
			allPotentialPlacements.Clear();
			tempPieceSet.Clear();
			QueryParameters queryParameters = new QueryParameters
			{
				layerMask = allPiecesMask
			};
			OverlapSphereCommand value = new OverlapSphereCommand(worldPosition, 1f, queryParameters);
			nearbyPiecesCommands[0] = value;
			OverlapSphereCommand.ScheduleBatch(nearbyPiecesCommands, nearbyPiecesResults, 1, 1024).Complete();
			for (int i = 0; i < 1024 && nearbyPiecesResults[i].instanceID != 0; i++)
			{
				BuilderPiece pieceInHand = piece;
				BuilderPiece builderPieceFromCollider = BuilderPiece.GetBuilderPieceFromCollider(nearbyPiecesResults[i].collider);
				if (!(builderPieceFromCollider != null) || tempPieceSet.Contains(builderPieceFromCollider))
				{
					continue;
				}
				tempPieceSet.Add(builderPieceFromCollider);
				if (CanPiecesPotentiallyOverlap(pieceInHand, rootPiece, state, builderPieceFromCollider))
				{
					for (int j = 0; j < builderPieceFromCollider.gridPlanes.Count; j++)
					{
						checkGridPlaneData.Add(new BuilderGridPlaneData(builderPieceFromCollider.gridPlanes[j], -1));
					}
				}
			}
			BuilderTableJobs.BuildTestPieceListForJob(piece, gridPlaneData);
			BuilderPotentialPlacement potentialPlacement = new BuilderPotentialPlacement
			{
				localPosition = localPosition,
				localRotation = localRotation,
				attachIndex = action.attachIndex,
				parentAttachIndex = action.parentAttachIndex,
				attachPiece = piece,
				parentPiece = piece2
			};
			CalcAllPotentialPlacements(gridPlaneData, checkGridPlaneData, potentialPlacement, allPotentialPlacements);
			piece.SetParentPiece(action.attachIndex, piece2, action.parentAttachIndex);
			for (int k = 0; k < allPotentialPlacements.Count; k++)
			{
				BuilderPotentialPlacement builderPotentialPlacement = allPotentialPlacements[k];
				BuilderAttachGridPlane builderAttachGridPlane = builderPotentialPlacement.attachPiece.gridPlanes[builderPotentialPlacement.attachIndex];
				BuilderAttachGridPlane builderAttachGridPlane2 = builderPotentialPlacement.parentPiece.gridPlanes[builderPotentialPlacement.parentAttachIndex];
				BuilderAttachGridPlane movingParentGrid = builderAttachGridPlane.GetMovingParentGrid();
				bool flag2 = movingParentGrid != null;
				BuilderAttachGridPlane movingParentGrid2 = builderAttachGridPlane2.GetMovingParentGrid();
				bool flag3 = movingParentGrid2 != null;
				if (flag2 == flag3 && (!flag2 || !(movingParentGrid != movingParentGrid2)))
				{
					SnapOverlap newOverlap = builderPool.CreateSnapOverlap(builderAttachGridPlane2, builderPotentialPlacement.attachBounds);
					builderAttachGridPlane.AddSnapOverlap(newOverlap);
					SnapOverlap newOverlap2 = builderPool.CreateSnapOverlap(builderAttachGridPlane, builderPotentialPlacement.parentAttachBounds);
					builderAttachGridPlane2.AddSnapOverlap(newOverlap2);
				}
			}
			piece.transform.SetLocalPositionAndRotation(localPosition, localRotation);
			if (piece2 != null && piece2.state == BuilderPiece.State.GrabbedLocal)
			{
				BuilderPiece rootPiece2 = piece2.GetRootPiece();
				BuilderPieceInteractor.instance.OnCountChangedForRoot(rootPiece2);
			}
			if (piece2 == null)
			{
				piece.SetActivateTimeStamp(action.timeStamp);
				piece.SetState(BuilderPiece.State.AttachedAndPlaced);
				SetIsDirty(dirty: true);
				if (flag)
				{
					BuilderPieceInteractor.instance.DisableCollisionsWithHands();
				}
			}
			else if (piece2.isArmShelf || piece2.state == BuilderPiece.State.AttachedToArm)
			{
				piece.SetState(BuilderPiece.State.AttachedToArm);
			}
			else if (piece2.isBuiltIntoTable || piece2.state == BuilderPiece.State.AttachedAndPlaced)
			{
				piece.SetActivateTimeStamp(action.timeStamp);
				piece.SetState(BuilderPiece.State.AttachedAndPlaced);
				if (piece2 != null)
				{
					BuilderPiece attachedBuiltInPiece = piece2.GetAttachedBuiltInPiece();
					if (attachedBuiltInPiece != null && attachedBuiltInPiece.TryGetPlotComponent(out var plot))
					{
						plot.OnPieceAttachedToPlot(piece);
					}
				}
				SetIsDirty(dirty: true);
				if (flag)
				{
					BuilderPieceInteractor.instance.DisableCollisionsWithHands();
				}
			}
			else if (piece2.state == BuilderPiece.State.Grabbed)
			{
				piece.SetState(BuilderPiece.State.Grabbed);
			}
			else if (piece2.state == BuilderPiece.State.GrabbedLocal)
			{
				piece.SetState(BuilderPiece.State.GrabbedLocal);
			}
			else
			{
				piece.SetState(BuilderPiece.State.AttachedToDropped);
			}
			break;
		}
		case BuilderActionType.DetachFromPiece:
		{
			BuilderPiece piece3 = piece;
			bool num3 = piece.state == BuilderPiece.State.GrabbedLocal;
			if (num3)
			{
				piece3 = piece.GetRootPiece();
			}
			if (piece.state == BuilderPiece.State.AttachedAndPlaced)
			{
				SetIsDirty(dirty: true);
				BuilderPiece attachedBuiltInPiece2 = piece.GetAttachedBuiltInPiece();
				if (attachedBuiltInPiece2 != null && attachedBuiltInPiece2.TryGetPlotComponent(out var plot2))
				{
					plot2.OnPieceDetachedFromPlot(piece);
				}
			}
			piece.ClearParentHeld();
			piece.ClearParentPiece();
			piece.transform.localScale = Vector3.one;
			if (num3)
			{
				BuilderPieceInteractor.instance.OnCountChangedForRoot(piece3);
			}
			break;
		}
		case BuilderActionType.MakePieceRoot:
			BuilderPiece.MakePieceRoot(piece);
			break;
		case BuilderActionType.DropPiece:
			piece.ClearParentHeld();
			piece.ClearParentPiece();
			piece.transform.localScale = Vector3.one;
			piece.SetState(BuilderPiece.State.Dropped);
			piece.transform.SetLocalPositionAndRotation(action.localPosition, action.localRotation);
			if (piece.rigidBody != null)
			{
				piece.rigidBody.position = action.localPosition;
				piece.rigidBody.rotation = action.localRotation;
				piece.rigidBody.linearVelocity = action.velocity;
				piece.rigidBody.angularVelocity = action.angVelocity;
			}
			break;
		case BuilderActionType.AttachToShelf:
		{
			piece.ClearParentHeld();
			piece.ClearParentPiece();
			int attachIndex = action.attachIndex;
			bool isLeftHand = action.isLeftHand;
			int parentAttachIndex = action.parentAttachIndex;
			float x = action.velocity.x;
			piece.transform.localScale = Vector3.one;
			piece.SetState(isLeftHand ? BuilderPiece.State.OnConveyor : BuilderPiece.State.OnShelf);
			if (isLeftHand)
			{
				if (attachIndex >= 0 && attachIndex < conveyors.Count)
				{
					BuilderConveyor builderConveyor = conveyors[attachIndex];
					float num = x / builderConveyor.GetFrameMovement();
					if ((uint)PhotonNetwork.ServerTimestamp >= (uint)parentAttachIndex)
					{
						uint num2 = (uint)(PhotonNetwork.ServerTimestamp - parentAttachIndex);
						num += (float)num2 / 1000f;
					}
					piece.shelfOwner = attachIndex;
					builderConveyor.OnShelfPieceCreated(piece, num);
				}
			}
			else if (attachIndex >= 0 && attachIndex < dispenserShelves.Count)
			{
				BuilderDispenserShelf builderDispenserShelf = dispenserShelves[attachIndex];
				piece.shelfOwner = attachIndex;
				builderDispenserShelf.OnShelfPieceCreated(piece, playfx: false);
			}
			else
			{
				piece.transform.SetLocalPositionAndRotation(action.localPosition, action.localRotation);
			}
			break;
		}
		}
	}

	public static bool AreStatesCompatibleForOverlap(BuilderPiece.State stateA, BuilderPiece.State stateB, BuilderPiece rootA, BuilderPiece rootB)
	{
		switch (stateA)
		{
		case BuilderPiece.State.None:
			return false;
		case BuilderPiece.State.AttachedAndPlaced:
			return stateB == BuilderPiece.State.AttachedAndPlaced;
		case BuilderPiece.State.AttachedToDropped:
		case BuilderPiece.State.Dropped:
		case BuilderPiece.State.OnShelf:
		case BuilderPiece.State.OnConveyor:
			if (stateB == BuilderPiece.State.AttachedToDropped || stateB == BuilderPiece.State.Dropped || stateB == BuilderPiece.State.OnShelf || stateB == BuilderPiece.State.OnConveyor)
			{
				return rootA.Equals(rootB);
			}
			return false;
		case BuilderPiece.State.Grabbed:
			if (stateB == BuilderPiece.State.Grabbed)
			{
				return rootA.Equals(rootB);
			}
			return false;
		case BuilderPiece.State.Displayed:
			return false;
		case BuilderPiece.State.GrabbedLocal:
			if (stateB == BuilderPiece.State.GrabbedLocal)
			{
				return rootA.heldInLeftHand == rootB.heldInLeftHand;
			}
			return false;
		case BuilderPiece.State.AttachedToArm:
		{
			if (stateB != BuilderPiece.State.AttachedToArm)
			{
				return false;
			}
			BuilderPiece obj = ((rootA.parentPiece != null) ? rootA.parentPiece : rootA);
			BuilderPiece obj2 = ((rootB.parentPiece != null) ? rootB.parentPiece : rootB);
			return obj.Equals(obj2);
		}
		default:
			return false;
		}
	}

	private void Awake()
	{
		if (zoneToInstance == null)
		{
			zoneToInstance = new Dictionary<GTZone, BuilderTable>(2);
		}
		if (!zoneToInstance.TryAdd(tableZone, this))
		{
			UnityEngine.Object.Destroy(this);
		}
		acceptableSqrDistFromCenter = Mathf.Pow(217f * pieceScale, 2f);
		if (buttonSnapRotation != null)
		{
			buttonSnapRotation.Setup(OnButtonFreeRotation);
			buttonSnapRotation.SetPressed(useSnapRotation);
		}
		if (buttonSnapPosition != null)
		{
			buttonSnapPosition.Setup(OnButtonFreePosition);
			buttonSnapPosition.SetPressed(usePlacementStyle != BuilderPlacementStyle.Float);
		}
		if (buttonSaveLayout != null)
		{
			buttonSaveLayout.Setup(OnButtonSaveLayout);
		}
		if (buttonClearLayout != null)
		{
			buttonClearLayout.Setup(OnButtonClearLayout);
		}
		isSetup = false;
		nextPieceId = 10000;
		placedLayer = LayerMask.NameToLayer("Gorilla Object");
		heldLayerLocal = LayerMask.NameToLayer("Prop");
		heldLayer = LayerMask.NameToLayer("BuilderProp");
		droppedLayer = LayerMask.NameToLayer("BuilderProp");
		currSnapParams = pushAndEaseParams;
		tableState = TableState.WaitingForZoneAndRoom;
		inRoom = false;
		inBuilderZone = false;
		builderNetworking.SetTable(this);
		plotOwners = new Dictionary<int, int>(10);
		doesLocalPlayerOwnPlot = false;
		queuedBuildCommands = new List<BuilderCommand>(1028);
		if (isTableMutable)
		{
			playerToArmShelfLeft = new Dictionary<int, int>(10);
			playerToArmShelfRight = new Dictionary<int, int>(10);
			rollBackBufferedCommands = new List<BuilderCommand>(1028);
			rollBackActions = new List<BuilderAction>(1028);
			rollForwardCommands = new List<BuilderCommand>(1028);
			droppedPieces = new List<BuilderPiece>(DROPPED_PIECE_LIMIT + 50);
			droppedPieceData = new List<DroppedPieceData>(DROPPED_PIECE_LIMIT + 50);
			SetupMonkeBlocksRoom();
			gridPlaneData = new NativeList<BuilderGridPlaneData>(1024, Allocator.Persistent);
			checkGridPlaneData = new NativeList<BuilderGridPlaneData>(1024, Allocator.Persistent);
			nearbyPiecesResults = new NativeArray<ColliderHit>(1024, Allocator.Persistent);
			nearbyPiecesCommands = new NativeArray<OverlapSphereCommand>(1, Allocator.Persistent);
			allPotentialPlacements = new List<BuilderPotentialPlacement>(1024);
		}
		else
		{
			rollBackBufferedCommands = new List<BuilderCommand>(128);
			rollBackActions = new List<BuilderAction>(128);
			rollForwardCommands = new List<BuilderCommand>(128);
		}
		SetupResources();
		if (!isTableMutable && linkedTerminal != null)
		{
			linkedTerminal.Init(this);
		}
	}

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}

	public static bool TryGetBuilderTableForZone(GTZone zone, out BuilderTable table)
	{
		if (zoneToInstance == null)
		{
			table = null;
			return false;
		}
		return zoneToInstance.TryGetValue(zone, out table);
	}

	private void SetupMonkeBlocksRoom()
	{
		if (shelves == null)
		{
			shelves = new List<BuilderShelf>(64);
		}
		if (shelvesRoot != null)
		{
			shelvesRoot.GetComponentsInChildren(shelves);
		}
		conveyors = new List<BuilderConveyor>(32);
		dispenserShelves = new List<BuilderDispenserShelf>(32);
		if (allShelvesRoot != null)
		{
			for (int i = 0; i < allShelvesRoot.Count; i++)
			{
				allShelvesRoot[i].GetComponentsInChildren(tempConveyors);
				conveyors.AddRange(tempConveyors);
				tempConveyors.Clear();
				allShelvesRoot[i].GetComponentsInChildren(tempDispensers);
				dispenserShelves.AddRange(tempDispensers);
				tempDispensers.Clear();
			}
		}
		recyclers = new List<BuilderRecycler>(5);
		if (recyclerRoot != null)
		{
			for (int j = 0; j < recyclerRoot.Count; j++)
			{
				recyclerRoot[j].GetComponentsInChildren(tempRecyclers);
				recyclers.AddRange(tempRecyclers);
				tempRecyclers.Clear();
			}
		}
		for (int k = 0; k < recyclers.Count; k++)
		{
			recyclers[k].recyclerID = k;
			recyclers[k].table = this;
		}
		dropZones = new List<BuilderDropZone>(6);
		dropZoneRoot.GetComponentsInChildren(dropZones);
		for (int l = 0; l < dropZones.Count; l++)
		{
			dropZones[l].dropZoneID = l;
			dropZones[l].table = this;
		}
		foreach (BuilderResourceMeter resourceMeter in resourceMeters)
		{
			resourceMeter.table = this;
		}
	}

	private void SetupResources()
	{
		maxResources = new int[3];
		if (totalResources != null && totalResources.quantities != null)
		{
			for (int i = 0; i < totalResources.quantities.Count; i++)
			{
				if (totalResources.quantities[i].type >= BuilderResourceType.Basic && totalResources.quantities[i].type < BuilderResourceType.Count)
				{
					maxResources[(int)totalResources.quantities[i].type] += totalResources.quantities[i].count;
				}
			}
		}
		usedResources = new int[3];
		reservedResources = new int[3];
		if (totalReservedResources != null && totalReservedResources.quantities != null)
		{
			for (int j = 0; j < totalReservedResources.quantities.Count; j++)
			{
				if (totalReservedResources.quantities[j].type >= BuilderResourceType.Basic && totalReservedResources.quantities[j].type < BuilderResourceType.Count)
				{
					reservedResources[(int)totalReservedResources.quantities[j].type] += totalReservedResources.quantities[j].count;
				}
			}
		}
		plotMaxResources = new int[3];
		if (resourcesPerPrivatePlot != null && resourcesPerPrivatePlot.quantities != null)
		{
			for (int k = 0; k < resourcesPerPrivatePlot.quantities.Count; k++)
			{
				if (resourcesPerPrivatePlot.quantities[k].type >= BuilderResourceType.Basic && resourcesPerPrivatePlot.quantities[k].type < BuilderResourceType.Count)
				{
					plotMaxResources[(int)resourcesPerPrivatePlot.quantities[k].type] += resourcesPerPrivatePlot.quantities[k].count;
				}
			}
		}
		OnAvailableResourcesChange();
	}

	private void Start()
	{
		if (NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom != inRoom)
		{
			SetInRoom(NetworkSystem.Instance.InRoom);
		}
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(HandleOnZoneChanged));
		HandleOnZoneChanged();
		RequestTableConfiguration();
		FetchSharedBlocksStartingMapConfig();
		PlayFabTitleDataCache.Instance.OnTitleDataUpdate.AddListener(OnTitleDataUpdate);
	}

	private void OnApplicationQuit()
	{
		ClearTable();
		tableState = TableState.WaitingForZoneAndRoom;
	}

	private void OnDestroy()
	{
		PlayFabTitleDataCache.Instance.OnTitleDataUpdate.RemoveListener(OnTitleDataUpdate);
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Remove(instance.onZoneChanged, new Action(HandleOnZoneChanged));
		if (isTableMutable)
		{
			if (gridPlaneData.IsCreated)
			{
				gridPlaneData.Dispose();
			}
			if (checkGridPlaneData.IsCreated)
			{
				checkGridPlaneData.Dispose();
			}
			if (nearbyPiecesResults.IsCreated)
			{
				nearbyPiecesResults.Dispose();
			}
			if (nearbyPiecesCommands.IsCreated)
			{
				nearbyPiecesCommands.Dispose();
			}
		}
		DestroyData();
	}

	private void HandleOnZoneChanged()
	{
		bool flag = ZoneManagement.instance.IsZoneActive(tableZone);
		SetInBuilderZone(flag);
	}

	public void InitIfNeeded()
	{
		if (isSetup || BuilderSetManager.instance == null)
		{
			return;
		}
		BuilderSetManager.instance.InitPieceDictionary();
		builderRenderer.BuildRenderer(BuilderSetManager.pieceList);
		baseGridPlanes.Clear();
		basePieces = new List<BuilderPiece>(1024);
		for (int i = 0; i < builtInPieceRoots.Count; i++)
		{
			builtInPieceRoots[i].SetActive(value: true);
			builtInPieceRoots[i].GetComponentsInChildren(includeInactive: false, tempPieces);
			basePieces.AddRange(tempPieces);
		}
		allPrivatePlots = new List<BuilderPiecePrivatePlot>(20);
		CreateData();
		for (int j = 0; j < basePieces.Count; j++)
		{
			BuilderPiece builderPiece = basePieces[j];
			builderPiece.SetTable(this);
			builderPiece.pieceId = 5 + j;
			builderPiece.SetScale(pieceScale);
			builderPiece.SetupPiece(gridSize);
			builderPiece.OnCreate();
			builderPiece.SetState(BuilderPiece.State.OnShelf, force: true);
			baseGridPlanes.AddRange(builderPiece.gridPlanes);
			if (builderPiece.IsPrivatePlot() && builderPiece.TryGetPlotComponent(out var plot))
			{
				allPrivatePlots.Add(plot);
			}
			AddPieceData(builderPiece);
		}
		builderPool = BuilderPool.instance;
		builderPool.Setup();
		StartCoroutine(builderPool.BuildFromPieceSets());
		BoxCollider[] array;
		if (isTableMutable)
		{
			for (int k = 0; k < conveyors.Count; k++)
			{
				conveyors[k].table = this;
				conveyors[k].shelfID = k;
				conveyors[k].Setup();
			}
			for (int l = 0; l < dispenserShelves.Count; l++)
			{
				dispenserShelves[l].table = this;
				dispenserShelves[l].shelfID = l;
				dispenserShelves[l].Setup();
			}
			conveyorManager.Setup(this);
			repelledPieceRoots = new HashSet<int>[repelHistoryLength];
			for (int m = 0; m < repelHistoryLength; m++)
			{
				repelledPieceRoots[m] = new HashSet<int>(10);
			}
			sharedBuildAreas = sharedBuildArea.GetComponents<BoxCollider>();
			array = sharedBuildAreas;
			for (int n = 0; n < array.Length; n++)
			{
				array[n].enabled = false;
			}
			sharedBuildArea.SetActive(value: false);
		}
		BoxCollider[] components = noBlocksArea.GetComponents<BoxCollider>();
		noBlocksAreas = new List<BoxCheckParams>(components.Length);
		array = components;
		foreach (BoxCollider boxCollider in array)
		{
			boxCollider.enabled = true;
			BoxCheckParams item = new BoxCheckParams
			{
				center = boxCollider.transform.TransformPoint(boxCollider.center),
				halfExtents = Vector3.Scale(boxCollider.transform.lossyScale, boxCollider.size) / 2f,
				rotation = boxCollider.transform.rotation
			};
			noBlocksAreas.Add(item);
			boxCollider.enabled = false;
		}
		noBlocksArea.SetActive(value: false);
		isSetup = true;
	}

	private void SetIsDirty(bool dirty)
	{
		if (isDirty != dirty)
		{
			OnSaveDirtyChanged?.Invoke(dirty);
		}
		isDirty = dirty;
	}

	private void FixedUpdate()
	{
		if (tableState != TableState.Ready && tableState != TableState.WaitForMasterResync)
		{
			return;
		}
		foreach (IBuilderPieceFunctional item in funcComponentsToRegisterFixed)
		{
			if (item != null)
			{
				fixedUpdateFunctionalComponents.Add(item);
			}
		}
		foreach (IBuilderPieceFunctional item2 in funcComponentsToUnregisterFixed)
		{
			fixedUpdateFunctionalComponents.Remove(item2);
		}
		funcComponentsToRegisterFixed.Clear();
		funcComponentsToUnregisterFixed.Clear();
		foreach (IBuilderPieceFunctional fixedUpdateFunctionalComponent in fixedUpdateFunctionalComponents)
		{
			fixedUpdateFunctionalComponent.FunctionalPieceFixedUpdate();
		}
	}

	public void Tick()
	{
		RunUpdate();
	}

	private void RunUpdate()
	{
		InitIfNeeded();
		UpdateTableState();
		if (isTableMutable)
		{
			UpdateDroppedPieces(Time.deltaTime);
			repelHistoryIndex = (repelHistoryIndex + 1) % repelHistoryLength;
			int num = (repelHistoryIndex + 1) % repelHistoryLength;
			repelledPieceRoots[num].Clear();
		}
	}

	public void AddQueuedCommand(BuilderCommand cmd)
	{
		queuedBuildCommands.Add(cmd);
	}

	public void ClearQueuedCommands()
	{
		if (queuedBuildCommands != null)
		{
			queuedBuildCommands.Clear();
		}
		RemoveRollBackActions();
		if (rollBackBufferedCommands != null)
		{
			rollBackBufferedCommands.Clear();
		}
		RemoveRollForwardCommands();
	}

	public int GetNumQueuedCommands()
	{
		if (queuedBuildCommands != null)
		{
			return queuedBuildCommands.Count;
		}
		return 0;
	}

	public void AddRollbackAction(BuilderAction action)
	{
		rollBackActions.Add(action);
	}

	public void RemoveRollBackActions()
	{
		rollBackActions.Clear();
	}

	public void RemoveRollBackActions(int localCommandId)
	{
		for (int num = rollBackActions.Count - 1; num >= 0; num--)
		{
			if (localCommandId == -1 || rollBackActions[num].localCommandId == localCommandId)
			{
				rollBackActions.RemoveAt(num);
			}
		}
	}

	public bool HasRollBackActionsForCommand(int localCommandId)
	{
		for (int i = 0; i < rollBackActions.Count; i++)
		{
			if (rollBackActions[i].localCommandId == localCommandId)
			{
				return true;
			}
		}
		return false;
	}

	public void AddRollForwardCommand(BuilderCommand command)
	{
		rollForwardCommands.Add(command);
	}

	public void RemoveRollForwardCommands()
	{
		rollForwardCommands.Clear();
	}

	public void RemoveRollForwardCommands(int localCommandId)
	{
		for (int num = rollForwardCommands.Count - 1; num >= 0; num--)
		{
			if (localCommandId == -1 || rollForwardCommands[num].localCommandId == localCommandId)
			{
				rollForwardCommands.RemoveAt(num);
			}
		}
	}

	public bool HasRollForwardCommand(int localCommandId)
	{
		for (int i = 0; i < rollForwardCommands.Count; i++)
		{
			if (rollForwardCommands[i].localCommandId == localCommandId)
			{
				return true;
			}
		}
		return false;
	}

	public bool ShouldRollbackBufferCommand(BuilderCommand cmd)
	{
		if (cmd.type == BuilderCommandType.Create || cmd.type == BuilderCommandType.CreateArmShelf)
		{
			return false;
		}
		if (rollBackActions.Count > 0)
		{
			if (cmd.player != null && cmd.player.IsLocal)
			{
				return !HasRollForwardCommand(cmd.localCommandId);
			}
			return true;
		}
		return false;
	}

	public void AddRollbackBufferedCommand(BuilderCommand bufferedCmd)
	{
		rollBackBufferedCommands.Add(bufferedCmd);
	}

	private void ExecuteRollBackActions()
	{
		for (int num = rollBackActions.Count - 1; num >= 0; num--)
		{
			ExecuteAction(rollBackActions[num]);
		}
		rollBackActions.Clear();
	}

	private void ExecuteRollbackBufferedCommands()
	{
		for (int i = 0; i < rollBackBufferedCommands.Count; i++)
		{
			BuilderCommand cmd = rollBackBufferedCommands[i];
			cmd.isQueued = false;
			cmd.canRollback = false;
			ExecuteBuildCommand(cmd);
		}
		rollBackBufferedCommands.Clear();
	}

	private void ExecuteRollForwardCommands()
	{
		tempRollForwardCommands.Clear();
		for (int i = 0; i < rollForwardCommands.Count; i++)
		{
			tempRollForwardCommands.Add(rollForwardCommands[i]);
		}
		rollForwardCommands.Clear();
		for (int j = 0; j < tempRollForwardCommands.Count; j++)
		{
			BuilderCommand cmd = tempRollForwardCommands[j];
			cmd.isQueued = true;
			cmd.canRollback = true;
			ExecuteBuildCommand(cmd);
		}
		tempRollForwardCommands.Clear();
	}

	private void UpdateRollForwardCommandData()
	{
		for (int i = 0; i < rollForwardCommands.Count; i++)
		{
			BuilderCommand value = rollForwardCommands[i];
			if (value.type == BuilderCommandType.Drop)
			{
				BuilderPiece piece = GetPiece(value.pieceId);
				if (piece != null && piece.rigidBody != null)
				{
					value.localPosition = piece.rigidBody.position;
					value.localRotation = piece.rigidBody.rotation;
					value.velocity = piece.rigidBody.linearVelocity;
					value.angVelocity = piece.rigidBody.angularVelocity;
					rollForwardCommands[i] = value;
				}
			}
		}
	}

	public bool TryRollbackAndReExecute(int localCommandId)
	{
		if (HasRollBackActionsForCommand(localCommandId))
		{
			if (rollBackBufferedCommands.Count > 0)
			{
				UpdateRollForwardCommandData();
				ExecuteRollBackActions();
				ExecuteRollbackBufferedCommands();
				ExecuteRollForwardCommands();
				RemoveRollBackActions(localCommandId);
				RemoveRollForwardCommands(localCommandId);
			}
			else
			{
				RemoveRollBackActions(localCommandId);
				RemoveRollForwardCommands(localCommandId);
			}
			return true;
		}
		return false;
	}

	public void RollbackFailedCommand(int localCommandId)
	{
		if (HasRollBackActionsForCommand(localCommandId))
		{
			UpdateRollForwardCommandData();
			ExecuteRollBackActions();
			ExecuteRollbackBufferedCommands();
			RemoveRollForwardCommands(-1);
			ExecuteRollForwardCommands();
		}
	}

	public TableState GetTableState()
	{
		return tableState;
	}

	public void SetTableState(TableState newState)
	{
		InitIfNeeded();
		if (newState == tableState)
		{
			return;
		}
		_ = tableState;
		_ = 3;
		tableState = newState;
		switch (tableState)
		{
		case TableState.WaitForInitialBuildMaster:
			nextPieceId = 10000;
			if (isTableMutable)
			{
				BuildInitialTableForPlayer();
			}
			else
			{
				BuildSelectedSharedMap();
			}
			break;
		case TableState.WaitingForInitalBuild:
			if (!isTableMutable && !NetworkSystem.Instance.IsMasterClient)
			{
				sharedBlocksMap = null;
				OnMapCleared?.Invoke();
			}
			break;
		case TableState.WaitForMasterResync:
			ClearQueuedCommands();
			ResetConveyors();
			break;
		case TableState.Ready:
			OnAvailableResourcesChange();
			if (!isTableMutable)
			{
				string arg = ((sharedBlocksMap == null) ? "" : sharedBlocksMap.MapID);
				OnMapLoaded?.Invoke(arg);
				SetPendingMap(null);
			}
			break;
		case TableState.WaitingForSharedMapLoad:
			ClearTable();
			ClearQueuedCommands();
			builderNetworking.ResetSerializedTableForAllPlayers();
			break;
		case TableState.BadData:
			ClearTable();
			ClearQueuedCommands();
			break;
		case TableState.ReceivingInitialBuild:
		case TableState.ReceivingMasterResync:
		case TableState.InitialBuild:
		case TableState.ExecuteQueuedCommands:
			break;
		}
	}

	public void SetPendingMap(string mapID)
	{
		pendingMapID = mapID;
	}

	public string GetPendingMap()
	{
		return pendingMapID;
	}

	public string GetCurrentMapID()
	{
		return sharedBlocksMap?.MapID;
	}

	public void LoadSharedMap(SharedBlocksManager.SharedBlocksMap map)
	{
		if (NetworkSystem.Instance.InRoom)
		{
			if (map.MapID.IsNullOrEmpty())
			{
				GTDev.LogWarning("Invalid map to load");
				OnMapLoadFailed?.Invoke("Invalid Map ID");
			}
			else if (tableState != TableState.Ready && tableState != TableState.BadData)
			{
				OnMapLoadFailed?.Invoke("WAIT FOR LOAD IN PROGRESS");
			}
			else
			{
				builderNetworking.RequestLoadSharedBlocksMap(map.MapID);
			}
		}
		else
		{
			OnMapLoadFailed?.Invoke("Not In Room");
		}
	}

	public void SetInRoom(bool inRoom)
	{
		this.inRoom = inRoom;
		bool flag = inRoom && inBuilderZone;
		if (!inRoom)
		{
			pendingMapID = null;
			sharedBlocksMap = null;
			OnMapCleared?.Invoke();
		}
		if (flag && tableState == TableState.WaitingForZoneAndRoom)
		{
			SetTableState(TableState.WaitingForInitalBuild);
			builderNetworking.PlayerEnterBuilder();
		}
		else if (!flag && tableState != TableState.WaitingForZoneAndRoom && !builderNetworking.IsPrivateMasterClient())
		{
			SetTableState(TableState.WaitingForZoneAndRoom);
			builderNetworking.PlayerExitBuilder();
		}
		else if (flag && PhotonNetwork.IsMasterClient && isTableMutable)
		{
			builderNetworking.RequestCreateArmShelfForPlayer(PhotonNetwork.LocalPlayer);
		}
		else if (!flag && builderNetworking.IsPrivateMasterClient() && isTableMutable)
		{
			RemoveArmShelfForPlayer(PhotonNetwork.LocalPlayer);
		}
	}

	public static bool IsLocalPlayerInBuilderZone()
	{
		ZoneEntityBSP zoneEntityBSP = GorillaTagger.Instance?.offlineVRRig?.zoneEntity;
		if (zoneEntityBSP == null)
		{
			return false;
		}
		if (!TryGetBuilderTableForZone(zoneEntityBSP.currentZone, out var table))
		{
			return false;
		}
		return table.IsInBuilderZone();
	}

	public bool IsInBuilderZone()
	{
		return inBuilderZone;
	}

	public void SetInBuilderZone(bool inBuilderZone)
	{
		this.inBuilderZone = inBuilderZone;
		ShowPieces(inBuilderZone);
		bool flag = inRoom && inBuilderZone;
		if (flag && tableState == TableState.WaitingForZoneAndRoom)
		{
			SetTableState(TableState.WaitingForInitalBuild);
			builderNetworking.PlayerEnterBuilder();
		}
		else if (!flag && tableState != TableState.WaitingForZoneAndRoom && !builderNetworking.IsPrivateMasterClient())
		{
			SetTableState(TableState.WaitingForZoneAndRoom);
			builderNetworking.PlayerExitBuilder();
		}
		else if (flag && PhotonNetwork.IsMasterClient)
		{
			builderNetworking.RequestCreateArmShelfForPlayer(PhotonNetwork.LocalPlayer);
		}
		else if (!flag && builderNetworking.IsPrivateMasterClient())
		{
			RemoveArmShelfForPlayer(PhotonNetwork.LocalPlayer);
		}
	}

	private void ShowPieces(bool show)
	{
		if (builderRenderer != null)
		{
			builderRenderer.Show(show);
		}
		if (pieces != null && basePieces != null)
		{
			for (int i = 0; i < pieces.Count; i++)
			{
				pieces[i].SetDirectRenderersVisible(show);
			}
			for (int j = 0; j < basePieces.Count; j++)
			{
				basePieces[j].SetDirectRenderersVisible(show);
			}
		}
	}

	private void UpdateTableState()
	{
		switch (tableState)
		{
		case TableState.InitialBuild:
		{
			BuilderTableNetworking.PlayerTableInitState localTableInit = builderNetworking.GetLocalTableInit();
			try
			{
				ClearTable();
				ClearQueuedCommands();
				byte[] array = GZipStream.UncompressBuffer(localTableInit.serializedTableState);
				localTableInit.totalSerializedBytes = array.Length;
				Array.Copy(array, 0, localTableInit.serializedTableState, 0, localTableInit.totalSerializedBytes);
				DeserializeTableState(localTableInit.serializedTableState, localTableInit.numSerializedBytes);
				if (tableState != TableState.BadData)
				{
					SetTableState(TableState.ExecuteQueuedCommands);
					SetIsDirty(dirty: true);
				}
				break;
			}
			catch (Exception)
			{
				SetTableState(TableState.BadData);
				break;
			}
		}
		case TableState.ExecuteQueuedCommands:
		{
			for (int j = 0; j < queuedBuildCommands.Count; j++)
			{
				BuilderCommand cmd = queuedBuildCommands[j];
				cmd.isQueued = true;
				ExecuteBuildCommand(cmd);
			}
			queuedBuildCommands.Clear();
			SetTableState(TableState.Ready);
			break;
		}
		case TableState.Ready:
		{
			JobHandle jobHandle = default(JobHandle);
			if (isTableMutable)
			{
				conveyorManager.UpdateManager();
				jobHandle = conveyorManager.ConstructJobHandle();
				JobHandle.ScheduleBatchedJobs();
				foreach (BuilderDispenserShelf dispenserShelf in dispenserShelves)
				{
					dispenserShelf.UpdateShelf();
				}
				foreach (BuilderPiecePrivatePlot allPrivatePlot in allPrivatePlots)
				{
					allPrivatePlot.UpdatePlot();
				}
				foreach (BuilderRecycler recycler in recyclers)
				{
					recycler.UpdateRecycler();
				}
				for (int i = shelfSliceUpdateIndex; i < dispenserShelves.Count; i += SHELF_SLICE_BUCKETS)
				{
					dispenserShelves[i].UpdateShelfSliced();
				}
				shelfSliceUpdateIndex = (shelfSliceUpdateIndex + 1) % SHELF_SLICE_BUCKETS;
			}
			foreach (IBuilderPieceFunctional item in funcComponentsToRegister)
			{
				if (item != null)
				{
					activeFunctionalComponents.Add(item);
				}
			}
			foreach (IBuilderPieceFunctional item2 in funcComponentsToUnregister)
			{
				activeFunctionalComponents.Remove(item2);
			}
			funcComponentsToRegister.Clear();
			funcComponentsToUnregister.Clear();
			foreach (IBuilderPieceFunctional activeFunctionalComponent in activeFunctionalComponents)
			{
				activeFunctionalComponent?.FunctionalPieceUpdate();
			}
			if (!isTableMutable)
			{
				break;
			}
			foreach (BuilderResourceMeter resourceMeter in resourceMeters)
			{
				resourceMeter.UpdateMeterFill();
			}
			CleanUpDroppedPiece();
			jobHandle.Complete();
			break;
		}
		}
	}

	private void RouteNewCommand(BuilderCommand cmd, bool force)
	{
		bool flag = ShouldExecuteCommand();
		if (force)
		{
			ExecuteBuildCommand(cmd);
		}
		else if (flag && ShouldRollbackBufferCommand(cmd))
		{
			AddRollbackBufferedCommand(cmd);
		}
		else if (flag)
		{
			ExecuteBuildCommand(cmd);
		}
		else if (ShouldQueueCommand())
		{
			AddQueuedCommand(cmd);
		}
		else
		{
			ShouldDiscardCommand();
		}
	}

	private void ExecuteBuildCommand(BuilderCommand cmd)
	{
		if (isTableMutable || cmd.type == BuilderCommandType.FunctionalStateChange)
		{
			switch (cmd.type)
			{
			case BuilderCommandType.Create:
				ExecutePieceCreated(cmd);
				break;
			case BuilderCommandType.Place:
				ExecutePiecePlacedWithActions(cmd);
				break;
			case BuilderCommandType.Grab:
				ExecutePieceGrabbedWithActions(cmd);
				break;
			case BuilderCommandType.Drop:
				ExecutePieceDroppedWithActions(cmd);
				break;
			case BuilderCommandType.Paint:
				ExecutePiecePainted(cmd);
				break;
			case BuilderCommandType.Recycle:
				ExecutePieceRecycled(cmd);
				break;
			case BuilderCommandType.ClaimPlot:
				ExecuteClaimPlot(cmd);
				break;
			case BuilderCommandType.FreePlot:
				ExecuteFreePlot(cmd);
				break;
			case BuilderCommandType.CreateArmShelf:
				ExecuteArmShelfCreated(cmd);
				break;
			case BuilderCommandType.PlayerLeftRoom:
				ExecutePlayerLeftRoom(cmd);
				break;
			case BuilderCommandType.FunctionalStateChange:
				ExecuteSetFunctionalPieceState(cmd);
				break;
			case BuilderCommandType.SetSelection:
				ExecuteSetSelection(cmd);
				break;
			case BuilderCommandType.Repel:
				ExecutePieceRepelled(cmd);
				break;
			case BuilderCommandType.Remove:
				break;
			}
		}
	}

	public void ClearTable()
	{
		ClearTableInternal();
	}

	private void ClearTableInternal()
	{
		tempDeletePieces.Clear();
		for (int i = 0; i < pieces.Count; i++)
		{
			tempDeletePieces.Add(pieces[i]);
		}
		if (isTableMutable)
		{
			droppedPieces.Clear();
			droppedPieceData.Clear();
		}
		for (int j = 0; j < tempDeletePieces.Count; j++)
		{
			tempDeletePieces[j].ClearParentPiece();
			tempDeletePieces[j].ClearParentHeld();
			tempDeletePieces[j].SetState(BuilderPiece.State.None);
			RemovePiece(tempDeletePieces[j]);
		}
		for (int k = 0; k < tempDeletePieces.Count; k++)
		{
			builderPool.DestroyPiece(tempDeletePieces[k]);
		}
		tempDeletePieces.Clear();
		pieces.Clear();
		pieceIDToIndexCache.Clear();
		nextPieceId = 10000;
		if (isTableMutable)
		{
			conveyorManager.OnClearTable();
			foreach (BuilderDispenserShelf dispenserShelf in dispenserShelves)
			{
				dispenserShelf.OnClearTable();
			}
			for (int l = 0; l < repelHistoryLength; l++)
			{
				repelledPieceRoots[l].Clear();
			}
		}
		funcComponentsToRegister.Clear();
		funcComponentsToUnregister.Clear();
		activeFunctionalComponents.Clear();
		foreach (BuilderPiece basePiece in basePieces)
		{
			foreach (BuilderAttachGridPlane gridPlane in basePiece.gridPlanes)
			{
				gridPlane.OnReturnToPool(builderPool);
			}
		}
		if (isTableMutable)
		{
			ClearBuiltInPlots();
			playerToArmShelfLeft.Clear();
			playerToArmShelfRight.Clear();
			if (BuilderPieceInteractor.instance != null)
			{
				BuilderPieceInteractor.instance.RemovePiecesFromHands();
			}
		}
	}

	private void ClearBuiltInPlots()
	{
		foreach (BuilderPiecePrivatePlot allPrivatePlot in allPrivatePlots)
		{
			allPrivatePlot.ClearPlot();
		}
		plotOwners.Clear();
		SetLocalPlayerOwnsPlot(ownsPlot: false);
	}

	private void OnDeserializeUpdatePlots()
	{
		foreach (BuilderPiecePrivatePlot allPrivatePlot in allPrivatePlots)
		{
			allPrivatePlot.RecountPlotCost();
		}
	}

	public void BuildPiecesOnShelves()
	{
		if (!isTableMutable || shelves == null)
		{
			return;
		}
		for (int i = 0; i < shelves.Count; i++)
		{
			if (shelves[i] != null)
			{
				shelves[i].Init();
			}
		}
		bool flag = true;
		while (flag)
		{
			flag = false;
			for (int j = 0; j < shelves.Count; j++)
			{
				if (shelves[j].HasOpenSlot())
				{
					shelves[j].BuildNextPiece(this);
					if (shelves[j].HasOpenSlot())
					{
						flag = true;
					}
				}
			}
		}
	}

	private void OnFinishedInitialTableBuild()
	{
		BuildPiecesOnShelves();
		SetTableState(TableState.Ready);
		CreateArmShelvesForPlayersInBuilder();
	}

	public int CreatePieceId()
	{
		int result = nextPieceId;
		if (nextPieceId == int.MaxValue)
		{
			nextPieceId = 20000;
		}
		nextPieceId++;
		return result;
	}

	public void ResetConveyors()
	{
		if (!isTableMutable)
		{
			return;
		}
		foreach (BuilderConveyor conveyor in conveyors)
		{
			conveyor.ResetConveyorState();
		}
	}

	public void RequestCreateConveyorPiece(int newPieceType, int materialType, int shelfID)
	{
		if (shelfID >= 0 && shelfID < conveyors.Count)
		{
			BuilderConveyor builderConveyor = conveyors[shelfID];
			if (!(builderConveyor == null))
			{
				Transform spawnTransform = builderConveyor.GetSpawnTransform();
				builderNetworking.CreateShelfPiece(newPieceType, spawnTransform.position, spawnTransform.rotation, materialType, BuilderPiece.State.OnConveyor, shelfID);
			}
		}
	}

	public void RequestCreateDispenserShelfPiece(int pieceType, Vector3 position, Quaternion rotation, int materialType, int shelfID)
	{
		if (shelfID >= 0 && shelfID < dispenserShelves.Count && !(dispenserShelves[shelfID] == null))
		{
			builderNetworking.CreateShelfPiece(pieceType, position, rotation, materialType, BuilderPiece.State.OnShelf, shelfID);
		}
	}

	public void CreateConveyorPiece(int pieceType, int pieceId, Vector3 position, Quaternion rotation, int materialType, int shelfID, int sendTimestamp)
	{
		if (shelfID >= 0 && shelfID < conveyors.Count && !(conveyors[shelfID] == null))
		{
			BuilderCommand cmd = new BuilderCommand
			{
				type = BuilderCommandType.Create,
				pieceType = pieceType,
				pieceId = pieceId,
				localPosition = position,
				localRotation = rotation,
				materialType = materialType,
				state = BuilderPiece.State.OnConveyor,
				parentPieceId = shelfID,
				parentAttachIndex = sendTimestamp,
				player = NetworkSystem.Instance.MasterClient
			};
			RouteNewCommand(cmd, force: false);
		}
	}

	public void CreateDispenserShelfPiece(int pieceType, int pieceId, Vector3 position, Quaternion rotation, int materialType, int shelfID)
	{
		if (shelfID >= 0 && shelfID < dispenserShelves.Count && !(dispenserShelves[shelfID] == null))
		{
			BuilderCommand cmd = new BuilderCommand
			{
				type = BuilderCommandType.Create,
				pieceType = pieceType,
				pieceId = pieceId,
				localPosition = position,
				localRotation = rotation,
				materialType = materialType,
				state = BuilderPiece.State.OnShelf,
				parentPieceId = shelfID,
				isLeft = true,
				player = NetworkSystem.Instance.MasterClient
			};
			RouteNewCommand(cmd, force: false);
		}
	}

	public void RequestShelfSelection(int shelfId, int groupID, bool isConveyor)
	{
		if (tableState == TableState.Ready)
		{
			builderNetworking.RequestShelfSelection(shelfId, groupID, isConveyor);
		}
	}

	public void VerifySetSelections()
	{
		if (!isTableMutable)
		{
			return;
		}
		foreach (BuilderConveyor conveyor in conveyors)
		{
			conveyor.VerifySetSelection();
		}
		foreach (BuilderDispenserShelf dispenserShelf in dispenserShelves)
		{
			dispenserShelf.VerifySetSelection();
		}
	}

	public bool ValidateShelfSelectionParams(int shelfId, int displayGroupID, bool isConveyor, Player player)
	{
		bool flag = shelfId >= 0 && ((isConveyor && shelfId < conveyors.Count) || (!isConveyor && shelfId < dispenserShelves.Count)) && BuilderSetManager.instance.DoesPlayerOwnDisplayGroup(player, displayGroupID);
		if (PhotonNetwork.IsMasterClient)
		{
			if (isConveyor)
			{
				BuilderConveyor builderConveyor = conveyors[shelfId];
				bool flag2 = IsPlayerHandNearAction(NetPlayer.Get(player), builderConveyor.transform.position, isLeftHand: false, checkBothHands: true, 4f);
				flag = flag && flag2;
			}
			else
			{
				BuilderDispenserShelf builderDispenserShelf = dispenserShelves[shelfId];
				bool flag3 = IsPlayerHandNearAction(NetPlayer.Get(player), builderDispenserShelf.transform.position, isLeftHand: false, checkBothHands: true, 4f);
				flag = flag && flag3;
			}
		}
		return flag;
	}

	private void SetConveyorSelection(int conveyorId, int setId)
	{
		BuilderConveyor builderConveyor = conveyors[conveyorId];
		if (!(builderConveyor == null))
		{
			builderConveyor.SetSelection(setId);
		}
	}

	private void SetDispenserSelection(int conveyorId, int setId)
	{
		BuilderDispenserShelf builderDispenserShelf = dispenserShelves[conveyorId];
		if (!(builderDispenserShelf == null))
		{
			builderDispenserShelf.SetSelection(setId);
		}
	}

	public void ChangeSetSelection(int shelfID, int setID, bool isConveyor)
	{
		BuilderCommand cmd = new BuilderCommand
		{
			type = BuilderCommandType.SetSelection,
			parentPieceId = shelfID,
			pieceType = setID,
			isLeft = isConveyor,
			player = NetworkSystem.Instance.MasterClient
		};
		RouteNewCommand(cmd, force: false);
	}

	public void ExecuteSetSelection(BuilderCommand cmd)
	{
		bool isLeft = cmd.isLeft;
		int parentPieceId = cmd.parentPieceId;
		int pieceType = cmd.pieceType;
		if (isLeft)
		{
			SetConveyorSelection(parentPieceId, pieceType);
		}
		else
		{
			SetDispenserSelection(parentPieceId, pieceType);
		}
	}

	public bool ValidateFunctionalPieceState(int pieceID, byte state, NetPlayer player)
	{
		BuilderPiece piece = GetPiece(pieceID);
		if (piece == null)
		{
			return false;
		}
		if (piece.functionalPieceComponent == null)
		{
			return false;
		}
		if (NetworkSystem.Instance.IsMasterClient && !player.IsMasterClient && !IsPlayerHandNearAction(player, piece.transform.position, isLeftHand: true, checkBothHands: false, piece.functionalPieceComponent.GetInteractionDistace()))
		{
			return false;
		}
		return piece.functionalPieceComponent.IsStateValid(state);
	}

	public void OnFunctionalStateRequest(int pieceID, byte state, NetPlayer player, int timeStamp)
	{
		BuilderPiece piece = GetPiece(pieceID);
		if (!(piece == null) && piece.functionalPieceComponent != null && player != null)
		{
			piece.functionalPieceComponent.OnStateRequest(state, player, timeStamp);
		}
	}

	public void SetFunctionalPieceState(int pieceID, byte state, NetPlayer player, int timeStamp)
	{
		BuilderCommand cmd = new BuilderCommand
		{
			type = BuilderCommandType.FunctionalStateChange,
			pieceId = pieceID,
			twist = state,
			player = player,
			serverTimeStamp = timeStamp
		};
		RouteNewCommand(cmd, force: false);
	}

	public void ExecuteSetFunctionalPieceState(BuilderCommand cmd)
	{
		BuilderPiece piece = GetPiece(cmd.pieceId);
		if (!(piece == null))
		{
			piece.SetFunctionalPieceState(cmd.twist, cmd.player, cmd.serverTimeStamp);
		}
	}

	public void RegisterFunctionalPiece(IBuilderPieceFunctional component)
	{
		if (component != null)
		{
			funcComponentsToRegister.Add(component);
		}
	}

	public void UnregisterFunctionalPiece(IBuilderPieceFunctional component)
	{
		if (component != null)
		{
			funcComponentsToUnregister.Add(component);
		}
	}

	public void RegisterFunctionalPieceFixedUpdate(IBuilderPieceFunctional component)
	{
		if (component != null)
		{
			funcComponentsToRegisterFixed.Add(component);
		}
	}

	public void UnregisterFunctionalPieceFixedUpdate(IBuilderPieceFunctional component)
	{
		if (component != null)
		{
			funcComponentsToRegisterFixed.Remove(component);
		}
	}

	public void RequestCreatePiece(int newPieceType, Vector3 position, Quaternion rotation, int materialType)
	{
	}

	public void CreatePiece(int pieceType, int pieceId, Vector3 position, Quaternion rotation, int materialType, BuilderPiece.State state, Player player)
	{
		BuilderCommand cmd = new BuilderCommand
		{
			type = BuilderCommandType.Create,
			pieceType = pieceType,
			pieceId = pieceId,
			localPosition = position,
			localRotation = rotation,
			materialType = materialType,
			state = state,
			player = NetPlayer.Get(player)
		};
		RouteNewCommand(cmd, force: false);
	}

	public void RequestRecyclePiece(BuilderPiece piece, bool playFX, int recyclerID)
	{
		builderNetworking.RequestRecyclePiece(piece.pieceId, piece.transform.position, piece.transform.rotation, playFX, recyclerID);
	}

	public void RecyclePiece(int pieceId, Vector3 position, Quaternion rotation, bool playFX, int recyclerID, Player player)
	{
		BuilderCommand cmd = new BuilderCommand
		{
			type = BuilderCommandType.Recycle,
			pieceId = pieceId,
			localPosition = position,
			localRotation = rotation,
			player = NetPlayer.Get(player),
			isLeft = playFX,
			parentPieceId = recyclerID
		};
		RouteNewCommand(cmd, force: false);
	}

	private bool ShouldExecuteCommand()
	{
		if (tableState != TableState.Ready)
		{
			return tableState == TableState.WaitForInitialBuildMaster;
		}
		return true;
	}

	private bool ShouldQueueCommand()
	{
		if (tableState != TableState.ReceivingInitialBuild && tableState != TableState.ReceivingMasterResync && tableState != TableState.InitialBuild)
		{
			return tableState == TableState.ExecuteQueuedCommands;
		}
		return true;
	}

	private bool ShouldDiscardCommand()
	{
		if (tableState != TableState.WaitingForInitalBuild && tableState != TableState.WaitForInitialBuildMaster)
		{
			return tableState == TableState.WaitingForZoneAndRoom;
		}
		return true;
	}

	public bool DoesChainContainPiece(BuilderPiece targetPiece, BuilderPiece firstInChain, BuilderPiece nextInChain)
	{
		if (targetPiece == null || firstInChain == null)
		{
			return false;
		}
		if (targetPiece.Equals(firstInChain))
		{
			return true;
		}
		if (nextInChain == null)
		{
			return false;
		}
		if (targetPiece.Equals(nextInChain))
		{
			return true;
		}
		if (firstInChain == nextInChain)
		{
			return false;
		}
		return DoesChainContainPiece(targetPiece, firstInChain, nextInChain.parentPiece);
	}

	public bool DoesChainContainChain(BuilderPiece chainARoot, BuilderPiece chainBAttachPiece)
	{
		if (chainARoot == null || chainBAttachPiece == null)
		{
			return false;
		}
		if (DoesChainContainPiece(chainARoot, chainBAttachPiece, chainBAttachPiece.parentPiece))
		{
			return true;
		}
		BuilderPiece builderPiece = chainARoot.firstChildPiece;
		while (builderPiece != null)
		{
			if (DoesChainContainChain(builderPiece, chainBAttachPiece))
			{
				return true;
			}
			builderPiece = builderPiece.nextSiblingPiece;
		}
		return false;
	}

	private bool IsPlayerHandNearAction(NetPlayer player, Vector3 worldPosition, bool isLeftHand, bool checkBothHands, float acceptableRadius = 2.5f)
	{
		bool flag = true;
		if (player != null && VRRigCache.Instance != null && VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			if (isLeftHand || checkBothHands)
			{
				flag = (worldPosition - playerRig.Rig.leftHandTransform.position).sqrMagnitude < acceptableRadius * acceptableRadius;
			}
			if (!isLeftHand || checkBothHands)
			{
				float sqrMagnitude = (worldPosition - playerRig.Rig.rightHandTransform.position).sqrMagnitude;
				flag = flag && sqrMagnitude < acceptableRadius * acceptableRadius;
			}
		}
		return flag;
	}

	public bool ValidatePlacePieceParams(int pieceId, int attachPieceId, sbyte bumpOffsetX, sbyte bumpOffsetZ, byte twist, int parentPieceId, int attachIndex, int parentAttachIndex, NetPlayer placedByPlayer)
	{
		BuilderPiece piece = GetPiece(pieceId);
		if (piece == null)
		{
			return false;
		}
		BuilderPiece piece2 = GetPiece(attachPieceId);
		if (piece2 == null)
		{
			return false;
		}
		if (piece.heldByPlayerActorNumber != placedByPlayer.ActorNumber)
		{
			return false;
		}
		if (piece.isBuiltIntoTable || piece2.isBuiltIntoTable)
		{
			return false;
		}
		if ((uint)twist > 3u)
		{
			return false;
		}
		BuilderPiece piece3 = GetPiece(parentPieceId);
		if (piece3 != null)
		{
			if (!BuilderPiece.CanPlayerAttachPieceToPiece(placedByPlayer.ActorNumber, piece2, piece3))
			{
				return false;
			}
			if (DoesChainContainChain(piece2, piece3))
			{
				return false;
			}
			if (attachIndex < 0 || attachIndex >= piece2.gridPlanes.Count)
			{
				return false;
			}
			if (piece3 != null && (parentAttachIndex < 0 || parentAttachIndex >= piece3.gridPlanes.Count))
			{
				return false;
			}
			if (piece3 != null)
			{
				bool num = (long)(twist % 2) == 1;
				BuilderAttachGridPlane builderAttachGridPlane = piece2.gridPlanes[attachIndex];
				int num2 = (num ? builderAttachGridPlane.length : builderAttachGridPlane.width);
				int num3 = (num ? builderAttachGridPlane.width : builderAttachGridPlane.length);
				BuilderAttachGridPlane builderAttachGridPlane2 = piece3.gridPlanes[parentAttachIndex];
				int num4 = Mathf.FloorToInt((float)builderAttachGridPlane2.width / 2f);
				int num5 = num4 - (builderAttachGridPlane2.width - 1);
				if (bumpOffsetX < num5 - num2 || bumpOffsetX > num4 + num2)
				{
					return false;
				}
				int num6 = Mathf.FloorToInt((float)builderAttachGridPlane2.length / 2f);
				int num7 = num6 - (builderAttachGridPlane2.length - 1);
				if (bumpOffsetZ < num7 - num3 || bumpOffsetZ > num6 + num3)
				{
					return false;
				}
			}
			if (placedByPlayer == null)
			{
				return false;
			}
			if (PhotonNetwork.IsMasterClient && piece3 != null)
			{
				piece2.BumpTwistToPositionRotation(twist, bumpOffsetX, bumpOffsetZ, attachIndex, piece3.gridPlanes[parentAttachIndex], out var _, out var _, out var worldPosition, out var worldRotation);
				Vector3 vector = piece2.transform.InverseTransformPoint(piece.transform.position);
				Vector3 worldPosition2 = worldPosition + worldRotation * vector;
				if (!IsPlayerHandNearAction(placedByPlayer, worldPosition2, piece.heldInLeftHand, checkBothHands: false))
				{
					return false;
				}
				if (!ValidatePieceWorldTransform(worldPosition, worldRotation))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public bool ValidatePlacePieceState(int pieceId, int attachPieceId, sbyte bumpOffsetX, sbyte bumpOffsetZ, byte twist, int parentPieceId, int attachIndex, int parentAttachIndex, Player placedByPlayer)
	{
		BuilderPiece piece = GetPiece(pieceId);
		if (piece == null)
		{
			return false;
		}
		BuilderPiece piece2 = GetPiece(attachPieceId);
		if (piece2 == null)
		{
			return false;
		}
		if (GetPiece(parentPieceId) == null)
		{
			return false;
		}
		if (placedByPlayer == null)
		{
			return false;
		}
		if (!piece2.GetRootPiece() == (bool)piece)
		{
			return false;
		}
		return true;
	}

	public void ExecutePieceCreated(BuilderCommand cmd)
	{
		if ((cmd.player == null || !cmd.player.IsLocal) && !ValidateCreatePieceParams(cmd.pieceType, cmd.pieceId, cmd.state, cmd.materialType))
		{
			return;
		}
		BuilderPiece builderPiece = CreatePieceInternal(cmd.pieceType, cmd.pieceId, cmd.localPosition, cmd.localRotation, cmd.state, cmd.materialType, 0, this);
		if (builderPiece != null && cmd.state == BuilderPiece.State.OnConveyor)
		{
			if (cmd.parentPieceId >= 0 && cmd.parentPieceId < conveyors.Count)
			{
				builderPiece.shelfOwner = cmd.parentPieceId;
				BuilderConveyor builderConveyor = conveyors[builderPiece.shelfOwner];
				int parentAttachIndex = cmd.parentAttachIndex;
				float timeOffset = 0f;
				if ((uint)PhotonNetwork.ServerTimestamp > (uint)parentAttachIndex)
				{
					timeOffset = (float)(uint)(PhotonNetwork.ServerTimestamp - parentAttachIndex) / 1000f;
				}
				builderConveyor.OnShelfPieceCreated(builderPiece, timeOffset);
			}
		}
		else if (builderPiece != null && cmd.isLeft && cmd.state == BuilderPiece.State.OnShelf && cmd.parentPieceId >= 0 && cmd.parentPieceId < dispenserShelves.Count)
		{
			builderPiece.shelfOwner = cmd.parentPieceId;
			dispenserShelves[builderPiece.shelfOwner].OnShelfPieceCreated(builderPiece, playfx: true);
		}
	}

	public void ExecutePieceRecycled(BuilderCommand cmd)
	{
		RecyclePieceInternal(cmd.pieceId, ignoreHaptics: false, cmd.isLeft, cmd.parentPieceId);
	}

	private bool ValidateCreatePieceParams(int newPieceType, int newPieceId, BuilderPiece.State state, int materialType)
	{
		if (GetPiecePrefab(newPieceType) == null)
		{
			return false;
		}
		if (GetPiece(newPieceId) != null)
		{
			return false;
		}
		return true;
	}

	private bool ValidateDeserializedRootPieceState(int pieceId, BuilderPiece.State state, int shelfOwner, int heldByActor, Vector3 localPosition, Quaternion localRotation)
	{
		switch (state)
		{
		case BuilderPiece.State.Grabbed:
		case BuilderPiece.State.GrabbedLocal:
			if (heldByActor == -1)
			{
				return false;
			}
			if (!isTableMutable)
			{
				GTDev.LogError($"Deserialized bad CreatePiece parameters. held piece in immutable table {pieceId}");
				return false;
			}
			if (localPosition.sqrMagnitude > 6.25f)
			{
				return false;
			}
			break;
		case BuilderPiece.State.Dropped:
			if (!ValidatePieceWorldTransform(localPosition, localRotation))
			{
				return false;
			}
			if (!isTableMutable)
			{
				GTDev.LogError($"Deserialized bad CreatePiece parameters. dropped piece in immutable table {pieceId}");
				return false;
			}
			break;
		case BuilderPiece.State.OnShelf:
		case BuilderPiece.State.Displayed:
			if (!isTableMutable || shelfOwner == -1)
			{
				if (!ValidatePieceWorldTransform(localPosition, localRotation))
				{
					return false;
				}
			}
			else if (shelfOwner < 0 || shelfOwner > dispenserShelves.Count - 1)
			{
				return false;
			}
			break;
		case BuilderPiece.State.OnConveyor:
			if (shelfOwner == -1)
			{
				return false;
			}
			if (!isTableMutable)
			{
				GTDev.LogError($"Deserialized bad CreatePiece parameters. OnConveyor piece in immutable table {pieceId}");
				return false;
			}
			if (shelfOwner < 0 || shelfOwner > conveyors.Count - 1)
			{
				return false;
			}
			break;
		case BuilderPiece.State.AttachedToArm:
			if (heldByActor == -1)
			{
				return false;
			}
			if (!isTableMutable)
			{
				GTDev.LogError($"Deserialized bad CreatePiece parameters. AttachedToArm piece in immutable table {pieceId}");
				return false;
			}
			if (localPosition.sqrMagnitude > 6.25f)
			{
				return false;
			}
			break;
		default:
			return false;
		}
		return true;
	}

	private bool ValidateDeserializedChildPieceState(int pieceId, BuilderPiece.State state)
	{
		switch (state)
		{
		case BuilderPiece.State.AttachedAndPlaced:
		case BuilderPiece.State.OnShelf:
		case BuilderPiece.State.Displayed:
			return true;
		case BuilderPiece.State.AttachedToDropped:
		case BuilderPiece.State.Grabbed:
		case BuilderPiece.State.GrabbedLocal:
		case BuilderPiece.State.AttachedToArm:
			if (!isTableMutable)
			{
				GTDev.LogError($"Deserialized bad CreatePiece parameters. Invalid state {state} of child piece {pieceId} in Immutable table");
				return false;
			}
			return true;
		default:
			return false;
		}
	}

	public bool ValidatePieceWorldTransform(Vector3 position, Quaternion rotation)
	{
		if (!position.IsValid(10000f) || !rotation.IsValid())
		{
			return false;
		}
		if ((roomCenter.position - position).sqrMagnitude > acceptableSqrDistFromCenter)
		{
			return false;
		}
		return ValidatePositionInArea(position);
	}

	public bool ValidatePositionInArea(Vector3 position)
	{
		foreach (SimpleAABB areaBound in m_areaBounds)
		{
			if (areaBound.IsInBounds(position))
			{
				return true;
			}
		}
		return false;
	}

	private BuilderPiece CreatePieceInternal(int newPieceType, int newPieceId, Vector3 position, Quaternion rotation, BuilderPiece.State state, int materialType, int activateTimeStamp, BuilderTable table)
	{
		if (GetPiecePrefab(newPieceType) == null)
		{
			return null;
		}
		if (!PhotonNetwork.IsMasterClient)
		{
			nextPieceId = newPieceId + 1;
		}
		BuilderPiece builderPiece = builderPool.CreatePiece(newPieceType, assertNotEmpty: false);
		builderPiece.SetScale(table.pieceScale);
		builderPiece.transform.SetPositionAndRotation(position, rotation);
		builderPiece.pieceType = newPieceType;
		builderPiece.pieceId = newPieceId;
		builderPiece.SetTable(table);
		builderPiece.gameObject.SetActive(value: true);
		builderPiece.SetupPiece(gridSize);
		builderPiece.OnCreate();
		builderPiece.activatedTimeStamp = ((state == BuilderPiece.State.AttachedAndPlaced) ? activateTimeStamp : 0);
		builderPiece.SetMaterial(materialType, force: true);
		builderPiece.SetState(state, force: true);
		AddPiece(builderPiece);
		return builderPiece;
	}

	private void RecyclePieceInternal(int pieceId, bool ignoreHaptics, bool playFX, int recyclerId)
	{
		BuilderPiece piece = GetPiece(pieceId);
		if (piece == null)
		{
			return;
		}
		if (playFX)
		{
			try
			{
				piece.PlayRecycleFx();
			}
			catch (Exception)
			{
			}
		}
		if (!ignoreHaptics)
		{
			BuilderPiece rootPiece = piece.GetRootPiece();
			if (rootPiece != null && rootPiece.IsHeldLocal())
			{
				GorillaTagger.Instance.StartVibration(piece.IsHeldInLeftHand(), GorillaTagger.Instance.tapHapticStrength, pushAndEaseParams.snapDelayTime * 2f);
			}
		}
		BuilderPiece builderPiece = piece.firstChildPiece;
		while (builderPiece != null)
		{
			int pieceId2 = builderPiece.pieceId;
			builderPiece = builderPiece.nextSiblingPiece;
			RecyclePieceInternal(pieceId2, ignoreHaptics: true, playFX, recyclerId);
		}
		if (isTableMutable && recyclerId >= 0 && recyclerId < recyclers.Count)
		{
			recyclers[recyclerId].OnRecycleRequestedAtRecycler(piece);
		}
		if (piece.state == BuilderPiece.State.OnConveyor && piece.shelfOwner >= 0 && piece.shelfOwner < conveyors.Count)
		{
			conveyors[piece.shelfOwner].OnShelfPieceRecycled(piece);
		}
		else if ((piece.state == BuilderPiece.State.OnShelf || piece.state == BuilderPiece.State.Displayed) && piece.shelfOwner >= 0 && piece.shelfOwner < dispenserShelves.Count)
		{
			dispenserShelves[piece.shelfOwner].OnShelfPieceRecycled(piece);
		}
		if (piece.isArmShelf && isTableMutable)
		{
			if (piece.armShelf != null)
			{
				piece.armShelf.piece = null;
				piece.armShelf = null;
			}
			if (piece.heldInLeftHand && playerToArmShelfLeft.TryGetValue(piece.heldByPlayerActorNumber, out var value) && value == piece.pieceId)
			{
				playerToArmShelfLeft.Remove(piece.heldByPlayerActorNumber);
			}
			if (!piece.heldInLeftHand && playerToArmShelfRight.TryGetValue(piece.heldByPlayerActorNumber, out var value2) && value2 == piece.pieceId)
			{
				playerToArmShelfRight.Remove(piece.heldByPlayerActorNumber);
			}
		}
		else if (PhotonNetwork.LocalPlayer.ActorNumber == piece.heldByPlayerActorNumber)
		{
			BuilderPieceInteractor.instance.RemovePieceFromHeld(piece);
		}
		_ = piece.pieceId;
		piece.ClearParentPiece();
		piece.ClearParentHeld();
		piece.SetState(BuilderPiece.State.None);
		RemovePiece(piece);
		builderPool.DestroyPiece(piece);
	}

	public BuilderPiece GetPiecePrefab(int pieceType)
	{
		return BuilderSetManager.instance.GetPiecePrefab(pieceType);
	}

	private bool ValidateAttachPieceParams(int pieceId, int attachIndex, int parentId, int parentAttachIndex, int piecePlacement)
	{
		if (pieceId == parentId)
		{
			return false;
		}
		BuilderPiece piece = GetPiece(pieceId);
		if (piece == null)
		{
			return false;
		}
		BuilderPiece piece2 = GetPiece(parentId);
		if (piece2 == null)
		{
			return false;
		}
		if ((piecePlacement & 0x3FFFF) != piecePlacement)
		{
			return false;
		}
		if (piece.isBuiltIntoTable)
		{
			return false;
		}
		if (DoesChainContainChain(piece, piece2))
		{
			return false;
		}
		if (attachIndex < 0 || attachIndex >= piece.gridPlanes.Count)
		{
			return false;
		}
		if (parentAttachIndex < 0 || parentAttachIndex >= piece2.gridPlanes.Count)
		{
			return false;
		}
		UnpackPiecePlacement(piecePlacement, out var twist, out var xOffset, out var zOffset);
		bool num = (long)(twist % 2) == 1;
		BuilderAttachGridPlane builderAttachGridPlane = piece.gridPlanes[attachIndex];
		int num2 = (num ? builderAttachGridPlane.length : builderAttachGridPlane.width);
		int num3 = (num ? builderAttachGridPlane.width : builderAttachGridPlane.length);
		BuilderAttachGridPlane builderAttachGridPlane2 = piece2.gridPlanes[parentAttachIndex];
		int num4 = Mathf.FloorToInt((float)builderAttachGridPlane2.width / 2f);
		int num5 = num4 - (builderAttachGridPlane2.width - 1);
		if (xOffset < num5 - num2 || xOffset > num4 + num2)
		{
			return false;
		}
		int num6 = Mathf.FloorToInt((float)builderAttachGridPlane2.length / 2f);
		int num7 = num6 - (builderAttachGridPlane2.length - 1);
		if (zOffset < num7 - num3 || zOffset > num6 + num3)
		{
			return false;
		}
		return true;
	}

	private void AttachPieceInternal(int pieceId, int attachIndex, int parentId, int parentAttachIndex, int placement)
	{
		BuilderPiece piece = GetPiece(pieceId);
		BuilderPiece piece2 = GetPiece(parentId);
		if (!(piece == null))
		{
			UnpackPiecePlacement(placement, out var twist, out var xOffset, out var zOffset);
			Vector3 localPosition = Vector3.zero;
			Quaternion localRotation;
			if (piece2 != null && parentAttachIndex >= 0 && parentAttachIndex < piece2.gridPlanes.Count)
			{
				piece.BumpTwistToPositionRotation(twist, xOffset, zOffset, attachIndex, piece2.gridPlanes[parentAttachIndex], out localPosition, out localRotation, out var _, out var _);
			}
			else
			{
				localRotation = Quaternion.Euler(0f, (float)(int)twist * 90f, 0f);
			}
			piece.SetParentPiece(attachIndex, piece2, parentAttachIndex);
			piece.transform.SetLocalPositionAndRotation(localPosition, localRotation);
		}
	}

	private void AttachPieceToActorInternal(int pieceId, int actorNumber, bool isLeftHand)
	{
		BuilderPiece piece = GetPiece(pieceId);
		if (piece == null)
		{
			return;
		}
		NetPlayer player = NetworkSystem.Instance.GetPlayer(actorNumber);
		if (!VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			return;
		}
		VRRig rig = playerRig.Rig;
		BodyDockPositions myBodyDockPositions = rig.myBodyDockPositions;
		Transform parentHeld = (isLeftHand ? myBodyDockPositions.leftHandTransform : myBodyDockPositions.rightHandTransform);
		if (piece.isArmShelf)
		{
			if (!isTableMutable)
			{
				return;
			}
			parentHeld = (isLeftHand ? rig.builderArmShelfLeft.pieceAnchor : rig.builderArmShelfRight.pieceAnchor);
			if (isLeftHand)
			{
				rig.builderArmShelfLeft.piece = piece;
				piece.armShelf = rig.builderArmShelfLeft;
				if (playerToArmShelfLeft.TryGetValue(actorNumber, out var value) && value != pieceId)
				{
					BuilderPiece piece2 = GetPiece(value);
					if (piece2 != null && piece2.isArmShelf)
					{
						piece2.ClearParentHeld();
						playerToArmShelfLeft.Remove(actorNumber);
						piece2.transform.GetPositionAndRotation(out var position, out var rotation);
						if (!ValidatePieceWorldTransform(position, rotation))
						{
							RecyclePieceInternal(piece2.pieceId, ignoreHaptics: true, playFX: false, -1);
						}
					}
				}
				playerToArmShelfLeft.TryAdd(actorNumber, pieceId);
			}
			else
			{
				rig.builderArmShelfRight.piece = piece;
				piece.armShelf = rig.builderArmShelfRight;
				if (playerToArmShelfRight.TryGetValue(actorNumber, out var value2) && value2 != pieceId)
				{
					BuilderPiece piece3 = GetPiece(value2);
					if (piece3 != null && piece3.isArmShelf)
					{
						piece3.ClearParentHeld();
						playerToArmShelfRight.Remove(actorNumber);
						piece3.transform.GetPositionAndRotation(out var position2, out var rotation2);
						if (!ValidatePieceWorldTransform(position2, rotation2))
						{
							RecyclePieceInternal(piece3.pieceId, ignoreHaptics: true, playFX: false, -1);
						}
					}
				}
				playerToArmShelfRight.TryAdd(actorNumber, pieceId);
			}
		}
		Vector3 localPosition = piece.transform.localPosition;
		Quaternion localRotation = piece.transform.localRotation;
		piece.ClearParentHeld();
		piece.ClearParentPiece();
		piece.SetParentHeld(parentHeld, actorNumber, isLeftHand);
		piece.transform.SetLocalPositionAndRotation(localPosition, localRotation);
		BuilderPiece.State newState = (player.IsLocal ? BuilderPiece.State.GrabbedLocal : BuilderPiece.State.Grabbed);
		if (piece.isArmShelf)
		{
			newState = BuilderPiece.State.AttachedToArm;
			piece.transform.localScale = Vector3.one;
		}
		piece.SetState(newState);
		if (!player.IsLocal)
		{
			BuilderPieceInteractor.instance.RemovePieceFromHeld(piece);
		}
		if (player.IsLocal && !piece.isArmShelf)
		{
			BuilderPieceInteractor.instance.AddPieceToHeld(piece, isLeftHand, localPosition, localRotation);
		}
	}

	public void RequestPlacePiece(BuilderPiece piece, BuilderPiece attachPiece, sbyte bumpOffsetX, sbyte bumpOffsetZ, byte twist, BuilderPiece parentPiece, int attachIndex, int parentAttachIndex)
	{
		if (tableState == TableState.Ready)
		{
			builderNetworking.RequestPlacePiece(piece, attachPiece, bumpOffsetX, bumpOffsetZ, twist, parentPiece, attachIndex, parentAttachIndex);
		}
	}

	public void PlacePiece(int localCommandId, int pieceId, int attachPieceId, sbyte bumpOffsetX, sbyte bumpOffsetZ, byte twist, int parentPieceId, int attachIndex, int parentAttachIndex, NetPlayer placedByPlayer, int timeStamp, bool force)
	{
		PiecePlacedInternal(localCommandId, pieceId, attachPieceId, bumpOffsetX, bumpOffsetZ, twist, parentPieceId, attachIndex, parentAttachIndex, placedByPlayer, timeStamp, force);
	}

	public void PiecePlacedInternal(int localCommandId, int pieceId, int attachPieceId, sbyte bumpOffsetX, sbyte bumpOffsetZ, byte twist, int parentPieceId, int attachIndex, int parentAttachIndex, NetPlayer placedByPlayer, int timeStamp, bool force)
	{
		if (force || placedByPlayer != NetworkSystem.Instance.LocalPlayer || !HasRollForwardCommand(localCommandId) || !TryRollbackAndReExecute(localCommandId))
		{
			BuilderCommand cmd = new BuilderCommand
			{
				type = BuilderCommandType.Place,
				pieceId = pieceId,
				bumpOffsetX = bumpOffsetX,
				bumpOffsetZ = bumpOffsetZ,
				twist = twist,
				attachPieceId = attachPieceId,
				parentPieceId = parentPieceId,
				attachIndex = attachIndex,
				parentAttachIndex = parentAttachIndex,
				player = placedByPlayer,
				canRollback = force,
				localCommandId = localCommandId,
				serverTimeStamp = timeStamp
			};
			RouteNewCommand(cmd, force);
		}
	}

	public void ExecutePiecePlacedWithActions(BuilderCommand cmd)
	{
		int pieceId = cmd.pieceId;
		int attachPieceId = cmd.attachPieceId;
		int parentPieceId = cmd.parentPieceId;
		int parentAttachIndex = cmd.parentAttachIndex;
		int attachIndex = cmd.attachIndex;
		NetPlayer player = cmd.player;
		int localCommandId = cmd.localCommandId;
		int actorNumber = cmd.player.ActorNumber;
		byte twist = cmd.twist;
		sbyte bumpOffsetX = cmd.bumpOffsetX;
		sbyte bumpOffsetZ = cmd.bumpOffsetZ;
		if ((player == null || !player.IsLocal) && !ValidatePlacePieceParams(pieceId, attachPieceId, bumpOffsetX, bumpOffsetZ, twist, parentPieceId, attachIndex, parentAttachIndex, player))
		{
			return;
		}
		BuilderPiece piece = GetPiece(pieceId);
		if (piece == null)
		{
			return;
		}
		BuilderPiece piece2 = GetPiece(attachPieceId);
		if (!(piece2 == null))
		{
			BuilderAction action = BuilderActions.CreateDetachFromPlayer(localCommandId, pieceId, actorNumber);
			BuilderAction action2 = BuilderActions.CreateMakeRoot(localCommandId, attachPieceId);
			BuilderAction action3 = BuilderActions.CreateAttachToPiece(localCommandId, attachPieceId, cmd.parentPieceId, cmd.attachIndex, cmd.parentAttachIndex, bumpOffsetX, bumpOffsetZ, twist, actorNumber, cmd.serverTimeStamp);
			if (cmd.canRollback)
			{
				BuilderAction action4 = BuilderActions.CreateDetachFromPiece(localCommandId, attachPieceId, actorNumber);
				BuilderAction action5 = BuilderActions.CreateMakeRoot(localCommandId, pieceId);
				BuilderAction action6 = BuilderActions.CreateAttachToPlayerRollback(localCommandId, piece);
				AddRollbackAction(action6);
				AddRollbackAction(action5);
				AddRollbackAction(action4);
				AddRollForwardCommand(cmd);
			}
			ExecuteAction(action);
			ExecuteAction(action2);
			ExecuteAction(action3);
			if (!cmd.isQueued)
			{
				piece2.PlayPlacementFx();
			}
		}
	}

	public bool ValidateGrabPieceParams(int pieceId, bool isLeftHand, Vector3 localPosition, Quaternion localRotation, NetPlayer grabbedByPlayer)
	{
		if (!localPosition.IsValid(10000f) || !localRotation.IsValid())
		{
			return false;
		}
		BuilderPiece piece = GetPiece(pieceId);
		if (piece == null)
		{
			return false;
		}
		if (piece.isBuiltIntoTable)
		{
			return false;
		}
		if (grabbedByPlayer == null)
		{
			return false;
		}
		if (!piece.CanPlayerGrabPiece(grabbedByPlayer.ActorNumber, piece.transform.position))
		{
			return false;
		}
		if (localPosition.sqrMagnitude > 6400f)
		{
			return false;
		}
		if (PhotonNetwork.IsMasterClient)
		{
			Vector3 position = piece.transform.position;
			if (!IsPlayerHandNearAction(grabbedByPlayer, position, isLeftHand, checkBothHands: false))
			{
				return false;
			}
		}
		return true;
	}

	public bool ValidateGrabPieceState(int pieceId, bool isLeftHand, Vector3 localPosition, Quaternion localRotation, Player grabbedByPlayer)
	{
		BuilderPiece piece = GetPiece(pieceId);
		if (piece == null)
		{
			return false;
		}
		if (piece.state == BuilderPiece.State.Displayed || piece.state == BuilderPiece.State.None)
		{
			return false;
		}
		return true;
	}

	public bool IsLocationWithinSharedBuildArea(Vector3 worldPosition)
	{
		Vector3 vector = sharedBuildArea.transform.InverseTransformPoint(worldPosition);
		BoxCollider[] array = sharedBuildAreas;
		foreach (BoxCollider boxCollider in array)
		{
			Vector3 vector2 = boxCollider.center + boxCollider.size / 2f;
			Vector3 vector3 = boxCollider.center - boxCollider.size / 2f;
			if (vector.x >= vector3.x && vector.x <= vector2.x && vector.y >= vector3.y && vector.y <= vector2.y && vector.z >= vector3.z && vector.z <= vector2.z)
			{
				return true;
			}
		}
		return false;
	}

	private bool NoBlocksCheck()
	{
		foreach (BoxCheckParams noBlocksArea in noBlocksAreas)
		{
			DebugUtil.DrawBox(noBlocksArea.center, noBlocksArea.rotation, noBlocksArea.halfExtents * 2f, Color.magenta);
			int num = 0;
			num |= 1 << placedLayer;
			int num2 = Physics.OverlapBoxNonAlloc(noBlocksArea.center, noBlocksArea.halfExtents, noBlocksCheckResults, noBlocksArea.rotation, num);
			for (int i = 0; i < num2; i++)
			{
				BuilderPiece builderPieceFromCollider = BuilderPiece.GetBuilderPieceFromCollider(noBlocksCheckResults[i]);
				if (builderPieceFromCollider != null && builderPieceFromCollider.GetTable() == this && builderPieceFromCollider.state == BuilderPiece.State.AttachedAndPlaced && !builderPieceFromCollider.isBuiltIntoTable)
				{
					GTDev.LogError($"Builder Table found piece {builderPieceFromCollider.pieceId} {builderPieceFromCollider.displayName} in NO BLOCK AREA {builderPieceFromCollider.transform.position}");
					return false;
				}
			}
		}
		return true;
	}

	public void RequestGrabPiece(BuilderPiece piece, bool isLefHand, Vector3 localPosition, Quaternion localRotation)
	{
		if (tableState == TableState.Ready)
		{
			builderNetworking.RequestGrabPiece(piece, isLefHand, localPosition, localRotation);
		}
	}

	public void GrabPiece(int localCommandId, int pieceId, bool isLeftHand, Vector3 localPosition, Quaternion localRotation, NetPlayer grabbedByPlayer, bool force)
	{
		PieceGrabbedInternal(localCommandId, pieceId, isLeftHand, localPosition, localRotation, grabbedByPlayer, force);
	}

	public void PieceGrabbedInternal(int localCommandId, int pieceId, bool isLeftHand, Vector3 localPosition, Quaternion localRotation, NetPlayer grabbedByPlayer, bool force)
	{
		if (force || grabbedByPlayer != NetworkSystem.Instance.LocalPlayer || !HasRollForwardCommand(localCommandId) || !TryRollbackAndReExecute(localCommandId))
		{
			BuilderCommand cmd = new BuilderCommand
			{
				type = BuilderCommandType.Grab,
				pieceId = pieceId,
				attachPieceId = -1,
				isLeft = isLeftHand,
				localPosition = localPosition,
				localRotation = localRotation,
				player = grabbedByPlayer,
				canRollback = force,
				localCommandId = localCommandId
			};
			RouteNewCommand(cmd, force);
		}
	}

	public void ExecutePieceGrabbedWithActions(BuilderCommand cmd)
	{
		int pieceId = cmd.pieceId;
		bool isLeft = cmd.isLeft;
		NetPlayer player = cmd.player;
		Vector3 localPosition = cmd.localPosition;
		Quaternion localRotation = cmd.localRotation;
		int localCommandId = cmd.localCommandId;
		int actorNumber = cmd.player.ActorNumber;
		if ((player == null || !player.Equals(NetworkSystem.Instance.LocalPlayer)) && !ValidateGrabPieceParams(pieceId, isLeft, localPosition, localRotation, player))
		{
			return;
		}
		BuilderPiece piece = GetPiece(pieceId);
		if (piece == null)
		{
			return;
		}
		bool num = PhotonNetwork.CurrentRoom.GetPlayer(piece.heldByPlayerActorNumber) != null;
		bool flag = BuilderPiece.IsDroppedState(piece.state);
		bool flag2 = piece.state == BuilderPiece.State.OnConveyor || piece.state == BuilderPiece.State.OnShelf || piece.state == BuilderPiece.State.Displayed;
		BuilderAction action = BuilderActions.CreateAttachToPlayer(localCommandId, pieceId, cmd.localPosition, cmd.localRotation, actorNumber, cmd.isLeft);
		BuilderAction action2 = BuilderActions.CreateDetachFromPlayer(localCommandId, pieceId, actorNumber);
		if (num)
		{
			BuilderAction action3 = BuilderActions.CreateDetachFromPlayer(localCommandId, pieceId, piece.heldByPlayerActorNumber);
			if (cmd.canRollback)
			{
				BuilderAction action4 = BuilderActions.CreateAttachToPlayerRollback(localCommandId, piece);
				AddRollbackAction(action4);
				AddRollbackAction(action2);
				AddRollForwardCommand(cmd);
			}
			ExecuteAction(action3);
			ExecuteAction(action);
		}
		else if (flag2)
		{
			BuilderAction action5;
			if (piece.state == BuilderPiece.State.OnConveyor)
			{
				int serverTimestamp = PhotonNetwork.ServerTimestamp;
				float splineProgressForPiece = conveyorManager.GetSplineProgressForPiece(piece);
				action5 = BuilderActions.CreateAttachToShelfRollback(localCommandId, piece, piece.shelfOwner, isConveyor: true, serverTimestamp, splineProgressForPiece);
			}
			else
			{
				if (piece.state == BuilderPiece.State.Displayed)
				{
					_ = NetworkSystem.Instance.LocalPlayer.ActorNumber;
				}
				action5 = BuilderActions.CreateAttachToShelfRollback(localCommandId, piece, piece.shelfOwner, isConveyor: false);
			}
			BuilderAction action6 = BuilderActions.CreateMakeRoot(localCommandId, pieceId);
			BuilderPiece rootPiece = piece.GetRootPiece();
			BuilderAction action7 = BuilderActions.CreateMakeRoot(localCommandId, rootPiece.pieceId);
			if (cmd.canRollback)
			{
				AddRollbackAction(action5);
				AddRollbackAction(action7);
				AddRollbackAction(action2);
				AddRollForwardCommand(cmd);
			}
			ExecuteAction(action6);
			ExecuteAction(action);
		}
		else if (flag)
		{
			BuilderAction action8 = BuilderActions.CreateMakeRoot(localCommandId, pieceId);
			BuilderPiece rootPiece2 = piece.GetRootPiece();
			BuilderAction action9 = BuilderActions.CreateDropPieceRollback(localCommandId, rootPiece2, actorNumber);
			BuilderAction action10 = BuilderActions.CreateMakeRoot(localCommandId, rootPiece2.pieceId);
			if (cmd.canRollback)
			{
				AddRollbackAction(action9);
				AddRollbackAction(action10);
				AddRollbackAction(action2);
				AddRollForwardCommand(cmd);
			}
			ExecuteAction(action8);
			ExecuteAction(action);
		}
		else if (piece.parentPiece != null)
		{
			BuilderAction action11 = BuilderActions.CreateDetachFromPiece(localCommandId, pieceId, actorNumber);
			BuilderAction action12 = BuilderActions.CreateAttachToPieceRollback(localCommandId, piece, actorNumber);
			if (cmd.canRollback)
			{
				AddRollbackAction(action12);
				AddRollbackAction(action2);
				AddRollForwardCommand(cmd);
			}
			ExecuteAction(action11);
			ExecuteAction(action);
		}
	}

	public bool ValidateDropPieceParams(int pieceId, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, NetPlayer droppedByPlayer)
	{
		if (!position.IsValid(10000f) || !rotation.IsValid() || !velocity.IsValid(10000f) || !angVelocity.IsValid(10000f))
		{
			return false;
		}
		BuilderPiece piece = GetPiece(pieceId);
		if (piece == null)
		{
			return false;
		}
		if (piece.isBuiltIntoTable)
		{
			return false;
		}
		if (droppedByPlayer == null)
		{
			return false;
		}
		if (velocity.sqrMagnitude > MAX_DROP_VELOCITY * MAX_DROP_VELOCITY)
		{
			return false;
		}
		if (angVelocity.sqrMagnitude > MAX_DROP_ANG_VELOCITY * MAX_DROP_ANG_VELOCITY)
		{
			return false;
		}
		if ((roomCenter.position - position).sqrMagnitude > acceptableSqrDistFromCenter || !ValidatePositionInArea(position))
		{
			return false;
		}
		if (piece.state == BuilderPiece.State.AttachedToArm)
		{
			if (piece.parentPiece == null)
			{
				return false;
			}
			if (piece.parentPiece.heldByPlayerActorNumber != droppedByPlayer.ActorNumber)
			{
				return false;
			}
		}
		else if (piece.heldByPlayerActorNumber != droppedByPlayer.ActorNumber)
		{
			return false;
		}
		if (PhotonNetwork.IsMasterClient && !IsPlayerHandNearAction(droppedByPlayer, position, piece.heldInLeftHand, checkBothHands: false))
		{
			return false;
		}
		return true;
	}

	public bool ValidateDropPieceState(int pieceId, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, Player droppedByPlayer)
	{
		BuilderPiece piece = GetPiece(pieceId);
		if (piece == null)
		{
			return false;
		}
		bool flag = piece.state == BuilderPiece.State.AttachedToArm;
		if ((!flag && piece.heldByPlayerActorNumber != droppedByPlayer.ActorNumber) || (flag && piece.parentPiece.heldByPlayerActorNumber != droppedByPlayer.ActorNumber))
		{
			return false;
		}
		return true;
	}

	public void RequestDropPiece(BuilderPiece piece, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity)
	{
		if (tableState == TableState.Ready)
		{
			builderNetworking.RequestDropPiece(piece, position, rotation, velocity, angVelocity);
		}
	}

	public void DropPiece(int localCommandId, int pieceId, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, NetPlayer droppedByPlayer, bool force)
	{
		PieceDroppedInternal(localCommandId, pieceId, position, rotation, velocity, angVelocity, droppedByPlayer, force);
	}

	public void PieceDroppedInternal(int localCommandId, int pieceId, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, NetPlayer droppedByPlayer, bool force)
	{
		if (force || droppedByPlayer != NetworkSystem.Instance.LocalPlayer || !HasRollForwardCommand(localCommandId) || !TryRollbackAndReExecute(localCommandId))
		{
			BuilderCommand cmd = new BuilderCommand
			{
				type = BuilderCommandType.Drop,
				pieceId = pieceId,
				parentPieceId = pieceId,
				localPosition = position,
				localRotation = rotation,
				velocity = velocity,
				angVelocity = angVelocity,
				player = droppedByPlayer,
				canRollback = force,
				localCommandId = localCommandId
			};
			RouteNewCommand(cmd, force);
		}
	}

	public void ExecutePieceDroppedWithActions(BuilderCommand cmd)
	{
		int pieceId = cmd.pieceId;
		int localCommandId = cmd.localCommandId;
		int actorNumber = cmd.player.ActorNumber;
		if (!ValidateDropPieceParams(pieceId, cmd.localPosition, cmd.localRotation, cmd.velocity, cmd.angVelocity, cmd.player))
		{
			return;
		}
		BuilderPiece piece = GetPiece(pieceId);
		if (piece == null)
		{
			return;
		}
		if (piece.state == BuilderPiece.State.AttachedToArm)
		{
			_ = piece.parentPiece;
			BuilderAction action = BuilderActions.CreateDetachFromPiece(localCommandId, pieceId, actorNumber);
			BuilderAction action2 = BuilderActions.CreateDropPiece(localCommandId, pieceId, cmd.localPosition, cmd.localRotation, cmd.velocity, cmd.angVelocity, actorNumber);
			if (cmd.canRollback)
			{
				BuilderAction action3 = BuilderActions.CreateAttachToPieceRollback(localCommandId, piece, actorNumber);
				AddRollbackAction(action3);
				AddRollForwardCommand(cmd);
			}
			ExecuteAction(action);
			ExecuteAction(action2);
		}
		else
		{
			BuilderAction action4 = BuilderActions.CreateDetachFromPlayer(localCommandId, pieceId, actorNumber);
			BuilderAction action5 = BuilderActions.CreateDropPiece(localCommandId, pieceId, cmd.localPosition, cmd.localRotation, cmd.velocity, cmd.angVelocity, actorNumber);
			if (cmd.canRollback)
			{
				BuilderAction action6 = BuilderActions.CreateAttachToPlayerRollback(localCommandId, piece);
				AddRollbackAction(action6);
				AddRollForwardCommand(cmd);
			}
			ExecuteAction(action4);
			ExecuteAction(action5);
		}
	}

	public void ExecutePieceRepelled(BuilderCommand cmd)
	{
		int pieceId = cmd.pieceId;
		int localCommandId = cmd.localCommandId;
		int actorNumber = cmd.player.ActorNumber;
		int attachPieceId = cmd.attachPieceId;
		BuilderPiece piece = GetPiece(pieceId);
		Vector3 velocity = cmd.velocity;
		if (piece == null || piece.isBuiltIntoTable || piece.isArmShelf || (piece.state != BuilderPiece.State.Grabbed && piece.state != BuilderPiece.State.GrabbedLocal && piece.state != BuilderPiece.State.Dropped && piece.state != BuilderPiece.State.AttachedToDropped && piece.state != BuilderPiece.State.AttachedToArm))
		{
			return;
		}
		if (attachPieceId >= 0 && attachPieceId < dropZones.Count)
		{
			BuilderDropZone builderDropZone = dropZones[attachPieceId];
			builderDropZone.PlayEffect();
			if (builderDropZone.overrideDirection)
			{
				velocity = builderDropZone.GetRepelDirectionWorld() * DROP_ZONE_REPEL;
			}
		}
		if (piece.heldByPlayerActorNumber >= 0)
		{
			BuilderAction action = BuilderActions.CreateDetachFromPlayer(localCommandId, pieceId, piece.heldByPlayerActorNumber);
			BuilderAction action2 = BuilderActions.CreateDropPiece(localCommandId, pieceId, cmd.localPosition, cmd.localRotation, velocity, cmd.angVelocity, actorNumber);
			ExecuteAction(action);
			ExecuteAction(action2);
		}
		else if (piece.state == BuilderPiece.State.AttachedToArm && piece.parentPiece != null)
		{
			BuilderAction action3 = BuilderActions.CreateDetachFromPiece(localCommandId, pieceId, piece.heldByPlayerActorNumber);
			BuilderAction action4 = BuilderActions.CreateDropPiece(localCommandId, pieceId, cmd.localPosition, cmd.localRotation, velocity, cmd.angVelocity, actorNumber);
			ExecuteAction(action3);
			ExecuteAction(action4);
		}
		else
		{
			BuilderAction action5 = BuilderActions.CreateDropPiece(localCommandId, pieceId, cmd.localPosition, cmd.localRotation, velocity, cmd.angVelocity, actorNumber);
			ExecuteAction(action5);
		}
	}

	private void CleanUpDroppedPiece()
	{
		if (PhotonNetwork.IsMasterClient && droppedPieces.Count > DROPPED_PIECE_LIMIT)
		{
			BuilderPiece builderPiece = FindFirstSleepingPiece();
			if (builderPiece != null && builderPiece.state == BuilderPiece.State.Dropped)
			{
				RequestRecyclePiece(builderPiece, playFX: false, -1);
				return;
			}
			Debug.LogErrorFormat("Piece {0} in Dropped List is {1}", builderPiece.pieceId, builderPiece.state);
		}
	}

	public void FreezeDroppedPiece(BuilderPiece piece)
	{
		int num = droppedPieces.IndexOf(piece);
		if (num >= 0)
		{
			DroppedPieceData value = droppedPieceData[num];
			value.droppedState = DroppedPieceState.Frozen;
			value.speedThreshCrossedTime = 0f;
			droppedPieceData[num] = value;
			if (piece.rigidBody != null)
			{
				piece.SetKinematic(kinematic: true, destroyImmediate: false);
			}
			piece.forcedFrozen = true;
		}
	}

	public void AddPieceToDropList(BuilderPiece piece)
	{
		droppedPieces.Add(piece);
		droppedPieceData.Add(new DroppedPieceData
		{
			speedThreshCrossedTime = 0f,
			droppedState = DroppedPieceState.Light,
			filteredSpeed = 0f
		});
	}

	private BuilderPiece FindFirstSleepingPiece()
	{
		if (droppedPieces.Count < 1)
		{
			return null;
		}
		_ = droppedPieces[0];
		for (int i = 0; i < droppedPieces.Count; i++)
		{
			if (droppedPieces[i].rigidBody != null && droppedPieces[i].rigidBody.IsSleeping())
			{
				BuilderPiece result = droppedPieces[i];
				droppedPieces.RemoveAt(i);
				droppedPieceData.RemoveAt(i);
				return result;
			}
		}
		BuilderPiece result2 = droppedPieces[0];
		droppedPieces.RemoveAt(0);
		droppedPieceData.RemoveAt(0);
		return result2;
	}

	public void RemovePieceFromDropList(BuilderPiece piece)
	{
		if (piece.state == BuilderPiece.State.Dropped)
		{
			droppedPieces.Remove(piece);
		}
	}

	private void UpdateDroppedPieces(float dt)
	{
		for (int i = 0; i < droppedPieces.Count; i++)
		{
			if (droppedPieceData[i].droppedState == DroppedPieceState.Frozen && droppedPieces[i].state == BuilderPiece.State.Dropped)
			{
				DroppedPieceData value = droppedPieceData[i];
				value.speedThreshCrossedTime += dt;
				if (value.speedThreshCrossedTime > 60f)
				{
					droppedPieces[i].forcedFrozen = false;
					droppedPieces[i].ClearCollisionHistory();
					droppedPieces[i].SetKinematic(kinematic: false);
					value.droppedState = DroppedPieceState.Light;
					value.speedThreshCrossedTime = 0f;
				}
				droppedPieceData[i] = value;
				continue;
			}
			Rigidbody rigidBody = droppedPieces[i].rigidBody;
			if (!(rigidBody != null))
			{
				continue;
			}
			DroppedPieceData value2 = droppedPieceData[i];
			float magnitude = rigidBody.linearVelocity.magnitude;
			value2.filteredSpeed = value2.filteredSpeed * 0.95f + magnitude * 0.05f;
			switch (value2.droppedState)
			{
			case DroppedPieceState.Light:
			{
				bool flag2 = value2.filteredSpeed < 0.05f;
				value2.speedThreshCrossedTime = (flag2 ? (value2.speedThreshCrossedTime + dt) : 0f);
				if (value2.speedThreshCrossedTime > 0f)
				{
					rigidBody.mass = 10000f;
					value2.droppedState = DroppedPieceState.Heavy;
					value2.speedThreshCrossedTime = 0f;
				}
				break;
			}
			case DroppedPieceState.Heavy:
			{
				value2.speedThreshCrossedTime += dt;
				bool flag = value2.filteredSpeed > 0.075f;
				value2.speedThreshCrossedTime = (flag ? (value2.speedThreshCrossedTime + dt) : 0f);
				if (value2.speedThreshCrossedTime > 0.5f)
				{
					rigidBody.mass = 1f;
					value2.droppedState = DroppedPieceState.Light;
					value2.speedThreshCrossedTime = 0f;
				}
				break;
			}
			}
			droppedPieceData[i] = value2;
		}
	}

	private void SetLocalPlayerOwnsPlot(bool ownsPlot)
	{
		doesLocalPlayerOwnPlot = ownsPlot;
		OnLocalPlayerClaimedPlot?.Invoke(doesLocalPlayerOwnPlot);
	}

	public void PlotClaimed(int plotPieceId, Player claimingPlayer)
	{
		BuilderCommand cmd = new BuilderCommand
		{
			type = BuilderCommandType.ClaimPlot,
			pieceId = plotPieceId,
			player = NetPlayer.Get(claimingPlayer)
		};
		RouteNewCommand(cmd, force: false);
	}

	public void ExecuteClaimPlot(BuilderCommand cmd)
	{
		int pieceId = cmd.pieceId;
		NetPlayer player = cmd.player;
		if (pieceId == -1)
		{
			return;
		}
		BuilderPiece piece = GetPiece(pieceId);
		if (!(piece == null) && piece.IsPrivatePlot() && player != null && plotOwners.TryAdd(player.ActorNumber, pieceId) && piece.TryGetPlotComponent(out var plot))
		{
			plot.ClaimPlotForPlayerNumber(player.ActorNumber);
			if (player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
			{
				SetLocalPlayerOwnsPlot(ownsPlot: true);
			}
		}
	}

	public void PlayerLeftRoom(int playerActorNumber)
	{
		BuilderCommand cmd = new BuilderCommand
		{
			type = BuilderCommandType.PlayerLeftRoom,
			pieceId = playerActorNumber,
			player = null
		};
		bool force = tableState == TableState.WaitForMasterResync;
		RouteNewCommand(cmd, force);
	}

	public void ExecutePlayerLeftRoom(BuilderCommand cmd)
	{
		int num = cmd.player?.ActorNumber ?? cmd.pieceId;
		FreePlotInternal(-1, num);
		if (playerToArmShelfLeft.TryGetValue(num, out var value))
		{
			RecyclePieceInternal(value, ignoreHaptics: true, playFX: false, -1);
		}
		playerToArmShelfLeft.Remove(num);
		if (playerToArmShelfRight.TryGetValue(num, out var value2))
		{
			RecyclePieceInternal(value2, ignoreHaptics: true, playFX: false, -1);
		}
		playerToArmShelfRight.Remove(num);
	}

	public void PlotFreed(int plotPieceId, Player claimingPlayer)
	{
		BuilderCommand cmd = new BuilderCommand
		{
			type = BuilderCommandType.FreePlot,
			pieceId = plotPieceId,
			player = NetPlayer.Get(claimingPlayer)
		};
		RouteNewCommand(cmd, force: false);
	}

	public void ExecuteFreePlot(BuilderCommand cmd)
	{
		int pieceId = cmd.pieceId;
		NetPlayer player = cmd.player;
		if (player != null)
		{
			FreePlotInternal(pieceId, player.ActorNumber);
		}
	}

	private void FreePlotInternal(int plotPieceId, int requestingPlayer)
	{
		if (plotPieceId == -1 && !plotOwners.TryGetValue(requestingPlayer, out plotPieceId))
		{
			return;
		}
		BuilderPiece piece = GetPiece(plotPieceId);
		if (!(piece == null) && piece.IsPrivatePlot() && piece.TryGetPlotComponent(out var plot))
		{
			int ownerActorNumber = plot.GetOwnerActorNumber();
			plotOwners.Remove(ownerActorNumber);
			plot.FreePlot();
			if (ownerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
			{
				SetLocalPlayerOwnsPlot(ownsPlot: false);
			}
		}
	}

	public bool DoesPlayerOwnPlot(int actorNum)
	{
		return plotOwners.ContainsKey(actorNum);
	}

	public void RequestPaintPiece(int pieceId, int materialType)
	{
		builderNetworking.RequestPaintPiece(pieceId, materialType);
	}

	public void PaintPiece(int pieceId, int materialType, Player paintingPlayer, bool force)
	{
		PaintPieceInternal(pieceId, materialType, paintingPlayer, force);
	}

	private void PaintPieceInternal(int pieceId, int materialType, Player paintingPlayer, bool force)
	{
		if (force || paintingPlayer != PhotonNetwork.LocalPlayer)
		{
			BuilderCommand cmd = new BuilderCommand
			{
				type = BuilderCommandType.Paint,
				pieceId = pieceId,
				materialType = materialType,
				player = NetPlayer.Get(paintingPlayer)
			};
			RouteNewCommand(cmd, force);
		}
	}

	public void ExecutePiecePainted(BuilderCommand cmd)
	{
		int pieceId = cmd.pieceId;
		int materialType = cmd.materialType;
		BuilderPiece piece = GetPiece(pieceId);
		if (piece != null && !piece.isBuiltIntoTable)
		{
			piece.SetMaterial(materialType);
		}
	}

	public void CreateArmShelvesForPlayersInBuilder()
	{
		if (!isTableMutable || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
		{
			return;
		}
		foreach (Player armShelfRequest in builderNetworking.armShelfRequests)
		{
			if (armShelfRequest != null)
			{
				builderNetworking.RequestCreateArmShelfForPlayer(armShelfRequest);
			}
		}
		builderNetworking.armShelfRequests.Clear();
	}

	public void RemoveArmShelfForPlayer(Player player)
	{
		if (!isTableMutable || player == null)
		{
			return;
		}
		if (tableState != TableState.Ready)
		{
			builderNetworking.armShelfRequests.Remove(player);
			return;
		}
		if (playerToArmShelfLeft.TryGetValue(player.ActorNumber, out var value))
		{
			BuilderPiece piece = GetPiece(value);
			playerToArmShelfLeft.Remove(player.ActorNumber);
			if (piece.armShelf != null)
			{
				piece.armShelf.piece = null;
				piece.armShelf = null;
			}
			if (PhotonNetwork.IsMasterClient)
			{
				builderNetworking.RequestRecyclePiece(value, piece.transform.position, piece.transform.rotation, playFX: false, -1);
			}
			else
			{
				DropPieceForPlayerLeavingInternal(piece, player.ActorNumber);
			}
		}
		if (playerToArmShelfRight.TryGetValue(player.ActorNumber, out var value2))
		{
			BuilderPiece piece2 = GetPiece(value2);
			playerToArmShelfRight.Remove(player.ActorNumber);
			if (piece2.armShelf != null)
			{
				piece2.armShelf.piece = null;
				piece2.armShelf = null;
			}
			if (PhotonNetwork.IsMasterClient)
			{
				builderNetworking.RequestRecyclePiece(value2, piece2.transform.position, piece2.transform.rotation, playFX: false, -1);
			}
			else
			{
				DropPieceForPlayerLeavingInternal(piece2, player.ActorNumber);
			}
		}
	}

	public void DropAllPiecesForPlayerLeaving(int playerActorNumber)
	{
		List<BuilderPiece> list = pieces;
		if (list == null)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			BuilderPiece builderPiece = list[i];
			if (builderPiece != null && builderPiece.heldByPlayerActorNumber == playerActorNumber && (builderPiece.state == BuilderPiece.State.Grabbed || builderPiece.state == BuilderPiece.State.GrabbedLocal))
			{
				DropPieceForPlayerLeavingInternal(builderPiece, playerActorNumber);
			}
		}
	}

	public void RecycleAllPiecesForPlayerLeaving(int playerActorNumber)
	{
		List<BuilderPiece> list = pieces;
		if (list == null)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			BuilderPiece builderPiece = list[i];
			if (builderPiece != null && builderPiece.heldByPlayerActorNumber == playerActorNumber && (builderPiece.state == BuilderPiece.State.Grabbed || builderPiece.state == BuilderPiece.State.GrabbedLocal))
			{
				RecyclePieceForPlayerLeavingInternal(builderPiece, playerActorNumber);
			}
		}
	}

	private void DropPieceForPlayerLeavingInternal(BuilderPiece piece, int playerActorNumber)
	{
		BuilderAction action = BuilderActions.CreateDetachFromPlayer(-1, piece.pieceId, playerActorNumber);
		BuilderAction action2 = BuilderActions.CreateDropPiece(-1, piece.pieceId, piece.transform.position, piece.transform.rotation, Vector3.zero, Vector3.zero, playerActorNumber);
		ExecuteAction(action);
		ExecuteAction(action2);
	}

	private void RecyclePieceForPlayerLeavingInternal(BuilderPiece piece, int playerActorNumber)
	{
		builderNetworking.RequestRecyclePiece(piece.pieceId, piece.transform.position, piece.transform.rotation, playFX: false, -1);
	}

	private void DetachPieceForPlayerLeavingInternal(BuilderPiece piece, int playerActorNumber)
	{
		BuilderAction action = BuilderActions.CreateDetachFromPiece(-1, piece.pieceId, playerActorNumber);
		BuilderAction action2 = BuilderActions.CreateDropPiece(-1, piece.pieceId, piece.transform.position, piece.transform.rotation, Vector3.zero, Vector3.zero, playerActorNumber);
		ExecuteAction(action);
		ExecuteAction(action2);
	}

	public void CreateArmShelf(int pieceIdLeft, int pieceIdRight, int pieceType, Player player)
	{
		BuilderCommand cmd = new BuilderCommand
		{
			type = BuilderCommandType.CreateArmShelf,
			pieceId = pieceIdLeft,
			pieceType = pieceType,
			player = NetPlayer.Get(player),
			isLeft = true
		};
		RouteNewCommand(cmd, force: false);
		BuilderCommand cmd2 = new BuilderCommand
		{
			type = BuilderCommandType.CreateArmShelf,
			pieceId = pieceIdRight,
			pieceType = pieceType,
			player = NetPlayer.Get(player),
			isLeft = false
		};
		RouteNewCommand(cmd2, force: false);
	}

	public void ExecuteArmShelfCreated(BuilderCommand cmd)
	{
		NetPlayer player = cmd.player;
		if (player == null)
		{
			return;
		}
		bool isLeft = cmd.isLeft;
		if (GetPiece(cmd.pieceId) != null || !VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
		{
			return;
		}
		BuilderArmShelf builderArmShelf = (isLeft ? playerRig.Rig.builderArmShelfLeft : playerRig.Rig.builderArmShelfRight);
		if (!(builderArmShelf != null))
		{
			return;
		}
		if (builderArmShelf.piece != null)
		{
			if (builderArmShelf.piece.isArmShelf && builderArmShelf.piece.isActiveAndEnabled)
			{
				builderArmShelf.piece.armShelf = null;
				RecyclePiece(builderArmShelf.piece.pieceId, builderArmShelf.piece.transform.position, builderArmShelf.piece.transform.rotation, playFX: false, -1, PhotonNetwork.LocalPlayer);
			}
			else
			{
				builderArmShelf.piece = null;
			}
			BuilderPiece builderPiece = (builderArmShelf.piece = CreatePieceInternal(cmd.pieceType, cmd.pieceId, builderArmShelf.pieceAnchor.position, builderArmShelf.pieceAnchor.rotation, BuilderPiece.State.AttachedToArm, -1, 0, this));
			builderPiece.armShelf = builderArmShelf;
			builderPiece.SetParentHeld(builderArmShelf.pieceAnchor, cmd.player.ActorNumber, isLeft);
			builderPiece.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			builderPiece.transform.localScale = Vector3.one;
			if (isLeft)
			{
				playerToArmShelfLeft.AddOrUpdate(player.ActorNumber, cmd.pieceId);
			}
			else
			{
				playerToArmShelfRight.AddOrUpdate(player.ActorNumber, cmd.pieceId);
			}
		}
		else
		{
			BuilderPiece builderPiece2 = (builderArmShelf.piece = CreatePieceInternal(cmd.pieceType, cmd.pieceId, builderArmShelf.pieceAnchor.position, builderArmShelf.pieceAnchor.rotation, BuilderPiece.State.AttachedToArm, -1, 0, this));
			builderPiece2.armShelf = builderArmShelf;
			builderPiece2.SetParentHeld(builderArmShelf.pieceAnchor, cmd.player.ActorNumber, isLeft);
			builderPiece2.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			builderPiece2.transform.localScale = Vector3.one;
			if (isLeft)
			{
				playerToArmShelfLeft.TryAdd(player.ActorNumber, cmd.pieceId);
			}
			else
			{
				playerToArmShelfRight.TryAdd(player.ActorNumber, cmd.pieceId);
			}
		}
	}

	public void ClearLocalArmShelf()
	{
		VRRig offlineVRRig = GorillaTagger.Instance.offlineVRRig;
		if (!(offlineVRRig != null))
		{
			return;
		}
		BuilderArmShelf builderArmShelfLeft = offlineVRRig.builderArmShelfLeft;
		if (builderArmShelfLeft != null)
		{
			BuilderPiece piece = builderArmShelfLeft.piece;
			builderArmShelfLeft.piece = null;
			if (piece != null)
			{
				piece.transform.SetParent(null);
			}
		}
		builderArmShelfLeft = offlineVRRig.builderArmShelfRight;
		if (builderArmShelfLeft != null)
		{
			BuilderPiece piece2 = builderArmShelfLeft.piece;
			builderArmShelfLeft.piece = null;
			if (piece2 != null)
			{
				piece2.transform.SetParent(null);
			}
		}
	}

	public void PieceEnteredDropZone(int pieceId, Vector3 worldPos, Quaternion worldRot, int dropZoneId)
	{
		Vector3 velocity = (roomCenter.position - worldPos).normalized * DROP_ZONE_REPEL;
		BuilderCommand cmd = new BuilderCommand
		{
			type = BuilderCommandType.Repel,
			pieceId = pieceId,
			parentPieceId = pieceId,
			attachPieceId = dropZoneId,
			localPosition = worldPos,
			localRotation = worldRot,
			velocity = velocity,
			angVelocity = Vector3.zero,
			player = NetworkSystem.Instance.MasterClient,
			canRollback = false
		};
		RouteNewCommand(cmd, force: false);
	}

	public bool ValidateRepelPiece(BuilderPiece piece)
	{
		if (!isSetup)
		{
			return false;
		}
		if (piece.isBuiltIntoTable || piece.isArmShelf)
		{
			return false;
		}
		if (piece.state == BuilderPiece.State.Grabbed || piece.state == BuilderPiece.State.GrabbedLocal || piece.state == BuilderPiece.State.Dropped || piece.state == BuilderPiece.State.AttachedToDropped || piece.state == BuilderPiece.State.AttachedToArm)
		{
			bool flag = false;
			for (int i = 0; i < repelHistoryLength; i++)
			{
				flag = flag || repelledPieceRoots[i].Contains(piece.pieceId);
				if (flag)
				{
					return false;
				}
			}
			repelledPieceRoots[repelHistoryIndex].Add(piece.pieceId);
			return true;
		}
		return false;
	}

	public void RepelPieceTowardTable(int pieceID)
	{
		BuilderPiece piece = GetPiece(pieceID);
		if (!(piece == null))
		{
			Vector3 position = piece.transform.position;
			Quaternion rotation = piece.transform.rotation;
			if (position.y < tableCenter.position.y)
			{
				position.y = tableCenter.position.y;
			}
			Vector3 linearVelocity = (tableCenter.position - position).normalized * DROP_ZONE_REPEL;
			if (piece.IsHeldLocal())
			{
				BuilderPieceInteractor.instance.RemovePieceFromHeld(piece);
			}
			piece.ClearParentHeld();
			piece.ClearParentPiece();
			piece.transform.localScale = Vector3.one;
			piece.SetState(BuilderPiece.State.Dropped);
			piece.transform.SetLocalPositionAndRotation(position, rotation);
			if (piece.rigidBody != null)
			{
				piece.rigidBody.position = position;
				piece.rigidBody.rotation = rotation;
				piece.rigidBody.linearVelocity = linearVelocity;
				piece.rigidBody.AddForce(Vector3.up * (DROP_ZONE_REPEL / 2f) * piece.rigidBody.mass, ForceMode.Impulse);
				piece.rigidBody.angularVelocity = Vector3.zero;
			}
		}
	}

	public BuilderPiece GetPiece(int pieceId)
	{
		if (pieceIDToIndexCache.TryGetValue(pieceId, out var value))
		{
			if (value >= 0 && value < pieces.Count)
			{
				return pieces[value];
			}
			pieceIDToIndexCache.Remove(pieceId);
		}
		for (int i = 0; i < pieces.Count; i++)
		{
			if (pieces[i].pieceId == pieceId)
			{
				pieceIDToIndexCache.Add(pieceId, i);
				return pieces[i];
			}
		}
		for (int j = 0; j < basePieces.Count; j++)
		{
			if (basePieces[j].pieceId == pieceId)
			{
				return basePieces[j];
			}
		}
		return null;
	}

	public void AddPiece(BuilderPiece piece)
	{
		pieces.Add(piece);
		UseResources(piece);
		AddPieceData(piece);
	}

	public void RemovePiece(BuilderPiece piece)
	{
		pieces.Remove(piece);
		AddResources(piece);
		RemovePieceData(piece);
		pieceIDToIndexCache.Clear();
	}

	private void CreateData()
	{
	}

	private void DestroyData()
	{
	}

	private int AddPieceData(BuilderPiece piece)
	{
		return -1;
	}

	public void UpdatePieceData(BuilderPiece piece)
	{
	}

	private void RemovePieceData(BuilderPiece piece)
	{
	}

	private int AddGridPlaneData(BuilderAttachGridPlane gridPlane)
	{
		return -1;
	}

	private void RemoveGridPlaneData(BuilderAttachGridPlane gridPlane)
	{
	}

	private int AddPrivatePlotData(BuilderPiecePrivatePlot plot)
	{
		return -1;
	}

	private void RemovePrivatePlotData(BuilderPiecePrivatePlot plot)
	{
	}

	public void OnButtonFreeRotation(BuilderOptionButton button, bool isLeftHand)
	{
		useSnapRotation = !useSnapRotation;
		button.SetPressed(useSnapRotation);
	}

	public void OnButtonFreePosition(BuilderOptionButton button, bool isLeftHand)
	{
		if (usePlacementStyle == BuilderPlacementStyle.Float)
		{
			usePlacementStyle = BuilderPlacementStyle.SnapDown;
		}
		else if (usePlacementStyle == BuilderPlacementStyle.SnapDown)
		{
			usePlacementStyle = BuilderPlacementStyle.Float;
		}
		button.SetPressed(usePlacementStyle != BuilderPlacementStyle.Float);
	}

	public void OnButtonSaveLayout(BuilderOptionButton button, bool isLeftHand)
	{
	}

	public void OnButtonClearLayout(BuilderOptionButton button, bool isLeftHand)
	{
	}

	public bool TryPlaceGridPlane(BuilderPiece piece, BuilderAttachGridPlane gridPlane, List<BuilderAttachGridPlane> checkGridPlanes, out BuilderPotentialPlacement potentialPlacement)
	{
		potentialPlacement = default(BuilderPotentialPlacement);
		potentialPlacement.Reset();
		Vector3 position = gridPlane.transform.position;
		Quaternion rotation = gridPlane.transform.rotation;
		if (gridSize <= 0f)
		{
			return false;
		}
		bool success = false;
		for (int i = 0; i < checkGridPlanes.Count; i++)
		{
			BuilderAttachGridPlane checkGridPlane = checkGridPlanes[i];
			TryPlaceGridPlaneOnGridPlane(piece, gridPlane, position, rotation, checkGridPlane, ref potentialPlacement, ref success);
		}
		return success;
	}

	public bool TryPlaceGridPlaneOnGridPlane(BuilderPiece piece, BuilderAttachGridPlane gridPlane, Vector3 gridPlanePos, Quaternion gridPlaneRot, BuilderAttachGridPlane checkGridPlane, ref BuilderPotentialPlacement potentialPlacement, ref bool success)
	{
		if (checkGridPlane.male == gridPlane.male)
		{
			return false;
		}
		if (checkGridPlane.piece == gridPlane.piece)
		{
			return false;
		}
		Transform center = checkGridPlane.center;
		Vector3 position = center.position;
		float sqrMagnitude = (position - gridPlanePos).sqrMagnitude;
		float num = checkGridPlane.boundingRadius + gridPlane.boundingRadius;
		if (sqrMagnitude > num * num)
		{
			return false;
		}
		Quaternion rotation = center.rotation;
		Quaternion quaternion = Quaternion.Inverse(rotation);
		Quaternion quaternion2 = quaternion * gridPlaneRot;
		if (Vector3.Dot(Vector3.up, quaternion2 * Vector3.up) < currSnapParams.maxUpDotProduct)
		{
			return false;
		}
		Vector3 vector = quaternion * (gridPlanePos - position);
		float y = vector.y;
		float num2 = 0f - Mathf.Abs(y);
		if (success && num2 < potentialPlacement.score)
		{
			return false;
		}
		if (Mathf.Abs(y) > 1f)
		{
			return false;
		}
		if ((gridPlane.male && y > currSnapParams.minOffsetY) || (!gridPlane.male && y < 0f - currSnapParams.minOffsetY))
		{
			return false;
		}
		if (Mathf.Abs(y) > currSnapParams.maxOffsetY)
		{
			return false;
		}
		BoingKit.QuaternionUtil.DecomposeSwingTwist(quaternion2, Vector3.up, out var _, out var twist);
		float maxTwistDotProduct = currSnapParams.maxTwistDotProduct;
		Vector3 lhs = twist * Vector3.forward;
		float num3 = Vector3.Dot(lhs, Vector3.forward);
		float num4 = Vector3.Dot(lhs, Vector3.right);
		bool flag = Mathf.Abs(num3) > maxTwistDotProduct;
		bool flag2 = Mathf.Abs(num4) > maxTwistDotProduct;
		if (!(flag || flag2))
		{
			return false;
		}
		uint num5 = 0u;
		float y2;
		if (flag)
		{
			y2 = ((num3 > 0f) ? 0f : 180f);
			num5 = ((!(num3 > 0f)) ? 2u : 0u);
		}
		else
		{
			y2 = ((num4 > 0f) ? 90f : 270f);
			num5 = ((num4 > 0f) ? 1u : 3u);
		}
		int num6 = (flag2 ? gridPlane.width : gridPlane.length);
		int num7 = (flag2 ? gridPlane.length : gridPlane.width);
		float num8 = ((num7 % 2 == 0) ? (gridSize / 2f) : 0f);
		float num9 = ((num6 % 2 == 0) ? (gridSize / 2f) : 0f);
		float num10 = ((checkGridPlane.width % 2 == 0) ? (gridSize / 2f) : 0f);
		float num11 = ((checkGridPlane.length % 2 == 0) ? (gridSize / 2f) : 0f);
		float num12 = num8 - num10;
		float num13 = num9 - num11;
		int num14 = Mathf.RoundToInt((vector.x - num12) / gridSize);
		int num15 = Mathf.RoundToInt((vector.z - num13) / gridSize);
		int num16 = num14 + Mathf.FloorToInt((float)num7 / 2f);
		int num17 = num15 + Mathf.FloorToInt((float)num6 / 2f);
		int num18 = num16 - (num7 - 1);
		int num19 = num17 - (num6 - 1);
		int num20 = Mathf.FloorToInt((float)checkGridPlane.width / 2f);
		int num21 = Mathf.FloorToInt((float)checkGridPlane.length / 2f);
		int num22 = num20 - (checkGridPlane.width - 1);
		int num23 = num21 - (checkGridPlane.length - 1);
		if (num18 > num20 || num16 < num22 || num19 > num21 || num17 < num23)
		{
			return false;
		}
		BuilderPiece rootPiece = checkGridPlane.piece.GetRootPiece();
		if (ShareSameRoot(gridPlane.piece, rootPiece))
		{
			return false;
		}
		if (!BuilderPiece.CanPlayerAttachPieceToPiece(PhotonNetwork.LocalPlayer.ActorNumber, gridPlane.piece, rootPiece))
		{
			return false;
		}
		BuilderPiece piece2 = checkGridPlane.piece;
		if (piece2 != null)
		{
			if (piece2.preventSnapUntilMoved > 0)
			{
				return false;
			}
			if (piece2.requestedParentPiece != null && ShareSameRoot(piece, piece2.requestedParentPiece))
			{
				return false;
			}
		}
		Quaternion quaternion3 = Quaternion.Euler(0f, y2, 0f);
		Quaternion quaternion4 = rotation * quaternion3;
		float x = (float)num14 * gridSize + num12;
		float z = (float)num15 * gridSize + num13;
		Vector3 vector2 = new Vector3(x, 0f, z);
		Vector3 vector3 = position + rotation * vector2;
		Transform center2 = gridPlane.center;
		Quaternion quaternion5 = quaternion4 * Quaternion.Inverse(center2.localRotation);
		Vector3 vector4 = piece.transform.InverseTransformPoint(center2.position);
		Vector3 localPosition = vector3 - quaternion5 * vector4;
		potentialPlacement.localPosition = localPosition;
		potentialPlacement.localRotation = quaternion5;
		potentialPlacement.score = num2;
		success = true;
		potentialPlacement.parentPiece = piece2;
		potentialPlacement.parentAttachIndex = checkGridPlane.attachIndex;
		potentialPlacement.attachDistance = Mathf.Abs(y);
		potentialPlacement.attachPlaneNormal = Vector3.up;
		if (!checkGridPlane.male)
		{
			potentialPlacement.attachPlaneNormal *= -1f;
		}
		if (potentialPlacement.parentPiece != null)
		{
			BuilderAttachGridPlane builderAttachGridPlane = potentialPlacement.parentPiece.gridPlanes[potentialPlacement.parentAttachIndex];
			potentialPlacement.localPosition = builderAttachGridPlane.transform.InverseTransformPoint(potentialPlacement.localPosition);
			potentialPlacement.localRotation = Quaternion.Inverse(builderAttachGridPlane.transform.rotation) * potentialPlacement.localRotation;
		}
		potentialPlacement.parentAttachBounds.min.x = Mathf.Max(num22, num18);
		potentialPlacement.parentAttachBounds.min.y = Mathf.Max(num23, num19);
		potentialPlacement.parentAttachBounds.max.x = Mathf.Min(num20, num16);
		potentialPlacement.parentAttachBounds.max.y = Mathf.Min(num21, num17);
		Vector2Int v = Vector2Int.zero;
		Vector2Int v2 = Vector2Int.zero;
		v.x = potentialPlacement.parentAttachBounds.min.x - num14;
		v2.x = potentialPlacement.parentAttachBounds.max.x - num14;
		v.y = potentialPlacement.parentAttachBounds.min.y - num15;
		v2.y = potentialPlacement.parentAttachBounds.max.y - num15;
		potentialPlacement.twist = (byte)num5;
		potentialPlacement.bumpOffsetX = (sbyte)num14;
		potentialPlacement.bumpOffsetZ = (sbyte)num15;
		int offsetX = ((num7 % 2 == 0) ? 1 : 0);
		int offsetY = ((num6 % 2 == 0) ? 1 : 0);
		if (flag && num3 < 0f)
		{
			v = Rotate180(v, offsetX, offsetY);
			v2 = Rotate180(v2, offsetX, offsetY);
		}
		else if (flag2 && num4 < 0f)
		{
			v = Rotate270(v, offsetX, offsetY);
			v2 = Rotate270(v2, offsetX, offsetY);
		}
		else if (flag2 && num4 > 0f)
		{
			v = Rotate90(v, offsetX, offsetY);
			v2 = Rotate90(v2, offsetX, offsetY);
		}
		potentialPlacement.attachBounds.min.x = Mathf.Min(v.x, v2.x);
		potentialPlacement.attachBounds.min.y = Mathf.Min(v.y, v2.y);
		potentialPlacement.attachBounds.max.x = Mathf.Max(v.x, v2.x);
		potentialPlacement.attachBounds.max.y = Mathf.Max(v.y, v2.y);
		return true;
	}

	private Vector2Int Rotate90(Vector2Int v, int offsetX, int offsetY)
	{
		return new Vector2Int(v.y * -1 + offsetY, v.x);
	}

	private Vector2Int Rotate270(Vector2Int v, int offsetX, int offsetY)
	{
		return new Vector2Int(v.y, v.x * -1 + offsetX);
	}

	private Vector2Int Rotate180(Vector2Int v, int offsetX, int offsetY)
	{
		return new Vector2Int(v.x * -1 + offsetX, v.y * -1 + offsetY);
	}

	public bool ShareSameRoot(BuilderAttachGridPlane plane, BuilderAttachGridPlane otherPlane)
	{
		if (plane == null || otherPlane == null || otherPlane.piece == null)
		{
			return false;
		}
		return ShareSameRoot(plane.piece, otherPlane.piece);
	}

	public static bool ShareSameRoot(BuilderPiece piece, BuilderPiece otherPiece)
	{
		if (otherPiece == null || piece == null)
		{
			return false;
		}
		if (piece == otherPiece)
		{
			return true;
		}
		BuilderPiece builderPiece = piece;
		int num = 2048;
		while (builderPiece.parentPiece != null && !builderPiece.parentPiece.isBuiltIntoTable)
		{
			builderPiece = builderPiece.parentPiece;
			num--;
			if (num <= 0)
			{
				return true;
			}
		}
		num = 2048;
		BuilderPiece builderPiece2 = otherPiece;
		while (builderPiece2.parentPiece != null && !builderPiece2.parentPiece.isBuiltIntoTable)
		{
			builderPiece2 = builderPiece2.parentPiece;
			num--;
			if (num <= 0)
			{
				return true;
			}
		}
		return builderPiece == builderPiece2;
	}

	public bool TryPlacePieceOnTableNoDrop(bool leftHand, BuilderPiece testPiece, List<BuilderAttachGridPlane> checkGridPlanesMale, List<BuilderAttachGridPlane> checkGridPlanesFemale, out BuilderPotentialPlacement potentialPlacement)
	{
		potentialPlacement = default(BuilderPotentialPlacement);
		potentialPlacement.Reset();
		if (this == null)
		{
			return false;
		}
		if (testPiece == null)
		{
			return false;
		}
		currSnapParams = pushAndEaseParams;
		return TryPlacePieceGridPlanesOnTableInternal(testPiece, maxPlacementChildDepth, checkGridPlanesMale, checkGridPlanesFemale, out potentialPlacement);
	}

	public bool TryPlacePieceOnTableNoDropJobs(NativeList<BuilderGridPlaneData> gridPlaneData, NativeList<BuilderPieceData> pieceData, NativeList<BuilderGridPlaneData> checkGridPlaneData, NativeList<BuilderPieceData> checkPieceData, out BuilderPotentialPlacement potentialPlacement, List<BuilderPotentialPlacement> allPlacements)
	{
		potentialPlacement = default(BuilderPotentialPlacement);
		potentialPlacement.Reset();
		if (this == null)
		{
			return false;
		}
		currSnapParams = pushAndEaseParams;
		NativeQueue<BuilderPotentialPlacementData> nativeQueue = new NativeQueue<BuilderPotentialPlacementData>(Allocator.TempJob);
		IJobParallelForExtensions.Schedule(new BuilderFindPotentialSnaps
		{
			gridSize = gridSize,
			currSnapParams = currSnapParams,
			gridPlanes = gridPlaneData,
			checkGridPlanes = checkGridPlaneData,
			worldToLocalPos = Vector3.zero,
			worldToLocalRot = Quaternion.identity,
			localToWorldPos = Vector3.zero,
			localToWorldRot = Quaternion.identity,
			potentialPlacements = nativeQueue.AsParallelWriter()
		}, gridPlaneData.Length, 32).Complete();
		BuilderPotentialPlacementData builderPotentialPlacementData = default(BuilderPotentialPlacementData);
		bool flag = false;
		while (!nativeQueue.IsEmpty())
		{
			BuilderPotentialPlacementData builderPotentialPlacementData2 = nativeQueue.Dequeue();
			if (!flag || builderPotentialPlacementData2.score > builderPotentialPlacementData.score)
			{
				builderPotentialPlacementData = builderPotentialPlacementData2;
				flag = true;
			}
		}
		if (flag)
		{
			potentialPlacement = builderPotentialPlacementData.ToPotentialPlacement(this);
		}
		if (flag)
		{
			nativeQueue.Clear();
			currSnapParams = overlapParams;
			Vector3 worldToLocalPos = -potentialPlacement.attachPiece.transform.position;
			Quaternion worldToLocalRot = Quaternion.Inverse(potentialPlacement.attachPiece.transform.rotation);
			BuilderAttachGridPlane builderAttachGridPlane = potentialPlacement.parentPiece.gridPlanes[potentialPlacement.parentAttachIndex];
			Quaternion localToWorldRot = builderAttachGridPlane.transform.rotation * potentialPlacement.localRotation;
			Vector3 localToWorldPos = builderAttachGridPlane.transform.TransformPoint(potentialPlacement.localPosition);
			IJobParallelForExtensions.Schedule(new BuilderFindPotentialSnaps
			{
				gridSize = gridSize,
				currSnapParams = currSnapParams,
				gridPlanes = gridPlaneData,
				checkGridPlanes = checkGridPlaneData,
				worldToLocalPos = worldToLocalPos,
				worldToLocalRot = worldToLocalRot,
				localToWorldPos = localToWorldPos,
				localToWorldRot = localToWorldRot,
				potentialPlacements = nativeQueue.AsParallelWriter()
			}, gridPlaneData.Length, 32).Complete();
			while (!nativeQueue.IsEmpty())
			{
				BuilderPotentialPlacementData builderPotentialPlacementData3 = nativeQueue.Dequeue();
				if (builderPotentialPlacementData3.attachDistance < currSnapParams.maxBlockSnapDist)
				{
					allPlacements.Add(builderPotentialPlacementData3.ToPotentialPlacement(this));
				}
			}
		}
		nativeQueue.Dispose();
		return flag;
	}

	public bool CalcAllPotentialPlacements(NativeList<BuilderGridPlaneData> gridPlaneData, NativeList<BuilderGridPlaneData> checkGridPlaneData, BuilderPotentialPlacement potentialPlacement, List<BuilderPotentialPlacement> allPlacements)
	{
		if (this == null)
		{
			return false;
		}
		bool result = false;
		currSnapParams = overlapParams;
		NativeQueue<BuilderPotentialPlacementData> nativeQueue = new NativeQueue<BuilderPotentialPlacementData>(Allocator.TempJob);
		nativeQueue.Clear();
		Vector3 worldToLocalPos = -potentialPlacement.attachPiece.transform.position;
		Quaternion worldToLocalRot = Quaternion.Inverse(potentialPlacement.attachPiece.transform.rotation);
		BuilderAttachGridPlane builderAttachGridPlane = potentialPlacement.parentPiece.gridPlanes[potentialPlacement.parentAttachIndex];
		Quaternion localToWorldRot = builderAttachGridPlane.transform.rotation * potentialPlacement.localRotation;
		Vector3 localToWorldPos = builderAttachGridPlane.transform.TransformPoint(potentialPlacement.localPosition);
		IJobParallelForExtensions.Schedule(new BuilderFindPotentialSnaps
		{
			gridSize = gridSize,
			currSnapParams = currSnapParams,
			gridPlanes = gridPlaneData,
			checkGridPlanes = checkGridPlaneData,
			worldToLocalPos = worldToLocalPos,
			worldToLocalRot = worldToLocalRot,
			localToWorldPos = localToWorldPos,
			localToWorldRot = localToWorldRot,
			potentialPlacements = nativeQueue.AsParallelWriter()
		}, gridPlaneData.Length, 32).Complete();
		while (!nativeQueue.IsEmpty())
		{
			BuilderPotentialPlacementData builderPotentialPlacementData = nativeQueue.Dequeue();
			if (builderPotentialPlacementData.attachDistance < currSnapParams.maxBlockSnapDist)
			{
				allPlacements.Add(builderPotentialPlacementData.ToPotentialPlacement(this));
			}
		}
		nativeQueue.Dispose();
		return result;
	}

	public bool CanPiecesPotentiallySnap(BuilderPiece pieceInHand, BuilderPiece piece)
	{
		BuilderPiece rootPiece = piece.GetRootPiece();
		if (rootPiece == pieceInHand)
		{
			return false;
		}
		if (!BuilderPiece.CanPlayerAttachPieceToPiece(PhotonNetwork.LocalPlayer.ActorNumber, pieceInHand, rootPiece))
		{
			return false;
		}
		if (piece.requestedParentPiece != null && ShareSameRoot(pieceInHand, piece.requestedParentPiece))
		{
			return false;
		}
		if (piece.preventSnapUntilMoved > 0)
		{
			return false;
		}
		return true;
	}

	public bool CanPiecesPotentiallyOverlap(BuilderPiece pieceInHand, BuilderPiece rootWhenPlaced, BuilderPiece.State stateWhenPlaced, BuilderPiece otherPiece)
	{
		BuilderPiece rootPiece = otherPiece.GetRootPiece();
		if (rootPiece == pieceInHand)
		{
			return false;
		}
		if (!BuilderPiece.CanPlayerAttachPieceToPiece(PhotonNetwork.LocalPlayer.ActorNumber, pieceInHand, rootPiece))
		{
			return false;
		}
		if (otherPiece.requestedParentPiece != null && ShareSameRoot(pieceInHand, otherPiece.requestedParentPiece))
		{
			return false;
		}
		if (otherPiece.preventSnapUntilMoved > 0)
		{
			return false;
		}
		BuilderPiece.State stateB = otherPiece.state;
		if (otherPiece.isBuiltIntoTable && !otherPiece.isArmShelf)
		{
			stateB = BuilderPiece.State.AttachedAndPlaced;
		}
		return AreStatesCompatibleForOverlap(stateWhenPlaced, stateB, rootWhenPlaced, rootPiece);
	}

	public void TryDropPiece(bool leftHand, BuilderPiece testPiece, Vector3 velocity, Vector3 angVelocity)
	{
		if (!(this == null) && !(testPiece == null))
		{
			RequestDropPiece(testPiece, testPiece.transform.position, testPiece.transform.rotation, velocity, angVelocity);
		}
	}

	public bool TryPlacePieceGridPlanesOnTableInternal(BuilderPiece testPiece, int recurse, List<BuilderAttachGridPlane> checkGridPlanesMale, List<BuilderAttachGridPlane> checkGridPlanesFemale, out BuilderPotentialPlacement potentialPlacement)
	{
		potentialPlacement = default(BuilderPotentialPlacement);
		potentialPlacement.Reset();
		bool result = false;
		bool flag = false;
		if (testPiece != null && testPiece.gridPlanes != null && testPiece.gridPlanes.Count > 0 && testPiece.gridPlanes != null)
		{
			for (int i = 0; i < testPiece.gridPlanes.Count; i++)
			{
				List<BuilderAttachGridPlane> checkGridPlanes = (testPiece.gridPlanes[i].male ? checkGridPlanesFemale : checkGridPlanesMale);
				if (TryPlaceGridPlane(testPiece, testPiece.gridPlanes[i], checkGridPlanes, out var potentialPlacement2))
				{
					if (potentialPlacement2.attachDistance < currSnapParams.snapAttachDistance * 1.1f)
					{
						flag = true;
					}
					if (potentialPlacement2.score > potentialPlacement.score && testPiece.preventSnapUntilMoved <= 0)
					{
						potentialPlacement = potentialPlacement2;
						potentialPlacement.attachIndex = i;
						potentialPlacement.attachPiece = testPiece;
						result = true;
					}
				}
			}
		}
		if (recurse > 0)
		{
			BuilderPiece builderPiece = testPiece.firstChildPiece;
			while (builderPiece != null)
			{
				if (TryPlacePieceGridPlanesOnTableInternal(builderPiece, recurse - 1, checkGridPlanesMale, checkGridPlanesFemale, out var potentialPlacement3))
				{
					if (potentialPlacement3.attachDistance < currSnapParams.snapAttachDistance * 1.1f)
					{
						flag = true;
					}
					if (potentialPlacement3.score > potentialPlacement.score && testPiece.preventSnapUntilMoved <= 0)
					{
						potentialPlacement = potentialPlacement3;
						result = true;
					}
				}
				builderPiece = builderPiece.nextSiblingPiece;
			}
		}
		if (testPiece.preventSnapUntilMoved > 0 && !flag)
		{
			testPiece.preventSnapUntilMoved--;
			UpdatePieceData(testPiece);
		}
		return result;
	}

	public void TryPlaceRandomlyOnTable(BuilderPiece piece)
	{
		BuilderAttachGridPlane builderAttachGridPlane = piece.gridPlanes[UnityEngine.Random.Range(0, piece.gridPlanes.Count)];
		List<BuilderAttachGridPlane> list = baseGridPlanes;
		int num = UnityEngine.Random.Range(0, list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			int index = (i + num) % list.Count;
			BuilderAttachGridPlane builderAttachGridPlane2 = list[index];
			if (builderAttachGridPlane2.male != builderAttachGridPlane.male && !(builderAttachGridPlane2.piece == builderAttachGridPlane.piece) && !ShareSameRoot(builderAttachGridPlane, builderAttachGridPlane2))
			{
				_ = Vector3.zero;
				_ = Quaternion.identity;
				BuilderPiece piece2 = builderAttachGridPlane2.piece;
				int attachIndex = builderAttachGridPlane2.attachIndex;
				Transform center = builderAttachGridPlane.center;
				Quaternion quaternion = builderAttachGridPlane2.transform.rotation * Quaternion.Inverse(center.localRotation);
				Vector3 vector = piece.transform.InverseTransformPoint(center.position);
				Vector3 vector2 = builderAttachGridPlane2.transform.position - quaternion * vector;
				if (piece2 != null)
				{
					BuilderAttachGridPlane builderAttachGridPlane3 = piece2.gridPlanes[attachIndex];
					Vector3 lossyScale = builderAttachGridPlane3.transform.lossyScale;
					Vector3 b = new Vector3(1f / lossyScale.x, 1f / lossyScale.y, 1f / lossyScale.z);
					_ = Quaternion.Inverse(builderAttachGridPlane3.transform.rotation) * Vector3.Scale(vector2 - builderAttachGridPlane3.transform.position, b);
					_ = Quaternion.Inverse(builderAttachGridPlane3.transform.rotation) * quaternion;
				}
				break;
			}
		}
	}

	public void UseResources(BuilderPiece piece)
	{
		BuilderResources cost = piece.cost;
		if (!(cost == null))
		{
			for (int i = 0; i < cost.quantities.Count; i++)
			{
				UseResource(cost.quantities[i]);
			}
		}
	}

	private void UseResource(BuilderResourceQuantity quantity)
	{
		if (quantity.type >= BuilderResourceType.Basic && quantity.type < BuilderResourceType.Count)
		{
			usedResources[(int)quantity.type] += quantity.count;
			if (tableState == TableState.Ready)
			{
				OnAvailableResourcesChange();
			}
		}
	}

	public void AddResources(BuilderPiece piece)
	{
		BuilderResources cost = piece.cost;
		if (!(cost == null))
		{
			for (int i = 0; i < cost.quantities.Count; i++)
			{
				AddResource(cost.quantities[i]);
			}
		}
	}

	private void AddResource(BuilderResourceQuantity quantity)
	{
		if (quantity.type >= BuilderResourceType.Basic && quantity.type < BuilderResourceType.Count)
		{
			usedResources[(int)quantity.type] -= quantity.count;
			if (tableState == TableState.Ready)
			{
				OnAvailableResourcesChange();
			}
		}
	}

	public bool HasEnoughUnreservedResources(BuilderResources resources)
	{
		if (resources == null)
		{
			return false;
		}
		for (int i = 0; i < resources.quantities.Count; i++)
		{
			if (!HasEnoughUnreservedResource(resources.quantities[i]))
			{
				return false;
			}
		}
		return true;
	}

	public bool HasEnoughUnreservedResource(BuilderResourceQuantity quantity)
	{
		if (quantity.type < BuilderResourceType.Basic || quantity.type >= BuilderResourceType.Count)
		{
			return false;
		}
		return usedResources[(int)quantity.type] + reservedResources[(int)quantity.type] + quantity.count <= maxResources[(int)quantity.type];
	}

	public bool HasEnoughResources(BuilderPiece piece)
	{
		BuilderResources cost = piece.cost;
		if (cost == null)
		{
			return false;
		}
		for (int i = 0; i < cost.quantities.Count; i++)
		{
			if (!HasEnoughResource(cost.quantities[i]))
			{
				return false;
			}
		}
		return true;
	}

	public bool HasEnoughResource(BuilderResourceQuantity quantity)
	{
		if (quantity.type < BuilderResourceType.Basic || quantity.type >= BuilderResourceType.Count)
		{
			return false;
		}
		return usedResources[(int)quantity.type] + quantity.count <= maxResources[(int)quantity.type];
	}

	public int GetAvailableResources(BuilderResourceType type)
	{
		if (type < BuilderResourceType.Basic || type >= BuilderResourceType.Count)
		{
			return 0;
		}
		return maxResources[(int)type] - usedResources[(int)type];
	}

	private void OnAvailableResourcesChange()
	{
		if (!isSetup || !isTableMutable)
		{
			return;
		}
		for (int i = 0; i < conveyors.Count; i++)
		{
			conveyors[i].OnAvailableResourcesChange();
		}
		foreach (BuilderResourceMeter resourceMeter in resourceMeters)
		{
			resourceMeter.OnAvailableResourcesChange();
		}
	}

	public int GetPrivateResourceLimitForType(int type)
	{
		if (plotMaxResources == null)
		{
			return 0;
		}
		return plotMaxResources[type];
	}

	private void WriteVector3(BinaryWriter writer, Vector3 data)
	{
		writer.Write(data.x);
		writer.Write(data.y);
		writer.Write(data.z);
	}

	private void WriteQuaternion(BinaryWriter writer, Quaternion data)
	{
		writer.Write(data.x);
		writer.Write(data.y);
		writer.Write(data.z);
		writer.Write(data.w);
	}

	private Vector3 ReadVector3(BinaryReader reader)
	{
		Vector3 result = default(Vector3);
		result.x = reader.ReadSingle();
		result.y = reader.ReadSingle();
		result.z = reader.ReadSingle();
		return result;
	}

	private Quaternion ReadQuaternion(BinaryReader reader)
	{
		Quaternion result = default(Quaternion);
		result.x = reader.ReadSingle();
		result.y = reader.ReadSingle();
		result.z = reader.ReadSingle();
		result.w = reader.ReadSingle();
		return result;
	}

	public static int PackPiecePlacement(byte twist, sbyte xOffset, sbyte zOffset)
	{
		int num = twist & 3;
		int num2 = xOffset + 128;
		int num3 = zOffset + 128;
		return num2 + (num3 << 8) + (num << 16);
	}

	public static void UnpackPiecePlacement(int packed, out byte twist, out sbyte xOffset, out sbyte zOffset)
	{
		int num = packed & 0xFF;
		int num2 = (packed >> 8) & 0xFF;
		int num3 = (packed >> 16) & 3;
		twist = (byte)num3;
		xOffset = (sbyte)(num - 128);
		zOffset = (sbyte)(num2 - 128);
	}

	private long PackSnapInfo(int attachGridIndex, int otherAttachGridIndex, Vector2Int min, Vector2Int max)
	{
		long num = Mathf.Clamp(attachGridIndex, 0, 31);
		long num2 = Mathf.Clamp(otherAttachGridIndex, 0, 31);
		long num3 = Mathf.Clamp(min.x + 1024, 0, 2047);
		long num4 = Mathf.Clamp(min.y + 1024, 0, 2047);
		long num5 = Mathf.Clamp(max.x + 1024, 0, 2047);
		long num6 = Mathf.Clamp(max.y + 1024, 0, 2047);
		return num + (num2 << 5) + (num3 << 10) + (num4 << 21) + (num5 << 32) + (num6 << 43);
	}

	private void UnpackSnapInfo(long packed, out int attachGridIndex, out int otherAttachGridIndex, out Vector2Int min, out Vector2Int max)
	{
		long num = packed & 0x1F;
		attachGridIndex = (int)num;
		num = (packed >> 5) & 0x1F;
		otherAttachGridIndex = (int)num;
		int x = (int)((packed >> 10) & 0x7FF) - 1024;
		int y = (int)((packed >> 21) & 0x7FF) - 1024;
		min = new Vector2Int(x, y);
		int x2 = (int)((packed >> 32) & 0x7FF) - 1024;
		int y2 = (int)((packed >> 43) & 0x7FF) - 1024;
		max = new Vector2Int(x2, y2);
	}

	private void OnTitleDataUpdate(string key)
	{
		if (key.Equals(SharedMapConfigTitleDataKey))
		{
			FetchSharedBlocksStartingMapConfig();
		}
	}

	private void FetchSharedBlocksStartingMapConfig()
	{
		if (!isTableMutable)
		{
			PlayFabTitleDataCache.Instance.GetTitleData(SharedMapConfigTitleDataKey, OnGetStartingMapConfigSuccess, OnGetStartingMapConfigFail);
		}
	}

	private void OnGetStartingMapConfigSuccess(string result)
	{
		ResetStartingMapConfig();
		if (result.IsNullOrEmpty())
		{
			return;
		}
		try
		{
			SharedBlocksManager.StartingMapConfig startingMapConfig = JsonUtility.FromJson<SharedBlocksManager.StartingMapConfig>(result);
			if (startingMapConfig.useMapID)
			{
				if (SharedBlocksManager.IsMapIDValid(startingMapConfig.mapID))
				{
					this.startingMapConfig.useMapID = true;
					this.startingMapConfig.mapID = startingMapConfig.mapID;
				}
				else
				{
					GTDev.LogError($"BuilderTable {tableZone} OnGetStartingMapConfigSuccess Title Data Default Map Config has Invalid Map ID");
				}
				return;
			}
			this.startingMapConfig.pageNumber = Mathf.Max(startingMapConfig.pageNumber, 0);
			this.startingMapConfig.pageSize = Mathf.Max(startingMapConfig.pageSize, 1);
			if (!startingMapConfig.sortMethod.IsNullOrEmpty() && (startingMapConfig.sortMethod.Equals(SharedBlocksManager.MapSortMethod.Top.ToString()) || startingMapConfig.sortMethod.Equals(SharedBlocksManager.MapSortMethod.NewlyCreated.ToString()) || startingMapConfig.sortMethod.Equals(SharedBlocksManager.MapSortMethod.RecentlyUpdated.ToString())))
			{
				this.startingMapConfig.sortMethod = startingMapConfig.sortMethod;
			}
			else
			{
				GTDev.LogError("BuilderTable " + tableZone.ToString() + " OnGetStartingMapConfigSuccess Unknown sort method " + startingMapConfig.sortMethod);
			}
			SharedBlocksManager.instance.RefreshPopularMapsForRandom();
		}
		catch (Exception ex)
		{
			GTDev.LogError("BuilderTable " + tableZone.ToString() + " OnGetStartingMapConfigSuccess Exception Deserializing " + ex.Message);
		}
	}

	private void OnGetStartingMapConfigFail(PlayFabError error)
	{
		GTDev.LogWarning("BuilderTable " + tableZone.ToString() + " OnGetStartingMapConfigFail " + error.Error);
		ResetStartingMapConfig();
	}

	private void ResetStartingMapConfig()
	{
		startingMapConfig = new SharedBlocksManager.StartingMapConfig
		{
			pageNumber = 0,
			pageSize = 50,
			sortMethod = SharedBlocksManager.MapSortMethod.Top.ToString(),
			useMapID = false,
			mapID = null
		};
	}

	private void RequestTableConfiguration()
	{
		SharedBlocksManager.instance.OnGetTableConfiguration += OnGetTableConfiguration;
		SharedBlocksManager.instance.RequestTableConfiguration();
	}

	private void OnGetTableConfiguration(string configString)
	{
		SharedBlocksManager.instance.OnGetTableConfiguration -= OnGetTableConfiguration;
		if (!configString.IsNullOrEmpty())
		{
			ParseTableConfiguration(configString);
		}
	}

	private void ParseTableConfiguration(string dataRecord)
	{
		if (string.IsNullOrEmpty(dataRecord))
		{
			return;
		}
		BuilderTableConfiguration builderTableConfiguration = JsonUtility.FromJson<BuilderTableConfiguration>(dataRecord);
		if (builderTableConfiguration == null)
		{
			return;
		}
		if (builderTableConfiguration.TableResourceLimits != null)
		{
			for (int i = 0; i < builderTableConfiguration.TableResourceLimits.Length; i++)
			{
				int num = builderTableConfiguration.TableResourceLimits[i];
				if (num >= 0)
				{
					maxResources[i] = num;
				}
			}
		}
		if (builderTableConfiguration.PlotResourceLimits != null)
		{
			for (int j = 0; j < builderTableConfiguration.PlotResourceLimits.Length; j++)
			{
				int num2 = builderTableConfiguration.PlotResourceLimits[j];
				if (num2 >= 0)
				{
					plotMaxResources[j] = num2;
				}
			}
		}
		int droppedPieceLimit = builderTableConfiguration.DroppedPieceLimit;
		if (droppedPieceLimit >= 0)
		{
			DROPPED_PIECE_LIMIT = droppedPieceLimit;
		}
		if (builderTableConfiguration.updateCountdownDate != null && !string.IsNullOrEmpty(builderTableConfiguration.updateCountdownDate))
		{
			try
			{
				DateTime.Parse(builderTableConfiguration.updateCountdownDate, CultureInfo.InvariantCulture);
				nextUpdateOverride = builderTableConfiguration.updateCountdownDate;
			}
			catch
			{
				nextUpdateOverride = string.Empty;
			}
		}
		else
		{
			nextUpdateOverride = string.Empty;
		}
		OnAvailableResourcesChange();
		OnTableConfigurationUpdated?.Invoke();
	}

	private void DumpTableConfig()
	{
		BuilderTableConfiguration builderTableConfiguration = new BuilderTableConfiguration();
		Array.Clear(builderTableConfiguration.TableResourceLimits, 0, builderTableConfiguration.TableResourceLimits.Length);
		Array.Clear(builderTableConfiguration.PlotResourceLimits, 0, builderTableConfiguration.PlotResourceLimits.Length);
		foreach (BuilderResourceQuantity quantity in totalResources.quantities)
		{
			if (quantity.type >= BuilderResourceType.Basic && (int)quantity.type < builderTableConfiguration.TableResourceLimits.Length)
			{
				builderTableConfiguration.TableResourceLimits[(int)quantity.type] = quantity.count;
			}
		}
		foreach (BuilderResourceQuantity quantity2 in resourcesPerPrivatePlot.quantities)
		{
			if (quantity2.type >= BuilderResourceType.Basic && (int)quantity2.type < builderTableConfiguration.PlotResourceLimits.Length)
			{
				builderTableConfiguration.PlotResourceLimits[(int)quantity2.type] = quantity2.count;
			}
		}
		builderTableConfiguration.DroppedPieceLimit = DROPPED_PIECE_LIMIT;
		builderTableConfiguration.updateCountdownDate = "1/10/2025 16:00:00";
		string text = JsonUtility.ToJson(builderTableConfiguration);
		Debug.Log("Configuration Dump \n" + text);
	}

	private string GetSaveDataTimeKey(int slot)
	{
		return personalBuildKey + slot.ToString("D2") + "Time";
	}

	private string GetSaveDataKey(int slot)
	{
		return personalBuildKey + slot.ToString("D2");
	}

	public void FindAndLoadSharedBlocksMap(string mapID)
	{
		SharedBlocksManager.instance.RequestMapDataFromID(mapID, FoundSharedBlocksMap);
	}

	public string GetSharedBlocksMapID()
	{
		if (sharedBlocksMap != null)
		{
			return sharedBlocksMap.MapID;
		}
		return string.Empty;
	}

	private void FoundSharedBlocksMap(SharedBlocksManager.SharedBlocksMap map)
	{
		if (NetworkSystem.Instance.IsMasterClient)
		{
			if (map == null || map.MapData.IsNullOrEmpty())
			{
				builderNetworking.LoadSharedBlocksFailedMaster((map == null) ? string.Empty : map.MapID);
				sharedBlocksMap = null;
				tableData = new BuilderTableData();
				ClearTable();
				ClearQueuedCommands();
				SetTableState(TableState.Ready);
			}
			else
			{
				sharedBlocksMap = map;
				SetTableState(TableState.WaitForInitialBuildMaster);
			}
		}
	}

	private void BuildInitialTableForPlayer()
	{
		if (!NetworkSystem.Instance.IsNull() && NetworkSystem.Instance.InRoom && NetworkSystem.Instance.SessionIsPrivate && NetworkSystem.Instance.GetLocalPlayer() != null && NetworkSystem.Instance.IsMasterClient)
		{
			if (!BuilderScanKiosk.IsSaveSlotValid(currentSaveSlot))
			{
				TryBuildingFromTitleData();
				return;
			}
			SharedBlocksManager.instance.OnFetchPrivateScanComplete += OnFetchPrivateScanComplete;
			SharedBlocksManager.instance.RequestFetchPrivateScan(currentSaveSlot);
		}
		else
		{
			TryBuildingFromTitleData();
		}
	}

	private void OnFetchPrivateScanComplete(int slot, bool success)
	{
		SharedBlocksManager.instance.OnFetchPrivateScanComplete -= OnFetchPrivateScanComplete;
		if (tableState != TableState.WaitForInitialBuildMaster)
		{
			return;
		}
		if (success && SharedBlocksManager.instance.TryGetPrivateScanResponse(slot, out var scanData))
		{
			if (!BuildTableFromJson(scanData, fromTitleData: false))
			{
				TryBuildingFromTitleData();
				return;
			}
			SetIsDirty(dirty: false);
			OnFinishedInitialTableBuild();
		}
		else
		{
			TryBuildingFromTitleData();
		}
	}

	private void BuildSelectedSharedMap()
	{
		if (!NetworkSystem.Instance.IsNull() && NetworkSystem.Instance.InRoom && NetworkSystem.Instance.IsMasterClient)
		{
			if (sharedBlocksMap != null && !sharedBlocksMap.MapData.IsNullOrEmpty())
			{
				TryBuildingSharedBlocksMap(sharedBlocksMap.MapData);
			}
			else if (SharedBlocksManager.IsMapIDValid(pendingMapID))
			{
				SharedBlocksManager.SharedBlocksMap map = new SharedBlocksManager.SharedBlocksMap
				{
					MapID = pendingMapID
				};
				LoadSharedMap(map);
			}
			else
			{
				FindStartingMap();
			}
		}
	}

	private void FindStartingMap()
	{
		if (hasStartingMap && Time.timeAsDouble < startingMapCacheTime + 60.0)
		{
			FoundDefaultSharedBlocksMap(success: true, startingMap);
		}
		else
		{
			if (getStartingMapInProgress)
			{
				return;
			}
			hasStartingMap = false;
			getStartingMapInProgress = true;
			if (startingMapConfig.useMapID && SharedBlocksManager.IsMapIDValid(startingMapConfig.mapID))
			{
				startingMap = new SharedBlocksManager.SharedBlocksMap
				{
					MapID = startingMapConfig.mapID
				};
				SharedBlocksManager.instance.RequestMapDataFromID(startingMapConfig.mapID, FoundTopMapData);
				return;
			}
			if (hasCachedTopMaps && Time.timeAsDouble <= lastGetTopMapsTime + 60.0)
			{
				ChooseMapFromList();
				return;
			}
			SharedBlocksManager.instance.OnGetPopularMapsComplete += FoundStartingMapList;
			if (!SharedBlocksManager.instance.RequestGetTopMaps(startingMapConfig.pageNumber, startingMapConfig.pageSize, startingMapConfig.sortMethod.ToString()))
			{
				FoundStartingMapList(success: false);
			}
		}
	}

	private void FoundStartingMapList(bool success)
	{
		SharedBlocksManager.instance.OnGetPopularMapsComplete -= FoundStartingMapList;
		if (success && SharedBlocksManager.instance.LatestPopularMaps.Count > 0)
		{
			startingMapList.Clear();
			startingMapList.AddRange(SharedBlocksManager.instance.LatestPopularMaps);
			hasCachedTopMaps = startingMapList.Count > 0;
			lastGetTopMapsTime = Time.time;
			ChooseMapFromList();
		}
		else
		{
			FoundDefaultSharedBlocksMap(success: false, null);
		}
	}

	private void ChooseMapFromList()
	{
		int index = UnityEngine.Random.Range(0, startingMapList.Count);
		startingMap = startingMapList[index];
		if (startingMap == null || !SharedBlocksManager.IsMapIDValid(startingMap.MapID))
		{
			FoundDefaultSharedBlocksMap(success: false, null);
		}
		else
		{
			SharedBlocksManager.instance.RequestMapDataFromID(startingMap.MapID, FoundTopMapData);
		}
	}

	private void FoundTopMapData(SharedBlocksManager.SharedBlocksMap map)
	{
		if (map == null || !SharedBlocksManager.IsMapIDValid(map.MapID) || map.MapID != startingMap.MapID)
		{
			FoundDefaultSharedBlocksMap(success: false, null);
			return;
		}
		hasStartingMap = true;
		startingMapCacheTime = Time.timeAsDouble;
		startingMap.MapData = map.MapData;
		FoundDefaultSharedBlocksMap(success: true, startingMap);
	}

	private void FoundDefaultSharedBlocksMap(bool success, SharedBlocksManager.SharedBlocksMap map)
	{
		getStartingMapInProgress = false;
		if (success && !map.MapData.IsNullOrEmpty())
		{
			startingMapCacheTime = Time.timeAsDouble;
			startingMap = map;
			hasStartingMap = true;
			sharedBlocksMap = map;
			TryBuildingSharedBlocksMap(sharedBlocksMap.MapData);
		}
		else
		{
			TryBuildingFromTitleData();
		}
	}

	private void TryBuildingSharedBlocksMap(string mapData)
	{
		if (tableState == TableState.WaitForInitialBuildMaster)
		{
			if (!BuildTableFromJson(mapData, fromTitleData: true))
			{
				GTDev.LogWarning("Unable to build shared blocks map");
				builderNetworking.LoadSharedBlocksFailedMaster(sharedBlocksMap.MapID);
				sharedBlocksMap = null;
				tableData = new BuilderTableData();
				ClearTable();
				ClearQueuedCommands();
				SetTableState(TableState.Ready);
			}
			else
			{
				StartCoroutine(CheckForNoBlocks());
			}
		}
	}

	private IEnumerator CheckForNoBlocks()
	{
		yield return null;
		if (!NoBlocksCheck())
		{
			GTDev.LogError("Failed No Blocks Check");
			builderNetworking.SharedBlocksOutOfBoundsMaster(sharedBlocksMap.MapID);
			sharedBlocksMap = null;
			tableData = new BuilderTableData();
			ClearTable();
			ClearQueuedCommands();
			SetTableState(TableState.Ready);
		}
		else
		{
			OnFinishedInitialTableBuild();
		}
	}

	private void TryBuildingFromTitleData()
	{
		SharedBlocksManager.instance.OnGetTitleDataBuildComplete += OnGetTitleDataBuildComplete;
		SharedBlocksManager.instance.FetchTitleDataBuild();
	}

	private void OnGetTitleDataBuildComplete(string titleDataBuild)
	{
		SharedBlocksManager.instance.OnGetTitleDataBuildComplete -= OnGetTitleDataBuildComplete;
		if (tableState != TableState.WaitForInitialBuildMaster)
		{
			return;
		}
		if (!titleDataBuild.IsNullOrEmpty())
		{
			if (!BuildTableFromJson(titleDataBuild, fromTitleData: true))
			{
				tableData = new BuilderTableData();
			}
		}
		else
		{
			tableData = new BuilderTableData();
		}
		OnFinishedInitialTableBuild();
	}

	public void SaveTableForPlayer(string busyStr, string blocksErrStr)
	{
		if (SharedBlocksManager.instance.IsWaitingOnRequest())
		{
			SetIsDirty(dirty: true);
			OnSaveFailure?.Invoke(busyStr);
			return;
		}
		saveInProgress = true;
		if (!BuilderScanKiosk.IsSaveSlotValid(currentSaveSlot))
		{
			saveInProgress = false;
			return;
		}
		if (!isDirty)
		{
			saveInProgress = false;
			OnSaveTimeUpdated?.Invoke();
			return;
		}
		if (!NoBlocksCheck())
		{
			saveInProgress = false;
			SetIsDirty(dirty: true);
			OnSaveFailure?.Invoke(blocksErrStr);
			return;
		}
		if (tableData == null)
		{
			tableData = new BuilderTableData();
		}
		SetIsDirty(dirty: false);
		tableData.numEdits++;
		string s = WriteTableToJson();
		s = Convert.ToBase64String(GZipStream.CompressString(s));
		SharedBlocksManager.instance.OnSavePrivateScanSuccess += OnSaveScanSuccess;
		SharedBlocksManager.instance.OnSavePrivateScanFailed += OnSaveScanFailure;
		SharedBlocksManager.instance.RequestSavePrivateScan(currentSaveSlot, s);
	}

	private void OnSaveScanSuccess(int scan)
	{
		SharedBlocksManager.instance.OnSavePrivateScanSuccess -= OnSaveScanSuccess;
		SharedBlocksManager.instance.OnSavePrivateScanFailed -= OnSaveScanFailure;
		saveInProgress = false;
		OnSaveSuccess?.Invoke();
	}

	private void OnSaveScanFailure(int scan, string message)
	{
		SharedBlocksManager.instance.OnSavePrivateScanSuccess -= OnSaveScanSuccess;
		SharedBlocksManager.instance.OnSavePrivateScanFailed -= OnSaveScanFailure;
		saveInProgress = false;
		SetIsDirty(dirty: true);
		OnSaveFailure?.Invoke(message);
	}

	private string WriteTableToJson()
	{
		tableData.Clear();
		tempDuplicateOverlaps.Clear();
		for (int i = 0; i < pieces.Count; i++)
		{
			if (pieces[i].state != BuilderPiece.State.AttachedAndPlaced)
			{
				continue;
			}
			tableData.pieceType.Add(pieces[i].overrideSavedPiece ? pieces[i].savedPieceType : pieces[i].pieceType);
			tableData.pieceId.Add(pieces[i].pieceId);
			tableData.parentId.Add((pieces[i].parentPiece == null) ? (-1) : pieces[i].parentPiece.pieceId);
			tableData.attachIndex.Add(pieces[i].attachIndex);
			tableData.parentAttachIndex.Add((pieces[i].parentPiece == null) ? (-1) : pieces[i].parentAttachIndex);
			tableData.placement.Add(pieces[i].GetPiecePlacement());
			tableData.materialType.Add(pieces[i].overrideSavedPiece ? pieces[i].savedMaterialType : pieces[i].materialType);
			BuilderMovingSnapPiece component = pieces[i].GetComponent<BuilderMovingSnapPiece>();
			int item = ((!(component == null)) ? component.GetTimeOffset() : 0);
			tableData.timeOffset.Add(item);
			for (int j = 0; j < pieces[i].gridPlanes.Count; j++)
			{
				if (pieces[i].gridPlanes[j] == null)
				{
					continue;
				}
				for (SnapOverlap snapOverlap = pieces[i].gridPlanes[j].firstOverlap; snapOverlap != null; snapOverlap = snapOverlap.nextOverlap)
				{
					if (snapOverlap.otherPlane.piece.state == BuilderPiece.State.AttachedAndPlaced || snapOverlap.otherPlane.piece.isBuiltIntoTable)
					{
						SnapOverlapKey item2 = BuildOverlapKey(pieces[i].pieceId, snapOverlap.otherPlane.piece.pieceId, j, snapOverlap.otherPlane.attachIndex);
						if (!tempDuplicateOverlaps.Contains(item2))
						{
							tempDuplicateOverlaps.Add(item2);
							long item3 = PackSnapInfo(j, snapOverlap.otherPlane.attachIndex, snapOverlap.bounds.min, snapOverlap.bounds.max);
							tableData.overlapingPieces.Add(pieces[i].pieceId);
							tableData.overlappedPieces.Add(snapOverlap.otherPlane.piece.pieceId);
							tableData.overlapInfo.Add(item3);
						}
					}
				}
			}
		}
		foreach (BuilderPiece basePiece in basePieces)
		{
			if (basePiece == null)
			{
				continue;
			}
			for (int k = 0; k < basePiece.gridPlanes.Count; k++)
			{
				if (basePiece.gridPlanes[k] == null)
				{
					continue;
				}
				for (SnapOverlap snapOverlap2 = basePiece.gridPlanes[k].firstOverlap; snapOverlap2 != null; snapOverlap2 = snapOverlap2.nextOverlap)
				{
					if (snapOverlap2.otherPlane.piece.state == BuilderPiece.State.AttachedAndPlaced || snapOverlap2.otherPlane.piece.isBuiltIntoTable)
					{
						SnapOverlapKey item4 = BuildOverlapKey(basePiece.pieceId, snapOverlap2.otherPlane.piece.pieceId, k, snapOverlap2.otherPlane.attachIndex);
						if (!tempDuplicateOverlaps.Contains(item4))
						{
							tempDuplicateOverlaps.Add(item4);
							long item5 = PackSnapInfo(k, snapOverlap2.otherPlane.attachIndex, snapOverlap2.bounds.min, snapOverlap2.bounds.max);
							tableData.overlapingPieces.Add(basePiece.pieceId);
							tableData.overlappedPieces.Add(snapOverlap2.otherPlane.piece.pieceId);
							tableData.overlapInfo.Add(item5);
						}
					}
				}
			}
		}
		tempDuplicateOverlaps.Clear();
		tableData.numPieces = tableData.pieceType.Count;
		return JsonUtility.ToJson(tableData);
	}

	private static SnapOverlapKey BuildOverlapKey(int pieceId, int otherPieceId, int attachGridIndex, int otherAttachGridIndex)
	{
		SnapOverlapKey result = new SnapOverlapKey
		{
			piece = pieceId
		};
		result.piece <<= 32;
		result.piece |= attachGridIndex;
		result.otherPiece = otherPieceId;
		result.otherPiece <<= 32;
		result.otherPiece |= otherAttachGridIndex;
		return result;
	}

	private bool BuildTableFromJson(string tableJson, bool fromTitleData)
	{
		if (string.IsNullOrEmpty(tableJson))
		{
			return false;
		}
		tableData = null;
		try
		{
			tableData = JsonUtility.FromJson<BuilderTableData>(tableJson);
		}
		catch
		{
		}
		try
		{
			if (tableData == null)
			{
				tableJson = GZipStream.UncompressString(Convert.FromBase64String(tableJson));
				tableData = JsonUtility.FromJson<BuilderTableData>(tableJson);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.ToString());
			return false;
		}
		if (tableData == null)
		{
			return false;
		}
		if (tableData.version < 4)
		{
			return false;
		}
		int num = ((tableData.pieceType != null) ? tableData.pieceType.Count : 0);
		if (num == 0)
		{
			OnDeserializeUpdatePlots();
			return true;
		}
		if (tableData.pieceId == null || tableData.pieceId.Count != num || tableData.placement == null || tableData.placement.Count != num)
		{
			GTDev.LogError("BuildTableFromJson Piece Count Mismatch");
			return false;
		}
		if (num >= maxResources[0])
		{
			GTDev.LogError($"BuildTableFromJson Failed sanity piece count check {num}");
			return false;
		}
		Dictionary<int, int> dictionary = new Dictionary<int, int>(num);
		bool flag = tableData.timeOffset != null && tableData.timeOffset.Count > 0;
		if (flag && tableData.timeOffset.Count != num)
		{
			GTDev.LogError("BuildTableFromJson Piece Count Mismatch (Time Offsets)");
			return false;
		}
		for (int i = 0; i < tableData.pieceType.Count; i++)
		{
			int num2 = CreatePieceId();
			if (!dictionary.TryAdd(tableData.pieceId[i], num2))
			{
				GTDev.LogError("BuildTableFromJson Piece id duplicate in save");
				ClearTable();
				return false;
			}
			int num3 = ((tableData.materialType != null && tableData.materialType.Count > i) ? tableData.materialType[i] : (-1));
			int newPieceType = tableData.pieceType[i];
			int num4 = num3;
			bool flag2 = true;
			BuilderPiece piecePrefab = GetPiecePrefab(tableData.pieceType[i]);
			if (piecePrefab == null)
			{
				ClearTable();
				return false;
			}
			if (!fromTitleData)
			{
				if (num4 == -1 && piecePrefab.materialOptions != null)
				{
					piecePrefab.materialOptions.GetDefaultMaterial(out var materialType, out var _, out var _);
					num4 = materialType;
				}
				flag2 = BuilderSetManager.instance.IsPieceOwnedLocally(tableData.pieceType[i], num4);
				if (!fromTitleData && !flag2)
				{
					if (!piecePrefab.fallbackInfo.materialSwapThisPrefab)
					{
						if (piecePrefab.fallbackInfo.prefab == null)
						{
							continue;
						}
						newPieceType = piecePrefab.fallbackInfo.prefab.name.GetStaticHash();
					}
					num4 = -1;
				}
			}
			if (piecePrefab.cost != null && piecePrefab.cost.quantities != null)
			{
				for (int j = 0; j < piecePrefab.cost.quantities.Count; j++)
				{
					BuilderResourceQuantity quantity = piecePrefab.cost.quantities[j];
					if (!HasEnoughResource(quantity))
					{
						if (quantity.type == BuilderResourceType.Basic)
						{
							ClearTable();
							GTDev.LogError("BuildTableFromJson saved table uses too many basic resource");
							return false;
						}
						GTDev.LogWarning("BuildTableFromJson saved table uses too many functional or decorative resource");
					}
				}
			}
			int num5 = (flag ? tableData.timeOffset[i] : 0);
			BuilderPiece builderPiece = CreatePieceInternal(newPieceType, num2, Vector3.zero, Quaternion.identity, BuilderPiece.State.AttachedAndPlaced, num4, NetworkSystem.Instance.ServerTimestamp - num5, this);
			if (builderPiece == null)
			{
				ClearTable();
				GTDev.LogError($"Piece Type {tableData.pieceType[i]} is not defined");
				return false;
			}
			if (!fromTitleData && !flag2)
			{
				builderPiece.overrideSavedPiece = true;
				builderPiece.savedPieceType = tableData.pieceType[i];
				builderPiece.savedMaterialType = num3;
			}
		}
		for (int k = 0; k < tableData.pieceType.Count; k++)
		{
			int parentAttachIndex = ((tableData.parentAttachIndex == null || tableData.parentAttachIndex.Count <= k) ? (-1) : tableData.parentAttachIndex[k]);
			int attachIndex = ((tableData.attachIndex == null || tableData.attachIndex.Count <= k) ? (-1) : tableData.attachIndex[k]);
			int valueOrDefault = dictionary.GetValueOrDefault(tableData.pieceId[k], -1);
			int parentId = -1;
			if (dictionary.TryGetValue(tableData.parentId[k], out var value))
			{
				parentId = value;
			}
			else if (tableData.parentId[k] < 10000 && tableData.parentId[k] >= 5)
			{
				parentId = tableData.parentId[k];
			}
			AttachPieceInternal(valueOrDefault, attachIndex, parentId, parentAttachIndex, tableData.placement[k]);
		}
		foreach (BuilderPiece piece3 in pieces)
		{
			if (piece3.state == BuilderPiece.State.AttachedAndPlaced)
			{
				piece3.OnPlacementDeserialized();
			}
		}
		OnDeserializeUpdatePlots();
		tempDuplicateOverlaps.Clear();
		if (tableData.overlapingPieces != null)
		{
			for (int l = 0; l < tableData.overlapingPieces.Count && l < tableData.overlappedPieces.Count && l < tableData.overlapInfo.Count; l++)
			{
				int num6 = -1;
				if (dictionary.TryGetValue(tableData.overlapingPieces[l], out var value2))
				{
					num6 = value2;
				}
				else if (tableData.overlapingPieces[l] < 10000 && tableData.overlapingPieces[l] >= 5)
				{
					num6 = tableData.overlapingPieces[l];
				}
				int num7 = -1;
				if (dictionary.TryGetValue(tableData.overlappedPieces[l], out var value3))
				{
					num7 = value3;
				}
				else if (tableData.overlappedPieces[l] < 10000 && tableData.overlappedPieces[l] >= 5)
				{
					num7 = tableData.overlappedPieces[l];
				}
				if (num6 == -1 || num7 == -1)
				{
					continue;
				}
				long packed = tableData.overlapInfo[l];
				BuilderPiece piece = GetPiece(num6);
				if (piece == null)
				{
					continue;
				}
				BuilderPiece piece2 = GetPiece(num7);
				if (piece2 == null)
				{
					continue;
				}
				UnpackSnapInfo(packed, out var attachGridIndex, out var otherAttachGridIndex, out var min, out var max);
				if (attachGridIndex >= 0 && attachGridIndex < piece.gridPlanes.Count && otherAttachGridIndex >= 0 && otherAttachGridIndex < piece2.gridPlanes.Count)
				{
					SnapOverlapKey item = BuildOverlapKey(num6, num7, attachGridIndex, otherAttachGridIndex);
					if (!tempDuplicateOverlaps.Contains(item))
					{
						tempDuplicateOverlaps.Add(item);
						piece.gridPlanes[attachGridIndex].AddSnapOverlap(builderPool.CreateSnapOverlap(piece2.gridPlanes[otherAttachGridIndex], new SnapBounds(min, max)));
					}
				}
			}
		}
		tempDuplicateOverlaps.Clear();
		return true;
	}

	public int SerializeTableState(byte[] bytes, int maxBytes)
	{
		MemoryStream memoryStream = new MemoryStream(bytes);
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		if (conveyors == null)
		{
			binaryWriter.Write(0);
		}
		else
		{
			binaryWriter.Write(conveyors.Count);
			foreach (BuilderConveyor conveyor in conveyors)
			{
				int selectedDisplayGroupID = conveyor.GetSelectedDisplayGroupID();
				binaryWriter.Write(selectedDisplayGroupID);
			}
		}
		if (dispenserShelves == null)
		{
			binaryWriter.Write(0);
		}
		else
		{
			binaryWriter.Write(dispenserShelves.Count);
			foreach (BuilderDispenserShelf dispenserShelf in dispenserShelves)
			{
				int selectedDisplayGroupID2 = dispenserShelf.GetSelectedDisplayGroupID();
				binaryWriter.Write(selectedDisplayGroupID2);
			}
		}
		childPieces.Clear();
		rootPieces.Clear();
		childPieces.EnsureCapacity(pieces.Count);
		rootPieces.EnsureCapacity(pieces.Count);
		foreach (BuilderPiece piece in pieces)
		{
			if (piece.parentPiece == null)
			{
				rootPieces.Add(piece);
			}
			else
			{
				childPieces.Add(piece);
			}
		}
		binaryWriter.Write(rootPieces.Count);
		for (int i = 0; i < rootPieces.Count; i++)
		{
			BuilderPiece builderPiece = rootPieces[i];
			binaryWriter.Write(builderPiece.pieceType);
			binaryWriter.Write(builderPiece.pieceId);
			binaryWriter.Write((byte)builderPiece.state);
			if (builderPiece.state == BuilderPiece.State.OnConveyor || builderPiece.state == BuilderPiece.State.OnShelf || builderPiece.state == BuilderPiece.State.Displayed)
			{
				binaryWriter.Write(builderPiece.shelfOwner);
			}
			else
			{
				binaryWriter.Write(builderPiece.heldByPlayerActorNumber);
			}
			binaryWriter.Write((byte)(builderPiece.heldInLeftHand ? 1u : 0u));
			binaryWriter.Write(builderPiece.materialType);
			long value = BitPackUtils.PackWorldPosForNetwork(builderPiece.transform.localPosition);
			int value2 = BitPackUtils.PackQuaternionForNetwork(builderPiece.transform.localRotation);
			binaryWriter.Write(value);
			binaryWriter.Write(value2);
			if (builderPiece.state == BuilderPiece.State.AttachedAndPlaced)
			{
				binaryWriter.Write(builderPiece.functionalPieceState);
				binaryWriter.Write(builderPiece.activatedTimeStamp);
			}
			if (builderPiece.state == BuilderPiece.State.OnConveyor)
			{
				binaryWriter.Write((!(conveyorManager == null)) ? conveyorManager.GetPieceCreateTimestamp(builderPiece) : 0);
			}
		}
		binaryWriter.Write(childPieces.Count);
		for (int j = 0; j < childPieces.Count; j++)
		{
			BuilderPiece builderPiece2 = childPieces[j];
			binaryWriter.Write(builderPiece2.pieceType);
			binaryWriter.Write(builderPiece2.pieceId);
			int value3 = ((builderPiece2.parentPiece == null) ? (-1) : builderPiece2.parentPiece.pieceId);
			binaryWriter.Write(value3);
			binaryWriter.Write(builderPiece2.attachIndex);
			binaryWriter.Write(builderPiece2.parentAttachIndex);
			binaryWriter.Write((byte)builderPiece2.state);
			if (builderPiece2.state == BuilderPiece.State.OnConveyor || builderPiece2.state == BuilderPiece.State.OnShelf || builderPiece2.state == BuilderPiece.State.Displayed)
			{
				binaryWriter.Write(builderPiece2.shelfOwner);
			}
			else
			{
				binaryWriter.Write(builderPiece2.heldByPlayerActorNumber);
			}
			binaryWriter.Write((byte)(builderPiece2.heldInLeftHand ? 1u : 0u));
			binaryWriter.Write(builderPiece2.materialType);
			int piecePlacement = builderPiece2.GetPiecePlacement();
			binaryWriter.Write(piecePlacement);
			if (builderPiece2.state == BuilderPiece.State.AttachedAndPlaced)
			{
				binaryWriter.Write(builderPiece2.functionalPieceState);
				binaryWriter.Write(builderPiece2.activatedTimeStamp);
			}
			if (builderPiece2.state == BuilderPiece.State.OnConveyor)
			{
				binaryWriter.Write((!(conveyorManager == null)) ? conveyorManager.GetPieceCreateTimestamp(builderPiece2) : 0);
			}
		}
		if (isTableMutable)
		{
			binaryWriter.Write(plotOwners.Count);
			foreach (KeyValuePair<int, int> plotOwner in plotOwners)
			{
				binaryWriter.Write(plotOwner.Key);
				binaryWriter.Write(plotOwner.Value);
			}
		}
		else
		{
			if (sharedBlocksMap == null || sharedBlocksMap.MapID == null || !SharedBlocksManager.IsMapIDValid(sharedBlocksMap.MapID))
			{
				for (int k = 0; k < mapIDBuffer.Length; k++)
				{
					mapIDBuffer[k] = 'a';
				}
			}
			else
			{
				for (int l = 0; l < mapIDBuffer.Length; l++)
				{
					mapIDBuffer[l] = sharedBlocksMap.MapID[l];
				}
			}
			binaryWriter.Write(mapIDBuffer);
		}
		_ = memoryStream.Position;
		overlapPieces.Clear();
		overlapOtherPieces.Clear();
		overlapPacked.Clear();
		tempDuplicateOverlaps.Clear();
		foreach (BuilderPiece piece2 in pieces)
		{
			if (piece2 == null)
			{
				continue;
			}
			for (int m = 0; m < piece2.gridPlanes.Count; m++)
			{
				if (piece2.gridPlanes[m] == null)
				{
					continue;
				}
				for (SnapOverlap snapOverlap = piece2.gridPlanes[m].firstOverlap; snapOverlap != null; snapOverlap = snapOverlap.nextOverlap)
				{
					SnapOverlapKey item = BuildOverlapKey(piece2.pieceId, snapOverlap.otherPlane.piece.pieceId, m, snapOverlap.otherPlane.attachIndex);
					if (!tempDuplicateOverlaps.Contains(item))
					{
						tempDuplicateOverlaps.Add(item);
						long item2 = PackSnapInfo(m, snapOverlap.otherPlane.attachIndex, snapOverlap.bounds.min, snapOverlap.bounds.max);
						overlapPieces.Add(piece2.pieceId);
						overlapOtherPieces.Add(snapOverlap.otherPlane.piece.pieceId);
						overlapPacked.Add(item2);
					}
				}
			}
		}
		foreach (BuilderPiece basePiece in basePieces)
		{
			if (basePiece == null)
			{
				continue;
			}
			for (int n = 0; n < basePiece.gridPlanes.Count; n++)
			{
				if (basePiece.gridPlanes[n] == null)
				{
					continue;
				}
				for (SnapOverlap snapOverlap2 = basePiece.gridPlanes[n].firstOverlap; snapOverlap2 != null; snapOverlap2 = snapOverlap2.nextOverlap)
				{
					SnapOverlapKey item3 = BuildOverlapKey(basePiece.pieceId, snapOverlap2.otherPlane.piece.pieceId, n, snapOverlap2.otherPlane.attachIndex);
					if (!tempDuplicateOverlaps.Contains(item3))
					{
						tempDuplicateOverlaps.Add(item3);
						long item4 = PackSnapInfo(n, snapOverlap2.otherPlane.attachIndex, snapOverlap2.bounds.min, snapOverlap2.bounds.max);
						overlapPieces.Add(basePiece.pieceId);
						overlapOtherPieces.Add(snapOverlap2.otherPlane.piece.pieceId);
						overlapPacked.Add(item4);
					}
				}
			}
		}
		tempDuplicateOverlaps.Clear();
		binaryWriter.Write(overlapPieces.Count);
		for (int num = 0; num < overlapPieces.Count; num++)
		{
			binaryWriter.Write(overlapPieces[num]);
			binaryWriter.Write(overlapOtherPieces[num]);
			binaryWriter.Write(overlapPacked[num]);
		}
		return (int)memoryStream.Position;
	}

	public void DeserializeTableState(byte[] bytes, int numBytes)
	{
		if (numBytes <= 0)
		{
			return;
		}
		VRRigCache.Instance.localRig.SpeakerHead.transform.GetPositionAndRotation(out var position, out var rotation);
		bool flag = ValidatePieceWorldTransform(position, rotation);
		int actorNumber = NetworkSystem.Instance.LocalPlayer.ActorNumber;
		BinaryReader binaryReader = new BinaryReader(new MemoryStream(bytes));
		tempPeiceIds.Clear();
		tempParentPeiceIds.Clear();
		tempAttachIndexes.Clear();
		tempParentAttachIndexes.Clear();
		tempParentActorNumbers.Clear();
		tempInLeftHand.Clear();
		tempPiecePlacement.Clear();
		int num = binaryReader.ReadInt32();
		bool flag2 = conveyors != null;
		for (int i = 0; i < num; i++)
		{
			int selection = binaryReader.ReadInt32();
			if (flag2 && i < conveyors.Count)
			{
				conveyors[i].SetSelection(selection);
			}
		}
		int num2 = binaryReader.ReadInt32();
		bool flag3 = dispenserShelves != null;
		for (int j = 0; j < num2; j++)
		{
			int selection2 = binaryReader.ReadInt32();
			if (flag3 && j < dispenserShelves.Count)
			{
				dispenserShelves[j].SetSelection(selection2);
			}
		}
		int num3 = binaryReader.ReadInt32();
		for (int k = 0; k < num3; k++)
		{
			int newPieceType = binaryReader.ReadInt32();
			int num4 = binaryReader.ReadInt32();
			BuilderPiece.State state = (BuilderPiece.State)binaryReader.ReadByte();
			int num5 = binaryReader.ReadInt32();
			bool item = binaryReader.ReadByte() != 0;
			int materialType = binaryReader.ReadInt32();
			long data = binaryReader.ReadInt64();
			int data2 = binaryReader.ReadInt32();
			Vector3 v = BitPackUtils.UnpackWorldPosFromNetwork(data);
			Quaternion q = BitPackUtils.UnpackQuaternionFromNetwork(data2);
			byte fState = (byte)((state == BuilderPiece.State.AttachedAndPlaced) ? binaryReader.ReadByte() : 0);
			int activateTimeStamp = ((state == BuilderPiece.State.AttachedAndPlaced) ? binaryReader.ReadInt32() : 0);
			int num6 = ((state == BuilderPiece.State.OnConveyor) ? binaryReader.ReadInt32() : 0);
			if (!v.IsValid(10000f) || !q.IsValid() || !ValidateCreatePieceParams(newPieceType, num4, state, materialType))
			{
				SetTableState(TableState.BadData);
				return;
			}
			int num7 = -1;
			if (state == BuilderPiece.State.OnConveyor || state == BuilderPiece.State.OnShelf || state == BuilderPiece.State.Displayed)
			{
				num7 = num5;
				num5 = -1;
			}
			if ((num5 == actorNumber && !flag) || !ValidateDeserializedRootPieceState(num4, state, num7, num5, v, q))
			{
				continue;
			}
			BuilderPiece builderPiece = CreatePieceInternal(newPieceType, num4, v, q, state, materialType, activateTimeStamp, this);
			tempPeiceIds.Add(num4);
			tempParentActorNumbers.Add(num5);
			tempInLeftHand.Add(item);
			builderPiece.SetFunctionalPieceState(fState, NetPlayer.Get(PhotonNetwork.MasterClient), PhotonNetwork.ServerTimestamp);
			if (num7 < 0 || !isTableMutable)
			{
				continue;
			}
			builderPiece.shelfOwner = num7;
			switch (state)
			{
			case BuilderPiece.State.OnConveyor:
			{
				BuilderConveyor builderConveyor = conveyors[num7];
				float timeOffset = 0f;
				if ((uint)PhotonNetwork.ServerTimestamp > (uint)num6)
				{
					timeOffset = (float)(uint)(PhotonNetwork.ServerTimestamp - num6) / 1000f;
				}
				builderConveyor.OnShelfPieceCreated(builderPiece, timeOffset);
				break;
			}
			case BuilderPiece.State.OnShelf:
			case BuilderPiece.State.Displayed:
				dispenserShelves[num7].OnShelfPieceCreated(builderPiece, playfx: false);
				break;
			}
		}
		for (int l = 0; l < tempPeiceIds.Count; l++)
		{
			if (tempParentActorNumbers[l] >= 0)
			{
				AttachPieceToActorInternal(tempPeiceIds[l], tempParentActorNumbers[l], tempInLeftHand[l]);
			}
		}
		tempPeiceIds.Clear();
		tempParentActorNumbers.Clear();
		tempInLeftHand.Clear();
		int num8 = binaryReader.ReadInt32();
		for (int m = 0; m < num8; m++)
		{
			int newPieceType2 = binaryReader.ReadInt32();
			int num9 = binaryReader.ReadInt32();
			int item2 = binaryReader.ReadInt32();
			int item3 = binaryReader.ReadInt32();
			int item4 = binaryReader.ReadInt32();
			BuilderPiece.State state2 = (BuilderPiece.State)binaryReader.ReadByte();
			int num10 = binaryReader.ReadInt32();
			bool item5 = binaryReader.ReadByte() != 0;
			int materialType2 = binaryReader.ReadInt32();
			int item6 = binaryReader.ReadInt32();
			byte fState2 = (byte)((state2 == BuilderPiece.State.AttachedAndPlaced) ? binaryReader.ReadByte() : 0);
			int activateTimeStamp2 = ((state2 == BuilderPiece.State.AttachedAndPlaced) ? binaryReader.ReadInt32() : 0);
			int num11 = ((state2 == BuilderPiece.State.OnConveyor) ? binaryReader.ReadInt32() : 0);
			if (!ValidateCreatePieceParams(newPieceType2, num9, state2, materialType2))
			{
				SetTableState(TableState.BadData);
				return;
			}
			int num12 = -1;
			if (state2 == BuilderPiece.State.OnConveyor || state2 == BuilderPiece.State.OnShelf || state2 == BuilderPiece.State.Displayed)
			{
				num12 = num10;
				num10 = -1;
			}
			if ((num10 == actorNumber && !flag) || !ValidateDeserializedChildPieceState(num9, state2))
			{
				continue;
			}
			BuilderPiece builderPiece2 = CreatePieceInternal(newPieceType2, num9, roomCenter.position, Quaternion.identity, state2, materialType2, activateTimeStamp2, this);
			builderPiece2.SetFunctionalPieceState(fState2, NetPlayer.Get(PhotonNetwork.MasterClient), PhotonNetwork.ServerTimestamp);
			tempPeiceIds.Add(num9);
			tempParentPeiceIds.Add(item2);
			tempAttachIndexes.Add(item3);
			tempParentAttachIndexes.Add(item4);
			tempParentActorNumbers.Add(num10);
			tempInLeftHand.Add(item5);
			tempPiecePlacement.Add(item6);
			if (num12 < 0 || !isTableMutable)
			{
				continue;
			}
			builderPiece2.shelfOwner = num12;
			switch (state2)
			{
			case BuilderPiece.State.OnConveyor:
			{
				BuilderConveyor builderConveyor2 = conveyors[num12];
				float timeOffset2 = 0f;
				if ((uint)PhotonNetwork.ServerTimestamp > (uint)num11)
				{
					timeOffset2 = (float)(uint)(PhotonNetwork.ServerTimestamp - num11) / 1000f;
				}
				builderConveyor2.OnShelfPieceCreated(builderPiece2, timeOffset2);
				break;
			}
			case BuilderPiece.State.OnShelf:
			case BuilderPiece.State.Displayed:
				dispenserShelves[num12].OnShelfPieceCreated(builderPiece2, playfx: false);
				break;
			}
		}
		for (int n = 0; n < tempPeiceIds.Count; n++)
		{
			if (!ValidateAttachPieceParams(tempPeiceIds[n], tempAttachIndexes[n], tempParentPeiceIds[n], tempParentAttachIndexes[n], tempPiecePlacement[n]))
			{
				RecyclePieceInternal(tempPeiceIds[n], ignoreHaptics: true, playFX: false, -1);
			}
			else
			{
				AttachPieceInternal(tempPeiceIds[n], tempAttachIndexes[n], tempParentPeiceIds[n], tempParentAttachIndexes[n], tempPiecePlacement[n]);
			}
		}
		for (int num13 = 0; num13 < tempPeiceIds.Count; num13++)
		{
			if (tempParentActorNumbers[num13] >= 0)
			{
				AttachPieceToActorInternal(tempPeiceIds[num13], tempParentActorNumbers[num13], tempInLeftHand[num13]);
			}
		}
		foreach (BuilderPiece piece3 in pieces)
		{
			if (piece3.state == BuilderPiece.State.AttachedAndPlaced)
			{
				piece3.OnPlacementDeserialized();
			}
		}
		if (isTableMutable)
		{
			plotOwners.Clear();
			doesLocalPlayerOwnPlot = false;
			int num14 = binaryReader.ReadInt32();
			for (int num15 = 0; num15 < num14; num15++)
			{
				int num16 = binaryReader.ReadInt32();
				int num17 = binaryReader.ReadInt32();
				if (plotOwners.TryAdd(num16, num17) && GetPiece(num17).TryGetPlotComponent(out var plot))
				{
					plot.ClaimPlotForPlayerNumber(num16);
					if (num16 == PhotonNetwork.LocalPlayer.ActorNumber)
					{
						doesLocalPlayerOwnPlot = true;
					}
				}
			}
			OnLocalPlayerClaimedPlot?.Invoke(doesLocalPlayerOwnPlot);
			OnDeserializeUpdatePlots();
		}
		else
		{
			mapIDBuffer = binaryReader.ReadChars(mapIDBuffer.Length);
			string mapID = new string(mapIDBuffer);
			if (SharedBlocksManager.IsMapIDValid(mapID))
			{
				sharedBlocksMap = new SharedBlocksManager.SharedBlocksMap
				{
					MapID = mapID
				};
			}
		}
		tempDuplicateOverlaps.Clear();
		int num18 = binaryReader.ReadInt32();
		for (int num19 = 0; num19 < num18; num19++)
		{
			int pieceId = binaryReader.ReadInt32();
			int num20 = binaryReader.ReadInt32();
			long packed = binaryReader.ReadInt64();
			BuilderPiece piece = GetPiece(pieceId);
			if (piece == null)
			{
				continue;
			}
			BuilderPiece piece2 = GetPiece(num20);
			if (piece2 == null)
			{
				continue;
			}
			UnpackSnapInfo(packed, out var attachGridIndex, out var otherAttachGridIndex, out var min, out var max);
			if (attachGridIndex >= 0 && attachGridIndex < piece.gridPlanes.Count && otherAttachGridIndex >= 0 && otherAttachGridIndex < piece2.gridPlanes.Count)
			{
				SnapOverlapKey item7 = BuildOverlapKey(pieceId, num20, attachGridIndex, otherAttachGridIndex);
				if (!tempDuplicateOverlaps.Contains(item7))
				{
					tempDuplicateOverlaps.Add(item7);
					piece.gridPlanes[attachGridIndex].AddSnapOverlap(builderPool.CreateSnapOverlap(piece2.gridPlanes[otherAttachGridIndex], new SnapBounds(min, max)));
				}
			}
		}
		tempDuplicateOverlaps.Clear();
	}
}
