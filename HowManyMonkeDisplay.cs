using System;
using TMPro;
using UnityEngine;

public class HowManyMonkeDisplay : MonoBehaviour, IGorillaSliceableSimple
{
	[SerializeField]
	private TMP_Text text;

	[SerializeField]
	private float observableDistance = 100f;

	[SerializeField]
	private GameObject observableActive;

	[SerializeField]
	private ParticleSystem particleSystem;

	[SerializeField]
	private AnimationCurve particleSystemRateToCount;

	private bool observable;

	private int currValue;

	private int nextValue;

	private float checkTime;

	public void OnEnable()
	{
		currValue = (nextValue = HowManyMonke.ThisMany);
		text.text = currValue.ToString("N0");
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	public void OnDisable()
	{
		HowManyMonke.OnCheck = (Action<int>)Delegate.Remove(HowManyMonke.OnCheck, new Action<int>(HowManyMonke_OnCheck));
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.LateUpdate);
	}

	private void OnDestroy()
	{
		HowManyMonke.OnCheck = (Action<int>)Delegate.Remove(HowManyMonke.OnCheck, new Action<int>(HowManyMonke_OnCheck));
	}

	private void HowManyMonke_OnCheck(int thisMany)
	{
		currValue = nextValue;
		nextValue = thisMany;
		checkTime = Time.time;
	}

	public void SliceUpdate()
	{
		float time = Mathf.Lerp(currValue, nextValue, (Time.time - checkTime) / HowManyMonke.RecheckDelay);
		text.text = time.ToString("N0");
		ParticleSystem.EmissionModule emission = particleSystem.emission;
		emission.rateOverTime = particleSystemRateToCount.Evaluate(time);
		float sqrMagnitude = (VRRig.LocalRig.transform.position - base.transform.position).sqrMagnitude;
		if (observable && sqrMagnitude > observableDistance)
		{
			observable = false;
			HowManyMonke.OnCheck = (Action<int>)Delegate.Remove(HowManyMonke.OnCheck, new Action<int>(HowManyMonke_OnCheck));
			if ((bool)observableActive)
			{
				observableActive.SetActive(observable);
			}
		}
		else if (!observable && sqrMagnitude < observableDistance)
		{
			observable = true;
			HowManyMonke.OnCheck = (Action<int>)Delegate.Combine(HowManyMonke.OnCheck, new Action<int>(HowManyMonke_OnCheck));
			if ((bool)observableActive)
			{
				observableActive.SetActive(observable);
			}
		}
	}
}
