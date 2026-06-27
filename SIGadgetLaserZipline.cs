using System;
using GorillaExtensions;
using GorillaLocomotion;
using GorillaLocomotion.Climbing;
using UnityEngine;

public class SIGadgetLaserZipline : SIGadget, ICallBack
{
	[SerializeField]
	private GameButtonActivatable m_buttonActivatable;

	[SerializeField]
	private Transform zipline;

	[SerializeField]
	private Vector3 ziplineAnchorOffset;

	[SerializeField]
	private GameObject laserBeam;

	[SerializeField]
	private AudioSource laserBeamAudio;

	[SerializeField]
	private AudioSource onUseAudio;

	[SerializeField]
	private float cooldownDuration;

	[SerializeField]
	private bool cooldownOnUseUntilTouchGround;

	[SerializeField]
	private int maxSuperchargeUses = 2;

	[SerializeField]
	private AudioClip audioSingleUse;

	[SerializeField]
	private AudioClip audioReusable;

	[SerializeField]
	private AudioClip audioUsedUp;

	[SerializeField]
	private SoundBankPlayer audioRecharged;

	[Header("Upgrades")]
	[SerializeField]
	private float upgradedSpeedBoost = 5f;

	[SerializeField]
	private float speedBoostVelocityCap = 10f;

	private bool hasActiveCallback;

	private VRRig activeCallbackOnRig;

	private bool wasTriggerPressed;

	private bool isLineBroken;

	private bool wasSlidingUngrounded;

	private Quaternion activatedAtRotation;

	private Vector3 activatedAtPoint;

	private Vector3 ziplineDirection;

	private float coolingDownUntilTimestamp;

	private ResettableUseCounter groundedCooldown;

	private float _speedBoost;

	private static int s_localPlayerVelocityFrame = -1;

	private static Vector3 s_LocalPlayerAccumulatedVelocity;

	private static int s_LocalPlayerNumAccumulatedVelocities;

	private static int s_LocalPlayerPositionFrame = -1;

	private static Vector3 s_LocalPlayerAccumulatedPositionOffset;

	private static Vector3 s_LocalPlayerAppliedPositionOffset;

	private static void AccumulateVelocity(Vector3 desiredVelocity)
	{
		if (s_localPlayerVelocityFrame != Time.frameCount)
		{
			s_localPlayerVelocityFrame = Time.frameCount;
			s_LocalPlayerAccumulatedVelocity = Vector3.zero;
			s_LocalPlayerNumAccumulatedVelocities = 0;
		}
		s_LocalPlayerAccumulatedVelocity += desiredVelocity;
		s_LocalPlayerNumAccumulatedVelocities++;
		GTPlayer.Instance.SetVelocity(s_LocalPlayerAccumulatedVelocity / s_LocalPlayerNumAccumulatedVelocities);
	}

	private static void ResetLocalAppliedPositionOffset()
	{
		if (s_LocalPlayerPositionFrame != Time.frameCount)
		{
			s_LocalPlayerPositionFrame = Time.frameCount;
			s_LocalPlayerAccumulatedPositionOffset = Vector3.zero;
			s_LocalPlayerAppliedPositionOffset = Vector3.zero;
		}
		else
		{
			GTPlayer.Instance.transform.position -= s_LocalPlayerAppliedPositionOffset;
		}
	}

	private static void ReapplyPositionOffset()
	{
		GTPlayer.Instance.transform.position += s_LocalPlayerAppliedPositionOffset;
	}

	private static void AccumulateAndApplyLocalPositionOffset(Vector3 offset)
	{
		s_LocalPlayerAccumulatedPositionOffset += offset;
		s_LocalPlayerNumAccumulatedVelocities++;
		s_LocalPlayerAppliedPositionOffset = s_LocalPlayerAccumulatedPositionOffset / s_LocalPlayerNumAccumulatedVelocities;
		GTPlayer.Instance.transform.position += s_LocalPlayerAppliedPositionOffset;
	}

	private void Awake()
	{
		m_buttonActivatable = GetComponent<GameButtonActivatable>();
		laserBeam.SetActive(value: false);
		laserBeam.transform.SetParent(null);
		groundedCooldown = new ResettableUseCounter(1, maxSuperchargeUses, ShowReady);
		GameEntity obj = gameEntity;
		obj.OnGrabbed = (Action)Delegate.Combine(obj.OnGrabbed, new Action(OnGrabbed));
		GameEntity obj2 = gameEntity;
		obj2.OnSnapped = (Action)Delegate.Combine(obj2.OnSnapped, new Action(OnSnapped));
		GameEntity obj3 = gameEntity;
		obj3.OnReleased = (Action)Delegate.Combine(obj3.OnReleased, new Action(OnReleased));
		GameEntity obj4 = gameEntity;
		obj4.OnUnsnapped = (Action)Delegate.Combine(obj4.OnUnsnapped, new Action(OnUnsnapped));
		gameEntity.OnStateChanged += OnEntityStateChanged;
	}

