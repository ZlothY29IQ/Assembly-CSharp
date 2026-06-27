using System;
using UnityEngine;
using UnityEngine.Audio;
using Voxels;

public class Voxel_Pickaxe : MonoBehaviour, IGameEntityComponent
{
	[Serializable]
	public struct InteractionPoint
	{
		public Transform transform;

		public Vector3 previousPosition;

		public Vector3 position;
	}

	public VoxelAction mine = new VoxelAction
	{
		strength = 1f,
		radius = 0.5f,
		operation = OperationType.Subtract
	};

	public InteractionPoint[] points;

	public AudioResource goodHit;

	public AudioResource badHit;

	public AudioSource sound;

	public float hitCooldown = 0.5f;

	public float minHitSpeed = 1f;

	public float minMineSpeed = 5f;

	public float alignThreshold = 0.7f;

	private GameEntity _gameEntity;

	private int _layerMask;

	private float _nextHitTime;

	private bool _isLocal;

	public bool Held { get; set; }

	private void Reset()
	{
		_layerMask = LayerMask.GetMask("Default");
	}

	private void Awake()
	{
		_gameEntity = GetComponent<GameEntity>();
		_layerMask = LayerMask.GetMask("Default");
		if (sound.transform == base.transform)
		{
			Debug.LogError("Audio source for " + base.name + " must be on a separate gameobject!", this);
		}
	}

	private void OnEnable()
	{
		if (_gameEntity != null)
		{
			GameEntity gameEntity = _gameEntity;
			gameEntity.OnGrabbed = (Action)Delegate.Combine(gameEntity.OnGrabbed, new Action(StartGrabbing));
			GameEntity gameEntity2 = _gameEntity;
			gameEntity2.OnReleased = (Action)Delegate.Combine(gameEntity2.OnReleased, new Action(StopGrabbing));
		}
		_isLocal = GetComponentInParent<VRRig>() == VRRig.LocalRig;
		ResetVelocity();
	}

	private void OnDisable()
	{
		if (_gameEntity != null)
		{
			GameEntity gameEntity = _gameEntity;
			gameEntity.OnGrabbed = (Action)Delegate.Remove(gameEntity.OnGrabbed, new Action(StartGrabbing));
			GameEntity gameEntity2 = _gameEntity;
			gameEntity2.OnReleased = (Action)Delegate.Remove(gameEntity2.OnReleased, new Action(StopGrabbing));
		}
	}

	private void FixedUpdate()
	{
		if (Held)
		{
			for (int i = 0; i < points.Length; i++)
			{
				UpdateInteractionPoint(ref points[i]);
			}
		}
	}

	private void StartGrabbing()
	{
		Held = true;
		VRRig componentInParent = GetComponentInParent<VRRig>();
		_isLocal = componentInParent == VRRig.LocalRig;
		ResetVelocity();
	}

	private void StopGrabbing()
	{
		Held = false;
	}

	private void ResetVelocity()
	{
		for (int i = 0; i < points.Length; i++)
		{
			points[i].position = (points[i].previousPosition = points[i].transform.position);
		}
	}

	private void UpdateInteractionPoint(ref InteractionPoint point)
	{
		point.previousPosition = point.position;
		point.position = point.transform.position;
		if (Time.time < _nextHitTime)
		{
			return;
		}
		Vector3 vector = (point.position - point.previousPosition) / Time.fixedDeltaTime;
		float magnitude = vector.magnitude;
		if (magnitude < minHitSpeed)
		{
			return;
		}
		bool flag = Vector3.Dot(vector.normalized, point.transform.forward) >= alignThreshold;
		if (!Physics.Linecast(point.previousPosition, point.position, out var hitInfo, _layerMask, QueryTriggerInteraction.Ignore))
		{
			return;
		}
		ChunkComponent component = hitInfo.collider.GetComponent<ChunkComponent>();
		if ((bool)component && flag && magnitude >= minMineSpeed)
		{
			Play(goodHit, hitInfo.point);
			if (_isLocal)
			{
				component.World.Mine(hitInfo, mine);
			}
		}
		else
		{
			Play(badHit, hitInfo.point);
		}
		_nextHitTime = Time.time + hitCooldown;
	}

	private void Play(AudioResource resource, Vector3 position)
	{
		if ((bool)resource)
		{
			sound.Stop();
			sound.resource = resource;
			sound.transform.position = position;
			sound.Play();
		}
	}

	public void OnEntityInit()
	{
		if (sound != null)
		{
			sound.transform.parent = null;
		}
	}

	public void OnEntityDestroy()
	{
		if (!ApplicationQuittingState.IsQuitting && !(this == null) && !(sound == null) && base.gameObject.scene.isLoaded)
		{
			sound.transform.parent = base.transform;
		}
	}

	public void OnEntityStateChange(long prevState, long newState)
	{
	}

	private void OnDrawGizmosSelected()
	{
		if (points != null)
		{
			Gizmos.color = Color.green;
			InteractionPoint[] array = points;
			for (int i = 0; i < array.Length; i++)
			{
				InteractionPoint interactionPoint = array[i];
				Gizmos.DrawWireSphere(interactionPoint.transform.position, 0.02f);
				Gizmos.DrawLine(interactionPoint.transform.position, interactionPoint.transform.position + interactionPoint.transform.forward * 0.5f);
			}
		}
	}
}
