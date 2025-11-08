using System;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaTagScripts.Builder;

public class BuilderPieceParticleEmitter : MonoBehaviour, IBuilderPieceComponent
{
	[SerializeField]
	private BuilderPiece myPiece;

	[SerializeField]
	private List<ParticleSystem> particles;

	private bool inBuilderZone;

	private bool isPieceActive;

	private void OnZoneChanged()
	{
		inBuilderZone = ZoneManagement.instance.IsZoneActive(myPiece.GetTable().tableZone);
		if (inBuilderZone && isPieceActive)
		{
			StartParticles();
		}
		else if (!inBuilderZone)
		{
			StopParticles();
		}
	}

	private void StopParticles()
	{
		foreach (ParticleSystem particle in particles)
		{
			if (particle.isPlaying)
			{
				particle.Stop();
				particle.Clear();
			}
		}
	}

	private void StartParticles()
	{
		foreach (ParticleSystem particle in particles)
		{
			if (!particle.isPlaying)
			{
				particle.Play();
			}
		}
	}

	public void OnPieceCreate(int pieceType, int pieceId)
	{
		StopParticles();
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Combine(instance.onZoneChanged, new Action(OnZoneChanged));
		OnZoneChanged();
	}

	public void OnPieceDestroy()
	{
		ZoneManagement instance = ZoneManagement.instance;
		instance.onZoneChanged = (Action)Delegate.Remove(instance.onZoneChanged, new Action(OnZoneChanged));
	}

	public void OnPiecePlacementDeserialized()
	{
	}

	public void OnPieceActivate()
	{
		isPieceActive = true;
		if (inBuilderZone)
		{
			StartParticles();
		}
	}

	public void OnPieceDeactivate()
	{
		isPieceActive = false;
		StopParticles();
	}
}
