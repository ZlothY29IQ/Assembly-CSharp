using GorillaExtensions;
using Unity.Cinemachine;
using UnityEngine;

public class FakeWheelDriver : MonoBehaviour
{
	[SerializeField]
	private Rigidbody myRigidBody;

	[SerializeField]
	private Vector3 thrust;

	[SerializeField]
	private Collider wheelCollider;

	[SerializeField]
	private float maxSpeed;

	[SerializeField]
	private float lateralFrictionForce;

	private Vector3 collisionPoint;

	private Vector3 collisionNormal;

	public bool hasCollision { get; private set; }

	public void SetThrust(Vector3 thrust)
	{
		this.thrust = thrust;
	}

	private void OnCollisionStay(Collision collision)
	{
		int num = 0;
		Vector3 zero = Vector3.zero;
		ContactPoint[] contacts = collision.contacts;
		for (int i = 0; i < contacts.Length; i++)
		{
			ContactPoint contactPoint = contacts[i];
			if (contactPoint.thisCollider == wheelCollider)
			{
				zero += contactPoint.point;
				num++;
			}
		}
		if (num > 0)
		{
			collisionNormal = collision.contacts[0].normal;
			collisionPoint = zero / num;
			hasCollision = true;
		}
	}

	private void FixedUpdate()
	{
		if (hasCollision)
		{
			Vector3 vector = base.transform.rotation * thrust;
			if (myRigidBody.linearVelocity.IsShorterThan(maxSpeed))
			{
				vector = vector.ProjectOntoPlane(collisionNormal).normalized * thrust.magnitude;
				myRigidBody.AddForceAtPosition(vector, collisionPoint);
			}
			Vector3 vector2 = myRigidBody.linearVelocity.ProjectOntoPlane(collisionNormal).ProjectOntoPlane(vector.normalized);
			if (vector2.IsLongerThan(lateralFrictionForce))
			{
				myRigidBody.AddForceAtPosition(-vector2.normalized * lateralFrictionForce, collisionPoint);
			}
			else
			{
				myRigidBody.AddForceAtPosition(-vector2, collisionPoint);
			}
		}
		hasCollision = false;
	}
}
