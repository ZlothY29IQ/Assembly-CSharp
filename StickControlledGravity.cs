using GorillaLocomotion;
using GorillaTag.Gravity;
using UnityEngine;
using UnityEngine.XR;

public class StickControlledGravity : MonoBehaviour
{
	[SerializeField]
	private float deadzone = 0.5f;

	[SerializeField]
	private XRNode stickHand = XRNode.LeftHand;

	[SerializeField]
	private bool enableXAxis;

	private bool triggered;

	private PersonalGravityZone zone;

	[OnEnterPlay_SetNull]
	public static StickControlledGravity Instance;

	private void Start()
	{
		zone = GetComponent<PersonalGravityZone>();
		if (base.isActiveAndEnabled)
		{
			Register();
		}
	}

	private void OnEnable()
	{
		if (zone != null)
		{
			Register();
		}
	}

	private void OnDisable()
	{
		if (zone != null)
		{
			Unregister();
		}
	}

	private void Register()
	{
		Instance = this;
		ControllerInputPoller.AddUpdateCallback(OnInputUpdate);
	}

	private void Unregister()
	{
		Instance = null;
		ControllerInputPoller.RemoveUpdateCallback(OnInputUpdate);
	}

	private void OnInputUpdate()
	{
		if (GTPlayerTransform.Instance.GravityZonesCount == 0)
		{
			return;
		}
		Vector2 vector = ControllerInputPoller.Primary2DAxis(stickHand);
		if (!enableXAxis)
		{
			vector.x = 0f;
		}
		if (vector.magnitude < deadzone)
		{
			triggered = false;
		}
		else if (!triggered)
		{
			triggered = true;
			if (enableXAxis && PlayerPrefFlags.Check(PlayerPrefFlags.Flag.GRAVDASH_FLIP_X))
			{
				vector.x = 0f - vector.x;
			}
			if (PlayerPrefFlags.Check(PlayerPrefFlags.Flag.GRAVDASH_FLIP_Y))
			{
				vector.y = 0f - vector.y;
			}
			Transform transform = GorillaTagger.Instance.mainCamera.transform;
			Vector3 v;
			if (Mathf.Abs(vector.x) > Mathf.Abs(vector.y))
			{
				v = ((vector.x > 0f) ? transform.right : (-transform.right));
			}
			else
			{
				Vector3 vector2 = Vector3.Cross(transform.right, GTPlayerTransform.PhysicsUp);
				v = ((vector.y > 0f) ? vector2 : (-vector2));
			}
			GTPlayerTransform.Instance.SetPersonalGravityDirection(-SnapToAxis(v));
		}
	}

	private static Vector3 SnapToAxis(Vector3 v)
	{
		float num = Mathf.Abs(v.x);
		float num2 = Mathf.Abs(v.y);
		float num3 = Mathf.Abs(v.z);
		if (num >= num2 && num >= num3)
		{
			return new Vector3(Mathf.Sign(v.x), 0f, 0f);
		}
		if (num2 >= num3)
		{
			return new Vector3(0f, Mathf.Sign(v.y), 0f);
		}
		return new Vector3(0f, 0f, Mathf.Sign(v.z));
	}
}
