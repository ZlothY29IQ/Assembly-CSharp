using System;
using UnityEngine;

public class SetStateConditional : StateMachineBehaviour
{
	public Animator parentAnimator;

	public string setToState;

	[SerializeField]
	private AnimStateHash _setToID;

	public float delay = 1f;

	protected TimeSince _sinceEnter;

	[NonSerialized]
	private bool _didSetup;

	private void OnValidate()
	{
		_setToID = setToState;
	}

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (!_didSetup)
		{
			parentAnimator = animator;
			Setup(animator, stateInfo, layerIndex);
			_didSetup = true;
		}
		_sinceEnter = TimeSince.Now();
	}

	public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if ((!(delay > 0f) || _sinceEnter.HasElapsed(delay, resetOnElapsed: true)) && CanSetState(animator, stateInfo, layerIndex))
		{
			animator.Play(_setToID);
		}
	}

	protected virtual void Setup(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
	}

	protected virtual bool CanSetState(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		return true;
	}
}
