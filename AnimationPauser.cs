using System.Threading.Tasks;
using UnityEngine;

public class AnimationPauser : StateMachineBehaviour
{
	[SerializeField]
	private int _maxTimeBetweenAnims = 5;

	[SerializeField]
	private int _minTimeBetweenAnims = 1;

	private int _animPauseDuration;

	private static readonly string Restart_Anim_Name = "RestartAnim";

	public override async void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateEnter(animator, stateInfo, layerIndex);
		_animPauseDuration = Random.Range(_minTimeBetweenAnims, _maxTimeBetweenAnims);
		await Task.Delay(_animPauseDuration * 1000);
		animator.SetTrigger(Restart_Anim_Name);
	}
}
