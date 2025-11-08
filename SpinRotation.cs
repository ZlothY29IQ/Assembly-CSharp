using UnityEngine;

public class SpinRotation : MonoBehaviour, ITickSystemTick
{
	[SerializeField]
	private Vector3 rotationPerSecondEuler;

	private Quaternion baseRotation;

	private float baseTime;

	public bool TickRunning { get; set; }

	public void Tick()
	{
		base.transform.localRotation = Quaternion.Euler(rotationPerSecondEuler * (Time.time - baseTime)) * baseRotation;
	}

	private void Awake()
	{
		baseRotation = base.transform.localRotation;
	}

	private void OnEnable()
	{
		TickSystem<object>.AddTickCallback(this);
		baseTime = Time.time;
	}

	private void OnDisable()
	{
		TickSystem<object>.RemoveTickCallback(this);
	}
}
