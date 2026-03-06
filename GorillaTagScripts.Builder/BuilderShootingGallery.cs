using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderShootingGallery : MonoBehaviour, IBuilderPieceComponent, IBuilderPieceFunctional
{
	private enum FunctionalState
	{
		Idle,
		HitWheel,
		HitCowboy
	}

	public BuilderPiece myPiece;

	[SerializeField]
	private Transform wheelTransform;

	[SerializeField]
	private Transform cowboyTransform;

	[SerializeField]
	private SlingshotProjectileHitNotifier wheelHitNotifier;

	[SerializeField]
	private SlingshotProjectileHitNotifier cowboyHitNotifier;

	[SerializeField]
	protected List<Collider> colliders;

	[SerializeField]
	protected SoundBankPlayer wheelHitSound;

	[SerializeField]
	protected Animation wheelHitAnimation;

	[SerializeField]
	protected SoundBankPlayer cowboyHitSound;

	[SerializeField]
	private Animation cowboyHitAnimation;

	[SerializeField]
	private float hitCooldown = 1f;

	private double lastHitTime;

	private FunctionalState currentState;

	private bool activated;

	[SerializeField]
	private float cowboyVelocity;

	[SerializeField]
	private Transform cowboyStart;

	[SerializeField]
	private Transform cowboyEnd;

	[SerializeField]
	private AnimationCurve cowboyCurve;

	[SerializeField]
	private float wheelVelocity;

	private Quaternion cowboyInitLocalRotation = Quaternion.identity;

	private Vector3 cowboyInitLocalPos = Vector3.zero;

	private Quaternion wheelInitLocalRot = Quaternion.identity;

	private float cowboyCycleDuration;

	private float wheelCycleDuration;

	private float distance;

	private float currT;

	private bool currForward;

	private float dtSinceServerUpdate;

	private int lastServerTimeStamp;

	private float rotateStartAmt;

	private float rotateAmt;

	private void Awake()
	{
		foreach (Collider collider in colliders)
		{
			collider.contactOffset = 0.0001f;
		}
		wheelHitNotifier.OnProjectileHit += OnWheelHit;
		cowboyHitNotifier.OnProjectileHit += OnCowboyHit;
	}

	private void OnDestroy()
	{
		wheelHitNotifier.OnProjectileHit -= OnWheelHit;
		cowboyHitNotifier.OnProjectileHit -= OnCowboyHit;
	}

	private void OnWheelHit(SlingshotProjectile projectile, Collision collision)
	{
		if (myPiece.state == BuilderPiece.State.AttachedAndPlaced && projectile.projectileOwner != null && projectile.projectileOwner == NetworkSystem.Instance.LocalPlayer && lastHitTime + (double)hitCooldown < (double)Time.time)
		{
			myPiece.GetTable().builderNetworking.RequestFunctionalPieceStateChange(myPiece.pieceId, 1);
		}
	}

	private void OnCowboyHit(SlingshotProjectile projectile, Collision collision)
	{
		if (myPiece.state == BuilderPiece.State.AttachedAndPlaced && projectile.projectileOwner != null && projectile.projectileOwner == NetworkSystem.Instance.LocalPlayer && lastHitTime + (double)hitCooldown < (double)Time.time)
		{
			myPiece.GetTable().builderNetworking.RequestFunctionalPieceStateChange(myPiece.pieceId, 2);
		}
	}

	private void CowboyHitEffects()
	{
		if (cowboyHitSound != null)
		{
			cowboyHitSound.Play();
		}
		if (cowboyHitAnimation != null && cowboyHitAnimation.clip != null)
		{
			cowboyHitAnimation.Play();
		}
	}

	private void WheelHitEffects()
	{
		if (wheelHitSound != null)
		{
			wheelHitSound.Play();
		}
		if (wheelHitAnimation != null && wheelHitAnimation.clip != null)
		{
			wheelHitAnimation.Play();
		}
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
		currentState = FunctionalState.Idle;
		cowboyInitLocalPos = cowboyTransform.transform.localPosition;
		cowboyInitLocalRotation = cowboyTransform.transform.localRotation;
		wheelInitLocalRot = wheelTransform.transform.localRotation;
		distance = Vector3.Distance(cowboyStart.position, cowboyEnd.position);
		cowboyCycleDuration = distance / (cowboyVelocity * myPiece.GetScale());
		wheelCycleDuration = 1f / wheelVelocity;
	}

	public void OnPieceDestroy()
	{
	}

	public void OnPiecePlacementDeserialized()
	{
		if (!activated && myPiece.state == BuilderPiece.State.AttachedAndPlaced)
		{
			myPiece.GetTable().RegisterFunctionalPieceFixedUpdate(this);
			activated = true;
		}
	}

	public void OnPieceActivate()
	{
		cowboyTransform.SetLocalPositionAndRotation(cowboyInitLocalPos, cowboyInitLocalRotation);
		wheelTransform.SetLocalPositionAndRotation(wheelTransform.localPosition, wheelInitLocalRot);
		if (!activated)
		{
			myPiece.GetTable().RegisterFunctionalPieceFixedUpdate(this);
			activated = true;
		}
	}

	public void OnPieceDeactivate()
	{
		if (currentState != FunctionalState.Idle)
		{
			myPiece.SetFunctionalPieceState(0, NetworkSystem.Instance.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			myPiece.GetTable().UnregisterFunctionalPiece(this);
		}
		if (activated)
		{
			myPiece.GetTable().UnregisterFunctionalPieceFixedUpdate(this);
			activated = false;
		}
		cowboyTransform.SetLocalPositionAndRotation(cowboyInitLocalPos, cowboyInitLocalRotation);
		wheelTransform.SetLocalPositionAndRotation(wheelTransform.localPosition, wheelInitLocalRot);
	}

	public void OnStateChanged(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (instigator != null && IsStateValid(newState))
		{
			if (newState == 1 && currentState == FunctionalState.Idle)
			{
				lastHitTime = Time.time;
				WheelHitEffects();
				myPiece.GetTable().RegisterFunctionalPiece(this);
			}
			else if (newState == 2 && currentState == FunctionalState.Idle)
			{
				lastHitTime = Time.time;
				CowboyHitEffects();
				myPiece.GetTable().RegisterFunctionalPiece(this);
			}
			currentState = (FunctionalState)newState;
		}
	}

	public void OnStateRequest(byte newState, NetPlayer instigator, int timeStamp)
	{
		if (NetworkSystem.Instance.IsMasterClient && IsStateValid(newState) && instigator != null && lastHitTime + (double)hitCooldown < (double)Time.time)
		{
			myPiece.GetTable().builderNetworking.FunctionalPieceStateChangeMaster(myPiece.pieceId, newState, instigator.GetPlayerRef(), timeStamp);
		}
	}

	public bool IsStateValid(byte state)
	{
		return state <= 2;
	}

	public void FunctionalPieceUpdate()
	{
		if (lastHitTime + (double)hitCooldown < (double)Time.time)
		{
			myPiece.SetFunctionalPieceState(0, NetworkSystem.Instance.LocalPlayer, NetworkSystem.Instance.ServerTimestamp);
			myPiece.GetTable().UnregisterFunctionalPiece(this);
		}
	}

	public void FunctionalPieceFixedUpdate()
	{
		if (myPiece.state == BuilderPiece.State.AttachedAndPlaced)
		{
			currT = CowboyCycleCompletionPercent();
			currForward = IsEvenCycle();
			float time = (currForward ? currT : (1f - currT));
			float num = WheelCycleCompletionPercent();
			float t = cowboyCurve.Evaluate(time);
			cowboyTransform.localPosition = Vector3.Lerp(cowboyStart.localPosition, cowboyEnd.localPosition, t);
			Quaternion localRotation = Quaternion.AngleAxis(num * 360f, Vector3.right);
			wheelTransform.localRotation = localRotation;
		}
	}

	private long NetworkTimeMs()
	{
		if (PhotonNetwork.InRoom)
		{
			return (uint)(PhotonNetwork.ServerTimestamp + int.MinValue);
		}
		return (long)(Time.time * 1000f);
	}

	private long CowboyCycleLengthMs()
	{
		return (long)(cowboyCycleDuration * 1000f);
	}

	private long WheelCycleLengthMs()
	{
		return (long)(wheelCycleDuration * 1000f);
	}

	public double CowboyPlatformTime()
	{
		long num = NetworkTimeMs();
		long num2 = CowboyCycleLengthMs();
		return (double)(num - num / num2 * num2) / 1000.0;
	}

	public double WheelPlatformTime()
	{
		long num = NetworkTimeMs();
		long num2 = WheelCycleLengthMs();
		return (double)(num - num / num2 * num2) / 1000.0;
	}

	public int CowboyCycleCount()
	{
		return (int)(NetworkTimeMs() / CowboyCycleLengthMs());
	}

	public float CowboyCycleCompletionPercent()
	{
		return Mathf.Clamp((float)(CowboyPlatformTime() / (double)cowboyCycleDuration), 0f, 1f);
	}

	public float WheelCycleCompletionPercent()
	{
		return Mathf.Clamp((float)(WheelPlatformTime() / (double)wheelCycleDuration), 0f, 1f);
	}

	public bool IsEvenCycle()
	{
		return CowboyCycleCount() % 2 == 0;
	}
}
