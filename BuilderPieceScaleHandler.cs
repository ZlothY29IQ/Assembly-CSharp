using System.Collections.Generic;
using GorillaTagScripts.Builder;
using UnityEngine;

public class BuilderPieceScaleHandler : MonoBehaviour, IBuilderPieceComponent
{
	[SerializeField]
	private BuilderPiece myPiece;

	[SerializeField]
	private List<BuilderScaleAudioRadius> audioScalers = new List<BuilderScaleAudioRadius>();

	[SerializeField]
	private List<BuilderScaleParticles> particleScalers = new List<BuilderScaleParticles>();

	public void OnPieceCreate(int pieceType, int pieceId)
	{
		foreach (BuilderScaleAudioRadius audioScaler in audioScalers)
		{
			audioScaler.SetScale(myPiece.GetScale());
		}
		foreach (BuilderScaleParticles particleScaler in particleScalers)
		{
			particleScaler.SetScale(myPiece.GetScale());
		}
	}

	public void OnPieceDestroy()
	{
		foreach (BuilderScaleAudioRadius audioScaler in audioScalers)
		{
			audioScaler.RevertScale();
		}
		foreach (BuilderScaleParticles particleScaler in particleScalers)
		{
			particleScaler.RevertScale();
		}
	}

	public void OnPiecePlacementDeserialized()
	{
	}

	public void OnPieceActivate()
	{
	}

	public void OnPieceDeactivate()
	{
	}
}
