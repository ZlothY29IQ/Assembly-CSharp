using System;
using System.Collections.Generic;
using GorillaTagScripts;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class BuilderPiecePrivatePlot : MonoBehaviour
{
	public enum PlotState
	{
		Vacant,
		Occupied
	}

	[SerializeField]
	private Color placementAllowedColor;

	[SerializeField]
	private Color placementDisallowedColor;

	[SerializeField]
	private Color overCapacityColor;

	public List<MeshRenderer> borderMeshes;

	public BoxCollider buildArea;

	[SerializeField]
	private TMP_Text tmpLabel;

	[SerializeField]
	private List<BuilderResourceMeter> resourceMeters;

	[NonSerialized]
	public int[] usedResources;

	[NonSerialized]
	public int[] tempResourceCount;

	[SerializeField]
	private GameObject plotClaimedFX;

	private BuilderPiece leftPotentialParent;

	private BuilderPiece rightPotentialParent;

	private bool isLeftOverPlot;

	private bool isRightOverPlot;

	private Bounds buildAreaBounds;

	[HideInInspector]
	public BuilderPiece piece;

	private int owningPlayerActorNumber;

	private int attachedPieceCount;

	[HideInInspector]
	public int privatePlotIndex;

	[HideInInspector]
	public PlotState plotState;

	private bool doesLocalPlayerOwnAPlot;

	private Queue<BuilderPiece> piecesToCount;

	private bool initDone;

	private MaterialPropertyBlock materialProps;

	private List<Renderer> zoneRenderers = new List<Renderer>(12);

	private bool inBuilderZone;

	private void Awake()
	{
		Init();
	}

	private void Init()
	{
		if (!initDone)
		{
			materialProps = new MaterialPropertyBlock();
			usedResources = new int[3];
			for (int i = 0; i < usedResources.Length; i++)
			{
				usedResources[i] = 0;
			}
			tempResourceCount = new int[3];
			piece = GetComponent<BuilderPiece>();
			SetPlotState(PlotState.Vacant);
			piecesToCount = new Queue<BuilderPiece>(1024);
			initDone = true;
			privatePlotIndex = -1;
		}
	}

	private void Start()
	{
		if (piece != null && piece.GetTable() != null)
		{
			BuilderTable table = piece.GetTable();
			doesLocalPlayerOwnAPlot = table.DoesPlayerOwnPlot(PhotonNetwork.LocalPlayer.ActorNumber);
			table.OnLocalPlayerClaimedPlot.AddListener(OnLocalPlayerClaimedPlot);
			UpdateVisuals();
			foreach (BuilderResourceMeter resourceMeter in resourceMeters)
			{
				resourceMeter.table = piece.GetTable();
			}
		}
		buildArea.gameObject.SetActive(value: true);
		buildArea.enabled = true;
		buildAreaBounds = buildArea.bounds;
		buildArea.gameObject.SetActive(value: false);
		buildArea.enabled = false;
		zoneRenderers.Clear();
		zoneRenderers.Add(tmpLabel.GetComponent<Renderer>());
		foreach (BuilderResourceMeter resourceMeter2 in resourceMeters)
		{
			zoneRenderers.AddRange(resourceMeter2.GetComponentsInChildren<Renderer>());
		}
		zoneRenderers.AddRange(borderMeshes);
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(OnZoneChanged));
		inBuilderZone = true;
		OnZoneChanged();
	}

	private void OnDestroy()
	{
		if (piece != null && piece.GetTable() != null)
		{
			piece.GetTable().OnLocalPlayerClaimedPlot.RemoveListener(OnLocalPlayerClaimedPlot);
		}
		if (ZoneManagement.instance != null)
		{
			ZoneManagement instance = ZoneManagement.instance;
			instance.onZoneChanged = (Action)Delegate.Remove(instance.onZoneChanged, new Action(OnZoneChanged));
		}
	}

	private void OnZoneChanged()
	{
		bool flag = ZoneManagement.instance.IsZoneActive(piece.GetTable().tableZone);
		inBuilderZone = flag;
	}

	private void OnLocalPlayerClaimedPlot(bool claim)
	{
		doesLocalPlayerOwnAPlot = claim;
		UpdateVisuals();
	}

	public void UpdatePlot()
	{
		if (BuilderPieceInteractor.instance == null || BuilderPieceInteractor.instance.heldChainLength == null || BuilderPieceInteractor.instance.heldChainLength.Length < 2 || !PhotonNetwork.InRoom)
		{
			return;
		}
		if (!initDone)
		{
			Init();
		}
		if ((plotState == PlotState.Occupied && owningPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber) || (plotState == PlotState.Vacant && !doesLocalPlayerOwnAPlot))
		{
			BuilderPiece parentPiece = BuilderPieceInteractor.instance.prevPotentialPlacement[0].parentPiece;
			BuilderPiece parentPiece2 = BuilderPieceInteractor.instance.prevPotentialPlacement[1].parentPiece;
			bool flag = false;
			if (parentPiece == null && leftPotentialParent != null)
			{
				isLeftOverPlot = false;
				leftPotentialParent = null;
				flag = true;
			}
			else if ((leftPotentialParent == null && parentPiece != null) || (parentPiece != null && !parentPiece.Equals(leftPotentialParent)))
			{
				BuilderPiece attachedBuiltInPiece = parentPiece.GetAttachedBuiltInPiece();
				isLeftOverPlot = attachedBuiltInPiece != null && attachedBuiltInPiece.Equals(piece);
				leftPotentialParent = parentPiece;
				flag = true;
			}
			if (parentPiece2 == null && rightPotentialParent != null)
			{
				isRightOverPlot = false;
				rightPotentialParent = null;
				flag = true;
			}
			else if ((rightPotentialParent == null && parentPiece2 != null) || (parentPiece2 != null && !parentPiece2.Equals(rightPotentialParent)))
			{
				BuilderPiece attachedBuiltInPiece2 = parentPiece2.GetAttachedBuiltInPiece();
				isRightOverPlot = attachedBuiltInPiece2 != null && attachedBuiltInPiece2.Equals(piece);
				rightPotentialParent = parentPiece2;
				flag = true;
			}
			if (flag)
			{
				UpdateVisuals();
			}
		}
		else if (isRightOverPlot || isLeftOverPlot)
		{
			isRightOverPlot = false;
			isLeftOverPlot = false;
			UpdateVisuals();
		}
		foreach (BuilderResourceMeter resourceMeter in resourceMeters)
		{
			resourceMeter.UpdateMeterFill();
		}
	}

	public void RecountPlotCost()
	{
		Init();
		piece.GetChainCost(usedResources);
		UpdateVisuals();
	}

	public void OnPieceAttachedToPlot(BuilderPiece attachPiece)
	{
		AddChainResourcesToCount(attachPiece, attach: true);
		UpdateVisuals();
	}

	public void OnPieceDetachedFromPlot(BuilderPiece detachPiece)
	{
		AddChainResourcesToCount(detachPiece, attach: false);
		UpdateVisuals();
	}

	public void ChangeAttachedPieceCount(int delta)
	{
		attachedPieceCount += delta;
		UpdateVisuals();
	}

	public void AddChainResourcesToCount(BuilderPiece chain, bool attach)
	{
		if (chain == null)
		{
			return;
		}
		piecesToCount.Clear();
		for (int i = 0; i < tempResourceCount.Length; i++)
		{
			tempResourceCount[i] = 0;
		}
		piecesToCount.Enqueue(chain);
		AddPieceCostToArray(chain, tempResourceCount);
		bool flag = false;
		while (piecesToCount.Count > 0 && !flag)
		{
			BuilderPiece builderPiece = piecesToCount.Dequeue().firstChildPiece;
			while (builderPiece != null)
			{
				piecesToCount.Enqueue(builderPiece);
				if (!AddPieceCostToArray(builderPiece, tempResourceCount))
				{
					Debug.LogWarning("Builder plot placing pieces over limits");
					flag = true;
					break;
				}
				builderPiece = builderPiece.nextSiblingPiece;
			}
		}
		for (int j = 0; j < usedResources.Length; j++)
		{
			if (attach)
			{
				usedResources[j] += tempResourceCount[j];
			}
			else
			{
				usedResources[j] -= tempResourceCount[j];
			}
		}
	}

	public void ClaimPlotForPlayerNumber(int player)
	{
		owningPlayerActorNumber = player;
		SetPlotState(PlotState.Occupied);
	}

	public int GetOwnerActorNumber()
	{
		return owningPlayerActorNumber;
	}

	public void ClearPlot()
	{
		Init();
		attachedPieceCount = 0;
		for (int i = 0; i < usedResources.Length; i++)
		{
			usedResources[i] = 0;
		}
		SetPlotState(PlotState.Vacant);
	}

	public void FreePlot()
	{
		SetPlotState(PlotState.Vacant);
	}

	public bool IsPlotClaimed()
	{
		return plotState != PlotState.Vacant;
	}

	public bool IsChainUnderCapacity(BuilderPiece chain)
	{
		if (chain == null)
		{
			return true;
		}
		piecesToCount.Clear();
		for (int i = 0; i < tempResourceCount.Length; i++)
		{
			tempResourceCount[i] = usedResources[i];
		}
		piecesToCount.Enqueue(chain);
		if (!AddPieceCostToArray(chain, tempResourceCount))
		{
			return false;
		}
		while (piecesToCount.Count > 0)
		{
			BuilderPiece builderPiece = piecesToCount.Dequeue().firstChildPiece;
			while (builderPiece != null)
			{
				piecesToCount.Enqueue(builderPiece);
				if (!AddPieceCostToArray(builderPiece, tempResourceCount))
				{
					return false;
				}
				builderPiece = builderPiece.nextSiblingPiece;
			}
		}
		return true;
	}

	public bool AddPieceCostToArray(BuilderPiece addedPiece, int[] array)
	{
		if (addedPiece == null)
		{
			return true;
		}
		if (addedPiece.cost != null)
		{
			foreach (BuilderResourceQuantity quantity in addedPiece.cost.quantities)
			{
				if (quantity.type >= BuilderResourceType.Basic && quantity.type < BuilderResourceType.Count)
				{
					array[(int)quantity.type] += quantity.count;
					if (array[(int)quantity.type] > piece.GetTable().GetPrivateResourceLimitForType((int)quantity.type))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public bool CanPlayerAttachToPlot(int actorNumber)
	{
		if (plotState != PlotState.Occupied || owningPlayerActorNumber != actorNumber)
		{
			if (plotState == PlotState.Vacant)
			{
				return !piece.GetTable().DoesPlayerOwnPlot(actorNumber);
			}
			return false;
		}
		return true;
	}

	public bool CanPlayerGrabFromPlot(int actorNumber, Vector3 worldPosition)
	{
		if (owningPlayerActorNumber == actorNumber || plotState == PlotState.Vacant)
		{
			return true;
		}
		if (piece.GetTable().plotOwners.TryGetValue(actorNumber, out var value))
		{
			BuilderPiece builderPiece = piece.GetTable().GetPiece(value);
			if (builderPiece != null && builderPiece.TryGetPlotComponent(out var plot))
			{
				return plot.IsLocationWithinPlotExtents(worldPosition);
			}
		}
		return false;
	}

	private void SetPlotState(PlotState newState)
	{
		plotState = newState;
		switch (plotState)
		{
		case PlotState.Vacant:
			owningPlayerActorNumber = -1;
			if (tmpLabel != null && !tmpLabel.text.Equals(string.Empty))
			{
				tmpLabel.text = string.Empty;
			}
			break;
		case PlotState.Occupied:
			if (tmpLabel != null && NetworkSystem.Instance != null)
			{
				string text = string.Empty;
				NetPlayer player = NetworkSystem.Instance.GetPlayer(owningPlayerActorNumber);
				if (player != null && VRRigCache.Instance.TryGetVrrig(player, out var playerRig))
				{
					text = playerRig.Rig.playerNameVisible;
				}
				if (string.IsNullOrEmpty(text) && !tmpLabel.text.Equals("OCCUPIED"))
				{
					tmpLabel.text = "OCCUPIED";
				}
				else if (!tmpLabel.text.Equals(text))
				{
					tmpLabel.text = text;
				}
			}
			else if (tmpLabel != null && !tmpLabel.text.Equals("OCCUPIED"))
			{
				tmpLabel.text = "OCCUPIED";
			}
			break;
		}
		UpdateVisuals();
	}

	public bool IsLocationWithinPlotExtents(Vector3 worldPosition)
	{
		if (!buildAreaBounds.Contains(worldPosition))
		{
			return false;
		}
		Vector3 vector = buildArea.transform.InverseTransformPoint(worldPosition);
		Vector3 vector2 = buildArea.center + buildArea.size / 2f;
		Vector3 vector3 = buildArea.center - buildArea.size / 2f;
		if (vector.x >= vector3.x && vector.x <= vector2.x && vector.y >= vector3.y && vector.y <= vector2.y && vector.z >= vector3.z)
		{
			return vector.z <= vector2.z;
		}
		return false;
	}

	public void OnAvailableResourceChange()
	{
		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		if (usedResources == null || piece.GetTable() == null)
		{
			return;
		}
		switch (plotState)
		{
		case PlotState.Vacant:
		{
			if (!doesLocalPlayerOwnAPlot)
			{
				UpdateVisualsForOwner();
				break;
			}
			SetBorderColor(placementDisallowedColor);
			for (int j = 0; j < resourceMeters.Count && j < 3; j++)
			{
				int privateResourceLimitForType2 = piece.GetTable().GetPrivateResourceLimitForType(j);
				if (privateResourceLimitForType2 != 0)
				{
					resourceMeters[j].SetNormalizedFillTarget((float)(privateResourceLimitForType2 - usedResources[j]) / (float)privateResourceLimitForType2);
				}
			}
			break;
		}
		case PlotState.Occupied:
		{
			if (owningPlayerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
			{
				UpdateVisualsForOwner();
				break;
			}
			SetBorderColor(placementDisallowedColor);
			for (int i = 0; i < resourceMeters.Count && i < 3; i++)
			{
				int privateResourceLimitForType = piece.GetTable().GetPrivateResourceLimitForType(i);
				if (privateResourceLimitForType != 0)
				{
					resourceMeters[i].SetNormalizedFillTarget((float)(privateResourceLimitForType - usedResources[i]) / (float)privateResourceLimitForType);
				}
			}
			break;
		}
		}
	}

	private void UpdateVisualsForOwner()
	{
		bool flag = true;
		if (usedResources == null || BuilderPieceInteractor.instance == null || BuilderPieceInteractor.instance.heldChainCost == null)
		{
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			int num = usedResources[i];
			if (isLeftOverPlot)
			{
				num += BuilderPieceInteractor.instance.heldChainCost[0][i];
			}
			if (isRightOverPlot)
			{
				num += BuilderPieceInteractor.instance.heldChainCost[1][i];
			}
			int privateResourceLimitForType = piece.GetTable().GetPrivateResourceLimitForType(i);
			if (num < privateResourceLimitForType)
			{
				flag = false;
			}
			if (privateResourceLimitForType != 0 && resourceMeters.Count > i)
			{
				resourceMeters[i].SetNormalizedFillTarget((float)(privateResourceLimitForType - num) / (float)privateResourceLimitForType);
			}
		}
		if (flag)
		{
			SetBorderColor(placementDisallowedColor);
		}
		else
		{
			SetBorderColor(placementAllowedColor);
		}
	}

	private void SetBorderColor(Color color)
	{
		borderMeshes[0].GetPropertyBlock(materialProps);
		materialProps.SetColor(ShaderProps._BaseColor, color);
		foreach (MeshRenderer borderMesh in borderMeshes)
		{
			borderMesh.SetPropertyBlock(materialProps);
		}
	}
}
