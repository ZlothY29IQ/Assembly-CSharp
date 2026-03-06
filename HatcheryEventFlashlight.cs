using UnityEngine;

public class HatcheryEventFlashlight : MonoBehaviourTick, IGorillaSliceableSimple
{
	private const float lightMaxDistance = 6f;

	private const float surfaceOffset = 1f;

	private const float enableThresholdCurl = 0.33f;

	private const float maxEnergy = 10f;

	private const float energyUsageRate = 1f;

	private const float energyChargeRate = 0.66f;

	private RaycastHit[] hits = new RaycastHit[20];

	private Light[] lightComponents;

	private GameLight[] gameLightComponents;

	private VRRig parentRig;

	private float currentEnergy;

	private float startingBrightness;

	private float lastUpdated;

	private bool playerLight;

	private bool wasLightEnabled;

	private bool wasLightSwitchedOn;

	public Transform lightStart;

	public Transform lightsParent;

	public Transform flashlight;

	public Transform[] lights;

	public AudioSource clickSource;

	private void Awake()
	{
		parentRig = GetComponentInParent<VRRig>();
		playerLight = parentRig.isOfflineVRRig;
		currentEnergy = 10f;
		lightComponents = new Light[lights.Length];
		gameLightComponents = new GameLight[lights.Length];
		for (int i = 0; i < lights.Length; i++)
		{
			lightComponents[i] = lights[i].GetComponent<Light>();
			gameLightComponents[i] = lights[i].GetComponent<GameLight>();
		}
		startingBrightness = lightComponents[0].intensity;
		lightsParent.gameObject.SetActive(value: false);
	}

	private new void OnEnable()
	{
		base.OnEnable();
		if (!playerLight)
		{
			GorillaSlicerSimpleManager.RegisterSliceable(this);
		}
	}

	private new void OnDisable()
	{
		base.OnDisable();
		if (!playerLight)
		{
			GorillaSlicerSimpleManager.UnregisterSliceable(this);
		}
	}

	private float MaxEnergy()
	{
		if (NetworkSystem.Instance.CurrentRoom == null)
		{
			return 10f;
		}
		return 10f * (1f / Mathf.Log((float)NetworkSystem.Instance.RoomPlayerCount + 1.72f));
	}

	public override void Tick()
	{
		if (playerLight)
		{
			SliceUpdate();
		}
	}

	public void SliceUpdate()
	{
		if (GameLightingManager.instance.IsDynamicLightingEnabled != flashlight.gameObject.activeSelf)
		{
			flashlight.gameObject.SetActive(GameLightingManager.instance.IsDynamicLightingEnabled);
		}
		if (GameLightingManager.instance.IsDynamicLightingEnabled)
		{
			float time = Time.time;
			float num = MaxEnergy();
			if (wasLightEnabled)
			{
				currentEnergy -= (time - lastUpdated) * 1f;
			}
			else
			{
				currentEnergy += (time - lastUpdated) * 0.66f;
			}
			currentEnergy = Mathf.Clamp(currentEnergy, 0f, MaxEnergy());
			bool flag = parentRig.rightIndex.calcT >= 0.33f;
			bool flag2 = flag && (!wasLightSwitchedOn || wasLightEnabled) && currentEnergy > 0f;
			if (flag2 != wasLightEnabled)
			{
				lightsParent.gameObject.SetActive(flag2);
				clickSource.Play();
			}
			if (flag2)
			{
				UpdateLightPositioning();
				UpdateLightBrightness(num);
			}
			lastUpdated = Time.time;
			wasLightSwitchedOn = flag;
			wasLightEnabled = flag2;
		}
	}

	private void UpdateLightPositioning()
	{
		int num = Physics.RaycastNonAlloc(lightStart.position, lightStart.forward, hits, 6f, -1, QueryTriggerInteraction.Ignore);
		float num2 = 6f;
		for (int i = 0; i < num; i++)
		{
			if (!(hits[i].distance > num2))
			{
				num2 = hits[i].distance;
			}
		}
		float num3 = ((num2 >= 2f) ? (num2 - 1f) : (num2 / 2f));
		for (int j = 0; j < lights.Length; j++)
		{
			lights[j].position = lightStart.position + lightStart.forward * (num3 * (float)(j + 1) / (float)lights.Length);
		}
	}

	private void UpdateLightBrightness(float _maxEnergy)
	{
		float intensity = startingBrightness / 5f * (1f + 4f * currentEnergy / _maxEnergy);
		for (int i = 0; i < lightComponents.Length; i++)
		{
			lightComponents[i].intensity = intensity;
			gameLightComponents[i].UpdateCachedLightColorAndIntensity();
		}
	}
}
