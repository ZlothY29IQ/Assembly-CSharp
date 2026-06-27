using System;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(GameGrabbable))]
[RequireComponent(typeof(GameSnappable))]
[RequireComponent(typeof(GameButtonActivatable))]
public class SIGadgetPlatformDeployer : SIGadget, I_SIDisruptable, IEnergyGadget
{
	private enum State
	{
		Idle,
		Deploying,
		Count
	}

	[SerializeField]
	private GameButtonActivatable buttonActivatable;

	[SerializeField]
	private SoundBankPlayer rechargeSFX;

	[SerializeField]
	private SoundBankPlayer blockedSFX;

	[SerializeField]
	private MeshRenderer blockedDisplayMesh;

	[SerializeField]
	private Material unblockedMat;

	[SerializeField]
	private Material blockedMat;

	[SerializeField]
	private GameObject platformPrefab;

	[Header("Activation")]
	[SerializeField]
	private bool isInstancePlace;

	[SerializeField]
	private float activationHandDistance = 0.2f;

	[SerializeField]
	private float inputSensitivity = 0.25f;

	[Header("Deploy")]
	[SerializeField]
	private float deployMinRequiredHandDistance = 0.2f;

	[SerializeField]
	private GameObject previewPlatform;

	[SerializeField]
	private float handInset = 0.1f;

	[SerializeField]
	private float handDepthOffset = 0.3f;

	[SerializeField]
	private MeshRenderer previewMesh;

	[SerializeField]
	private Material validPreviewMaterial;

	[SerializeField]
	private Material invalidPreviewMaterial;

	[Header("Charges")]
	private int maxCharges = 3;

	private float chargeRecoveryTime = 10f;

	private SIChargeDisplay chargeDisplay;

	[SerializeField]
	private int maxChargesDefault = 3;

	[SerializeField]
	private int maxChargesHighCapacity = 5;

	[SerializeField]
	private SIChargeDisplay chargeDisplayDefault;

	[SerializeField]
	private SIChargeDisplay chargeDisplayHighCapacity;

	[SerializeField]
	private float chargeRecoveryTimeDefault = 10f;

	[SerializeField]
	private float chargeRecoveryTimeFast = 5f;

	private State state;

	private bool wasInputPressed;

	private float remainingRechargeTime;

	private SIUpgradeSet instanceUpgrades;

	private const float MAX_DEPLOY_DIST = 2f;

	private int deployedPlatformCount;

	public bool UsesEnergy => true;

	public bool IsFull => remainingRechargeTime <= 0f;

	private void Start()
	{
		previewPlatform.SetActive(value: false);
		GameEntity obj = gameEntity;
		obj.OnReleased = (Action)Delegate.Combine(obj.OnReleased, new Action(HandleStopInteraction));
		GameEntity obj2 = gameEntity;
		obj2.OnUnsnapped = (Action)Delegate.Combine(obj2.OnUnsnapped, new Action(HandleStopInteraction));
	}

	private void OnDestroy()
	{
		GameEntity obj = gameEntity;
		obj.OnReleased = (Action)Delegate.Remove(obj.OnReleased, new Action(HandleStopInteraction));
		GameEntity obj2 = gameEntity;
		obj2.OnUnsnapped = (Action)Delegate.Remove(obj2.OnUnsnapped, new Action(HandleStopInteraction));
	}

	private void HandleStopInteraction()
	{
		SetState(State.Idle);
	}

	public void UpdateRecharge(float dt)
	{
		if (!(remainingRechargeTime > 0f))
		{
			return;
		}
		int num = Mathf.CeilToInt(remainingRechargeTime / chargeRecoveryTime);
		remainingRechargeTime = Mathf.Max(remainingRechargeTime - dt, 0f);
		int num2 = Mathf.CeilToInt(remainingRechargeTime / chargeRecoveryTime);
		chargeDisplay.UpdateDisplay(maxCharges - num2);
		if (num2 != num && gameEntity.IsHeldOrSnappedByLocalPlayer)
		{
			rechargeSFX.Play();
			if (FindAttachedHand(out var isLeft))
			{
				GorillaTagger.Instance.StartVibration(isLeft, GorillaTagger.Instance.tapHapticStrength * 0.5f, GorillaTagger.Instance.tapHapticDuration * 0.5f);
			}
		}
	}

	protected override void OnUpdateAuthority(float dt)
	{
		switch (state)
		{
		case State.Idle:
			if (CheckInitInputs())
			{
				if (IsChargeAvailable())
				{
					if (isInstancePlace)
					{
						if (!wasInputPressed)
						{
							TryDeployInstantPlatform();
						}
					}
					else
					{
						SetStateAuthority(State.Deploying);
					}
				}
				wasInputPressed = true;
			}
			else
			{
				wasInputPressed = false;
			}
			break;
		case State.Deploying:
			if (CheckReleaseInputs())
			{
				if (IsChargeAvailable())
				{
					TryDeployPlatform();
				}
				SetStateAuthority(State.Idle);
			}
			else
			{
				UpdatePreview();
			}
			break;
		}
	}

