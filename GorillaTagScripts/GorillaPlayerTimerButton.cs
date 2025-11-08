using UnityEngine;

namespace GorillaTagScripts;

public class GorillaPlayerTimerButton : MonoBehaviour
{
	private float lastTriggeredTime;

	[SerializeField]
	private bool isStartButton;

	[SerializeField]
	private bool isBothStartAndStop;

	[SerializeField]
	private float debounceTime = 0.5f;

	[SerializeField]
	private MeshRenderer mesh;

	[SerializeField]
	private Color pressColor;

	[SerializeField]
	private Color notPressedColor;

	private MaterialPropertyBlock materialProps;

	private bool isInitialized;

	private void Awake()
	{
		materialProps = new MaterialPropertyBlock();
	}

	private void Start()
	{
		TryInit();
	}

	private void OnEnable()
	{
		TryInit();
	}

	private void TryInit()
	{
		if (!isInitialized && !(PlayerTimerManager.instance == null))
		{
			PlayerTimerManager.instance.OnTimerStopped.AddListener(OnTimerStopped);
			PlayerTimerManager.instance.OnLocalTimerStarted.AddListener(OnLocalTimerStarted);
			if (isBothStartAndStop)
			{
				isStartButton = !PlayerTimerManager.instance.IsLocalTimerStarted();
			}
			isInitialized = true;
		}
	}

	private void OnDisable()
	{
		if (PlayerTimerManager.instance != null)
		{
			PlayerTimerManager.instance.OnTimerStopped.RemoveListener(OnTimerStopped);
			PlayerTimerManager.instance.OnLocalTimerStarted.RemoveListener(OnLocalTimerStarted);
		}
		isInitialized = false;
	}

	private void OnLocalTimerStarted()
	{
		if (isBothStartAndStop)
		{
			isStartButton = false;
		}
	}

	private void OnTimerStopped(int actorNum, int timeDelta)
	{
		if (isBothStartAndStop && actorNum == NetworkSystem.Instance.LocalPlayer.ActorNumber)
		{
			isStartButton = true;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (base.enabled)
		{
			GorillaTriggerColliderHandIndicator componentInParent = other.GetComponentInParent<GorillaTriggerColliderHandIndicator>();
			if (!(componentInParent == null) && !(Time.time < lastTriggeredTime + debounceTime) && NetworkSystem.Instance.InRoom)
			{
				GorillaTagger.Instance.StartVibration(componentInParent.isLeftHand, GorillaTagger.Instance.tapHapticStrength, GorillaTagger.Instance.tapHapticDuration);
				mesh.GetPropertyBlock(materialProps);
				materialProps.SetColor(ShaderProps._BaseColor, pressColor);
				mesh.SetPropertyBlock(materialProps);
				PlayerTimerManager.instance.RequestTimerToggle(isStartButton);
				lastTriggeredTime = Time.time;
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (base.enabled && !(other.GetComponentInParent<GorillaTriggerColliderHandIndicator>() == null))
		{
			mesh.GetPropertyBlock(materialProps);
			materialProps.SetColor(ShaderProps._BaseColor, notPressedColor);
			mesh.SetPropertyBlock(materialProps);
		}
	}
}
