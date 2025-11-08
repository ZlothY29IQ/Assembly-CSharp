using System;
using Photon.Pun;
using UnityEngine;

public class SIGadgetTapTeleporter : SIGadget
{
	[SerializeField]
	private GameButtonActivatable buttonActivatable;

	[SerializeField]
	private GameObject teleportPointPrefab;

	[SerializeField]
	private SoundBankPlayer blockedSFX;

	[SerializeField]
	private float placementDelay = 0.5f;

	[SerializeField]
	private Renderer identifierColorDisplay;

	[SerializeField]
	private Renderer selectionColorDisplay;

	[SerializeField]
	private Material selectionColor1;

	[SerializeField]
	private Material selectionColor2;

	[SerializeField]
	private float portalDefaultDuration = 30f;

	private float placementCheckDistance = 0.3f;

	private SIGadgetTapTeleporterDeployable _selection1Teleport;

	private SIGadgetTapTeleporterDeployable _selection2Teleport;

	private bool isHandTapSetup;

	private bool isActivated;

	private float nextPlacementDelay;

	private int nextSelectionId;

	private SIUpgradeSet instanceUpgrades;

	private float minBrightness = 0.3f;

	private float maxBrightness = 1f;

	[SerializeField]
	private LayerMask overlapCheckLayers;

	[SerializeField]
	private float nearOffset = 0.11f;

	[SerializeField]
	private float farOffset = 0.664f;

	[SerializeField]
	private float overlapCheckRadius = 0.1f;

	private Collider[] overlapCheckResults = new Collider[1];

	public Color identifierColor { get; private set; }

	public bool useStealthTeleporters { get; private set; }

	public bool isVelocityPreserved { get; private set; }

	public bool hasInfiniteDuration { get; private set; }

