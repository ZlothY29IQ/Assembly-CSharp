using UnityEngine;

public class CosmeticFan : MonoBehaviour
{
	[SerializeField]
	private Vector3 axis;

	[SerializeField]
	private float spinUpDuration = 0.3f;

	[SerializeField]
	private float spinDownDuration = 0.3f;

	[SerializeField]
	private float maxSpeed = 360f;

	private float currentSpeed;

	private float targetSpeed;

	private float currentAccelRate;

	private float spinUpRate;

	private float spinDownRate;

	private void Start()
	{
		spinUpRate = maxSpeed / spinUpDuration;
		spinDownRate = maxSpeed / spinDownDuration;
	}

	public void Run()
	{
		targetSpeed = maxSpeed;
		if (spinUpDuration > 0f)
		{
			base.enabled = true;
			currentAccelRate = spinUpRate;
		}
		else
		{
			currentSpeed = maxSpeed;
		}
		base.enabled = true;
	}

	public void Stop()
	{
		targetSpeed = 0f;
		if (spinDownDuration > 0f)
		{
			base.enabled = true;
			currentAccelRate = spinDownRate;
		}
		else
		{
			currentSpeed = 0f;
		}
	}

	public void InstantStop()
	{
		targetSpeed = 0f;
		currentSpeed = 0f;
		base.enabled = false;
	}

	private void Update()
	{
		currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, currentAccelRate * Time.deltaTime);
		base.transform.localRotation = base.transform.localRotation * Quaternion.AngleAxis(currentSpeed * Time.deltaTime, axis);
		if (currentSpeed == 0f && targetSpeed == 0f)
		{
			base.enabled = false;
		}
	}
}
