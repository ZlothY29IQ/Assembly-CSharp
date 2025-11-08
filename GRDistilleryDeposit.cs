using UnityEngine;

public class GRDistilleryDeposit : MonoBehaviour
{
	public float hapticStrength;

	public float hapticDuration;

	private GRDistillery _distillery;

	private void Start()
	{
		_distillery = GetComponentInParent<GRDistillery>();
	}

	private void OnTriggerEnter(Collider other)
	{
	}
}
