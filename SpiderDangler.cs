using UnityEngine;

public class SpiderDangler : MonoBehaviour
{
	public struct RopeSegment
	{
		public Vector3 pos;

		public Vector3 posOld;

		public RopeSegment(Vector3 pos)
		{
			this.pos = pos;
			posOld = pos;
		}
	}

	public Transform endTransform;

	public Vector4 spinSpeeds = new Vector4(0.1f, 0.2f, 0.3f, 0.4f);

	public Vector4 spinScales = new Vector4(180f, 90f, 120f, 180f);

	private LineRenderer lineRenderer;

	private RopeSegment[] ropeSegs;

	private float ropeSegLen;

	private float ropeSegLenScaled;

	private const int kSegmentCount = 6;

	private const float kVelocityDamper = 0.95f;

	private const int kConstraintCalculationIterations = 8;

	protected void Awake()
	{
		lineRenderer = GetComponent<LineRenderer>();
		Vector3 position = base.transform.position;
		float magnitude = (endTransform.position - position).magnitude;
		ropeSegLen = magnitude / 6f;
		ropeSegs = new RopeSegment[6];
		for (int i = 0; i < 6; i++)
		{
			ropeSegs[i] = new RopeSegment(position);
			position.y -= ropeSegLen;
		}
	}

	protected void FixedUpdate()
	{
		Simulate();
	}

	protected void LateUpdate()
	{
		DrawRope();
		Vector3 normalized = (ropeSegs[ropeSegs.Length - 2].pos - ropeSegs[ropeSegs.Length - 1].pos).normalized;
		endTransform.position = ropeSegs[ropeSegs.Length - 1].pos;
		endTransform.up = normalized;
		Vector4 vector = spinSpeeds * Time.time;
		vector = new Vector4(Mathf.Sin(vector.x), Mathf.Sin(vector.y), Mathf.Sin(vector.z), Mathf.Sin(vector.w));
		vector.Scale(spinScales);
		endTransform.Rotate(Vector3.up, vector.x + vector.y + vector.z + vector.w);
	}

	private void Simulate()
	{
		ropeSegLenScaled = ropeSegLen * base.transform.lossyScale.x;
		Vector3 vector = new Vector3(0f, -0.5f, 0f) * Time.fixedDeltaTime;
		for (int i = 1; i < 6; i++)
		{
			Vector3 vector2 = ropeSegs[i].pos - ropeSegs[i].posOld;
			ropeSegs[i].posOld = ropeSegs[i].pos;
			ropeSegs[i].pos += vector2 * 0.95f;
			ropeSegs[i].pos += vector;
		}
		for (int j = 0; j < 8; j++)
		{
			ApplyConstraint();
		}
	}

	private void ApplyConstraint()
	{
		ropeSegs[0].pos = base.transform.position;
		ApplyConstraintSegment(ref ropeSegs[0], ref ropeSegs[1], 0f, 1f);
		for (int i = 1; i < 5; i++)
		{
			ApplyConstraintSegment(ref ropeSegs[i], ref ropeSegs[i + 1], 0.5f, 0.5f);
		}
	}

	private void ApplyConstraintSegment(ref RopeSegment segA, ref RopeSegment segB, float dampenA, float dampenB)
	{
		float num = (segA.pos - segB.pos).magnitude - ropeSegLenScaled;
		Vector3 vector = (segA.pos - segB.pos).normalized * num;
		segA.pos -= vector * dampenA;
		segB.pos += vector * dampenB;
	}

	private void DrawRope()
	{
		Vector3[] array = new Vector3[6];
		for (int i = 0; i < 6; i++)
		{
			array[i] = ropeSegs[i].pos;
		}
		lineRenderer.positionCount = array.Length;
		lineRenderer.SetPositions(array);
	}
}
