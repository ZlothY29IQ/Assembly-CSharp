using System;
using System.Collections.Generic;
using GorillaNetworking;
using UnityEngine;

public class GRReviveStation : MonoBehaviour
{
	public AudioSource audioSource;

	public ParticleSystem[] particleEffects;

	[SerializeField]
	private double reviveCooldownSeconds;

	private Dictionary<int, DateTime> cooldownStartTime = new Dictionary<int, DateTime>();

	private GhostReactor reactor;

	public int Index { get; set; }

	public void Init(GhostReactor reactor, int index)
	{
		this.reactor = reactor;
		Index = index;
	}

	public void SetReviveCooldownSeconds(double seconds)
	{
		reviveCooldownSeconds = seconds;
	}

	public double GetReviveCooldownSeconds()
	{
		return reviveCooldownSeconds;
	}

	public double CalculateRemainingReviveCooldownSeconds(int ActorNumber)
	{
		if (reviveCooldownSeconds == 0.0)
		{
			return 0.0;
		}
		if (cooldownStartTime.ContainsKey(ActorNumber))
		{
			return reviveCooldownSeconds - (GorillaComputer.instance.GetServerTime() - cooldownStartTime[ActorNumber]).TotalSeconds;
		}
		return 0.0;
	}

	public void RevivePlayer(GRPlayer player)
	{
		if (!(player != null))
		{
			return;
		}
		int actorNumber = player.gamePlayer.rig.OwningNetPlayer.ActorNumber;
		cooldownStartTime[actorNumber] = GorillaComputer.instance.GetServerTime();
		if (player.State == GRPlayer.GRPlayerState.Alive && player.Hp >= player.MaxHp)
		{
			return;
		}
		player.OnPlayerRevive(reactor.grManager);
		if (audioSource != null)
		{
			audioSource.Play();
		}
		if (particleEffects != null)
		{
			for (int i = 0; i < particleEffects.Length; i++)
			{
				particleEffects[i].Play();
			}
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		Rigidbody attachedRigidbody = collider.attachedRigidbody;
		if (!(attachedRigidbody != null))
		{
			return;
		}
		VRRig component = attachedRigidbody.GetComponent<VRRig>();
		if (!(component != null))
		{
			return;
		}
		GRPlayer component2 = component.GetComponent<GRPlayer>();
		if (component2 != null && (component2.State != GRPlayer.GRPlayerState.Alive || component2.Hp < component2.MaxHp))
		{
			if (!NetworkSystem.Instance.InRoom && component == VRRig.LocalRig)
			{
				RevivePlayer(component2);
			}
			if (reactor.grManager.IsAuthority() && CalculateRemainingReviveCooldownSeconds(component2.gamePlayer.rig.OwningNetPlayer.ActorNumber) <= 0.0)
			{
				reactor.grManager.RequestPlayerRevive(this, component2);
			}
		}
	}
}
