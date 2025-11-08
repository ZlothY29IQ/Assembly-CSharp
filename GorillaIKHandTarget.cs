using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GorillaIKHandTarget : MonoBehaviour
{
	public GameObject handToStickTo;

	public bool isLeftHand;

	public float hapticStrength;

	private Rigidbody thisRigidbody;

	private XRController controllerReference;

	private void Start()
	{
		thisRigidbody = base.gameObject.GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		thisRigidbody.MovePosition(handToStickTo.transform.position);
		base.transform.rotation = handToStickTo.transform.rotation;
	}

	private void OnCollisionEnter(Collision collision)
	{
	}
}