	protected override void OnUpdateRemote(float dt)
	{
		State state = (State)gameEntity.GetState();
		if (state != this.state)
		{
			SetState(state);
		}
		State state2 = this.state;
		if (state2 != State.Idle && state2 == State.Deploying)
		{
			UpdatePreview();
		}
	}

	private bool CheckInitInputs()
	{
		if (!buttonActivatable.CheckInput(inputSensitivity))
		{
			return false;
		}
		if (isInstancePlace)
		{
			return true;
		}
		GamePlayer gamePlayer = GamePlayerLocal.instance.gamePlayer;
		Vector3 position = gamePlayer.leftHand.position;
		Vector3 position2 = gamePlayer.rightHand.position;
		if (Vector3.Distance(position, position2) > activationHandDistance)
		{
			return false;
		}
		return true;
	}

	private bool CheckReleaseInputs()
	{
		return !buttonActivatable.CheckInput(inputSensitivity);
	}

	private bool IsChargeAvailable()
	{
		if ((float)maxCharges * chargeRecoveryTime - remainingRechargeTime > chargeRecoveryTime)
		{
			return true;
		}
		return false;
	}

	private void SpendCharge()
	{
		remainingRechargeTime += chargeRecoveryTime;
	}

	private static bool IsLeftHandOrSnapSlot(int handIndex)
	{
		if (handIndex != 0)
		{
			return handIndex == 2;
		}
		return true;
	}

	private void TryDeployInstantPlatform()
	{
		if (IsBlocked())
		{
			blockedSFX.Play();
		}
		else
		{
			if (!TryGetGamePlayer(out var player))
			{
				return;
			}
			int num = player.FindSnapIndex(gameEntity.id);
			if (num == -1)
			{
				num = player.FindHandIndex(gameEntity.id);
			}
			if (num != -1)
			{
				Vector3 vector;
				Quaternion quaternion;
				if (gameEntity.IsHeldByLocalPlayer())
				{
					vector = base.transform.position - base.transform.up * handDepthOffset;
					quaternion = base.transform.rotation;
					Debug.DrawRay(base.transform.position, -base.transform.up * 0.3f, Color.blue, 10f);
					Debug.DrawRay(base.transform.position, base.transform.forward * 0.3f, Color.blue, 10f);
					Debug.DrawRay(vector, quaternion * Vector3.forward * 0.3f, Color.green, 10f);
				}
				else
				{
					Transform obj = (IsLeftHandOrSnapSlot(num) ? player.leftHand : player.rightHand);
					vector = obj.position;
					Vector3 up = obj.up;
					Vector3 right = obj.right;
					Debug.DrawRay(vector, right * 0.3f, Color.red, 10f);
					Debug.DrawRay(vector, up * 0.3f, Color.red, 10f);
					quaternion = Quaternion.LookRotation(up, right);
					vector += right * handDepthOffset;
					Debug.DrawRay(vector, quaternion * Vector3.forward * 0.3f, Color.green, 10f);
				}
				DeployPlatform(vector, quaternion);
			}
		}
	}

	private void TryDeployPlatform()
	{
		GamePlayer gamePlayer = GamePlayerLocal.instance.gamePlayer;
		Vector3 position = gamePlayer.leftHand.position;
		Vector3 position2 = gamePlayer.rightHand.position;
		if (Vector3.Distance(position, position2) > deployMinRequiredHandDistance)
		{
			Vector3 pos;
			Quaternion rot;
			Vector3 scale;
			if (IsBlocked())
			{
				blockedSFX.Play();
			}
			else if (TryGetPlatformPosRotScale(out pos, out rot, out scale))
			{
				DeployPlatform(pos, rot);
			}
		}
	}

	private void DeployPlatform(Vector3 pos, Quaternion rot)
	{
		SpendCharge();
		CreateLocalPlatformInstance(pos, rot);
		int actorNumber = NetworkSystem.Instance.LocalPlayer.ActorNumber;
		if (gameEntity.IsAuthority())
		{
			SendAuthorityToClientRPC(0, new object[3] { actorNumber, pos, rot });
		}
		else
		{
			SendClientToAuthorityRPC(0, new object[3] { actorNumber, pos, rot });
		}
	}

	public override void ProcessClientToAuthorityRPC(PhotonMessageInfo info, int rpcID, object[] data)
	{
		if (rpcID == 0 && data != null && data.Length == 3 && GameEntityManager.ValidateDataType<int>(data[0], out var _) && GameEntityManager.ValidateDataType<Vector3>(data[1], out var dataAsType2) && GameEntityManager.ValidateDataType<Quaternion>(data[2], out var dataAsType3) && gameEntity.IsAttachedToPlayer(NetPlayer.Get(info.Sender)) && !(Vector3.Distance(base.transform.position, dataAsType2) > 2f))
		{
			CreateLocalPlatformInstance(dataAsType2, dataAsType3);
			SendAuthorityToClientRPC(0, data);
		}
	}

