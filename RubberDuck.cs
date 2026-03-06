using System;
using GorillaExtensions;
using GorillaTag;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

public class RubberDuck : TransferrableObject
{
	[DebugOption]
	public bool disableActivation;

	[DebugOption]
	public bool disableDeactivation;

	private SkinnedMeshRenderer skinRenderer;

	[FormerlySerializedAs("duckieLerp")]
	public float blendShapeMaxWeight = 1f;

	private int tempHandPos;

	[GorillaSoundLookup]
	[SerializeField]
	private int squeezeSound = 75;

	[GorillaSoundLookup]
	[SerializeField]
	private int squeezeReleaseSound = 76;

	[GorillaSoundLookup]
	public int[] squeezeSoundBank;

	[GorillaSoundLookup]
	public int[] squeezeReleaseSoundBank;

	public float squeezeStrength = 0.05f;

	public float releaseStrength = 0.03f;

	public ParticleSystem particleFX;

	[Tooltip("The emission rate of the particle effect when not squeezed.")]
	public float particleFXEmissionIdle = 0.8f;

	[Tooltip("The emission rate of the particle effect when squeezed.")]
	public float particleFXEmissionSqueeze = 10f;

	[Tooltip("The animation of the particle effect returning to the idle emission rate. X axis is time, Y axis is the emission lerp value where 0 is idle, 1 is squeezed.")]
	public AnimationCurve particleFXEmissionCooldownCurve;

	private bool hasSkinRenderer;

	private ParticleSystem.EmissionModule pFXEmissionModule;

	private bool hasParticleFX;

	private float squeezeTimeElapsed;

	[SerializeField]
	private RubberDuckEvents _events;

	[SerializeField]
	private bool _raiseActivate = true;

	[SerializeField]
	private bool _raiseDeactivate = true;

	[SerializeField]
	private SoundEffects _sfxActivate;

	[SerializeField]
	private bool _fxActive;

	public bool fxActive
	{
		get
		{
			if (hasParticleFX)
			{
				return _fxActive;
			}
			return false;
		}
		set
		{
			if (hasParticleFX)
			{
				pFXEmissionModule.enabled = value;
				_fxActive = value;
			}
		}
	}

	public int SqueezeSound
	{
		get
		{
			if (squeezeSoundBank.Length > 1)
			{
				return squeezeSoundBank[UnityEngine.Random.Range(0, squeezeSoundBank.Length)];
			}
			if (squeezeSoundBank.Length == 1)
			{
				return squeezeSoundBank[0];
			}
			return squeezeSound;
		}
	}

	public int SqueezeReleaseSound
	{
		get
		{
			if (squeezeReleaseSoundBank.Length > 1)
			{
				return squeezeReleaseSoundBank[UnityEngine.Random.Range(0, squeezeReleaseSoundBank.Length)];
			}
			if (squeezeReleaseSoundBank.Length == 1)
			{
				return squeezeReleaseSoundBank[0];
			}
			return squeezeReleaseSound;
		}
	}

	public override void OnSpawn(VRRig rig)
	{
		base.OnSpawn(rig);
		if (skinRenderer == null)
		{
			skinRenderer = GetComponentInChildren<SkinnedMeshRenderer>(includeInactive: true);
		}
		hasSkinRenderer = skinRenderer != null;
		myThreshold = 0.7f;
		hysterisis = 0.3f;
		hasParticleFX = particleFX != null;
		if (hasParticleFX)
		{
			pFXEmissionModule = particleFX.emission;
			pFXEmissionModule.rateOverTime = particleFXEmissionIdle;
		}
		fxActive = false;
	}

