using UnityEngine;

public class GorillaTurnSlider : MonoBehaviour
{
	public float zRange;

	public float maxValue;

	public float minValue;

	public GorillaTurning gorillaTurn;

	private float startingZ;

	public Vector3 startingLocation;

	private void Awake()
	{
		startingLocation = base.transform.position;
		SetPosition(gorillaTurn.currentSpeed);
	}

	private void FixedUpdate()
	{
	}

	public void SetPosition(float speed)
	{
		float num = startingLocation.x - zRange / 2f;
		float num2 = startingLocation.x + zRange / 2f;
		float x = (speed - minValue) * (num2 - num) / (maxValue - minValue) + num;
		base.transform.position = new Vector3(x, startingLocation.y, startingLocation.z);
	}

	public float InterpolateValue(float value)
	{
		float num = startingLocation.x - zRange / 2f;
		float num2 = startingLocation.x + zRange / 2f;
		return (value - num) / (num2 - num) * (maxValue - minValue) + minValue;
	}

	public void OnSliderRelease()
	{
		if (zRange != 0f && (base.transform.position - startingLocation).magnitude > zRange / 2f)
		{
			if (base.transform.position.x > startingLocation.x)
			{
				base.transform.position = new Vector3(startingLocation.x + zRange / 2f, startingLocation.y, startingLocation.z);
			}
			else
			{
				base.transform.position = new Vector3(startingLocation.x - zRange / 2f, startingLocation.y, startingLocation.z);
			}
		}
	}
}
