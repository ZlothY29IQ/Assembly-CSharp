using System;
using System.Collections;
using GorillaExtensions;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class VerletLine : MonoBehaviour
{
	[Serializable]
	public struct LineNode
	{
		public Vector3 position;

		public Vector3 lastPosition;

		public Vector3 acceleration;
	}

	public Transform lineStart;

	public Transform lineEnd;

	[Space]
	public LineRenderer line;

	public Rigidbody endRigidbody;

	public Transform endRigidbodyParent;

	public Vector3 endLineAnchorLocalPosition;

	private Vector3 rigidBodyStartingLocalPosition;

	[Space]
	public int segmentNumber = 10;

	public float segmentLength = 0.03f;

	public float segmentTargetLength = 0.03f;

	public float segmentMaxLength = 0.03f;

	public float segmentMinLength = 0.03f;

	[Space]
	public Vector3 gravity = new Vector3(0f, -9.81f, 0f);

	public int simIterations = 6;

	public float tension = 10f;

	public float tensionScale = 1f;

	public float endMaxSpeed = 48f;

	[FormerlySerializedAs("lerpSpeed")]
	[Space]
	public float resizeSpeed = 1f;

	public float resizeScale = 1f;

	[NonSerialized]
	private LineNode[] _nodes = new LineNode[0];

	[NonSerialized]
	private Vector3[] _positions = new Vector3[0];

	private float totalLineLength;

	[SerializeField]
	private bool onlyPullAtEdges;

	[SerializeField]
	private bool scaleLineWidth = true;

	private void Awake()
	{
		_nodes = new LineNode[segmentNumber];
		_positions = new Vector3[segmentNumber];
		for (int i = 0; i < segmentNumber; i++)
		{
			float t = (float)i / (float)(segmentNumber - 1);
			Vector3 vector = Vector3.Lerp(lineStart.position, lineEnd.position, t);
			_nodes[i] = new LineNode
			{
				position = vector,
				lastPosition = vector,
				acceleration = gravity
			};
		}
		line.positionCount = _nodes.Length;
		endRigidbody = lineEnd.GetComponent<Rigidbody>();
		if ((bool)endRigidbody)
		{
			endRigidbody.maxLinearVelocity = endMaxSpeed;
			endRigidbodyParent = endRigidbody.transform.parent;
			rigidBodyStartingLocalPosition = endRigidbody.transform.localPosition;
			endRigidbody.transform.parent = null;
			endRigidbody.gameObject.SetActive(value: false);
		}
		totalLineLength = segmentLength * (float)segmentNumber;
	}

	private void OnEnable()
	{
		if ((bool)endRigidbody)
		{
			endRigidbody.gameObject.SetActive(value: true);
			endRigidbody.transform.localPosition = endRigidbodyParent.TransformPoint(rigidBodyStartingLocalPosition);
		}
	}

	private void OnDisable()
	{
		if ((bool)endRigidbody)
		{
			endRigidbody.gameObject.SetActive(value: false);
		}
	}

	public void SetLength(float total, float delay = 0f)
	{
		segmentTargetLength = total / (float)segmentNumber;
		if (segmentTargetLength < segmentMinLength)
		{
			segmentTargetLength = segmentMinLength;
		}
		if (segmentTargetLength > segmentMaxLength)
		{
			segmentTargetLength = segmentMaxLength;
		}
		if (delay >= 0.01f)
		{
			StartCoroutine(ResizeAfterDelay(delay));
		}
	}

	public void AddSegmentLength(float amount, float delay = 0f)
	{
		segmentTargetLength = segmentLength + amount;
		if (!(segmentTargetLength <= 0f))
		{
			if (segmentTargetLength > segmentMaxLength)
			{
				segmentTargetLength = segmentMaxLength;
			}
			if (delay >= 0.01f)
			{
				StartCoroutine(ResizeAfterDelay(delay));
			}
		}
	}

	public void RemoveSegmentLength(float amount, float delay = 0f)
	{
		segmentTargetLength = segmentLength - amount;
		if (segmentTargetLength <= segmentMinLength)
		{
			segmentTargetLength = (segmentLength = segmentMinLength);
		}
		else if (delay >= 0.01f)
		{
			StartCoroutine(ResizeAfterDelay(delay));
		}
	}

	private IEnumerator ResizeAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
	}

	private void Update()
	{
		if (segmentLength.Approx(segmentTargetLength, 0.1f))
		{
			segmentLength = segmentTargetLength;
			return;
		}
		segmentLength = Mathf.Lerp(segmentLength, segmentTargetLength, resizeSpeed * resizeScale * Time.deltaTime);
		if (scaleLineWidth)
		{
			line.widthMultiplier = base.transform.lossyScale.x;
		}
	}

	public void ForceTotalLength(float totalLength)
	{
		float num = totalLength / (float)((segmentNumber < 1) ? 1 : segmentNumber);
		segmentLength = (segmentTargetLength = num);
		totalLineLength = segmentLength * (float)segmentNumber;
	}

	private void FixedUpdate()
	{
		for (int i = 0; i < _nodes.Length; i++)
		{
			Simulate(ref _nodes[i], Time.fixedDeltaTime);
		}
		for (int j = 0; j < simIterations; j++)
		{
			for (int k = 0; k < _nodes.Length - 1; k++)
			{
				LimitDistance(ref _nodes[k], ref _nodes[k + 1], segmentLength);
			}
		}
		_nodes[0].position = lineStart.position;
		if ((bool)endRigidbody)
		{
			if (onlyPullAtEdges)
			{
				if ((endRigidbody.transform.position - lineStart.position).IsLongerThan(totalLineLength))
				{
					Vector3 vector = lineStart.position + (endRigidbody.transform.position - lineStart.position).normalized * totalLineLength;
					endRigidbody.linearVelocity += (vector - endRigidbody.transform.position) / Time.fixedDeltaTime;
					if (endRigidbody.linearVelocity.IsLongerThan(endMaxSpeed))
					{
						endRigidbody.linearVelocity = endRigidbody.linearVelocity.normalized * endMaxSpeed;
					}
				}
			}
			else
			{
				Vector3 force = (_nodes[^1].position - lineEnd.position) * (tension * tensionScale);
				_ = endRigidbody.rotation;
				Quaternion.LookRotation(_nodes[^1].position - _nodes[^2].position);
				if (!endRigidbody.isKinematic)
				{
					endRigidbody.AddForceAtPosition(force, endRigidbody.transform.TransformPoint(endLineAnchorLocalPosition));
				}
			}
		}
		_nodes[^1].position = lineEnd.position;
		for (int l = 0; l < _nodes.Length; l++)
		{
			_positions[l] = _nodes[l].position;
		}
		line.SetPositions(_positions);
	}

	private static void Simulate(ref LineNode p, float dt)
	{
		Vector3 position = p.position;
		p.position += p.position - p.lastPosition + p.acceleration * (dt * dt);
		p.lastPosition = position;
	}

	private static void LimitDistance(ref LineNode p1, ref LineNode p2, float restLength)
	{
		Vector3 vector = p2.position - p1.position;
		float num = vector.magnitude + 1E-05f;
		float num2 = (num - restLength) / num;
		p1.position += vector * (num2 * 0.5f);
		p2.position -= vector * (num2 * 0.5f);
	}
}
