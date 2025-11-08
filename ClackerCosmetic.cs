using GorillaExtensions;
using Unity.Cinemachine;
using UnityEngine;

public class ClackerCosmetic : MonoBehaviour
{
	private struct PerArmData
	{
		public ClackerCosmetic parent;

		public Transform transform;

		public Vector3 velocity;

		public Vector3 lastWorldPosition;

		public void UpdateArm()
		{
			Vector3 target = transform.TransformPoint(parent.LocalCenterOfMass);
			Vector3 vector = lastWorldPosition + velocity * Time.deltaTime * parent.drag;
			Vector3 vector2 = transform.parent.TransformDirection(parent.LocalRotationAxis);
			Vector3 current = transform.position + (vector - transform.position).ProjectOntoPlane(vector2).normalized * parent.centerOfMassRadius;
			current = Vector3.MoveTowards(current, target, parent.localFriction * Time.deltaTime);
			velocity = (current - lastWorldPosition) / Time.deltaTime;
			velocity += Vector3.down * parent.gravity * Time.deltaTime;
			lastWorldPosition = current;
			transform.rotation = Quaternion.LookRotation(vector2, current - transform.position) * parent.RotationCorrection;
			lastWorldPosition = transform.TransformPoint(parent.LocalCenterOfMass);
		}

		public void SetPosition(Vector3 newPosition)
		{
			Vector3 forward = transform.parent.TransformDirection(parent.LocalRotationAxis);
			transform.rotation = Quaternion.LookRotation(forward, newPosition - transform.position) * parent.RotationCorrection;
			lastWorldPosition = transform.TransformPoint(parent.LocalCenterOfMass);
		}
	}

	[SerializeField]
	private TransferrableObject parentHoldable;

	[SerializeField]
	private Transform clackerArm1;

	[SerializeField]
	private Transform clackerArm2;

	[SerializeField]
	private Vector3 LocalCenterOfMass;

	[SerializeField]
	private Vector3 LocalRotationAxis;

	[SerializeField]
	private Vector3 RotationCorrectionEuler;

	[SerializeField]
	private float drag;

	[SerializeField]
	private float gravity;

	[SerializeField]
	private float localFriction;

	[SerializeField]
	private float minimumClackSpeed;

	[SerializeField]
	private SoundBankPlayer lightClackAudio;

	[SerializeField]
	private float mediumClackSpeed;

	[SerializeField]
	private SoundBankPlayer mediumClackAudio;

	[SerializeField]
	private float heavyClackSpeed;

	[SerializeField]
	private SoundBankPlayer heavyClackAudio;

	[SerializeField]
	private float collisionDistance;

	private float centerOfMassRadius;

	[SerializeField]
	private float pushApartStrength;

	private PerArmData arm1;

	private PerArmData arm2;

	private Quaternion RotationCorrection;

	private void Start()
	{
		LocalRotationAxis = LocalRotationAxis.normalized;
		arm1.parent = this;
		arm2.parent = this;
		arm1.transform = clackerArm1;
		arm2.transform = clackerArm2;
		arm1.lastWorldPosition = clackerArm1.transform.TransformPoint(LocalCenterOfMass);
		arm2.lastWorldPosition = clackerArm2.transform.TransformPoint(LocalCenterOfMass);
		centerOfMassRadius = LocalCenterOfMass.magnitude;
		RotationCorrection = Quaternion.Euler(RotationCorrectionEuler);
	}

	private void Update()
	{
		Vector3 lastWorldPosition = arm1.lastWorldPosition;
		arm1.UpdateArm();
		arm2.UpdateArm();
		Vector3 eulerAngles = clackerArm1.transform.eulerAngles;
		Vector3 eulerAngles2 = clackerArm2.transform.eulerAngles;
		Mathf.DeltaAngle(eulerAngles.y, eulerAngles2.y);
		if (!(arm1.lastWorldPosition - arm2.lastWorldPosition).IsShorterThan(collisionDistance))
		{
			return;
		}
		float sqrMagnitude = (arm1.velocity - arm2.velocity).sqrMagnitude;
		if (parentHoldable.InHand())
		{
			if (sqrMagnitude > heavyClackSpeed * heavyClackSpeed)
			{
				heavyClackAudio.Play();
			}
			else if (sqrMagnitude > mediumClackSpeed * mediumClackSpeed)
			{
				mediumClackAudio.Play();
			}
			else if (sqrMagnitude > minimumClackSpeed * minimumClackSpeed)
			{
				lightClackAudio.Play();
			}
		}
		Vector3 vector = (arm1.lastWorldPosition + arm2.lastWorldPosition) / 2f;
		Vector3 vector2 = (arm1.lastWorldPosition - arm2.lastWorldPosition).normalized * (collisionDistance + 0.001f) / 2f;
		Vector3 vector3 = vector + vector2;
		Vector3 vector4 = vector - vector2;
		if ((lastWorldPosition - vector3).IsLongerThan(lastWorldPosition - vector4))
		{
			vector2 = -vector2;
		}
		arm1.SetPosition(vector + vector2);
		arm2.SetPosition(vector - vector2);
		ref Vector3 velocity = ref arm1.velocity;
		ref Vector3 velocity2 = ref arm2.velocity;
		Vector3 velocity3 = arm2.velocity;
		Vector3 velocity4 = arm1.velocity;
		velocity = velocity3;
		velocity2 = velocity4;
		Vector3 vector5 = (arm1.lastWorldPosition - arm2.lastWorldPosition).normalized * pushApartStrength * Mathf.Sqrt(sqrMagnitude);
		arm1.velocity += vector5;
		arm2.velocity -= vector5;
	}
}
