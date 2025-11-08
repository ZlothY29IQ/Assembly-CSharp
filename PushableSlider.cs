using UnityEngine;

public class PushableSlider : MonoBehaviour
{
	[SerializeField]
	private float farPushDist = 0.015f;

	[SerializeField]
	private float maxXOffset;

	[SerializeField]
	private float minXOffset;

	private Matrix4x4 _localSpace;

	private Vector3 _startingPos;

	private Vector3 _previousLocalPosition;

	private float _cachedProgress;

	private bool _initialized;

	public void Awake()
	{
		Initialize();
	}

	private void Initialize()
	{
		if (!_initialized)
		{
			_initialized = true;
			_localSpace = base.transform.worldToLocalMatrix;
			_startingPos = base.transform.localPosition;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (!base.enabled)
		{
			return;
		}
		GorillaTriggerColliderHandIndicator componentInParent = other.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
		if (componentInParent == null)
		{
			return;
		}
		Vector3 vector = _localSpace.MultiplyPoint3x4(other.transform.position);
		Vector3 vector2 = base.transform.localPosition - _startingPos - vector;
		float num = Mathf.Abs(vector2.x);
		if (num < farPushDist)
		{
			Vector3 currentVelocity = componentInParent.currentVelocity;
			if (Mathf.Sign(vector2.x) == Mathf.Sign((_localSpace.rotation * currentVelocity).x))
			{
				vector2.x = Mathf.Sign(vector2.x) * (farPushDist - num);
				vector2.y = 0f;
				vector2.z = 0f;
				Vector3 vector3 = base.transform.localPosition - _startingPos + vector2;
				vector3.x = Mathf.Clamp(vector3.x, minXOffset, maxXOffset);
				base.transform.localPosition = GetXOffsetVector(vector3.x + _startingPos.x);
				GorillaTagger.Instance.StartVibration(componentInParent.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
			}
		}
	}

	private Vector3 GetXOffsetVector(float x)
	{
		return new Vector3(x, _startingPos.y, _startingPos.z);
	}

	public void SetProgress(float value)
	{
		Initialize();
		value = Mathf.Clamp(value, 0f, 1f);
		float num = Mathf.Lerp(minXOffset, maxXOffset, value);
		base.transform.localPosition = GetXOffsetVector(_startingPos.x + num);
		_previousLocalPosition = new Vector3(num, 0f, 0f);
		_cachedProgress = value;
	}

	public float GetProgress()
	{
		Initialize();
		Vector3 vector = base.transform.localPosition - _startingPos;
		if (vector == _previousLocalPosition)
		{
			return _cachedProgress;
		}
		_previousLocalPosition = vector;
		_cachedProgress = (vector.x - minXOffset) / (maxXOffset - minXOffset);
		return _cachedProgress;
	}
}