	internal override void OnEnable()
	{
		base.OnEnable();
		if (_events == null)
		{
			_events = base.gameObject.GetOrAddComponent<RubberDuckEvents>();
			NetPlayer netPlayer = ((base.myOnlineRig != null) ? base.myOnlineRig.creator : ((!(base.myRig != null)) ? null : ((base.myRig.creator != null) ? base.myRig.creator : NetworkSystem.Instance.LocalPlayer)));
			if (netPlayer != null)
			{
				_events.Init(netPlayer);
			}
			else
			{
				Debug.LogError("Failed to get a reference to the Photon Player needed to hook up the cosmetic event");
			}
		}
		if (_events != null)
		{
			_events.Activate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnSqueezeActivate);
			_events.Deactivate += new Action<int, int, object[], PhotonMessageInfoWrapped>(OnSqueezeDeactivate);
		}
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		if (_events != null)
		{
			_events.Activate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnSqueezeActivate);
			_events.Deactivate -= new Action<int, int, object[], PhotonMessageInfoWrapped>(OnSqueezeDeactivate);
			_events.Dispose();
			_events = null;
		}
	}

	private void OnSqueezeActivate(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender == target && info.senderID == ownerRig.creator.ActorNumber)
		{
			SqueezeActivateLocal();
		}
	}

	private void SqueezeActivateLocal()
	{
		PlayParticleFX(particleFXEmissionSqueeze);
		if ((bool)_sfxActivate && !_sfxActivate.isPlaying)
		{
			_sfxActivate.PlayNext();
		}
	}

	private void OnSqueezeDeactivate(int sender, int target, object[] args, PhotonMessageInfoWrapped info)
	{
		if (sender == target)
		{
			MonkeAgent.IncrementRPCCall(info, "OnSqueezeDeactivate");
			if (info.senderID == ownerRig.creator.ActorNumber)
			{
				SqueezeDeactivateLocal();
			}
		}
	}

	private void SqueezeDeactivateLocal()
	{
		PlayParticleFX(particleFXEmissionIdle);
	}

	public override void TriggeredLateUpdate()
	{
		base.TriggeredLateUpdate();
		float num = 0f;
		if (InHand())
		{
			tempHandPos = ((base.myOnlineRig != null) ? base.myOnlineRig.ReturnHandPosition() : base.myRig.ReturnHandPosition());
			num = ((currentState != PositionState.InLeftHand) ? ((float)Mathf.FloorToInt((float)(tempHandPos % 10) / 1f)) : ((float)Mathf.FloorToInt((float)(tempHandPos % 10000) / 1000f)));
		}
		if (hasSkinRenderer)
		{
			skinRenderer.SetBlendShapeWeight(0, Mathf.Lerp(skinRenderer.GetBlendShapeWeight(0), num * 11.1f, blendShapeMaxWeight));
		}
		if (fxActive)
		{
			squeezeTimeElapsed += Time.deltaTime;
			pFXEmissionModule.rateOverTime = Mathf.Lerp(particleFXEmissionIdle, particleFXEmissionSqueeze, particleFXEmissionCooldownCurve.Evaluate(squeezeTimeElapsed));
			if (squeezeTimeElapsed > particleFXEmissionSqueeze)
			{
				fxActive = false;
			}
		}
	}

	public override void OnActivate()
	{
		base.OnActivate();
		if (IsMyItem())
		{
			bool flag = currentState == PositionState.InLeftHand;
			RigContainer localRig = VRRigCache.Instance.localRig;
			int num = SqueezeSound;
			localRig.Rig.PlayHandTapLocal(num, flag, 0.33f);
			if ((bool)localRig.netView)
			{
				localRig.netView.SendRPC("RPC_PlayHandTap", RpcTarget.Others, num, flag, 0.33f);
			}
			GorillaTagger.Instance.StartVibration(flag, squeezeStrength, Time.deltaTime);
		}
		if (_raiseActivate)
		{
			if (RoomSystem.JoinedRoom)
			{
				_events?.Activate?.RaiseAll();
			}
			else
			{
				SqueezeActivateLocal();
			}
		}
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();
		if (IsMyItem())
		{
			bool flag = currentState == PositionState.InLeftHand;
			int num = SqueezeReleaseSound;
			Debug.Log("Squeezy Deactivate: " + num);
			VRRigCache.Instance.localRig.Rig.PlayHandTapLocal(num, flag, 0.33f);
			if ((bool)GorillaGameManager.instance && VRRigCache.Instance.TryGetVrrig(NetworkSystem.Instance.LocalPlayer, out var playerRig))
			{
				playerRig.Rig.netView.SendRPC("RPC_PlayHandTap", RpcTarget.Others, num, flag, 0.33f);
			}
			GorillaTagger.Instance.StartVibration(flag, releaseStrength, Time.deltaTime);
		}
		if (_raiseDeactivate)
		{
			if (RoomSystem.JoinedRoom)
			{
				_events?.Deactivate?.RaiseAll();
			}
			else
			{
				SqueezeDeactivateLocal();
			}
		}
	}

	public void PlayParticleFX(float rate)
	{
		if (hasParticleFX && (currentState == PositionState.InLeftHand || currentState == PositionState.InRightHand))
		{
			if (!fxActive)
			{
				fxActive = true;
			}
			squeezeTimeElapsed = 0f;
			pFXEmissionModule.rateOverTime = rate;
		}
	}

	public override bool CanActivate()
	{
		return !disableActivation;
	}

	public override bool CanDeactivate()
	{
		return !disableDeactivation;
	}
}
