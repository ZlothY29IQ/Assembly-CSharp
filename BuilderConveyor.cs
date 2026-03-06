using System.Collections.Generic;
using GorillaTagScripts;
using Photon.Pun;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class BuilderConveyor : MonoBehaviour
{
	[Header("Set Selection")]
	[SerializeField]
	private BuilderSetSelector setSelector;

	public List<BuilderPieceSet.BuilderPieceCategory> _includeCategories;

	[HideInInspector]
	public BuilderTable table;

	public int shelfID = -1;

	[Header("Conveyor Properties")]
	[SerializeField]
	private Transform spawnTransform;

	[SerializeField]
	private SplineContainer spline;

	private float conveyorMoveSpeed = 0.2f;

	private float spawnDelay = 1.5f;

	private double nextSpawnTime;

	private int nextPieceToSpawn;

	private BuilderPieceSet.BuilderDisplayGroup currentDisplayGroup;

	private int loopCount;

	private List<BuilderPieceSet.PieceInfo> piecesInSet = new List<BuilderPieceSet.PieceInfo>(10);

	private Queue<int> grabbedPieceTypes;

	private Queue<int> grabbedPieceMaterials;

	private List<BuilderPiece> piecesOnConveyor = new List<BuilderPiece>(10);

	private Vector3 moveDirection;

	private bool waitForResourceChange;

	private bool initialized;

	private float splineLength = 1f;

	private int maxItemsOnSpline;

	private UnityEngine.Splines.BezierCurve _evaluateCurve;

	public NativeSpline nativeSpline;

	private bool shouldVerifySetSelection;

	private void Start()
	{
		InitIfNeeded();
	}

	public void Setup()
	{
		InitIfNeeded();
	}

	private void InitIfNeeded()
	{
		if (initialized)
		{
			return;
		}
		nextPieceToSpawn = 0;
		grabbedPieceTypes = new Queue<int>(10);
		grabbedPieceMaterials = new Queue<int>(10);
		setSelector.Setup(_includeCategories);
		currentDisplayGroup = setSelector.GetSelectedGroup();
		piecesInSet.Clear();
		foreach (BuilderPieceSet.BuilderPieceSubset pieceSubset in currentDisplayGroup.pieceSubsets)
		{
			if (_includeCategories.Contains(pieceSubset.pieceCategory))
			{
				piecesInSet.AddRange(pieceSubset.pieceInfos);
			}
		}
		double timeAsDouble = Time.timeAsDouble;
		nextSpawnTime = timeAsDouble + (double)spawnDelay;
		setSelector.OnSelectedGroup.AddListener(OnSelectedSetChange);
		initialized = true;
		splineLength = spline.Splines[0].GetLength();
		maxItemsOnSpline = Mathf.RoundToInt(splineLength / (conveyorMoveSpeed * spawnDelay)) + 5;
		nativeSpline = new NativeSpline(spline.Splines[0], spline.transform.localToWorldMatrix, Allocator.Persistent);
	}

	public int GetMaxItemsOnConveyor()
	{
		return Mathf.RoundToInt(splineLength / (conveyorMoveSpeed * spawnDelay)) + 5;
	}

	public float GetFrameMovement()
	{
		return conveyorMoveSpeed / splineLength;
	}

	private void OnDestroy()
	{
		if (setSelector != null)
		{
			setSelector.OnSelectedGroup.RemoveListener(OnSelectedSetChange);
		}
		nativeSpline.Dispose();
	}

	public void OnSelectedSetChange(int displayGroupID)
	{
		if (table.GetTableState() == BuilderTable.TableState.Ready)
		{
			table.RequestShelfSelection(shelfID, displayGroupID, isConveyor: true);
		}
	}

	public void SetSelection(int displayGroupID)
	{
		setSelector.SetSelection(displayGroupID);
		currentDisplayGroup = setSelector.GetSelectedGroup();
		piecesInSet.Clear();
		foreach (BuilderPieceSet.BuilderPieceSubset pieceSubset in currentDisplayGroup.pieceSubsets)
		{
			if (_includeCategories.Contains(pieceSubset.pieceCategory))
			{
				piecesInSet.AddRange(pieceSubset.pieceInfos);
			}
		}
		nextPieceToSpawn = 0;
		loopCount = 0;
	}

	public int GetSelectedDisplayGroupID()
	{
		return setSelector.GetSelectedGroup().GetDisplayGroupIdentifier();
	}

	public void UpdateConveyor()
	{
		if (!initialized)
		{
			Setup();
		}
		for (int num = piecesOnConveyor.Count - 1; num >= 0; num--)
		{
			BuilderPiece builderPiece = piecesOnConveyor[num];
			if (builderPiece.state != BuilderPiece.State.OnConveyor)
			{
				if (PhotonNetwork.LocalPlayer.IsMasterClient && builderPiece.state != BuilderPiece.State.None)
				{
					grabbedPieceTypes.Enqueue(builderPiece.pieceType);
					grabbedPieceMaterials.Enqueue(builderPiece.materialType);
				}
				builderPiece.shelfOwner = -1;
				piecesOnConveyor.RemoveAt(num);
				table.conveyorManager.RemovePieceFromJob(builderPiece);
			}
		}
	}

	public void RemovePieceFromConveyor(Transform pieceTransform)
	{
		foreach (BuilderPiece item in piecesOnConveyor)
		{
			if (item.transform == pieceTransform)
			{
				piecesOnConveyor.Remove(item);
				item.shelfOwner = -1;
				table.RequestRecyclePiece(item, playFX: false, -1);
				break;
			}
		}
	}

	private Vector3 EvaluateSpline(float t)
	{
		_evaluateCurve = nativeSpline.GetCurve(nativeSpline.SplineToCurveT(t, out var curveT));
		return CurveUtility.EvaluatePosition(_evaluateCurve, curveT);
	}

	public void UpdateShelfSliced()
	{
		if (!PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			return;
		}
		if (shouldVerifySetSelection)
		{
			BuilderPieceSet.BuilderDisplayGroup selectedGroup = setSelector.GetSelectedGroup();
			if (selectedGroup == null || !BuilderSetManager.instance.DoesAnyPlayerInRoomOwnPieceSet(selectedGroup.setID))
			{
				int defaultGroupID = setSelector.GetDefaultGroupID();
				if (defaultGroupID != -1)
				{
					OnSelectedSetChange(defaultGroupID);
				}
			}
			shouldVerifySetSelection = false;
		}
		if (!waitForResourceChange)
		{
			double timeAsDouble = Time.timeAsDouble;
			if (timeAsDouble >= nextSpawnTime)
			{
				SpawnNextPiece();
				nextSpawnTime = timeAsDouble + (double)spawnDelay;
			}
		}
	}

	public void VerifySetSelection()
	{
		shouldVerifySetSelection = true;
	}

	public void OnAvailableResourcesChange()
	{
		waitForResourceChange = false;
	}

	public Transform GetSpawnTransform()
	{
		return spawnTransform;
	}

	public void OnShelfPieceCreated(BuilderPiece piece, float timeOffset)
	{
		float num = timeOffset * conveyorMoveSpeed / splineLength;
		if (num > 1f)
		{
			Debug.LogWarningFormat("Piece {0} add to shelf time {1}", piece.pieceId, num);
		}
		_ = piecesOnConveyor.Count;
		piecesOnConveyor.Add(piece);
		float num2 = Mathf.Clamp(num, 0f, 1f);
		Vector3 vector = EvaluateSpline(num2);
		Quaternion rotation = spawnTransform.rotation * Quaternion.Euler(piece.desiredShelfRotationOffset);
		Vector3 position = vector + spawnTransform.rotation * piece.desiredShelfOffset;
		piece.transform.SetPositionAndRotation(position, rotation);
		if (num <= 1f)
		{
			table.conveyorManager.AddPieceToJob(piece, num2, shelfID);
		}
	}

	public void OnShelfPieceRecycled(BuilderPiece piece)
	{
		piecesOnConveyor.Remove(piece);
		if (piece != null)
		{
			table.conveyorManager.RemovePieceFromJob(piece);
		}
	}

	public void OnClearTable()
	{
		piecesOnConveyor.Clear();
		grabbedPieceTypes.Clear();
		grabbedPieceMaterials.Clear();
	}

	public void ResetConveyorState()
	{
		for (int num = piecesOnConveyor.Count - 1; num >= 0; num--)
		{
			BuilderPiece builderPiece = piecesOnConveyor[num];
			if (!(builderPiece == null))
			{
				BuilderTable.BuilderCommand cmd = new BuilderTable.BuilderCommand
				{
					type = BuilderTable.BuilderCommandType.Recycle,
					pieceId = builderPiece.pieceId,
					localPosition = builderPiece.transform.position,
					localRotation = builderPiece.transform.rotation,
					player = NetworkSystem.Instance.LocalPlayer,
					isLeft = false,
					parentPieceId = -1
				};
				table.ExecutePieceRecycled(cmd);
			}
		}
		OnClearTable();
	}

	private void SpawnNextPiece()
	{
		FindNextAffordablePieceType(out var pieceType, out var materialType);
		if (pieceType != -1)
		{
			table.RequestCreateConveyorPiece(pieceType, materialType, shelfID);
		}
	}

	private void FindNextAffordablePieceType(out int pieceType, out int materialType)
	{
		if (grabbedPieceTypes.Count > 0)
		{
			pieceType = grabbedPieceTypes.Dequeue();
			materialType = grabbedPieceMaterials.Dequeue();
			return;
		}
		pieceType = -1;
		materialType = -1;
		if (piecesInSet.Count <= 0)
		{
			return;
		}
		for (int i = nextPieceToSpawn; i < piecesInSet.Count; i++)
		{
			BuilderPiece piecePrefab = piecesInSet[i].piecePrefab;
			if (table.HasEnoughResources(piecePrefab))
			{
				if (i + 1 >= piecesInSet.Count)
				{
					loopCount++;
					loopCount = Mathf.Max(0, loopCount);
				}
				nextPieceToSpawn = (i + 1) % piecesInSet.Count;
				pieceType = piecePrefab.name.GetStaticHash();
				materialType = GetMaterialType(piecesInSet[i]);
				return;
			}
		}
		loopCount++;
		loopCount = Mathf.Max(0, loopCount);
		for (int j = 0; j < nextPieceToSpawn; j++)
		{
			BuilderPiece piecePrefab2 = piecesInSet[j].piecePrefab;
			if (table.HasEnoughResources(piecePrefab2))
			{
				nextPieceToSpawn = (j + 1) % piecesInSet.Count;
				pieceType = piecePrefab2.name.GetStaticHash();
				materialType = GetMaterialType(piecesInSet[j]);
				return;
			}
		}
		waitForResourceChange = true;
	}

	private int GetMaterialType(BuilderPieceSet.PieceInfo info)
	{
		if (info.piecePrefab.materialOptions != null && info.overrideSetMaterial && info.pieceMaterialTypes.Length != 0)
		{
			int num = loopCount % info.pieceMaterialTypes.Length;
			string text = info.pieceMaterialTypes[num];
			if (string.IsNullOrEmpty(text))
			{
				Debug.LogErrorFormat("Empty Material Override for piece {0} in set {1}", info.piecePrefab.name, currentDisplayGroup.displayName);
				return -1;
			}
			return text.GetHashCode();
		}
		if (string.IsNullOrEmpty(currentDisplayGroup.defaultMaterial))
		{
			return -1;
		}
		return currentDisplayGroup.defaultMaterial.GetHashCode();
	}
}
