using UnityEngine;

public class GorillaIKHandTarget : MonoBehaviour
{
	public GameObject handToStickTo;

	private Rigidbody thisRigidbody;

	private void Start()
	{
		thisRigidbody = base.gameObject.GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		thisRigidbody.MovePosition(handToStickTo.transform.position);
		base.transform.rotation = handToStickTo.transform.rotation;
	}
}
