using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GRDamageFlash
{
	public enum State
	{
		Idle,
		Playing,
		Cooldown
	}

	public Material flashMaterial;

	public float flashDuration = 0.1f;

	public float flashCooldown = 0.1f;

	public List<Renderer> flashRenderers;

	private SimpleStateMachine<State> stateMachine;

	private List<Material> flashRendererDefaultMaterial;

	public void Setup()
	{
		flashRendererDefaultMaterial = new List<Material>(flashRenderers.Count);
		stateMachine = new SimpleStateMachine<State>();
		for (int i = 0; i < flashRenderers.Count; i++)
		{
			flashRendererDefaultMaterial.Add(flashRenderers[i].sharedMaterial);
		}
		stateMachine.Setup(State.Idle, OnStateStart, OnStateEnd, OnStateUpdate);
	}

	public void Play()
	{
		if (stateMachine.GetState() == State.Idle)
		{
			stateMachine.SetState(State.Playing);
		}
	}

	public void OnStateStart(State state)
	{
		if (state == State.Playing)
		{
			for (int i = 0; i < flashRenderers.Count; i++)
			{
				flashRenderers[i].material = flashMaterial;
			}
		}
	}

	public void OnStateEnd(State state)
	{
		if (state == State.Playing)
		{
			for (int i = 0; i < flashRenderers.Count; i++)
			{
				flashRenderers[i].material = flashRendererDefaultMaterial[i];
			}
		}
	}

	public void OnStateUpdate(State state)
	{
		switch (state)
		{
		case State.Playing:
			if (stateMachine.IsStateFinished(Time.timeAsDouble, flashDuration))
			{
				stateMachine.SetState((flashCooldown > 0f) ? State.Cooldown : State.Idle);
			}
			break;
		case State.Cooldown:
			if (stateMachine.IsStateFinished(Time.timeAsDouble, flashCooldown))
			{
				stateMachine.SetState(State.Idle);
			}
			break;
		}
	}

	public void Stop()
	{
		stateMachine.SetState(State.Idle);
	}

	public void Update()
	{
		stateMachine.Update();
	}
}
