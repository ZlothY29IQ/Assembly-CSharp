using UnityEngine;

public class Pendulum : MonoBehaviour
{
	public float MaxAngleDeflection = 10f;

	public float SpeedOfPendulum = 1f;

	public Transform ClockPendulum;

	private Transform pendulum;

	private void Start()
	{
		pendulum = (ClockPendulum = base.gameObject.GetComponent<Transform>());
	}

	private void Update()
	{
		if ((bool)pendulum)
		{
			float z = MaxAngleDeflection * Mathf.Sin(Time.time * SpeedOfPendulum);
			pendulum.localRotation = Quaternion.Euler(0f, 0f, z);
		}
	}
}