	private void ClearCallback()
	{
		if (hasActiveCallback)
		{
			activeCallbackOnRig.RemoveLateUpdateCallback(this);
			activeCallbackOnRig = null;
			hasActiveCallback = false;
			SIPlayer.LocalPlayer.OnKnockback -= OnKnockback;
		}
	}

	private void ShowReady(bool isReady)
	{
		if (isReady)
		{
			audioRecharged.Play();
		}
	}

	private void OnDestroy()
	{
		ClearCallback();
		laserBeam.gameObject.Destroy();
	}

	private void OnGrabbed()
	{
	}

	private void OnSnapped()
	{
	}

	private void OnReleased()
	{
		wasTriggerPressed = false;
		laserBeam.SetActive(value: false);
		ClearCallback();
	}

	private void OnUnsnapped()
	{
		wasTriggerPressed = false;
		laserBeam.SetActive(value: false);
		ClearCallback();
	}

	protected override void OnUpdateAuthority(float dt)
	{
		bool num = m_buttonActivatable.CheckInput();
		bool flag = GTPlayer.Instance.IsGroundedButt || GTPlayer.Instance.IsGroundedHand || GTPlayer.Instance.IsTentacleActive;
		if (flag)
		{
			groundedCooldown.Reset();
		}
		if (num)
		{
			if (isLineBroken)
			{
				return;
			}
			if (flag)
			{
				if (wasSlidingUngrounded)
				{
					isLineBroken = true;
					laserBeam.SetActive(value: false);
					gameEntity.RequestState(gameEntity.id, GetStateLong());
					return;
				}
			}
			else
			{
				wasSlidingUngrounded = true;
			}
			if (!wasTriggerPressed)
			{
				if (IsBlocked(SIExclusionType.AffectsLocalMovement))
				{
					isLineBroken = true;
					laserBeam.SetActive(value: false);
					return;
				}
				if (Time.time < coolingDownUntilTimestamp)
				{
					isLineBroken = true;
					laserBeam.SetActive(value: false);
					return;
				}
				if (cooldownOnUseUntilTouchGround && !groundedCooldown.TryUse())
				{
					isLineBroken = true;
					laserBeam.SetActive(value: false);
					return;
				}
				SuperInfectionManager activeSuperInfectionManager = SuperInfectionManager.activeSuperInfectionManager;
				if ((object)activeSuperInfectionManager == null || !activeSuperInfectionManager.IsSupercharged)
				{
					onUseAudio.PlayOneShot(audioSingleUse);
				}
				else
				{
					onUseAudio.PlayOneShot(groundedCooldown.IsReady ? audioReusable : audioUsedUp);
				}
				laserBeam.SetActive(value: true);
				laserBeam.transform.localPosition = Vector3.zero;
				VRRig.LocalRig.AddLateUpdateCallback(this);
				SIPlayer.LocalPlayer.OnKnockback += OnKnockback;
				activeCallbackOnRig = VRRig.LocalRig;
				hasActiveCallback = true;
				activatedAtPoint = zipline.transform.TransformPoint(ziplineAnchorOffset);
				ziplineDirection = zipline.transform.forward;
				Vector3 up = VRRig.LocalRig.transform.up;
				if (Vector3.Dot(ziplineDirection, up) > 0f)
				{
					ziplineDirection = -ziplineDirection;
				}
				if (Vector3.Dot(ziplineDirection, up) > -0.5f)
				{
					ziplineDirection = Vector3.ProjectOnPlane(ziplineDirection, up);
					ziplineDirection.Normalize();
					ziplineDirection += up * -0.5f;
					ziplineDirection.Normalize();
				}
				activatedAtRotation = Quaternion.LookRotation(ziplineDirection);
				wasTriggerPressed = true;
				wasSlidingUngrounded = !GTPlayer.Instance.IsGroundedButt && !GTPlayer.Instance.IsGroundedHand;
				gameEntity.RequestState(gameEntity.id, GetStateLong());
			}
			if (IsBlocked(SIExclusionType.AffectsLocalMovement))
			{
				isLineBroken = true;
				laserBeam.SetActive(value: false);
				gameEntity.RequestState(gameEntity.id, GetStateLong());
				return;
			}
			Vector3 rigidbodyVelocity = GTPlayer.Instance.RigidbodyVelocity;
			GTPlayer.Instance.LaserZiplineActiveAtFrame = Time.frameCount + 1;
			float magnitude = rigidbodyVelocity.magnitude;
			float num2 = Vector3.Dot(GTPlayer.Instance.RigidbodyVelocity, ziplineDirection);
			if (_speedBoost > 0f && num2 < speedBoostVelocityCap)
			{
				num2 += Time.deltaTime * _speedBoost;
			}
			AccumulateVelocity(ziplineDirection * num2);
			UpdateAudioPitch(magnitude);
			wasTriggerPressed = true;
		}
		else if (wasTriggerPressed)
		{
			laserBeam.SetActive(value: false);
			zipline.transform.localRotation = Quaternion.identity;
			isLineBroken = false;
			wasTriggerPressed = false;
			wasSlidingUngrounded = false;
			coolingDownUntilTimestamp = Time.time + cooldownDuration;
			float num3 = Vector3.Dot(GTPlayer.Instance.RigidbodyVelocity, ziplineDirection);
			AccumulateVelocity(ziplineDirection * num3);
			if (FindAttachedHand(out var isLeft))
			{
				GorillaVelocityTracker interactPointVelocityTracker = GTPlayer.Instance.GetInteractPointVelocityTracker(isLeft);
				float scale = GTPlayer.Instance.scale;
				Vector3 vector = GTPlayer.Instance.turnParent.transform.rotation * -interactPointVelocityTracker.GetAverageVelocity(worldSpace: false, 0.1f, doMagnitudeCheck: true) * scale;
				vector = Vector3.ClampMagnitude(vector, 5.5f * scale);
				GTPlayer.Instance.AddForce(vector, ForceMode.VelocityChange);
			}
			gameEntity.RequestState(gameEntity.id, GetStateLong());
		}
		else
		{
			isLineBroken = false;
			Vector3 vector2 = zipline.parent.forward;
			Vector3 up2 = VRRig.LocalRig.transform.up;
			if (Mathf.Abs(Vector3.Dot(vector2, up2)) < 0.5f)
			{
				vector2 = Vector3.ProjectOnPlane(vector2, up2);
				vector2.Normalize();
				vector2 += up2 * -0.5f;
			}
			Quaternion b = zipline.parent.InverseTransformRotation(Quaternion.LookRotation(vector2));
			zipline.transform.localRotation = Quaternion.Lerp(zipline.transform.localRotation, b, Time.deltaTime * 25f);
		}
	}

