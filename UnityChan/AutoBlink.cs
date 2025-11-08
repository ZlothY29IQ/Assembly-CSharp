using System.Collections;
using UnityEngine;

namespace UnityChan;

public class AutoBlink : MonoBehaviour
{
	private enum Status
	{
		Close,
		HalfClose,
		Open
	}

	public bool isActive = true;

	public SkinnedMeshRenderer ref_SMR_EYE_DEF;

	public SkinnedMeshRenderer ref_SMR_EL_DEF;

	public float ratio_Close = 85f;

	public float ratio_HalfClose = 20f;

	[HideInInspector]
	public float ratio_Open;

	private bool timerStarted;

	private bool isBlink;

	public float timeBlink = 0.4f;

	private float timeRemining;

	public float threshold = 0.3f;

	public float interval = 3f;

	private Status eyeStatus;

	private void Awake()
	{
	}

	private void Start()
	{
		ResetTimer();
		StartCoroutine("RandomChange");
	}

	private void ResetTimer()
	{
		timeRemining = timeBlink;
		timerStarted = false;
	}

	private void Update()
	{
		if (!timerStarted)
		{
			eyeStatus = Status.Close;
			timerStarted = true;
		}
		if (timerStarted)
		{
			timeRemining -= Time.deltaTime;
			if (timeRemining <= 0f)
			{
				eyeStatus = Status.Open;
				ResetTimer();
			}
			else if (timeRemining <= timeBlink * 0.3f)
			{
				eyeStatus = Status.HalfClose;
			}
		}
	}

	private void LateUpdate()
	{
		if (isActive && isBlink)
		{
			switch (eyeStatus)
			{
			case Status.Close:
				SetCloseEyes();
				break;
			case Status.HalfClose:
				SetHalfCloseEyes();
				break;
			case Status.Open:
				SetOpenEyes();
				isBlink = false;
				break;
			}
		}
	}

	private void SetCloseEyes()
	{
		ref_SMR_EYE_DEF.SetBlendShapeWeight(6, ratio_Close);
		ref_SMR_EL_DEF.SetBlendShapeWeight(6, ratio_Close);
	}

	private void SetHalfCloseEyes()
	{
		ref_SMR_EYE_DEF.SetBlendShapeWeight(6, ratio_HalfClose);
		ref_SMR_EL_DEF.SetBlendShapeWeight(6, ratio_HalfClose);
	}

	private void SetOpenEyes()
	{
		ref_SMR_EYE_DEF.SetBlendShapeWeight(6, ratio_Open);
		ref_SMR_EL_DEF.SetBlendShapeWeight(6, ratio_Open);
	}

	private IEnumerator RandomChange()
	{
		while (true)
		{
			float num = Random.Range(0f, 1f);
			if (!isBlink && num > threshold)
			{
				isBlink = true;
			}
			yield return new WaitForSeconds(interval);
		}
	}
}
