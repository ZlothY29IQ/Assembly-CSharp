using GorillaTag.Cosmetics;
using UnityEngine;
using UnityEngine.Events;

public class RCShip : RCHoverboard
{
	[Header("RCShip - Events")]
	public UnityEvent OnFire;

	public UnityEvent<bool> OnCannonSideChanged;

	public UnityEvent OnMoveStarted;

	public UnityEvent OnMoveStopped;

	[Header("RCShip - Cannon Rotation")]
	[SerializeField]
	private Transform cannonTransform;

	[SerializeField]
	private float leftYaw = -45f;

	[SerializeField]
	private float rightYaw = 45f;

	[SerializeField]
	private float cannonYawSpeed = 240f;

	[Header("RCShip - Input")]
	[Range(0f, 1f)]
	[SerializeField]
	private float triggerPressThreshold = 0.6f;

	[Range(0f, 1f)]
	[SerializeField]
	private float triggerReleaseThreshold = 0.1f;

	[Range(0f, 1f)]
	[SerializeField]
	private float facePressThreshold = 0.6f;

	[Range(0f, 1f)]
	[SerializeField]
	private float faceReleaseThreshold = 0.1f;

	[Header("RCShip - Movement Detection")]
	[Tooltip("Minimum speed to consider the ship moving")]
	[SerializeField]
	private float movingSpeedThreshold = 0.05f;

	private bool prevTriggerDown;

	private bool prevFaceDown;

	private bool faceIsDown;

	private bool triggerIsDown;

	private bool armedAfterMobilize;

	private bool cannonToLeft;

	private const byte CannonLeftBit = 1;

	private const byte FireFlipBit = 2;

	private const byte MovingBit = 4;

	private bool lastFireFlip;

	private bool lastCannonToLeft;

	private bool lastIsMoving;

	private bool isMovingShared;

	private byte GetDataB()
	{
		if (!hasNetworkSync)
		{
			return 0;
		}
		return networkSync.syncedState.dataB;
	}

	private void SetDataB(byte b)
	{
		if (hasNetworkSync)
		{
			networkSync.syncedState.dataB = b;
		}
	}

	private void WriteCannonBit(bool toLeft)
	{
		if (hasNetworkSync)
		{
			byte dataB = GetDataB();
			dataB = (toLeft ? ((byte)(dataB | 1)) : ((byte)(dataB & -2)));
			SetDataB(dataB);
		}
	}

	private bool ReadCannonBit()
	{
		if (!hasNetworkSync)
		{
			return cannonToLeft;
		}
		return (GetDataB() & 1) != 0;
	}

	private bool ReadFireFlip()
	{
		return (GetDataB() & 2) != 0;
	}

	protected override void AuthorityUpdate(float dt)
	{
		base.AuthorityUpdate(dt);
		float trigger = activeInput.trigger;
		float num = (int)activeInput.buttons;
		if (localState == State.Mobilized && localStatePrev != State.Mobilized)
		{
			armedAfterMobilize = false;
			if (trigger >= triggerReleaseThreshold)
			{
				triggerIsDown = true;
			}
		}
		if (localState == State.Mobilized)
		{
			if (!armedAfterMobilize && trigger <= triggerReleaseThreshold)
			{
				armedAfterMobilize = true;
				triggerIsDown = false;
			}
			if (armedAfterMobilize)
			{
				if (!triggerIsDown && trigger >= triggerPressThreshold)
				{
					triggerIsDown = true;
					OnFire?.Invoke();
					if (hasNetworkSync)
					{
						byte dataB = GetDataB();
						dataB ^= 2;
						SetDataB(dataB);
						lastFireFlip = (dataB & 2) != 0;
					}
				}
				else if (triggerIsDown && trigger <= triggerReleaseThreshold)
				{
					triggerIsDown = false;
				}
			}
			if (!faceIsDown && num >= facePressThreshold)
			{
				faceIsDown = true;
				cannonToLeft = !cannonToLeft;
				WriteCannonBit(cannonToLeft);
			}
			else if (faceIsDown && num <= faceReleaseThreshold)
			{
				faceIsDown = false;
			}
		}
		else
		{
			if (faceIsDown && num <= faceReleaseThreshold)
			{
				faceIsDown = false;
			}
			armedAfterMobilize = false;
			if (triggerIsDown && trigger <= triggerReleaseThreshold)
			{
				triggerIsDown = false;
			}
		}
		if (hasNetworkSync)
		{
			byte dataB2 = GetDataB();
			if (localState == State.Mobilized && rb != null && rb.linearVelocity.sqrMagnitude >= movingSpeedThreshold * movingSpeedThreshold)
			{
				dataB2 |= 4;
				isMovingShared = true;
			}
			else
			{
				dataB2 = (byte)(dataB2 & -5);
				isMovingShared = false;
			}
			SetDataB(dataB2);
		}
		else
		{
			isMovingShared = localState == State.Mobilized && rb != null && rb.linearVelocity.sqrMagnitude >= movingSpeedThreshold * movingSpeedThreshold;
		}
	}

	protected override void RemoteUpdate(float dt)
	{
		base.RemoteUpdate(dt);
		if (!hasNetworkSync)
		{
			return;
		}
		cannonToLeft = ReadCannonBit();
		bool flag = ReadFireFlip();
		if (!base.HasLocalAuthority)
		{
			if (flag != lastFireFlip)
			{
				lastFireFlip = flag;
				OnFire?.Invoke();
			}
			byte dataB = GetDataB();
			isMovingShared = (dataB & 4) != 0;
		}
		else
		{
			lastFireFlip = flag;
			isMovingShared = localState == State.Mobilized && rb != null && rb.linearVelocity.sqrMagnitude >= movingSpeedThreshold * movingSpeedThreshold;
		}
	}

	protected override void SharedUpdate(float dt)
	{
		base.SharedUpdate(dt);
		if (cannonTransform != null)
		{
			float target = (cannonToLeft ? leftYaw : rightYaw);
			Vector3 localEulerAngles = cannonTransform.localEulerAngles;
			localEulerAngles.z = Mathf.MoveTowardsAngle(localEulerAngles.z, target, cannonYawSpeed * dt);
			cannonTransform.localEulerAngles = localEulerAngles;
		}
		if (cannonToLeft != lastCannonToLeft)
		{
			lastCannonToLeft = cannonToLeft;
			OnCannonSideChanged?.Invoke(cannonToLeft);
		}
		bool flag = localState == State.Mobilized && isMovingShared;
		if (flag != lastIsMoving)
		{
			lastIsMoving = flag;
			if (flag)
			{
				OnMoveStarted?.Invoke();
			}
			else
			{
				OnMoveStopped?.Invoke();
			}
		}
	}
}
