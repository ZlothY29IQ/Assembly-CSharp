using System;
using GorillaExtensions;
using GorillaTagScripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Flocking : MonoBehaviour
{
	public enum FishState
	{
		flock,
		patrol,
		followFood
	}

	[Tooltip("Speed is randomly generated from min and max speed")]
	public float minSpeed = 2f;

	public float maxSpeed = 4f;

	public float rotationSpeed = 360f;

	[Tooltip("Maximum distance to the neighbours to form a flocking group")]
	public float maxNeighbourDistance = 4f;

	public float eatFoodDuration = 10f;

	[Tooltip("How fast should it follow the food? This value multiplies by the current speed")]
	public float followFoodSpeedMult = 3f;

	[Tooltip("How fast should it run away from players hand?")]
	public float avoidHandSpeed = 1.2f;

	[FormerlySerializedAs("avoidanceDistance")]
	[Tooltip("When flocking they will avoid each other if the distance between them is less than this value")]
	public float flockingAvoidanceDistance = 2f;

	[Tooltip("Follow the fish food until they are this far from it")]
	[FormerlySerializedAs("distanceToFollowFood")]
	public double FollowFoodStopDistance = 0.20000000298023224;

	[Tooltip("Follow any fake fish food until they are this far from it")]
	[FormerlySerializedAs("distanceToFollowFakeFood")]
	public float FollowFakeFoodStopDistance = 2f;

	private float speed;

	private Vector3 averageHeading;

	private Vector3 averagePosition;

	private float feedingTimeStarted;

	private GameObject projectileGameObject;

	private bool followingFood;

	private FlockingManager manager;

	private GameObjectManagerWithId _fishSceneGameObjectsManager;

	private UnityEvent<string, Transform> sendIdEvent;

	private FishState fishState;

	[HideInInspector]
	public Vector3 pos;

	[HideInInspector]
	public Quaternion rot;

	private float velocity;

	private bool isTurning;

	private bool isRealFood;

	public float avointPointRadius = 0.5f;

	private float cacheSpeed;

	public FlockingManager.FishArea FishArea { get; set; }

	private void Awake()
	{
		manager = GetComponentInParent<FlockingManager>();
	}

	private void Start()
	{
		speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
		fishState = FishState.patrol;
	}

	private void OnDisable()
	{
		FlockingManager flockingManager = manager;
		flockingManager.onFoodDetected = (UnityAction<FlockingManager.FishFood>)Delegate.Remove(flockingManager.onFoodDetected, new UnityAction<FlockingManager.FishFood>(HandleOnFoodDetected));
		FlockingManager flockingManager2 = manager;
		flockingManager2.onFoodDestroyed = (UnityAction<BoxCollider>)Delegate.Remove(flockingManager2.onFoodDestroyed, new UnityAction<BoxCollider>(HandleOnFoodDestroyed));
		FlockingUpdateManager.UnregisterFlocking(this);
	}

	public void InvokeUpdate()
	{
		if (manager == null)
		{
			manager = GetComponentInParent<FlockingManager>();
		}
		AvoidPlayerHands();
		MaybeTurn();
		switch (fishState)
		{
		case FishState.patrol:
			if (UnityEngine.Random.Range(0, 10) < 2)
			{
				SwitchState(FishState.flock);
			}
			break;
		case FishState.flock:
			Flock(FishArea.nextWaypoint);
			SwitchState(FishState.patrol);
			break;
		case FishState.followFood:
			if (isTurning)
			{
				return;
			}
			if (isRealFood)
			{
				if ((double)Vector3.Distance(base.transform.position, projectileGameObject.transform.position) > FollowFoodStopDistance)
				{
					FollowFood();
					break;
				}
				followingFood = false;
				Flock(projectileGameObject.transform.position);
				feedingTimeStarted += Time.deltaTime;
				if (feedingTimeStarted > eatFoodDuration)
				{
					SwitchState(FishState.patrol);
				}
			}
			else if (Vector3.Distance(base.transform.position, projectileGameObject.transform.position) > FollowFakeFoodStopDistance)
			{
				FollowFood();
			}
			else
			{
				followingFood = false;
				SwitchState(FishState.patrol);
			}
			break;
		}
		if (!followingFood)
		{
			base.transform.Translate(0f, 0f, speed * Time.deltaTime);
		}
		pos = base.transform.position;
		rot = base.transform.rotation;
	}

	private void MaybeTurn()
	{
		if (!manager.IsInside(base.transform.position, FishArea))
		{
			Turn(FishArea.colliderCenter);
			if (Vector3.Angle(FishArea.colliderCenter - base.transform.position, Vector3.forward) > 5f)
			{
				isTurning = true;
			}
		}
		else
		{
			isTurning = false;
		}
	}

	private void Turn(Vector3 towardPoint)
	{
		isTurning = true;
		Quaternion to = Quaternion.LookRotation(towardPoint - base.transform.position);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, rotationSpeed * Time.deltaTime);
	}

	private void SwitchState(FishState state)
	{
		fishState = state;
	}

	private void Flock(Vector3 nextGoal)
	{
		Vector3 zero = Vector3.zero;
		Vector3 zero2 = Vector3.zero;
		float num = 1f;
		int num2 = 0;
		foreach (Flocking fish in FishArea.fishList)
		{
			if (!(fish.gameObject != base.gameObject))
			{
				continue;
			}
			float num3 = Vector3.Distance(fish.transform.position, base.transform.position);
			if (num3 <= maxNeighbourDistance)
			{
				zero += fish.transform.position;
				num2++;
				if (num3 < flockingAvoidanceDistance)
				{
					zero2 += base.transform.position - fish.transform.position;
				}
				num += fish.speed;
			}
		}
		if (num2 > 0)
		{
			fishState = FishState.flock;
			zero = zero / num2 + (nextGoal - base.transform.position);
			speed = num / (float)num2;
			speed = Mathf.Clamp(speed, minSpeed, maxSpeed);
			Vector3 vector = zero + zero2 - base.transform.position;
			if (vector != Vector3.zero)
			{
				Quaternion to = Quaternion.LookRotation(vector);
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, rotationSpeed * Time.deltaTime);
			}
		}
	}

	private void HandleOnFoodDetected(FlockingManager.FishFood fishFood)
	{
		bool flag = false;
		BoxCollider[] colliders = FishArea.colliders;
		foreach (BoxCollider boxCollider in colliders)
		{
			if (fishFood.collider == boxCollider)
			{
				flag = true;
			}
		}
		if (flag)
		{
			SwitchState(FishState.followFood);
			feedingTimeStarted = 0f;
			projectileGameObject = fishFood.slingshotProjectile.gameObject;
			isRealFood = fishFood.isRealFood;
		}
	}

	private void HandleOnFoodDestroyed(BoxCollider collider)
	{
		bool flag = false;
		BoxCollider[] colliders = FishArea.colliders;
		foreach (BoxCollider boxCollider in colliders)
		{
			if (collider == boxCollider)
			{
				flag = true;
			}
		}
		if (flag)
		{
			SwitchState(FishState.patrol);
			projectileGameObject = null;
			followingFood = false;
		}
	}

	private void FollowFood()
	{
		followingFood = true;
		Quaternion to = Quaternion.LookRotation(projectileGameObject.transform.position - base.transform.position);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, rotationSpeed * Time.deltaTime);
		base.transform.position = Vector3.MoveTowards(base.transform.position, projectileGameObject.transform.position, speed * followFoodSpeedMult * Time.deltaTime);
	}

	private void AvoidPlayerHands()
	{
		foreach (GameObject avoidPoint in FlockingManager.avoidPoints)
		{
			Vector3 position = avoidPoint.transform.position;
			if ((base.transform.position - position).IsShorterThan(avointPointRadius))
			{
				Vector3 randomPointInsideCollider = manager.GetRandomPointInsideCollider(FishArea);
				Turn(randomPointInsideCollider);
				speed = avoidHandSpeed;
			}
		}
	}

	internal void SetSyncPosRot(Vector3 syncPos, Quaternion syncRot)
	{
		if (manager == null)
		{
			manager = GetComponentInParent<FlockingManager>();
		}
		if (FishArea == null)
		{
			Debug.LogError("FISH AREA NULL");
		}
		if (syncRot.IsValid())
		{
			rot = syncRot;
		}
		if (syncPos.IsValid(10000f))
		{
			pos = manager.RestrictPointToArea(syncPos, FishArea);
		}
	}

	private void OnEnable()
	{
		if (manager == null)
		{
			manager = GetComponentInParent<FlockingManager>();
		}
		FlockingManager flockingManager = manager;
		flockingManager.onFoodDetected = (UnityAction<FlockingManager.FishFood>)Delegate.Combine(flockingManager.onFoodDetected, new UnityAction<FlockingManager.FishFood>(HandleOnFoodDetected));
		FlockingManager flockingManager2 = manager;
		flockingManager2.onFoodDestroyed = (UnityAction<BoxCollider>)Delegate.Combine(flockingManager2.onFoodDestroyed, new UnityAction<BoxCollider>(HandleOnFoodDestroyed));
		FlockingUpdateManager.RegisterFlocking(this);
	}
}
