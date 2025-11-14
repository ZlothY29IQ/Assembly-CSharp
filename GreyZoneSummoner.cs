using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class GreyZoneSummoner : MonoBehaviour
{
	[SerializeField]
	private Transform summoningFocusPoint;

	[SerializeField]
	private Transform candlesParent;

	[SerializeField]
	private PlayableDirector candlesTimeline;

	[SerializeField]
	private TriggerEventNotifier areaTriggerNotifier;

	[SerializeField]
	private SphereCollider areaTriggerCollider;

	[SerializeField]
	private GorillaPressableButton greyZoneActivationButton;

	[SerializeField]
	private List<AudioSource> summoningTones = new List<AudioSource>();

	[SerializeField]
	private float summoningTonesMaxVolume = 1f;

	[SerializeField]
	private float summoningTonesFadeOverlap = 0.5f;

	[SerializeField]
	private float summoningTonesFadeTime = 4f;

	[SerializeField]
	private List<GorillaPressableButton> greyZoneGravityFactorButtons = new List<GorillaPressableButton>();

	private GreyZoneManager greyZoneManager;

	public Vector3 SummoningFocusPoint => summoningFocusPoint.position;

	public float SummonerMaxDistance => areaTriggerCollider.radius + 1f;

	private void OnEnable()
	{
		greyZoneManager = GreyZoneManager.Instance;
		if (!(greyZoneManager == null))
		{
			greyZoneManager.RegisterSummoner(this);
			areaTriggerNotifier.TriggerEnterEvent += ColliderEnteredArea;
			areaTriggerNotifier.TriggerExitEvent += ColliderExitedArea;
		}
	}

	private void OnDisable()
	{
		if (GreyZoneManager.Instance != null)
		{
			GreyZoneManager.Instance.DeregisterSummoner(this);
		}
		areaTriggerNotifier.TriggerEnterEvent -= ColliderEnteredArea;
		areaTriggerNotifier.TriggerExitEvent -= ColliderExitedArea;
	}

	public void UpdateProgressFeedback(bool greyZoneAvailable)
	{
		if (greyZoneManager == null)
		{
			return;
		}
		if (greyZoneAvailable && !candlesParent.gameObject.activeSelf)
		{
			candlesParent.gameObject.SetActive(value: true);
		}
		candlesTimeline.time = (double)Mathf.Clamp01(greyZoneManager.SummoningProgress) * candlesTimeline.duration;
		candlesTimeline.Evaluate();
		if (!greyZoneManager.GreyZoneActive)
		{
			float value = (float)summoningTones.Count * greyZoneManager.SummoningProgress;
			for (int i = 0; i < summoningTones.Count; i++)
			{
				float num = Mathf.InverseLerp(i, (float)i + 1f + summoningTonesFadeOverlap, value);
				summoningTones[i].volume = num * summoningTonesMaxVolume;
			}
		}
		greyZoneActivationButton.isOn = greyZoneManager.GreyZoneActive;
		greyZoneActivationButton.UpdateColor();
		for (int j = 0; j < greyZoneGravityFactorButtons.Count; j++)
		{
			greyZoneGravityFactorButtons[j].isOn = greyZoneManager.GravityFactorSelection == j;
			greyZoneGravityFactorButtons[j].UpdateColor();
		}
	}

	public void OnGreyZoneActivated()
	{
		StopAllCoroutines();
		StartCoroutine(FadeOutSummoningTones());
	}

	private IEnumerator FadeOutSummoningTones()
	{
		float fadeStartTime = Time.time;
		float fadeRate = 1f / summoningTonesFadeTime;
		while (Time.time < fadeStartTime + summoningTonesFadeTime)
		{
			for (int i = 0; i < summoningTones.Count; i++)
			{
				summoningTones[i].volume = Mathf.MoveTowards(summoningTones[i].volume, 0f, summoningTonesMaxVolume * fadeRate * Time.deltaTime);
			}
			yield return null;
		}
		for (int j = 0; j < summoningTones.Count; j++)
		{
			summoningTones[j].volume = 0f;
		}
	}

	public void ColliderEnteredArea(TriggerEventNotifier notifier, Collider other)
	{
		ZoneEntityBSP component = other.GetComponent<ZoneEntityBSP>();
		VRRig vRRig = ((component != null) ? component.entityRig : null);
		if (vRRig != null && greyZoneManager != null)
		{
			greyZoneManager.VRRigEnteredSummonerProximity(vRRig, this);
		}
	}

	public void ColliderExitedArea(TriggerEventNotifier notifier, Collider other)
	{
		ZoneEntityBSP component = other.GetComponent<ZoneEntityBSP>();
		VRRig vRRig = ((component != null) ? component.entityRig : null);
		if (vRRig != null && greyZoneManager != null)
		{
			greyZoneManager.VRRigExitedSummonerProximity(vRRig, this);
		}
	}
}
