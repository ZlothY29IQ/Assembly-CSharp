using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RandomAnimationSelector : MonoBehaviour, IGorillaSliceableSimple
{
	[SerializeField]
	private string animationTriggerName;

	private int animationTrigger;

	[SerializeField]
	private string animationSelectName;

	private int animationSelect;

	[Range(0f, 1f)]
	[SerializeField]
	private float animationChancePerSecond = 0.33f;

	private Animator animator;

	private float lastSliceUpdateTime;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		animationTrigger = Animator.StringToHash(animationTriggerName);
		animationSelect = Animator.StringToHash(animationSelectName);
	}

	public void OnEnable()
	{
		if (animator != null)
		{
			GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
			lastSliceUpdateTime = Time.time;
		}
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
	}

	public void SliceUpdate()
	{
		float num = Time.time - lastSliceUpdateTime;
		lastSliceUpdateTime = Time.time;
		float num2 = 1f - Mathf.Exp((0f - animationChancePerSecond) * num);
		if (Random.value < num2)
		{
			float value = Time.time - (float)(int)Time.time;
			animator.SetFloat(animationSelect, value);
			animator.SetTrigger(animationTrigger);
		}
	}
}
