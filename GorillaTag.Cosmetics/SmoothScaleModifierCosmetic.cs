using UnityEngine;
using UnityEngine.Events;

namespace GorillaTag.Cosmetics;

public class SmoothScaleModifierCosmetic : MonoBehaviour
{
	private enum State
	{
		None,
		Reset,
		Scaling,
		Scaled
	}

	[Tooltip("The GameObject to scale up or down. This should reference the cosmetic mesh or object you want to visually modify.")]
	[SerializeField]
	private GameObject objectPrefab;

	[Tooltip("The target scale applied when scaling is triggered.")]
	[SerializeField]
	private Vector3 targetScale = new Vector3(2f, 2f, 2f);

	[Tooltip("Speed at which the object scales toward its target or initial size")]
	[SerializeField]
	private float speed = 2f;

	[Tooltip("Invoked once when the object reaches the target scale.")]
	public UnityEvent onScaled;

	[Tooltip("Invoked once when the object returns to its initial scale.")]
	public UnityEvent onReset;

	private State currentState;

	private Vector3 initialScale;

	private void Awake()
	{
		initialScale = objectPrefab.transform.localScale;
	}

	private void OnEnable()
	{
		UpdateState(State.Reset);
	}

	private void Update()
	{
		switch (currentState)
		{
		case State.Reset:
			SmoothScale(objectPrefab.transform.localScale, initialScale);
			if (Vector3.Distance(objectPrefab.transform.localScale, initialScale) < 0.01f)
			{
				objectPrefab.transform.localScale = initialScale;
				if (onReset != null)
				{
					onReset.Invoke();
				}
				UpdateState(State.None);
			}
			break;
		case State.Scaling:
			SmoothScale(objectPrefab.transform.localScale, targetScale);
			if (Vector3.Distance(objectPrefab.transform.localScale, targetScale) < 0.01f)
			{
				objectPrefab.transform.localScale = targetScale;
				if (onScaled != null)
				{
					onScaled.Invoke();
				}
				UpdateState(State.Scaled);
			}
			break;
		case State.None:
		case State.Scaled:
			break;
		}
	}

	private void SmoothScale(Vector3 initial, Vector3 target)
	{
		objectPrefab.transform.localScale = Vector3.MoveTowards(initial, target, speed * Time.deltaTime);
	}

	private void UpdateState(State newState)
	{
		currentState = newState;
	}

	public void TriggerScale()
	{
		if (currentState != State.Scaled)
		{
			UpdateState(State.Scaling);
		}
	}

	public void TriggerReset()
	{
		if (currentState != State.Reset)
		{
			UpdateState(State.Reset);
		}
	}
}