	private long GetStateLong()
	{
		if (wasTriggerPressed && !isLineBroken)
		{
			return BitPackUtils.PackAnchoredPosRotForNetwork(activatedAtPoint, activatedAtRotation);
		}
		return 0L;
	}

	protected override void OnUpdateRemote(float dt)
	{
		if (laserBeam.activeSelf && activeCallbackOnRig != null)
		{
			UpdateAudioPitch(activeCallbackOnRig.LatestVelocity().magnitude);
		}
	}

	private void OnKnockback(Vector3 knockbackVector)
	{
		if (wasTriggerPressed)
		{
			isLineBroken = true;
			laserBeam.SetActive(value: false);
		}
	}

	private void OnEntityStateChanged(long oldState, long newState)
	{
		if (IsEquippedLocal() || activatedLocally)
		{
			return;
		}
		if (newState != 0L)
		{
			int attachedPlayerActorNr = gameEntity.AttachedPlayerActorNr;
			if (attachedPlayerActorNr >= 1 && GamePlayer.TryGetGamePlayer(attachedPlayerActorNr, out var out_gamePlayer))
			{
				BitPackUtils.UnpackAnchoredPosRotForNetwork(newState, out_gamePlayer.rig.transform.position, out var pos, out var rot);
				activatedAtPoint = pos;
				activatedAtRotation = rot;
				ziplineDirection = rot * Vector3.forward;
				laserBeam.SetActive(value: true);
				out_gamePlayer.rig.AddLateUpdateCallback(this);
				activeCallbackOnRig = out_gamePlayer.rig;
				hasActiveCallback = true;
				wasTriggerPressed = true;
				isLineBroken = false;
			}
		}
		else
		{
			wasTriggerPressed = false;
			isLineBroken = false;
			laserBeam.SetActive(value: false);
			ClearCallback();
		}
	}

	public void CallBack()
	{
		if (!wasTriggerPressed || isLineBroken)
		{
			ClearCallback();
			return;
		}
		if (IsEquippedLocal())
		{
			ResetLocalAppliedPositionOffset();
			Vector3 point = activatedAtPoint - zipline.transform.TransformPoint(ziplineAnchorOffset);
			point = GTExt.ProjectOnPlane(point, Vector3.zero, ziplineDirection);
			if (point.sqrMagnitude > 1f)
			{
				isLineBroken = true;
				laserBeam.SetActive(value: false);
				ReapplyPositionOffset();
				return;
			}
			AccumulateAndApplyLocalPositionOffset(point);
		}
		zipline.transform.rotation = activatedAtRotation;
		Vector3 position = activatedAtPoint + Vector3.Project(zipline.transform.TransformPoint(ziplineAnchorOffset) - activatedAtPoint, ziplineDirection);
		laserBeam.transform.position = position;
		laserBeam.transform.rotation = activatedAtRotation;
	}

	private void UpdateAudioPitch(float playerSpeed)
	{
		laserBeamAudio.pitch = 1f + playerSpeed / 30f;
	}

	public override void ApplyUpgradeNodes(SIUpgradeSet withUpgrades)
	{
		_speedBoost = (withUpgrades.Contains(SIUpgradeType.AirControl_Zipline_Speed) ? upgradedSpeedBoost : 0f);
	}
}
