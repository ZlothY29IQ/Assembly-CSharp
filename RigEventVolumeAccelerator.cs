using GorillaLocomotion;
using UnityEngine;

[RequireComponent(typeof(RigEventVolume))]
public class RigEventVolumeAccelerator : MonoBehaviour
{
	private const float min_mult = 100f;

	private RigEventVolume rev;

	[SerializeField]
	private float multiplier;

	private void Awake()
	{
		rev = GetComponent<RigEventVolume>();
		if (Mathf.Abs(multiplier) < 100f)
		{
			if (multiplier < 0f)
			{
				multiplier = -100f;
			}
			else
			{
				multiplier = 100f;
			}
		}
	}

	private void FixedUpdate()
	{
		if (rev.LocalRigPresent)
		{
			GTPlayer.Instance.AddForce(GTPlayer.Instance.AveragedVelocity * multiplier * Time.fixedDeltaTime, ForceMode.Acceleration);
		}
	}
}
