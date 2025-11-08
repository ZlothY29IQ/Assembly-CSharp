using System;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class GRAbilityInterpolatedMovement
{
	public enum InterpType
	{
		Linear,
		EaseOut
	}

	public Vector3 velocity = Vector3.zero;

	private Vector3 startPos;

	private Vector3 endPos;

	public float duration;

	public double endTime;

	public float maxVelocityMagnitude = 2f;

	private Transform root;

	private Rigidbody rb;

	public InterpType interpolationType;

	private int walkableArea = -1;

	public void Setup(Transform root)
	{
		this.root = root;
		rb = root.gameObject.GetComponent<Rigidbody>();
		walkableArea = NavMesh.GetAreaFromName("walkable");
	}

	public void InitFromVelocityAndDuration(Vector3 velocity, float duration)
	{
		this.velocity = velocity;
		this.duration = duration;
		_ = velocity.magnitude;
	}

	public void Start()
	{
		startPos = root.position;
		endPos = startPos + velocity * duration;
		endTime = Time.timeAsDouble + (double)duration;
		if (NavMesh.SamplePosition(endPos, out var hit, 5f, walkableArea))
		{
			endPos = hit.position;
		}
	}

	public void Stop()
	{
	}

	public bool IsDone()
	{
		return Time.timeAsDouble >= endTime;
	}

	public void Update(float dt)
	{
		Vector3 position = root.position;
		float num = Mathf.Clamp01(1f - (float)((endTime - Time.timeAsDouble) / (double)duration));
		InterpType interpType = interpolationType;
		Vector3 vector = ((interpType == InterpType.Linear || interpType != InterpType.EaseOut) ? Vector3.Lerp(startPos, endPos, num) : Vector3.Lerp(startPos, endPos, AbilityHelperFunctions.EaseOutPower(num, 2.5f)));
		vector.y = Mathf.Lerp(startPos.y, endPos.y, num * num);
		if (NavMesh.Raycast(position, vector, out var hit, walkableArea))
		{
			vector = hit.position;
		}
		root.position = vector;
		if (rb != null)
		{
			rb.position = vector;
		}
	}
}
