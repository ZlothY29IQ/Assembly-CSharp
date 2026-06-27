using System;
using UnityEngine;

public class GorillaPressableDelayButton : GorillaPressableButton, IGorillaSliceableSimple
{
	private Collider touching;

	private float pressStartTime;

	private float progress;

	[SerializeField]
	[Range(0.01f, 5f)]
	public float delayTime = 1f;

	[SerializeField]
	private Transform fillBar;

	private Vector3 fillbarStartingScale;

	private Vector3 fillBarScale;

	public event Action onPressBegin;

	public event Action onPressAbort;

	private void Awake()
	{
		if (!(fillBar == null))
		{
			fillBarScale = (fillbarStartingScale = fillBar.localScale);
			UpdateFillBar();
		}
	}

	private new void OnTriggerEnter(Collider collider)
	{
		if (base.enabled && touchTime + debounceTime < Time.time && !touching && !(collider.GetComponentInParent<GorillaTriggerColliderHandIndicator>() == null))
		{
			touching = collider;
			pressStartTime = Time.unscaledTime;
			progress = 0f;
			UpdateFillBar();
			this.onPressBegin?.Invoke();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!(other != touching))
		{
			touching = null;
			progress = 0f;
			UpdateFillBar();
			this.onPressAbort?.Invoke();
		}
	}

	public new void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public new void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		if (!(touching == null))
		{
			float num = Time.unscaledTime - pressStartTime;
			progress = ((delayTime > 0f) ? Mathf.Clamp01(num / delayTime) : ((num > 0f) ? 1f : 0f));
			if (num > delayTime)
			{
				base.OnTriggerEnter(touching);
				touching = null;
				progress = 0f;
			}
			UpdateFillBar();
		}
	}

	public void SetFillBar(Transform newFillBar)
	{
		fillBar = newFillBar;
		if (!(fillBar == null))
		{
			fillBarScale = (fillbarStartingScale = fillBar.localScale);
			UpdateFillBar();
		}
	}

	private void UpdateFillBar()
	{
		if (!(fillBar == null))
		{
			fillBarScale.x = fillbarStartingScale.x * progress;
			fillBar.localScale = fillBarScale;
		}
	}
}
