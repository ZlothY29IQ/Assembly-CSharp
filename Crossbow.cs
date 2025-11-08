using UnityEngine;

public class Crossbow : ProjectileWeapon
{
	[SerializeField]
	private Transform launchPosition;

	[SerializeField]
	private float launchSpeed;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private float crankTotalDegreesToReload;

	[SerializeField]
	private TransferrableObjectHoldablePart_Crank[] cranks;

	[SerializeField]
	private MeshRenderer dummyProjectile;

	[SerializeField]
	private AudioSource reloadAudio;

	[SerializeField]
	private AudioClip reloadComplete_audioClip;

	[SerializeField]
	private float crankSoundContinueDuration = 0.1f;

	[SerializeField]
	private float crankSoundDegreesThreshold = 0.1f;

	private AnimHashId FireHashID = "Fire";

	private AnimHashId ReloadFractionHashID = "ReloadFraction";

	private float totalCrankDegrees;

	private float loadFraction;

	private float playingCrankSoundUntilTimestamp;

	private float crankSoundDegrees;

	private bool wasPressingTrigger;

	protected override void Awake()
	{
		base.Awake();
		TransferrableObjectHoldablePart_Crank[] array = cranks;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetOnCrankedCallback(OnCrank);
		}
		SetReloadFraction(0f);
	}

	public void SetReloadFraction(float newFraction)
	{
		loadFraction = Mathf.Clamp01(newFraction);
		animator.SetFloat(ReloadFractionHashID, loadFraction);
		if (loadFraction == 1f && !dummyProjectile.enabled)
		{
			shootSfx.GTPlayOneShot(reloadComplete_audioClip);
			dummyProjectile.enabled = true;
		}
		else if (loadFraction < 1f && dummyProjectile.enabled)
		{
			dummyProjectile.enabled = false;
		}
	}

	private void OnCrank(float degrees)
	{
		if (loadFraction != 1f)
		{
			totalCrankDegrees += degrees;
			crankSoundDegrees += degrees;
			if (Mathf.Abs(crankSoundDegrees) > crankSoundDegreesThreshold)
			{
				playingCrankSoundUntilTimestamp = Time.time + crankSoundContinueDuration;
				crankSoundDegrees = 0f;
			}
			if (!reloadAudio.isPlaying && Time.time < playingCrankSoundUntilTimestamp)
			{
				reloadAudio.GTPlay();
			}
			SetReloadFraction(Mathf.Abs(totalCrankDegrees / crankTotalDegreesToReload));
			if (loadFraction >= 1f)
			{
				totalCrankDegrees = 0f;
			}
		}
	}

	protected override Vector3 GetLaunchPosition()
	{
		return launchPosition.position;
	}

	protected override Vector3 GetLaunchVelocity()
	{
		return launchPosition.forward * launchSpeed * base.myRig.scaleFactor;
	}

	protected override void LateUpdateLocal()
	{
		base.LateUpdateLocal();
		if (!InHand())
		{
			wasPressingTrigger = false;
			return;
		}
		if ((InLeftHand() ? base.myRig.leftIndex.calcT : base.myRig.rightIndex.calcT) > 0.5f)
		{
			if (loadFraction == 1f && !wasPressingTrigger)
			{
				SetReloadFraction(0f);
				animator.SetTrigger(FireHashID);
				LaunchProjectile();
			}
			wasPressingTrigger = true;
		}
		else
		{
			wasPressingTrigger = false;
		}
		if (itemState.HasFlag(ItemStates.State0))
		{
			if (loadFraction < 1f)
			{
				itemState &= (ItemStates)(-2);
			}
		}
		else if (loadFraction == 1f)
		{
			itemState |= ItemStates.State0;
		}
	}

	protected override void LateUpdateReplicated()
	{
		base.LateUpdateReplicated();
		if (InHand())
		{
			if (itemState.HasFlag(ItemStates.State0))
			{
				SetReloadFraction(1f);
			}
			else if (loadFraction == 1f)
			{
				SetReloadFraction(0f);
			}
		}
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		if (reloadAudio.isPlaying && Time.time > playingCrankSoundUntilTimestamp)
		{
			reloadAudio.GTStop();
		}
	}
}
