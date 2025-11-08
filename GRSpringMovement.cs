using UnityEngine;

public class GRSpringMovement
{
	public float tension = 1f;

	public float dampening = 0.7f;

	public float target;

	public bool hardStopAtTarget = true;

	public float pos;

	public float speed;

	private bool wasAlreadyAtTargetLastUpdate;

	public GRSpringMovement(float _tension, float _dampening)
	{
		tension = _tension;
		dampening = _dampening;
	}

	public void Reset()
	{
		pos = 0f;
		target = 0f;
		speed = 0f;
		wasAlreadyAtTargetLastUpdate = false;
	}

	public void SetHardStopAtTarget(bool _hardStopAtTarget)
	{
		if (hardStopAtTarget != _hardStopAtTarget)
		{
			hardStopAtTarget = _hardStopAtTarget;
			speed = 0f;
		}
	}

	public void Update()
	{
		wasAlreadyAtTargetLastUpdate = pos == target && speed == 0f;
		float num = pos;
		float num2 = 0.001f;
		float num3 = Mathf.Min(Time.deltaTime, 0.05f);
		float num4 = 6.2832f / tension;
		float num5 = num4 * num4 * (target - pos) - 2f * dampening * num4 * speed;
		speed += num5 * num3;
		pos += speed * num3;
		if (hardStopAtTarget)
		{
			if ((num <= pos && pos + num2 >= target) || (num >= pos && pos - num2 <= target))
			{
				speed = 0f;
				pos = target;
			}
		}
		else if (Mathf.Abs(num - target) < num2 && Mathf.Abs(speed) < num2)
		{
			speed = 0f;
			pos = target;
		}
	}

	public bool HitTargetLastUpdate()
	{
		if (IsAtTarget())
		{
			return !wasAlreadyAtTargetLastUpdate;
		}
		return false;
	}

	public bool IsAtTarget()
	{
		if (pos == target)
		{
			return speed == 0f;
		}
		return false;
	}
}
