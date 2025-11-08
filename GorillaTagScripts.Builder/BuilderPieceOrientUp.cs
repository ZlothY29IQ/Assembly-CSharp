using BoingKit;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderPieceOrientUp : MonoBehaviour, IBuilderPieceComponent
{
	[SerializeField]
	private Transform alwaysFaceUp;

	public void OnPieceCreate(int pieceType, int pieceId)
	{
	}

	public void OnPieceDestroy()
	{
	}

	public void OnPiecePlacementDeserialized()
	{
		if (alwaysFaceUp != null)
		{
			QuaternionUtil.DecomposeSwingTwist(alwaysFaceUp.parent.rotation, Vector3.up, out var _, out var twist);
			alwaysFaceUp.rotation = twist;
		}
	}

	public void OnPieceActivate()
	{
		if (alwaysFaceUp != null)
		{
			QuaternionUtil.DecomposeSwingTwist(alwaysFaceUp.parent.rotation, Vector3.up, out var _, out var twist);
			alwaysFaceUp.rotation = twist;
		}
	}

	public void OnPieceDeactivate()
	{
	}
}
