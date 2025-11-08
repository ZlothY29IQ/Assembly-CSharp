using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(GameEntity))]
public abstract class SIGadget : MonoBehaviour, IGameEntityComponent, IPrefabRequirements, IGameActivatable
{
	[Serializable]
	private struct UpgradeVisual
	{
		public GameObject[] objects;

		[Tooltip("For the objects to become activated, you must match AT LEAST ONE appearRequirement (if there are any), and not match any disappearRequirements.")]
		public SIUpgradeType[] appearRequirements;

		[Tooltip("For the objects to become deactivated, you must match AT LEAST ONE disappearRequirement (if there are any).")]
		public SIUpgradeType[] disappearRequirements;

		public void Update(SIUpgradeSet withUpgrades)
		{
			bool flag = true;
			if (appearRequirements.Length != 0)
			{
				flag = false;
				SIUpgradeType[] array = appearRequirements;
				foreach (SIUpgradeType upgrade in array)
				{
					if (withUpgrades.Contains(upgrade))
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				SIUpgradeType[] array = disappearRequirements;
				foreach (SIUpgradeType upgrade2 in array)
				{
					if (withUpgrades.Contains(upgrade2))
					{
						flag = false;
						break;
					}
				}
			}
			GameObject[] array2 = objects;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].SetActive(flag);
			}
		}
	}

	public GameEntity gameEntity;

	[Tooltip("Add additional required prefabs here.  These will be automatically added to the GameEntityManager factory.")]
	public GameEntity[] additionalRequiredPrefabs;

	public float sleepTime = 10f;

	private bool shouldSleep = true;

	private bool isSleeping;

	private float timeReleased;

	protected bool activatedLocally;

	[SerializeField]
	private SITechTreePageId pageId;

	public Action<SIUpgradeSet> OnPostRefreshVisuals;

	private static int uniqueId = 101;

	private bool didApplyId;

	[SerializeField]
	private UpgradeVisual[] UpgradeBasedVisuals;

	private readonly List<SIExclusionZone> appliedExclusionZones = new List<SIExclusionZone>();

	public SITechTreePageId PageId
	{
		get
		{
			return pageId;
		}
		set
		{
			pageId = value;
		}
	}

	public IEnumerable<GameEntity> RequiredPrefabs => additionalRequiredPrefabs;

	protected virtual void Update()
	{
		if (!ApplicationQuittingState.IsQuitting)
		{
			float deltaTime = Time.deltaTime;
			if (IsEquippedLocal() || activatedLocally)
			{
				OnUpdateAuthority(deltaTime);
			}
			else
			{
				OnUpdateRemote(deltaTime);
			}
		}
	}

	protected virtual void OnUpdateAuthority(float dt)
	{
		SleepAfterDelay();
	}

	protected virtual void OnUpdateRemote(float dt)
	{
		SleepAfterDelay();
	}

	protected virtual bool IsEquippedLocal()
	{
		if (!gameEntity.IsHeldByLocalPlayer())
		{
			return gameEntity.IsSnappedByLocalPlayer();
		}
		return true;
	}

	protected Vector2 GetJoystickInput()
	{
		if (!ShouldProcessInput())
		{
			return default(Vector2);
		}
		return ControllerInputPoller.Primary2DAxis((gameEntity.heldByHandIndex == 0 || gameEntity.snappedJoint == SnapJointType.ArmL) ? XRNode.LeftHand : XRNode.RightHand);
	}

	protected bool ShouldProcessInput()
	{
		if (this.gameEntity.IsHeldByLocalPlayer())
		{
			return true;
		}
		if (this.gameEntity.IsSnappedByLocalPlayer() && GamePlayer.TryGetGamePlayer(this.gameEntity.snappedByActorNumber, out var out_gamePlayer))
		{
			GameEntity gameEntity = this.gameEntity.snappedJoint switch
			{
				SnapJointType.ArmL => out_gamePlayer.GetGrabbedGameEntity(0), 
				SnapJointType.ArmR => out_gamePlayer.GetGrabbedGameEntity(1), 
				_ => null, 
			};
			if ((bool)gameEntity)
			{
				return gameEntity.GetComponent<IGameActivatable>() == null;
			}
			return true;
		}
		return false;
	}

	public void SleepAfterDelay()
	{
		if (!isSleeping && shouldSleep && !(Time.time < timeReleased + sleepTime))
		{
			GetComponent<Rigidbody>().isKinematic = true;
			isSleeping = true;
		}
	}

	public virtual SIUpgradeSet FilterUpgradeNodes(SIUpgradeSet upgrades)
	{
		return upgrades;
	}

	public virtual void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
	}

	public virtual void RefreshUpgradeVisuals(SIUpgradeSet withUpgrades)
	{
		UpgradeVisual[] upgradeBasedVisuals = UpgradeBasedVisuals;
		foreach (UpgradeVisual upgradeVisual in upgradeBasedVisuals)
		{
			upgradeVisual.Update(withUpgrades);
		}
		OnPostRefreshVisuals?.Invoke(withUpgrades);
	}

	protected virtual void OnEnable()
	{
		if (!didApplyId)
		{
			GameObject obj = base.gameObject;
			obj.name = obj.name + "[" + uniqueId + "]";
			didApplyId = true;
			uniqueId++;
		}
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(GrabInitialization));
		GameEntity obj3 = gameEntity;
		obj3.OnGrabbed = (Action)Delegate.Combine(obj3.OnGrabbed, new Action(GrabInitialization));
		GameEntity obj4 = gameEntity;
		obj4.OnReleased = (Action)Delegate.Combine(obj4.OnReleased, new Action(ReleaseInitialization));
		GameEntity obj5 = gameEntity;
		obj5.OnUnsnapped = (Action)Delegate.Combine(obj5.OnUnsnapped, new Action(ReleaseInitialization));
		timeReleased = Time.time;
	}

	protected virtual void OnDisable()
	{
		GameEntity obj = gameEntity;
		obj.OnSnapped = (Action)Delegate.Remove(obj.OnSnapped, new Action(GrabInitialization));
		GameEntity obj2 = gameEntity;
		obj2.OnGrabbed = (Action)Delegate.Remove(obj2.OnGrabbed, new Action(GrabInitialization));
		GameEntity obj3 = gameEntity;
		obj3.OnReleased = (Action)Delegate.Remove(obj3.OnReleased, new Action(ReleaseInitialization));
		GameEntity obj4 = gameEntity;
		obj4.OnUnsnapped = (Action)Delegate.Remove(obj4.OnUnsnapped, new Action(ReleaseInitialization));
		LeaveAllExclusionZones();
	}

	public void GrabInitialization()
	{
		isSleeping = false;
		shouldSleep = false;
		if (gameEntity.IsHeldByLocalPlayer() && !(gameEntity.manager.GetComponent<SuperInfectionManager>()?.zoneSuperInfection == null))
		{
			bool isMine = SIPlayer.LocalPlayer.activePlayerGadgets.Contains(gameEntity.GetNetId());
			SIProgression.Instance.UpdateHeldGadgetsTelemetry(PageId, isMine, 1);
		}
	}

	public void ReleaseInitialization()
	{
		shouldSleep = true;
		isSleeping = false;
		timeReleased = Time.time;
		if (gameEntity.WasLastHeldByLocalPlayer() && !(gameEntity.manager.GetComponent<SuperInfectionManager>()?.zoneSuperInfection == null))
		{
			bool isMine = SIPlayer.LocalPlayer.activePlayerGadgets.Contains(gameEntity.GetNetId());
			SIProgression.Instance.UpdateHeldGadgetsTelemetry(PageId, isMine, -1);
		}
	}

	public bool FindAttachedHand(out bool isLeft, bool checkHeld = true, bool checkSnapped = true)
	{
		isLeft = false;
		int num = -1;
		if (checkHeld && GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			num = out_gamePlayer.FindHandIndex(gameEntity.id);
		}
		if (num == -1 && checkSnapped && GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out var out_gamePlayer2))
		{
			num = out_gamePlayer2.FindSnapIndex(gameEntity.id);
		}
		if (num == -1)
		{
			return false;
		}
		isLeft = GamePlayer.IsLeftHand(num);
		return true;
	}

	public int GetAttachedPlayerActorNumber()
	{
		if (gameEntity.heldByActorNumber == -1)
		{
			return gameEntity.snappedByActorNumber;
		}
		return gameEntity.heldByActorNumber;
	}

	public virtual void OnEntityInit()
	{
	}

	public virtual void OnEntityDestroy()
	{
	}

	public virtual void OnEntityStateChange(long prevState, long newState)
	{
	}

	public virtual void ProcessClientToAuthorityRPC(PhotonMessageInfo info, int rpcID, object[] data)
	{
	}

	public virtual void ProcessAuthorityToClientRPC(PhotonMessageInfo info, int rpcID, object[] data)
	{
	}

	public virtual void ProcessClientToClientRPC(PhotonMessageInfo info, int rpcID, object[] data)
	{
	}

	public void SendClientToAuthorityRPC(int rpcID)
	{
		SuperInfectionManager sIManagerForZone = SuperInfectionManager.GetSIManagerForZone(gameEntity.manager.zone);
		if (sIManagerForZone != null)
		{
			sIManagerForZone.CallRPC(SuperInfectionManager.ClientToAuthorityRPC.CallEntityRPC, new object[2]
			{
				gameEntity.GetNetId(),
				rpcID
			});
		}
	}

	public void SendClientToAuthorityRPC(int rpcID, object[] data)
	{
		SuperInfectionManager sIManagerForZone = SuperInfectionManager.GetSIManagerForZone(gameEntity.manager.zone);
		if (sIManagerForZone != null)
		{
			sIManagerForZone.CallRPC(SuperInfectionManager.ClientToAuthorityRPC.CallEntityRPCData, new object[3]
			{
				gameEntity.GetNetId(),
				rpcID,
				data
			});
		}
	}

	public void SendAuthorityToClientRPC(int rpcID)
	{
		SuperInfectionManager sIManagerForZone = SuperInfectionManager.GetSIManagerForZone(gameEntity.manager.zone);
		if (sIManagerForZone != null)
		{
			sIManagerForZone.CallRPC(SuperInfectionManager.AuthorityToClientRPC.CallEntityRPC, new object[2]
			{
				gameEntity.GetNetId(),
				rpcID
			});
		}
	}

	public void SendAuthorityToClientRPC(int rpcID, object[] data)
	{
		SuperInfectionManager sIManagerForZone = SuperInfectionManager.GetSIManagerForZone(gameEntity.manager.zone);
		if (sIManagerForZone != null)
		{
			sIManagerForZone.CallRPC(SuperInfectionManager.AuthorityToClientRPC.CallEntityRPCData, new object[3]
			{
				gameEntity.GetNetId(),
				rpcID,
				data
			});
		}
	}

	public void SendClientToClientRPC(int rpcID)
	{
		SuperInfectionManager sIManagerForZone = SuperInfectionManager.GetSIManagerForZone(gameEntity.manager.zone);
		if (sIManagerForZone != null)
		{
			sIManagerForZone.CallRPC(SuperInfectionManager.ClientToClientRPC.CallEntityRPC, new object[2]
			{
				gameEntity.GetNetId(),
				rpcID
			});
		}
	}

	public void SendClientToClientRPC(int rpcID, object[] data)
	{
		SuperInfectionManager sIManagerForZone = SuperInfectionManager.GetSIManagerForZone(gameEntity.manager.zone);
		if (sIManagerForZone != null)
		{
			sIManagerForZone.CallRPC(SuperInfectionManager.ClientToClientRPC.CallEntityRPCData, new object[3]
			{
				gameEntity.GetNetId(),
				rpcID,
				data
			});
		}
	}

	public void ApplyExclusionZone(SIExclusionZone exclusionZone)
	{
		if (!appliedExclusionZones.Contains(exclusionZone))
		{
			if (appliedExclusionZones.Count == 0)
			{
				appliedExclusionZones.Add(exclusionZone);
				HandleBlockedActionChanged(isBlocked: true);
			}
			else
			{
				appliedExclusionZones.Add(exclusionZone);
			}
		}
	}

	public void LeaveExclusionZone(SIExclusionZone exclusionZone)
	{
		if (appliedExclusionZones.Contains(exclusionZone))
		{
			appliedExclusionZones.Remove(exclusionZone);
			if (appliedExclusionZones.Count == 0)
			{
				HandleBlockedActionChanged(isBlocked: false);
			}
		}
	}

	private void LeaveAllExclusionZones()
	{
		foreach (SIExclusionZone appliedExclusionZone in appliedExclusionZones)
		{
			if (appliedExclusionZone != null)
			{
				appliedExclusionZone.ClearGadget(this);
			}
		}
		appliedExclusionZones.Clear();
	}

	protected bool IsBlocked()
	{
		return appliedExclusionZones.Count > 0;
	}

	protected virtual void HandleBlockedActionChanged(bool isBlocked)
	{
	}
}
