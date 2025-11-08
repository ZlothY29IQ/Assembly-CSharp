using System;
using UnityEngine;

public class KinematicTestMotion : MonoBehaviour
{
	public enum UpdateType
	{
		Update,
		LateUpdate,
		FixedUpdate
	}

	public enum MoveType
	{
		TransformPosition,
		RigidbodyMovePosition
	}

	public Transform start;

	public Transform end;

	public Rigidbody rigidbody;

	public UpdateType updateType;

	public MoveType moveType = MoveType.RigidbodyMovePosition;

	public float period = 4f;

	private void FixedUpdate()
	{
		if (updateType == UpdateType.FixedUpdate)
		{
			UpdatePosition(Time.time);
		}
	}

	private void Update()
	{
		if (updateType == UpdateType.Update)
		{
			UpdatePosition(Time.time);
		}
	}

	private void LateUpdate()
	{
		if (updateType == UpdateType.LateUpdate)
		{
			UpdatePosition(Time.time);
		}
	}

	private void UpdatePosition(float time)
	{
		float t = Mathf.Sin(time * 2f * MathF.PI * period) * 0.5f + 0.5f;
		Vector3 position = Vector3.Lerp(start.position, end.position, t);
		if (moveType == MoveType.TransformPosition)
		{
			base.transform.position = position;
		}
		else if (moveType == MoveType.RigidbodyMovePosition)
		{
			rigidbody.MovePosition(position);
		}
	}
}
