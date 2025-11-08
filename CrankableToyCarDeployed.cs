using UnityEngine;

public class CrankableToyCarDeployed : MonoBehaviour
{
	[SerializeField]
	private Rigidbody rb;

	[SerializeField]
	private FakeWheelDriver wheelDriver;

	[SerializeField]
	private Vector3 maxThrust;

	[SerializeField]
	private AnimationCurve thrustCurve;

	private float startedAtTimestamp;

	private float expiresAtTimestamp;

	private CrankableToyCarHoldable holdable;

	[SerializeField]
	private AudioSource drivingAudio;

	[SerializeField]
	private AudioSource offGroundDrivingAudio;

	private bool isRemote;

	public void Deploy(CrankableToyCarHoldable holdable, Vector3 launchPos, Quaternion launchRot, Vector3 releaseVel, float lifetime, bool isRemote = false)
	{
		this.holdable = holdable;
		holdable.OnCarDeployed();
		base.transform.position = launchPos;
		base.transform.rotation = launchRot;
		base.transform.localScale = holdable.transform.lossyScale;
		rb.linearVelocity = releaseVel;
		startedAtTimestamp = Time.time;
		expiresAtTimestamp = Time.time + lifetime;
		this.isRemote = isRemote;
	}

	private void Update()
	{
		if (!isRemote && Time.time > expiresAtTimestamp)
		{
			if (holdable != null)
			{
				holdable.OnCarReturned();
			}
			return;
		}
		if (!wheelDriver.hasCollision)
		{
			expiresAtTimestamp -= Time.deltaTime;
			if (!offGroundDrivingAudio.isPlaying)
			{
				offGroundDrivingAudio.GTPlay();
				drivingAudio.Stop();
			}
		}
		else if (!drivingAudio.isPlaying)
		{
			drivingAudio.GTPlay();
			offGroundDrivingAudio.Stop();
		}
		float time = Mathf.InverseLerp(startedAtTimestamp, expiresAtTimestamp, Time.time);
		float num = thrustCurve.Evaluate(time);
		wheelDriver.SetThrust(maxThrust * num);
	}
}
