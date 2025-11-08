using GorillaExtensions;
using UnityEngine;

public class SpinWithGorillaSpeed : MonoBehaviour
{
	[Tooltip("Get the velocity from this component when determining the spin speed. If this is unset, it will use the unsmoothed velocity of the parent VRRig component.")]
	[SerializeField]
	private GorillaVelocityEstimator optionalVelocityEstimator;

	[SerializeField]
	private Quaternion axisOfRotation = Quaternion.identity;

	[SerializeField]
	private Vector3 centerOfRotation = Vector3.zero;

	[Tooltip("The reported speed will be divided by this value before being used to sample AnimationCurves, to allow them to be in the range 0-1.")]
	[SerializeField]
	private float maxSpeed;

	[SerializeField]
	private AnimationCurve degreesPerSecondAtSpeed;

	[SerializeField]
	private bool clockwise;

	[Tooltip("The Y component of the reported speed will be multiplied by this value. At 0, falling will have no effect on the rotation speed.")]
	[SerializeField]
	private float verticalSpeedInfluence = 1f;

	[Header("Ticking sound")]
	[Tooltip("After this many degrees of rotation, a \"tick\" sound will play.")]
	[SerializeField]
	private float tickSoundDegrees = 360f;

	[SerializeField]
	private AnimationCurve tickVolumeAtSpeed;

	[SerializeField]
	private AnimationCurve tickPitchAtSpeed;

	[SerializeField]
	private AudioSource tickSound;

	[SerializeField]
	private AudioClip[] tickClips;

	private VRRig rig;

	private Quaternion initialRotation;

	private Vector3 spinAxis;

	private float currentAngle;

	private float tickAngle;

	private void Awake()
	{
		rig = GetComponentInParent<VRRig>();
		initialRotation = base.transform.localRotation;
		spinAxis = initialRotation * axisOfRotation * Vector3.forward;
	}

	private void Update()
	{
		Vector3 vector = ((optionalVelocityEstimator != null) ? optionalVelocityEstimator.linearVelocity : rig.LatestVelocity());
		vector.y *= verticalSpeedInfluence;
		float time = vector.magnitude / maxSpeed;
		float num = Time.deltaTime * degreesPerSecondAtSpeed.Evaluate(time) * (clockwise ? (-1f) : 1f);
		currentAngle = Mathf.Repeat(currentAngle + num, 360f);
		Quaternion quaternion = initialRotation * Quaternion.AngleAxis(currentAngle, spinAxis);
		base.transform.SetLocalPositionAndRotation(quaternion * centerOfRotation, quaternion);
		if (tickSound != null && tickClips.Length != 0)
		{
			tickAngle += num;
			if (tickAngle >= tickSoundDegrees)
			{
				tickSound.pitch = tickPitchAtSpeed.Evaluate(time);
				tickSound.volume = tickVolumeAtSpeed.Evaluate(time);
				tickSound.clip = tickClips.GetRandomItem();
				tickSound.GTPlay();
				tickAngle = Mathf.Repeat(tickAngle, tickSoundDegrees);
			}
		}
	}

	private void OnDisable()
	{
		currentAngle = 0f;
		tickAngle = 0f;
	}
}
