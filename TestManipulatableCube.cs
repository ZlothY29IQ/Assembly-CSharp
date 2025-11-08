using UnityEngine;

public class TestManipulatableCube : ManipulatableObject
{
	public float breakDistance = 0.2f;

	public float maxXOffset;

	public float minXOffset;

	public float maxYOffset;

	public float minYOffset;

	public float maxZOffset;

	public float minZOffset;

	public bool applyReleaseVelocity;

	public float releaseDrag = 1f;

	private Matrix4x4 localSpace;

	private Vector3 startingPos;

	private Vector3 velocity;

	private void Awake()
	{
		localSpace = base.transform.worldToLocalMatrix;
		startingPos = base.transform.localPosition;
	}

	protected override void OnStartManipulation(GameObject grabbingHand)
	{
	}

	protected override void OnStopManipulation(GameObject releasingHand, Vector3 releaseVelocity)
	{
		if (applyReleaseVelocity)
		{
			velocity = localSpace.MultiplyVector(releaseVelocity);
		}
	}

	protected override bool ShouldHandDetach(GameObject hand)
	{
		Vector3 position = base.transform.position;
		Vector3 position2 = hand.transform.position;
		return Vector3.SqrMagnitude(position - position2) > breakDistance * breakDistance;
	}

	protected override void OnHeldUpdate(GameObject hand)
	{
		Vector3 localPosition = localSpace.MultiplyPoint3x4(hand.transform.position);
		localPosition.x = Mathf.Clamp(localPosition.x, minXOffset, maxXOffset);
		localPosition.y = Mathf.Clamp(localPosition.y, minYOffset, maxYOffset);
		localPosition.z = Mathf.Clamp(localPosition.z, minZOffset, maxZOffset);
		localPosition += startingPos;
		base.transform.localPosition = localPosition;
	}

	protected override void OnReleasedUpdate()
	{
		if (velocity != Vector3.zero)
		{
			Vector3 localPosition = localSpace.MultiplyPoint(base.transform.position);
			localPosition += velocity * Time.deltaTime;
			if (localPosition.x < minXOffset)
			{
				localPosition.x = minXOffset;
				velocity.x = 0f;
			}
			else if (localPosition.x > maxXOffset)
			{
				localPosition.x = maxXOffset;
				velocity.x = 0f;
			}
			if (localPosition.y < minYOffset)
			{
				localPosition.y = minYOffset;
				velocity.y = 0f;
			}
			else if (localPosition.y > maxYOffset)
			{
				localPosition.y = maxYOffset;
				velocity.y = 0f;
			}
			if (localPosition.z < minZOffset)
			{
				localPosition.z = minZOffset;
				velocity.z = 0f;
			}
			else if (localPosition.z > maxZOffset)
			{
				localPosition.z = maxZOffset;
				velocity.z = 0f;
			}
			localPosition += startingPos;
			base.transform.localPosition = localPosition;
			velocity *= 1f - releaseDrag * Time.deltaTime;
			if (velocity.sqrMagnitude < 0.001f)
			{
				velocity = Vector3.zero;
			}
		}
	}

	public Matrix4x4 GetLocalSpace()
	{
		return localSpace;
	}

	public void SetCubeToSpecificPosition(Vector3 pos)
	{
		Vector3 localPosition = localSpace.MultiplyPoint3x4(pos);
		localPosition.x = Mathf.Clamp(localPosition.x, minXOffset, maxXOffset);
		localPosition.y = Mathf.Clamp(localPosition.y, minYOffset, maxYOffset);
		localPosition.z = Mathf.Clamp(localPosition.z, minZOffset, maxZOffset);
		localPosition += startingPos;
		base.transform.localPosition = localPosition;
	}

	public void SetCubeToSpecificPosition(float x, float y, float z)
	{
		Vector3 localPosition = new Vector3(0f, 0f, 0f);
		localPosition.x = Mathf.Clamp(x, minXOffset, maxXOffset);
		localPosition.y = Mathf.Clamp(y, minYOffset, maxYOffset);
		localPosition.z = Mathf.Clamp(z, minZOffset, maxZOffset);
		localPosition += startingPos;
		base.transform.localPosition = localPosition;
	}
}
