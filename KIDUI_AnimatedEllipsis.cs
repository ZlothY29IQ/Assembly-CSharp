using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class KIDUI_AnimatedEllipsis : MonoBehaviour
{
	[Header("Ellipsis Spawning")]
	[SerializeField]
	private bool _animateOnStart = true;

	[SerializeField]
	private int _ellipsisCount = 3;

	[SerializeField]
	private GameObject _ellipsisPrefab;

	[SerializeField]
	private GameObject _ellipsisRoot;

	[SerializeField]
	private List<float> _ellipsisStartingValues = new List<float>();

	[Header("Animation Settings")]
	[SerializeField]
	private bool _shouldLerp;

	[SerializeField]
	private AnimationCurve _ellipsisAnimationCurve;

	[SerializeField]
	private float _animationSpeedMultiplier = 0.25f;

	[SerializeField]
	private float _startingScale = 0.33f;

	[SerializeField]
	private float _intermediaryScale = 0.66f;

	[SerializeField]
	private float _endScale = 1f;

	[SerializeField]
	private float _scaleDuration = 0.25f;

	[SerializeField]
	private float _pauseBetweenScale = 0.25f;

	[SerializeField]
	private float _pauseBetweenCycles = 0.5f;

	private bool _runAnimation;

	private float _nextChange;

	private (GameObject ellipsis, float startingScale, float currentScale, float lerpT)[] _ellipsisObjects;

	private Coroutine _animationCoroutine;

	private void Awake()
	{
		if (_ellipsisObjects == null)
		{
			SetupEllipsis();
		}
	}

	private void Start()
	{
	}

	private void OnDisable()
	{
		StopAnimation();
	}

	private void SetupEllipsis()
	{
		if (_ellipsisRoot == null)
		{
			_ellipsisRoot = base.gameObject;
		}
		_ellipsisObjects = new(GameObject, float, float, float)[_ellipsisStartingValues.Count];
		for (int i = 0; i < _ellipsisStartingValues.Count; i++)
		{
			float num = _ellipsisStartingValues[i];
			_ellipsisObjects[i].ellipsis = Object.Instantiate(_ellipsisPrefab, _ellipsisRoot.transform);
			_ellipsisObjects[i].ellipsis.transform.localScale = new Vector3(num, num, num);
			_ellipsisObjects[i].startingScale = (_ellipsisObjects[i].currentScale = num);
		}
	}

	private IEnumerator EllipsisAnimation()
	{
		int currIndex = 0;
		while (_runAnimation)
		{
			for (int i = 0; i < _ellipsisObjects.Length; i++)
			{
				int num = i - currIndex;
				if (num < 0)
				{
					num = _ellipsisStartingValues.Count + num;
				}
				float num2 = _ellipsisStartingValues[num];
				_ellipsisObjects[i].ellipsis.transform.localScale = Vector3.one * num2;
			}
			currIndex++;
			if (currIndex >= _ellipsisObjects.Length)
			{
				currIndex = 0;
			}
			yield return new WaitForSeconds(_pauseBetweenScale);
		}
	}

	private IEnumerator EllipsisAnimation2()
	{
		float time = 0f;
		while (_runAnimation)
		{
			for (int i = 0; i < _ellipsisObjects.Length; i++)
			{
				float offsetTime = _scaleDuration / (float)(_ellipsisObjects.Length + 1) * (float)i;
				float num = LerpLoop(_startingScale, _endScale, time, offsetTime, _scaleDuration);
				_ellipsisObjects[i].ellipsis.transform.localScale = new Vector3(num, num, num);
			}
			time += Time.deltaTime * _animationSpeedMultiplier;
			yield return null;
		}
	}

	public async Task StartAnimation()
	{
		if (_ellipsisObjects == null)
		{
			SetupEllipsis();
		}
		if (_animationCoroutine != null)
		{
			Debug.LogWarningFormat("[KID::UI::ELLIPSIS] Animation is already running.");
			await StopAnimation();
		}
		for (int i = 0; i < _ellipsisCount; i++)
		{
			_ellipsisObjects[i].ellipsis.transform.localScale = new Vector3(_ellipsisObjects[i].startingScale, _ellipsisObjects[i].startingScale, _ellipsisObjects[i].startingScale);
		}
		_ellipsisRoot.SetActive(value: true);
		_runAnimation = true;
		if (_shouldLerp)
		{
			_animationCoroutine = StartCoroutine(EllipsisAnimation2());
		}
		else
		{
			_animationCoroutine = StartCoroutine(EllipsisAnimation());
		}
	}

	public async Task StopAnimation()
	{
		_runAnimation = false;
		StopAllCoroutines();
		await Task.Delay(100);
		_animationCoroutine = null;
		_ellipsisRoot.SetActive(value: false);
	}

	public float LerpLoop(float start, float end, float time, float offsetTime, float duration)
	{
		float time2 = (offsetTime - time) % duration / duration;
		float t = _ellipsisAnimationCurve.Evaluate(time2);
		return Mathf.Lerp(start, end, t);
	}
}
