using UnityEngine;

public class GorillaPlaySpaceForces : MonoBehaviour
{
	public GameObject rightHand;

	public GameObject leftHand;

	public Collider bodyCollider;

	private Collider leftHandCollider;

	private Collider rightHandCollider;

	public Transform rightHandTransform;

	public Transform leftHandTransform;

	private Rigidbody leftHandRigidbody;

	private Rigidbody rightHandRigidbody;

	public Vector3 bodyColliderOffset;

	public float forceConstant;

	private Vector3 lastLeftHandPosition;

	private Vector3 lastRightHandPosition;

	private Rigidbody playspaceRigidbody;

	public Transform headsetTransform;

	private void Start()
	{
		playspaceRigidbody = GetComponent<Rigidbody>();
		leftHandRigidbody = leftHand.GetComponent<Rigidbody>();
		leftHandCollider = leftHand.GetComponent<Collider>();
		rightHandRigidbody = rightHand.GetComponent<Rigidbody>();
		rightHandCollider = rightHand.GetComponent<Collider>();
	}

	private void FixedUpdate()
	{
		if (!(Time.time < 0.1f))
		{
			bodyCollider.transform.position = headsetTransform.position + bodyColliderOffset;
		}
	}
}
