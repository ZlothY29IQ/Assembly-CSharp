using GorillaTagScripts.Builder;
using Photon.Pun;
using UnityEngine;

public class ArtilleryCannon : MonoBehaviour
{
	[Header("Network State")]
	[SerializeField]
	private XSceneRef stateRef;

	[Header("Cranks")]
	[SerializeField]
	private ArtilleryCrank pitchCrank;

	[SerializeField]
	private ArtilleryCrank yawCrank;

	[Header("Rotation")]
	[SerializeField]
	private Transform yawTransform;

	[SerializeField]
	private Transform pitchTransform;

	[Header("Firing")]
	[SerializeField]
	private Transform muzzle;

	[SerializeField]
	private GameObject projectilePrefab;

	[SerializeField]
	private float launchSpeed = 30f;

	[SerializeField]
	private AudioSource fireSound;

	[SerializeField]
	private SlingshotProjectile.AOEKnockbackConfig knockbackConfig;

	[Header("Fire Trigger")]
	[Tooltip("When a projectile hits this notifier, the cannon fires.")]
	[SerializeField]
	private SlingshotProjectileHitNotifier fireHitNotifier;

	private ArtilleryCannonState state;

	private int projectileHash;

	private int LocalActorNr
	{
		get
		{
			if (PhotonNetwork.LocalPlayer == null)
			{
				return -1;
			}
			return PhotonNetwork.LocalPlayer.ActorNumber;
		}
	}

	private void Awake()
	{
		if (projectilePrefab != null)
		{
			projectileHash = PoolUtils.GameObjHashCode(projectilePrefab);
		}
	}

	private void OnEnable()
	{
		if (fireHitNotifier != null)
		{
			fireHitNotifier.OnProjectileHit += OnFireProjectileHit;
		}
		if (stateRef.TryResolve(out ArtilleryCannonState result))
		{
			Bind(result);
		}
		else
		{
			stateRef.AddCallbackOnLoad(OnStateSceneLoaded);
		}
	}

	private void OnDisable()
	{
		if (fireHitNotifier != null)
		{
			fireHitNotifier.OnProjectileHit -= OnFireProjectileHit;
		}
		stateRef.RemoveCallbackOnLoad(OnStateSceneLoaded);
		Unbind();
	}

	private void OnStateSceneLoaded()
	{
		if (stateRef.TryResolve(out ArtilleryCannonState result))
		{
			Bind(result);
		}
	}

	private void Bind(ArtilleryCannonState newState)
	{
		if (!(state == newState))
		{
			Unbind();
			state = newState;
			if (!(state == null))
			{
				state.onRotationChanged += OnRotationChanged;
				state.onFired += OnFiredRemote;
				ApplyRotation();
			}
		}
	}

	private void Unbind()
	{
		if (!(state == null))
		{
			state.onRotationChanged -= OnRotationChanged;
			state.onFired -= OnFiredRemote;
			state = null;
		}
	}

	private void LateUpdate()
	{
		if (!(state == null))
		{
			int localActorNr = LocalActorNr;
			if (pitchCrank != null && state.pitchCrankSync.holderActorNr == localActorNr)
			{
				state.UpdateLocalCrankState(0, pitchCrank.IsHeldLeftHand, pitchCrank.CurrentAngle);
			}
			if (yawCrank != null && state.yawCrankSync.holderActorNr == localActorNr)
			{
				state.UpdateLocalCrankState(1, yawCrank.IsHeldLeftHand, yawCrank.CurrentAngle);
			}
			UpdateRemoteCrankVisual(pitchCrank, state.pitchCrankSync, localActorNr);
			UpdateRemoteCrankVisual(yawCrank, state.yawCrankSync, localActorNr);
		}
	}

	private void UpdateRemoteCrankVisual(ArtilleryCrank crank, ArtilleryCannonState.CrankSyncState syncState, int localActor)
	{
		if (crank == null || syncState.holderActorNr == localActor)
		{
			return;
		}
		if (syncState.holderActorNr != -1)
		{
			VRRig vRRig = ArtilleryCannonState.FindRigForActor(syncState.holderActorNr);
			if (vRRig != null)
			{
				crank.UpdateFromRemoteHand(vRRig, syncState.isLeftHand);
				return;
			}
		}
		crank.SetVisualAngle(syncState.angle);
	}

	internal bool IsCrankHeldLocally(int crankIndex)
	{
		return (crankIndex == 0 ? ref state.pitchCrankSync : ref state.yawCrankSync).holderActorNr == LocalActorNr;
	}

	internal bool OnCrankGrabbed(int crankIndex, bool isLeftHand)
	{
		return state.NotifyCrankGrabbed(crankIndex, isLeftHand);
	}

	internal void OnCrankReleased(int crankIndex, float finalAngle)
	{
		state.NotifyCrankReleased(crankIndex, finalAngle);
	}

	internal void OnCrankInput(int crankIndex, float degrees)
	{
		state.NotifyCrankInput(crankIndex, degrees);
		ApplyRotation();
	}

	private void OnRotationChanged()
	{
		ApplyRotation();
	}

	private void ApplyRotation()
	{
		if (!(state == null))
		{
			if (yawTransform != null)
			{
				yawTransform.localRotation = Quaternion.Euler(0f, state.CurrentYaw, 0f);
			}
			if (pitchTransform != null)
			{
				pitchTransform.localRotation = Quaternion.Euler(0f - state.CurrentPitch, 0f, 0f);
			}
		}
	}

	public void Fire()
	{
		if (!(state == null) && state.TryFire())
		{
			FireLocal();
		}
	}

	private void OnFireProjectileHit(SlingshotProjectile projectile, Collision collision)
	{
		Fire();
	}

	private void OnFiredRemote()
	{
		FireLocal();
	}

	private void FireLocal()
	{
		if (!(projectilePrefab == null) && !(muzzle == null))
		{
			Vector3 position = muzzle.position;
			Vector3 forward = muzzle.forward;
			GameObject obj = ObjectPools.instance.Instantiate(projectileHash);
			obj.transform.position = position;
			obj.transform.rotation = Quaternion.LookRotation(forward);
			BuilderProjectile component = obj.GetComponent<BuilderProjectile>();
			if (component != null)
			{
				component.aoeKnockbackConfig = knockbackConfig;
			}
			Rigidbody component2 = obj.GetComponent<Rigidbody>();
			if (component2 != null)
			{
				component2.linearVelocity = forward * launchSpeed;
			}
			if (fireSound != null)
			{
				fireSound.GTPlay();
			}
		}
	}
}
