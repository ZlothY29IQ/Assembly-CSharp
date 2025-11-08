using System.Collections.Generic;
using UnityEngine;

public class ParticleCollisionListener : MonoBehaviour
{
	public ParticleSystem target;

	[SerializeReference]
	private List<ParticleCollisionEvent> _events = new List<ParticleCollisionEvent>();

	private void Awake()
	{
		_events = new List<ParticleCollisionEvent>();
	}

	protected virtual void OnCollisionEvent(ParticleCollisionEvent ev)
	{
	}

	public void OnParticleCollision(GameObject other)
	{
		int collisionEvents = target.GetCollisionEvents(other, _events);
		for (int i = 0; i < collisionEvents; i++)
		{
			OnCollisionEvent(_events[i]);
		}
	}
}
