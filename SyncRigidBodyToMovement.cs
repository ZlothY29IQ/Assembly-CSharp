using BoingKit;
using UnityEngine;

public class SyncRigidBodyToMovement : MonoBehaviour
{
	[SerializeField]
	private Rigidbody targetRigidbody;

	private Transform targetParent;

	private void Awake()
	{
		targetParent = targetRigidbody.transform.parent;
		targetRigidbody.transform.parent = null;
		targetRigidbody.gameObject.SetActive(value: false);
	}

	private void OnEnable()
	{
		targetRigidbody.gameObject.SetActive(value: true);
		targetRigidbody.transform.position = base.transform.position;
		targetRigidbody.transform.rotation = base.transform.rotation;
	}

	private void OnDisable()
	{
		targetRigidbody.gameObject.SetActive(value: false);
	}

	private void FixedUpdate()
	{
		targetRigidbody.linearVelocity = (base.transform.position - targetRigidbody.position) / Time.fixedDeltaTime;
		targetRigidbody.angularVelocity = QuaternionUtil.ToAngularVector(Quaternion.Inverse(targetRigidbody.rotation) * base.transform.rotation) / Time.fixedDeltaTime;
	}
}
