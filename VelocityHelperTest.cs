using UnityEngine;

public class VelocityHelperTest : MonoBehaviour
{
	public Vector3 velocity;

	public float speed;

	[Space]
	public Vector3 lastVelocity;

	public Vector3 lastPosition;

	[Space]
	[SerializeField]
	private float[] _deltaTimes = new float[5];

	private void Setup()
	{
		lastPosition = base.transform.position;
		lastVelocity = Vector3.zero;
		velocity = Vector3.zero;
		speed = 0f;
	}

	private void Start()
	{
		Setup();
	}

	private void FixedUpdate()
	{
		float deltaTime = Time.deltaTime;
		Vector3 position = base.transform.position;
		Vector3 b = (position - lastPosition) / deltaTime;
		velocity = Vector3.Lerp(lastVelocity, b, deltaTime);
		speed = velocity.magnitude;
		lastPosition = position;
		lastVelocity = b;
	}

	private void Update()
	{
	}
}
