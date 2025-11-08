using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderAnimateOnTap : BuilderPieceTappable
{
	[SerializeField]
	private Animation anim;

	public override void OnTapReplicated()
	{
		base.OnTapReplicated();
		anim.Rewind();
		anim.Play();
	}
}
