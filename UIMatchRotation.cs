using UnityEngine;

public class UIMatchRotation : MonoBehaviour
{
	private enum State
	{
		Ready,
		Rotating
	}

	[SerializeField]
	private Transform referenceTransform;

	[SerializeField]
	private float threshold = 0.35f;

	[SerializeField]
	private float lerpSpeed = 5f;

	private State state;

	private void Start()
	{
		referenceTransform = Camera.main.transform;
		base.transform.forward = x0z(referenceTransform.forward);
	}

	private void Update()
	{
		Vector3 lhs = x0z(base.transform.forward);
		Vector3 vector = x0z(referenceTransform.forward);
		float num = Vector3.Dot(lhs, vector);
		switch (state)
		{
		case State.Ready:
			if (num < 1f - threshold)
			{
				state = State.Rotating;
			}
			break;
		case State.Rotating:
			base.transform.forward = Vector3.Lerp(base.transform.forward, vector, Time.deltaTime * lerpSpeed);
			if (Vector3.Dot(base.transform.forward, vector) > 0.995f)
			{
				state = State.Ready;
			}
			break;
		}
	}

	private Vector3 x0z(Vector3 vector)
	{
		vector.y = 0f;
		return vector.normalized;
	}
}
