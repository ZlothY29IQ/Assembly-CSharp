using UnityEngine;

namespace GorillaTagScripts;

public class Flower : MonoBehaviour
{
	public enum FlowerState
	{
		None = -1,
		Healthy,
		Middle,
		Wilted
	}

	private Animator anim;

	private SkinnedMeshRenderer meshRenderer;

	[HideInInspector]
	public GorillaTimer timer;

	private BeePerchPoint perchPoint;

	public ParticleSystem wateredFx;

	public ParticleSystem sparkleFx;

	public GameObject meshStatesGameObject;

	public GameObject[] meshStates;

	private static readonly int healthy_to_middle = Animator.StringToHash("healthy_to_middle");

	private static readonly int middle_to_healthy = Animator.StringToHash("middle_to_healthy");

	private static readonly int wilted_to_middle = Animator.StringToHash("wilted_to_middle");

	private static readonly int middle_to_wilted = Animator.StringToHash("middle_to_wilted");

	private FlowerState currentState;

	private string id;

	private bool shouldUpdateVisuals;

	private FlowerState lastState;

	public bool IsWatered { get; private set; }

	private void Awake()
	{
		shouldUpdateVisuals = true;
		anim = GetComponent<Animator>();
		timer = GetComponent<GorillaTimer>();
		perchPoint = GetComponent<BeePerchPoint>();
		timer.onTimerStopped.AddListener(HandleOnFlowerTimerEnded);
		currentState = FlowerState.None;
		wateredFx = wateredFx.GetComponent<ParticleSystem>();
		IsWatered = false;
		meshRenderer = GetComponent<SkinnedMeshRenderer>();
		meshRenderer.enabled = false;
		anim.enabled = false;
	}

	private void OnDestroy()
	{
		timer.onTimerStopped.RemoveListener(HandleOnFlowerTimerEnded);
	}

	public void WaterFlower(bool isWatered = false)
	{
		IsWatered = isWatered;
		switch (currentState)
		{
		case FlowerState.None:
			UpdateFlowerState(FlowerState.Healthy);
			break;
		case FlowerState.Middle:
			if (isWatered)
			{
				UpdateFlowerState(FlowerState.Healthy, isWatered: true);
			}
			else
			{
				UpdateFlowerState(FlowerState.Wilted);
			}
			break;
		case FlowerState.Healthy:
			if (!isWatered)
			{
				UpdateFlowerState(FlowerState.Middle);
			}
			break;
		case FlowerState.Wilted:
			if (isWatered)
			{
				UpdateFlowerState(FlowerState.Middle, isWatered: true);
			}
			break;
		}
	}

	public void UpdateFlowerState(FlowerState newState, bool isWatered = false, bool updateVisual = true)
	{
		if (FlowersManager.Instance.IsMine)
		{
			timer.RestartTimer();
		}
		ChangeState(newState);
		if ((bool)perchPoint)
		{
			perchPoint.enabled = currentState == FlowerState.Healthy;
		}
		if (updateVisual)
		{
			LocalUpdateFlowers(newState, isWatered);
		}
	}

	private void LocalUpdateFlowers(FlowerState state, bool isWatered = false)
	{
		GameObject[] array = meshStates;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		if (!shouldUpdateVisuals)
		{
			meshStates[(int)currentState].SetActive(value: true);
			return;
		}
		if (isWatered && (bool)wateredFx)
		{
			wateredFx.Play();
		}
		meshRenderer.enabled = true;
		anim.enabled = true;
		switch (state)
		{
		case FlowerState.Healthy:
			anim.SetTrigger(middle_to_healthy);
			break;
		case FlowerState.Middle:
			if (lastState == FlowerState.Wilted)
			{
				anim.SetTrigger(wilted_to_middle);
			}
			else
			{
				anim.SetTrigger(healthy_to_middle);
			}
			break;
		case FlowerState.Wilted:
			anim.SetTrigger(middle_to_wilted);
			break;
		}
	}

	private void HandleOnFlowerTimerEnded(GorillaTimer _timer)
	{
		if (FlowersManager.Instance.IsMine && timer == _timer)
		{
			WaterFlower();
		}
	}

	private void ChangeState(FlowerState state)
	{
		lastState = currentState;
		currentState = state;
	}

	public FlowerState GetCurrentState()
	{
		return currentState;
	}

	public void OnAnimationIsDone(int state)
	{
		if (meshRenderer.enabled)
		{
			for (int i = 0; i < meshStates.Length; i++)
			{
				bool active = i == (int)currentState;
				meshStates[i].SetActive(active);
			}
			anim.enabled = false;
			meshRenderer.enabled = false;
		}
	}

	public void UpdateVisuals(bool enable)
	{
		shouldUpdateVisuals = enable;
		meshStatesGameObject.SetActive(enable);
	}

	public void AnimCatch()
	{
		if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
		{
			OnAnimationIsDone(0);
		}
	}
}
