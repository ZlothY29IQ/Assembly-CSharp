using UnityEngine;

public class SetStateIfNoOverlaps : SetStateConditional
{
	public VolumeCast _volume;

	protected override void Setup(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		_volume = animator.GetComponent<VolumeCast>();
	}

	protected override bool CanSetState(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		bool num = _volume.CheckOverlaps();
		if (num)
		{
			_sinceEnter = 0f;
		}
		return !num;
	}
}
