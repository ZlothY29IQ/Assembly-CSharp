using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class HapticsWithDistance : MonoBehaviour, ITickSystemTick
{
	[SerializeField]
	[Tooltip("X is the normalized distance and should start at 0 and end at 1. Y is the vibration amplitude and can be anywhere from 0-1.")]
	private AnimationCurve vibrationIntensityByDistance;

	private float inverseColliderRadius;

	private float vibrationMult = 1f;

	private Transform leftOfflineHand;

	private Transform rightOfflineHand;

	public bool TickRunning { get; set; }

	private bool OnWrongLayer()
	{
		return base.gameObject.layer != 18;
	}

	public void SetVibrationMult(float mult)
	{
		vibrationMult = mult;
	}

	public void FingerFlexVibrationMult(bool dummy, float mult)
	{
		SetVibrationMult(mult);
	}

	private void Awake()
	{
		inverseColliderRadius = 1f / GetComponent<SphereCollider>().radius;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.TryGetComponent<GorillaGrabber>(out var component) && component.enabled)
		{
			if (component.IsLeftHand)
			{
				leftOfflineHand = component.transform;
				TickSystem<object>.AddTickCallback(this);
			}
			else if (component.IsRightHand)
			{
				rightOfflineHand = component.transform;
				TickSystem<object>.AddTickCallback(this);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (leftOfflineHand == other.transform)
		{
			leftOfflineHand = null;
			if (!rightOfflineHand)
			{
				TickSystem<object>.RemoveTickCallback(this);
			}
		}
		else if (rightOfflineHand == other.transform)
		{
			rightOfflineHand = null;
			if (!leftOfflineHand)
			{
				TickSystem<object>.RemoveTickCallback(this);
			}
		}
	}

	private void OnDisable()
	{
		leftOfflineHand = null;
		rightOfflineHand = null;
		TickSystem<object>.RemoveTickCallback(this);
	}

	public void Tick()
	{
		Vector3 position = base.transform.position;
		if ((bool)leftOfflineHand)
		{
			GorillaTagger.Instance.StartVibration(forLeftController: true, vibrationMult * vibrationIntensityByDistance.Evaluate(Vector3.Distance(leftOfflineHand.position, position) * inverseColliderRadius), Time.deltaTime);
		}
		if ((bool)rightOfflineHand)
		{
			GorillaTagger.Instance.StartVibration(forLeftController: false, vibrationMult * vibrationIntensityByDistance.Evaluate(Vector3.Distance(rightOfflineHand.position, position) * inverseColliderRadius), Time.deltaTime);
		}
	}
}
