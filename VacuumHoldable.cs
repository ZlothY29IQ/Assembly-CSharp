using UnityEngine;

public class VacuumHoldable : TransferrableObject
{
	private enum VacuumState
	{
		None = 1,
		Active
	}

	[Tooltip("Emission rate will be increase when the trigger button is pressed.")]
	public ParticleSystem particleFX;

	[Tooltip("Sound will loop and fade in/out volume when trigger pressed.")]
	public AudioSource audioSource;

	private float activationVibrationStartStrength = 0.8f;

	private float activationVibrationStartDuration = 0.05f;

	private float activationVibrationLoopStrength = 0.005f;

	private float activationStartTime;

	private bool hasAudioSource;

	public override void OnSpawn(VRRig rig)
	{
		base.OnSpawn(rig);
		itemState = ItemStates.State0;
	}

	internal override void OnEnable()
	{
		base.OnEnable();
		itemState = ItemStates.State0;
		hasAudioSource = audioSource != null && audioSource.clip != null;
	}

	internal override void OnDisable()
	{
		base.OnDisable();
		itemState = ItemStates.State0;
		if (particleFX.isPlaying)
		{
			particleFX.Stop();
		}
		if (hasAudioSource && audioSource.isPlaying)
		{
			audioSource.GTStop();
		}
	}

	private void InitToDefault()
	{
		itemState = ItemStates.State0;
		if (particleFX.isPlaying)
		{
			particleFX.Stop();
		}
		if (hasAudioSource && audioSource.isPlaying)
		{
			audioSource.GTStop();
		}
	}

	public override void ResetToDefaultState()
	{
		base.ResetToDefaultState();
		InitToDefault();
	}

	protected override void LateUpdateShared()
	{
		base.LateUpdateShared();
		if (!IsMyItem() && base.myOnlineRig != null && base.myOnlineRig.muted)
		{
			itemState = ItemStates.State0;
		}
		if (itemState == ItemStates.State0)
		{
			if (particleFX.isPlaying)
			{
				particleFX.Stop();
			}
			if (hasAudioSource && audioSource.isPlaying)
			{
				audioSource.GTStop();
			}
			return;
		}
		if (!particleFX.isEmitting)
		{
			particleFX.Play();
		}
		if (hasAudioSource && !audioSource.isPlaying)
		{
			audioSource.GTPlay();
		}
		if (IsMyItem() && Time.time > activationStartTime + activationVibrationStartDuration)
		{
			GorillaTagger.Instance.StartVibration(currentState == PositionState.InLeftHand, activationVibrationLoopStrength, Time.deltaTime);
		}
	}

	public override void OnActivate()
	{
		base.OnActivate();
		itemState = ItemStates.State1;
		if (IsMyItem())
		{
			activationStartTime = Time.time;
			GorillaTagger.Instance.StartVibration(currentState == PositionState.InLeftHand, activationVibrationStartStrength, activationVibrationStartDuration);
		}
	}

	public override void OnDeactivate()
	{
		base.OnDeactivate();
		itemState = ItemStates.State0;
	}
}
