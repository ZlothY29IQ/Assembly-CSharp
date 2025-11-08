using UnityEngine;
using UnityEngine.Events;

public class SteeringWheelCosmetic : MonoBehaviour
{
	[SerializeField]
	private float cooldown = 1.5f;

	[SerializeField]
	private float dramaticTurnThreshold = 35f;

	[SerializeField]
	private UnityEvent onHornHit;

	[SerializeField]
	private UnityEvent onDramaticTurn;

	private float lastHornTime = -999f;

	private float lastZAngle;

	private void Start()
	{
	}

	public void TryHornHit()
	{
		if (Time.time > lastHornTime + cooldown)
		{
			lastHornTime = Time.time;
			onHornHit?.Invoke();
		}
	}

	private void Update()
	{
		float z = base.transform.localEulerAngles.z;
		if (Mathf.Abs(Mathf.DeltaAngle(lastZAngle, z)) >= dramaticTurnThreshold)
		{
			onDramaticTurn?.Invoke();
		}
		lastZAngle = z;
	}
}
