using System.Collections.Generic;
using UnityEngine;

namespace GorillaTag.Gravity;

public class MonkeGravityManager : MonoBehaviour
{
	[SerializeField]
	private float defaultRotationSpeed = 10f;

	[SerializeField]
	private float defaultGravityStrength = -9.3f;

	private static readonly CallbackContainerUnique<BasicGravityZone> k_zones = new CallbackContainerUnique<BasicGravityZone>(5);

	private static readonly Dictionary<Collider, MonkeGravityController> k_allowedColliders = new Dictionary<Collider, MonkeGravityController>(10);

	private static readonly CallbackContainerUnique<MonkeGravityController> k_controllers = new CallbackContainerUnique<MonkeGravityController>(10);

	private static GravityInfo k_defaultGravityInfo = new GravityInfo
	{
		gravityUpDirection = Vector3.up,
		rotationDirection = Vector3.up,
		rotationSpeed = 10f,
		gravityStrength = -9.3f,
		rotate = false
	};

	public static GravityInfo DefaultGravityInfo => k_defaultGravityInfo;

	private void Awake()
	{
		k_defaultGravityInfo.rotationDirection = (k_defaultGravityInfo.gravityUpDirection = -Physics.gravity.normalized);
		k_defaultGravityInfo.rotationSpeed = defaultRotationSpeed;
		k_defaultGravityInfo.gravityStrength = defaultGravityStrength;
	}

	private void FixedUpdate()
	{
		k_zones.RunCallbacks();
		k_controllers.RunCallbacks();
	}

	public static void AddMonkeGravityController(MonkeGravityController gravity)
	{
		Collider activatorCollider = gravity.ActivatorCollider;
		k_allowedColliders.TryAdd(activatorCollider, gravity);
		k_controllers.Add(in gravity);
	}

	public static void RemoveMonkeGravityController(MonkeGravityController gravity)
	{
		k_allowedColliders.Remove(gravity.ActivatorCollider);
		k_controllers.Remove(in gravity);
	}

	public static (bool found, MonkeGravityController target) GetMonkeGravityController(Collider collider)
	{
		MonkeGravityController value;
		return (found: k_allowedColliders.TryGetValue(collider, out value), target: value);
	}

	public static void AddGravityCallback(BasicGravityZone zone)
	{
		k_zones.Add(in zone);
	}

	public static void RemoveGravityCallback(BasicGravityZone zone)
	{
		k_zones.Remove(in zone);
	}
}
