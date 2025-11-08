using System;
using System.Diagnostics;
using UnityEngine;

public abstract class LerpComponent : MonoBehaviour
{
	[SerializeField]
	[Range(0f, 1f)]
	protected float _lerp;

	[SerializeField]
	protected float _lerpLength = 1f;

	[Space]
	[SerializeField]
	protected LerpChangedEvent _onLerpChanged;

	[SerializeField]
	protected bool _previewInEditor = true;

	[NonSerialized]
	private bool _previewing;

	[NonSerialized]
	private bool _cancelPreview;

	[NonSerialized]
	private bool _rendering;

	[NonSerialized]
	private int _lastState;

	[NonSerialized]
	private float _prevLerpFrom;

	[NonSerialized]
	private float _prevLerpTo;

	public float Lerp
	{
		get
		{
			return _lerp;
		}
		set
		{
			float num = Mathf.Clamp01(value);
			if (!Mathf.Approximately(_lerp, num))
			{
				_onLerpChanged?.Invoke(num);
			}
			_lerp = num;
		}
	}

	public float LerpTime
	{
		get
		{
			return _lerpLength;
		}
		set
		{
			_lerpLength = ((value < 0f) ? 0f : value);
		}
	}

	protected virtual bool CanRender => true;

	protected abstract void OnLerp(float t);

	protected void RenderLerp()
	{
		OnLerp(_lerp);
	}

	protected virtual int GetState()
	{
		return (_lerp, 779562875).GetHashCode();
	}

	protected virtual void Validate()
	{
		if (_lerpLength < 0f)
		{
			_lerpLength = 0f;
		}
	}

	[Conditional("UNITY_EDITOR")]
	private void OnDrawGizmosSelected()
	{
	}

	[Conditional("UNITY_EDITOR")]
	private void TryEditorRender(bool playModeCheck = true)
	{
	}

	[Conditional("UNITY_EDITOR")]
	private void LerpToOne()
	{
	}

	[Conditional("UNITY_EDITOR")]
	private void LerpToZero()
	{
	}

	[Conditional("UNITY_EDITOR")]
	private void StartPreview(float lerpFrom, float lerpTo)
	{
	}
}
