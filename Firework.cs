using System.Linq;
using UnityEngine;

public class Firework : MonoBehaviour
{
	[SerializeField]
	private FireworksController _controller;

	[Space]
	public Transform origin;

	public Transform target;

	[Space]
	public Color colorOrigin = Color.cyan;

	public Color colorTarget = Color.magenta;

	[Space]
	public AudioSource sourceOrigin;

	public AudioSource sourceTarget;

	[Space]
	public ParticleSystem trail;

	[Space]
	public ParticleSystem[] explosions;

	[Space]
	public bool doTrail = true;

	public bool doTrailAudio = true;

	public bool doExplosion = true;

	private void Launch()
	{
		if (Application.isPlaying && (bool)_controller)
		{
			_controller.Launch(this);
		}
	}

	private void OnValidate()
	{
		if (!_controller)
		{
			_controller = GetComponentInParent<FireworksController>();
		}
		if (!_controller)
		{
			return;
		}
		Firework[] fireworks = _controller.fireworks;
		if (!fireworks.Contains(this))
		{
			fireworks = (from x in fireworks.Concat(new Firework[1] { this })
				where x != null
				select x).ToArray();
			_controller.fireworks = fireworks;
		}
	}

	private void OnDrawGizmos()
	{
		if ((bool)_controller)
		{
			_controller.RenderGizmo(this, Color.cyan);
		}
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)_controller)
		{
			_controller.RenderGizmo(this, Color.yellow);
		}
	}
}
