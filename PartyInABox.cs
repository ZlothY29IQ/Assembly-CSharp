using System;
using UnityEngine;

public class PartyInABox : MonoBehaviour
{
	[Serializable]
	private struct ForceTransform
	{
		public Transform transform;

		public Vector3 localPosition;

		public Quaternion localRotation;

		public void Apply()
		{
			transform.localPosition = localPosition;
			transform.localRotation = localRotation;
		}
	}

	[SerializeField]
	private TransferrableObject parentHoldable;

	[SerializeField]
	private ParticleSystem particles;

	[SerializeField]
	private Animation anim;

	[SerializeField]
	private SpringyWobbler spring;

	[SerializeField]
	private AudioSource partyAudio;

	[SerializeField]
	private float partyHapticStrength;

	[SerializeField]
	private float partyHapticDuration;

	private bool isReleased;

	[SerializeField]
	private ForceTransform[] forceTransforms;

	private void Awake()
	{
		Reset();
	}

	private void OnEnable()
	{
		Reset();
	}

	public void Cranked_ReleaseParty()
	{
		if (parentHoldable.IsLocalObject())
		{
			ReleaseParty();
		}
	}

	public void ReleaseParty()
	{
		if (!isReleased)
		{
			if (parentHoldable.IsLocalObject())
			{
				parentHoldable.itemState |= TransferrableObject.ItemStates.State0;
				GorillaTagger.Instance.StartVibration(forLeftController: true, partyHapticStrength, partyHapticDuration);
				GorillaTagger.Instance.StartVibration(forLeftController: false, partyHapticStrength, partyHapticDuration);
			}
			isReleased = true;
			spring.enabled = true;
			anim.Play();
			particles.Play();
			partyAudio.Play();
		}
	}

	private void Update()
	{
		if (parentHoldable.IsLocalObject())
		{
			return;
		}
		if (parentHoldable.itemState.HasFlag(TransferrableObject.ItemStates.State0))
		{
			if (!isReleased)
			{
				ReleaseParty();
			}
		}
		else if (isReleased)
		{
			Reset();
		}
	}

	public void Reset()
	{
		isReleased = false;
		parentHoldable.itemState &= (TransferrableObject.ItemStates)(-2);
		spring.enabled = false;
		anim.Stop();
		ForceTransform[] array = forceTransforms;
		foreach (ForceTransform forceTransform in array)
		{
			forceTransform.Apply();
		}
	}
}
