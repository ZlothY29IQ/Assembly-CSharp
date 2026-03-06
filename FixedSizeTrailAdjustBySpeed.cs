using System;
using UnityEngine;
using UnityEngine.Serialization;

public class FixedSizeTrailAdjustBySpeed : MonoBehaviour
{
	[Serializable]
	public struct GradientKey(Color color, float time)
	{
		public Color color = color;

		public float time = time;
	}

	public FixedSizeTrail trail;

	public bool adjustPhysics = true;

	private Vector3 _rawVelocity;

	private float _rawSpeed;

	private float _speed;

	private float _lastSpeed;

	private Vector3 _lastPosition;

	private Vector3 _initGravity;

	public Vector3 gravityOffset = Vector3.zero;

	[Space]
	public float retractMin = 0.5f;

	[Space]
	[FormerlySerializedAs("sizeIncreaseSpeed")]
	public float expandSpeed = 16f;

	[FormerlySerializedAs("sizeDecreaseSpeed")]
	public float retractSpeed = 4f;

	[Space]
	public float minSpeed;

	public float minLength = 1f;

	public Gradient minColors = GradientHelper.FromColor(new Color(0f, 1f, 1f, 1f));

	[Space]
	public float maxSpeed = 10f;

	public float maxLength = 8f;

	public Gradient maxColors = GradientHelper.FromColor(new Color(1f, 1f, 0f, 1f));

	[Space]
	[SerializeField]
	private Gradient _mixGradient = new Gradient
	{
		colorKeys = new GradientColorKey[8],
		alphaKeys = Array.Empty<GradientAlphaKey>()
	};

	private void Start()
	{
		Setup();
	}

	private void OnEnable()
	{
		ResetTrailState();
	}

	private void OnDisable()
	{
		ResetTrailState();
	}

	private void ResetTrailState()
	{
		_rawVelocity = Vector3.zero;
		_rawSpeed = 0f;
		_speed = 0f;
		_lastSpeed = 0f;
		_lastPosition = base.transform.position;
		if ((bool)trail)
		{
			trail.length = minLength;
			trail.Setup();
			LerpTrailColors(0f);
		}
	}

	private void Setup()
	{
		_lastPosition = base.transform.position;
		_rawVelocity = Vector3.zero;
		_rawSpeed = 0f;
		_speed = 0f;
		if ((bool)trail)
		{
			_initGravity = trail.gravity;
			trail.applyPhysics = adjustPhysics;
		}
		LerpTrailColors();
	}

	private void LerpTrailColors(float t = 0.5f)
	{
		GradientColorKey[] colorKeys = _mixGradient.colorKeys;
		int num = colorKeys.Length;
		for (int i = 0; i < num; i++)
		{
			float time = (float)i / (float)(num - 1);
			Color a = minColors.Evaluate(time);
			Color b = maxColors.Evaluate(time);
			Color color = Color.Lerp(a, b, t);
			colorKeys[i].color = color;
			colorKeys[i].time = time;
		}
		_mixGradient.colorKeys = colorKeys;
		if ((bool)trail)
		{
			trail.renderer.colorGradient = _mixGradient;
		}
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		Vector3 position = base.transform.position;
		_rawVelocity = (position - _lastPosition) / deltaTime;
		_rawSpeed = _rawVelocity.magnitude;
		if (_rawSpeed > retractMin)
		{
			_speed += expandSpeed * deltaTime;
		}
		if (_rawSpeed <= retractMin)
		{
			_speed -= retractSpeed * deltaTime;
		}
		if (_speed > maxSpeed)
		{
			_speed = maxSpeed;
		}
		_speed = Mathf.Lerp(_lastSpeed, _speed, 0.5f);
		if (_speed < 0.01f)
		{
			_speed = 0f;
		}
		AdjustTrail();
		_lastSpeed = _speed;
		_lastPosition = position;
	}

	private void AdjustTrail()
	{
		if ((bool)trail)
		{
			float num = MathUtils.Linear(_speed, minSpeed, maxSpeed, 0f, 1f);
			float length = MathUtils.Linear(num, 0f, 1f, minLength, maxLength);
			trail.length = length;
			LerpTrailColors(num);
			if (adjustPhysics)
			{
				Transform transform = base.transform;
				Vector3 vector = transform.forward * gravityOffset.z + transform.right * gravityOffset.x + transform.up * gravityOffset.y;
				Vector3 b = (_initGravity + vector) * (1f - num);
				trail.gravity = Vector3.Lerp(Vector3.zero, b, 0.5f);
			}
		}
	}
}
