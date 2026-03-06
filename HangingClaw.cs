using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HangingClaw : MonoBehaviourPostTick
{
	public struct RopeSegment(Vector3 p)
	{
		public Vector3 pos = p;

		public Vector3 posOld = p;
	}

	public Transform endTransform;

	public Transform heightCap;

	private int segmentCount = 6;

	public float segmentMassKg = 1f;

	public float endMassKg = 5f;

	public float ropeStiffness = 0.9f;

	public float slackFraction = 0.02f;

	public Vector3 gravity = new Vector3(0f, -9.8f, 0f);

	public float velocityDamping = 0.98f;

	private float maxY;

	private LineRenderer lineRenderer;

	private RopeSegment[] ropeSegs;

	private float baseSegLen;

	private float targetSegLenScaled;

	private float[] invMass;

	protected void Awake()
	{
		lineRenderer = GetComponent<LineRenderer>();
		Vector3 position = base.transform.position;
		segmentCount = 4;
		float magnitude = (endTransform.position - position).magnitude;
		segmentCount = Mathf.Max(2, segmentCount);
		baseSegLen = magnitude / (float)segmentCount;
		ropeSegs = new RopeSegment[segmentCount];
		invMass = new float[segmentCount];
		for (int i = 0; i < segmentCount; i++)
		{
			Vector3 p = Vector3.Lerp(position, endTransform.position, (float)i / (float)(segmentCount - 1));
			ropeSegs[i] = new RopeSegment(p);
		}
		invMass[0] = 0f;
		for (int j = 1; j < segmentCount - 1; j++)
		{
			invMass[j] = 1f / Mathf.Max(0.0001f, segmentMassKg);
		}
		invMass[segmentCount - 1] = 1f / Mathf.Max(0.0001f, endMassKg);
	}

	public override void PostTick()
	{
		Simulate();
		DrawRope();
		int num = segmentCount - 1;
		_ = segmentCount;
		endTransform.position = ropeSegs[num].pos;
	}

	private void Simulate()
	{
		float num = baseSegLen;
		targetSegLenScaled = num * (1f + slackFraction);
		float num2 = 0.01111f;
		float f = Time.time * 0.5f;
		Vector3 vector = gravity * num2 * num2;
		Vector3 topPos = base.transform.position + new Vector3(0f, 0.012f * Mathf.Sin(f), 0.02f * Mathf.Cos(f));
		for (int i = 1; i < segmentCount; i++)
		{
			Vector3 vector2 = ropeSegs[i].pos - ropeSegs[i].posOld;
			ropeSegs[i].posOld = ropeSegs[i].pos;
			ropeSegs[i].pos += vector2 * velocityDamping + vector;
		}
		int num3 = 3;
		for (int j = 0; j < num3; j++)
		{
			ApplyConstraints(topPos);
		}
	}

	private void ApplyConstraints(Vector3 topPos)
	{
		ropeSegs[0].pos = topPos;
		ropeSegs[0].posOld = topPos;
		float stiffness = Mathf.Clamp01(ropeStiffness);
		for (int i = 0; i < segmentCount - 1; i++)
		{
			ApplyConstraintSegment(ref ropeSegs[i], ref ropeSegs[i + 1], invMass[i], invMass[i + 1], stiffness);
		}
	}

	private void ApplyConstraintSegment(ref RopeSegment a, ref RopeSegment b, float wA, float wB, float stiffness)
	{
		Vector3 vector = b.pos - a.pos;
		float magnitude = vector.magnitude;
		if (magnitude < 1E-06f)
		{
			return;
		}
		float num = magnitude - targetSegLenScaled;
		if (!(Mathf.Abs(num) < 1E-06f))
		{
			Vector3 vector2 = vector / magnitude;
			float num2 = wA + wB;
			if (!(num2 <= 0f))
			{
				Vector3 vector3 = vector2 * (num * stiffness);
				a.pos += vector3 * (wA / num2);
				b.pos += -vector3 * (wB / num2);
			}
		}
	}

	private void DrawRope()
	{
		if (lineRenderer == null)
		{
			return;
		}
		lineRenderer.positionCount = segmentCount;
		for (int i = 0; i < segmentCount; i++)
		{
			Vector3 pos = ropeSegs[i].pos;
			if ((bool)heightCap && pos.y > heightCap.position.y)
			{
				pos.y = heightCap.position.y;
			}
			lineRenderer.SetPosition(i, ropeSegs[i].pos);
		}
	}
}