	public override void ProcessAuthorityToClientRPC(PhotonMessageInfo info, int rpcID, object[] data)
	{
		if (rpcID == 0 && data != null && data.Length == 3 && GameEntityManager.ValidateDataType<int>(data[0], out var dataAsType) && GameEntityManager.ValidateDataType<Vector3>(data[1], out var dataAsType2) && GameEntityManager.ValidateDataType<Quaternion>(data[2], out var dataAsType3) && dataAsType != NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			CreateLocalPlatformInstance(dataAsType2, dataAsType3);
		}
	}

	private void CreateLocalPlatformInstance(Vector3 pos, Quaternion rot)
	{
		if (deployedPlatformCount >= maxCharges)
		{
			return;
		}
		GameObject gameObject = ObjectPools.instance.Instantiate(platformPrefab);
		if (!(gameObject != null))
		{
			return;
		}
		SIGadgetPlatformDeployerPlatform component = gameObject.GetComponent<SIGadgetPlatformDeployerPlatform>();
		if (component != null)
		{
			deployedPlatformCount++;
			component.OnDisabled = (Action)Delegate.Combine(component.OnDisabled, (Action)delegate
			{
				deployedPlatformCount--;
			});
		}
		gameObject.transform.SetPositionAndRotation(pos, rot);
		if (gameObject.TryGetComponent<ISIGameDeployable>(out var component2))
		{
			component2.ApplyUpgrades(instanceUpgrades);
		}
	}

	private void SetStateAuthority(State newState)
	{
		SetState(newState);
		gameEntity.RequestState(gameEntity.id, (long)newState);
	}

	private void SetState(State newState)
	{
		if (newState != state && CanChangeState((long)newState))
		{
			state = newState;
			switch (state)
			{
			case State.Idle:
				SetPreviewVisibility(enabled: false);
				break;
			case State.Deploying:
				SetPreviewVisibility(enabled: true);
				break;
			}
		}
	}

	public bool CanChangeState(long newStateIndex)
	{
		if (newStateIndex < 0 || newStateIndex >= 2)
		{
			return false;
		}
		return true;
	}

	private void SetPreviewVisibility(bool enabled)
	{
		previewPlatform.SetActive(enabled);
		if (enabled)
		{
			UpdatePreview();
		}
	}

	private void UpdatePreview()
	{
		if (!TryGetPlatformPosRotScale(out var pos, out var rot, out var scale))
		{
			return;
		}
		previewPlatform.transform.SetPositionAndRotation(pos, rot);
		previewPlatform.transform.localScale = scale;
		if (TryGetGamePlayer(out var player))
		{
			Vector3 position = player.leftHand.position;
			Vector3 position2 = player.rightHand.position;
			if (Vector3.Distance(position, position2) > deployMinRequiredHandDistance)
			{
				previewMesh.material = validPreviewMaterial;
			}
			else
			{
				previewMesh.material = invalidPreviewMaterial;
			}
		}
	}

	private bool TryGetPlatformPosRotScale(out Vector3 pos, out Quaternion rot, out Vector3 scale)
	{
		pos = Vector3.zero;
		rot = Quaternion.identity;
		scale = Vector3.one;
		if (TryGetGamePlayer(out var player))
		{
			Vector3 position = player.leftHand.position;
			Vector3 position2 = player.rightHand.position;
			Vector3 position3 = player.rig.head.rigTarget.position;
			Vector3 vector = (position + position2) / 2f;
			Vector3 normalized = (position3 - vector).normalized;
			Vector3 forward = Vector3.ProjectOnPlane((position - position2).normalized, normalized);
			pos = vector + -normalized * handDepthOffset;
			rot = Quaternion.LookRotation(forward, normalized);
			return true;
		}
		return false;
	}

	private bool TryGetGamePlayer(out GamePlayer player)
	{
		player = null;
		if (GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out player))
		{
			return true;
		}
		if (GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out player))
		{
			return true;
		}
		return false;
	}

	public override void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
		instanceUpgrades = withUpgrades;
		bool flag = withUpgrades.Contains(SIUpgradeType.Platform_Capacity);
		maxCharges = (flag ? maxChargesHighCapacity : maxChargesDefault);
		chargeDisplay = (flag ? chargeDisplayHighCapacity : chargeDisplayDefault);
		chargeRecoveryTime = (withUpgrades.Contains(SIUpgradeType.Platform_Cooldown) ? chargeRecoveryTimeFast : chargeRecoveryTimeDefault);
	}

	public void Disrupt(float disruptTime)
	{
		remainingRechargeTime = (float)maxCharges * chargeRecoveryTime + disruptTime;
	}

	protected override void HandleBlockedActionChanged(bool isBlocked)
	{
		blockedDisplayMesh.material = (isBlocked ? blockedMat : unblockedMat);
	}
}