	public override void OnEntityInit()
	{
		gameEntity.OnStateChanged += HandleStateChanged;
		gameEntity.onEntityDestroyed += HandleOnDestroyed;
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(HandleHandAttached));
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(HandleHandAttached));
		GameEntity obj3 = gameEntity;
		obj3.OnReleased = (Action)Delegate.Combine(obj3.OnReleased, new Action(HandleHandDetach));
		GameEntity obj4 = gameEntity;
		obj4.OnUnsnapped = (Action)Delegate.Combine(obj4.OnUnsnapped, new Action(HandleHandDetach));
		identifierColor = GenerateColor(gameEntity.GetNetId());
		ApplyIdentifierColor();
		UpdateNextSelectionDisplay();
	}

	private void HandleOnDestroyed(GameEntity entity)
	{
		if (gameEntity.IsAuthority())
		{
			if ((bool)_selection1Teleport)
			{
				gameEntity.manager.RequestDestroyItem(_selection1Teleport.gameEntity.id);
			}
			if ((bool)_selection2Teleport)
			{
				gameEntity.manager.RequestDestroyItem(_selection2Teleport.gameEntity.id);
			}
		}
	}

	private new void OnDisable()
	{
		HandleHandDetach();
	}

	private void HandleHandAttached()
	{
		if (IsEquippedLocal())
		{
			isHandTapSetup = true;
			GorillaTagger.Instance.OnHandTap += HandleOnHandTap;
		}
	}

	private void HandleHandDetach()
	{
		if (isHandTapSetup)
		{
			isHandTapSetup = false;
			GorillaTagger.Instance.OnHandTap -= HandleOnHandTap;
		}
		isActivated = false;
	}

	private void HandleOnHandTap(bool isLeft, Vector3 position, Vector3 normal)
	{
		if (FindAttachedHand(out var isLeft2) && isLeft == isLeft2 && isActivated)
		{
			PlaceTapTeleporter(position, normal);
		}
	}

	private Color GenerateColor(int seed)
	{
		UnityEngine.Random.InitState(seed);
		float num = Mathf.Lerp(maxBrightness, minBrightness, UnityEngine.Random.value);
		float num2 = Mathf.Lerp(maxBrightness, minBrightness, UnityEngine.Random.value);
		Color black = Color.black;
		switch (UnityEngine.Random.Range(0, 3))
		{
		case 0:
			black.r = num;
			black.g = num2;
			break;
		case 1:
			black.g = num;
			black.b = num2;
			break;
		case 2:
			black.b = num;
			black.r = num2;
			break;
		}
		return black;
	}

	protected override void OnUpdateAuthority(float dt)
	{
		isActivated = buttonActivatable.CheckInput();
		if (nextPlacementDelay > 0f)
		{
			nextPlacementDelay -= dt;
		}
	}

	private void PlaceTapTeleporter(Vector3 position, Vector3 normal)
	{
		if (!(nextPlacementDelay > 0f) && CheckValidTeleporterPlacement(position, normal))
		{
			if (IsBlocked())
			{
				blockedSFX.Play();
				return;
			}
			SendClientToAuthorityRPC(0, new object[4]
			{
				position,
				Quaternion.LookRotation(normal, base.transform.forward),
				nextSelectionId,
				hasInfiniteDuration ? (-1f) : portalDefaultDuration
			});
			CycleSelection();
			nextPlacementDelay = placementDelay;
		}
	}

	private bool CheckValidTeleporterPlacement(Vector3 position, Vector3 direction)
	{
		Vector3 point = position + direction * nearOffset;
		Vector3 point2 = position + direction * farOffset;
		return Physics.OverlapCapsuleNonAlloc(point, point2, overlapCheckRadius, overlapCheckResults, overlapCheckLayers) == 0;
	}

	public override void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
		instanceUpgrades = withUpgrades;
		useStealthTeleporters = withUpgrades.Contains(SIUpgradeType.Tapteleport_Stealth);
		isVelocityPreserved = withUpgrades.Contains(SIUpgradeType.Tapteleport_Keep_Velocity);
		hasInfiniteDuration = withUpgrades.Contains(SIUpgradeType.Tapteleport_Infinite_Use);
	}

	public override void ProcessClientToAuthorityRPC(PhotonMessageInfo info, int rpcID, object[] data)
	{
		if (rpcID == 0 && data != null && data.Length == 4 && GameEntityManager.ValidateDataType<Vector3>(data[0], out var dataAsType) && GameEntityManager.ValidateDataType<Quaternion>(data[1], out var dataAsType2) && GameEntityManager.ValidateDataType<int>(data[2], out var dataAsType3) && dataAsType3 >= 0 && dataAsType3 <= 100 && GameEntityManager.ValidateDataType<float>(data[3], out var dataAsType4) && gameEntity.IsAttachedToPlayer(NetPlayer.Get(info.Sender)) && !(Vector3.Distance(dataAsType, base.transform.position) > placementCheckDistance) && CheckValidTeleporterPlacement(dataAsType, dataAsType2 * Vector3.forward))
		{
			RemoveTeleporter(dataAsType3);
			PlaceNewTapTeleporter(dataAsType, dataAsType2, dataAsType3, dataAsType4);
		}
	}

	public override void ProcessClientToClientRPC(PhotonMessageInfo info, int rpcID, object[] data)
	{
		if (rpcID == 0 && data != null && data.Length == 1 && GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType) && dataAsType >= 0 && dataAsType <= 1 && gameEntity.IsAttachedToPlayer(NetPlayer.Get(info.Sender)))
		{
			nextSelectionId = dataAsType;
			UpdateNextSelectionDisplay();
		}
	}

	private void RemoveTeleporter(int selectId)
	{
		switch (selectId)
		{
		case 0:
			if (_selection1Teleport != null && _selection1Teleport.gameObject.activeSelf)
			{
				gameEntity.manager.RequestDestroyItem(_selection1Teleport.gameEntity.id);
				_selection1Teleport = null;
			}
			break;
		case 1:
			if (_selection2Teleport != null && _selection2Teleport.gameObject.activeSelf)
			{
				gameEntity.manager.RequestDestroyItem(_selection2Teleport.gameEntity.id);
				_selection2Teleport = null;
			}
			break;
		}
	}

	private void PlaceNewTapTeleporter(Vector3 position, Quaternion rotation, int selectionId, float duration)
	{
		GameEntityId gameEntityId = gameEntity.manager.RequestCreateItem(teleportPointPrefab.gameObject.name.GetStaticHash(), position, rotation, BitPackUtils.PackIntsIntoLong(selectionId, (int)duration));
		if (!(gameEntityId != GameEntityId.Invalid))
		{
			return;
		}
		SIGadgetTapTeleporterDeployable component = gameEntity.manager.GetGameEntity(gameEntityId).GetComponent<SIGadgetTapTeleporterDeployable>();
		switch (selectionId)
		{
		case 0:
			if (_selection2Teleport != null)
			{
				_selection2Teleport.SetLink(this, component);
			}
			component.SetLink(this, _selection2Teleport);
			_selection1Teleport = component;
			break;
		case 1:
			if (_selection1Teleport != null)
			{
				_selection1Teleport.SetLink(this, component);
			}
			component.SetLink(this, _selection1Teleport);
			_selection2Teleport = component;
			break;
		}
		UpdateNewTeleporters();
	}

	private void UpdateNewTeleporters()
	{
		int value = (_selection1Teleport ? _selection1Teleport.gameEntity.GetNetId() : 0);
		int value2 = (_selection2Teleport ? _selection2Teleport.gameEntity.GetNetId() : 0);
		long newState = BitPackUtils.PackIntsIntoLong(value, value2);
		gameEntity.RequestState(gameEntity.id, newState);
	}

	private void HandleStateChanged(long oldState, long newState)
	{
		if (!gameEntity.IsAuthority())
		{
			BitPackUtils.UnpackIntsFromLong(newState, out var value, out var value2);
			GameEntity gameEntityFromNetId = gameEntity.manager.GetGameEntityFromNetId(value);
			if (gameEntityFromNetId != null)
			{
				_selection1Teleport = gameEntityFromNetId.GetComponent<SIGadgetTapTeleporterDeployable>();
			}
			else
			{
				_selection1Teleport = null;
			}
			GameEntity gameEntityFromNetId2 = gameEntity.manager.GetGameEntityFromNetId(value2);
			if (gameEntityFromNetId2 != null)
			{
				_selection2Teleport = gameEntityFromNetId2.GetComponent<SIGadgetTapTeleporterDeployable>();
			}
			else
			{
				_selection2Teleport = null;
			}
		}
	}

	private void ApplyIdentifierColor()
	{
		identifierColorDisplay.material.color = identifierColor;
	}

	private void UpdateNextSelectionDisplay()
	{
		if (nextSelectionId == 0)
		{
			selectionColorDisplay.material = selectionColor1;
		}
		else if (nextSelectionId == 1)
		{
			selectionColorDisplay.material = selectionColor2;
		}
	}

	public void CycleSelection()
	{
		nextSelectionId = (nextSelectionId + 1) % 2;
		UpdateNextSelectionDisplay();
		SendClientToClientRPC(0, new object[1] { nextSelectionId });
	}
}
