using UnityEngine;

public class RotateXform : MonoBehaviour
{
	public enum Mode
	{
		Local,
		World
	}

	public Transform xform;

	public Vector3 speed = Vector3.zero;

	public Mode mode;

	public float speedFactor = 0.0625f;

	private void Update()
	{
		if ((bool)xform)
		{
			Vector3 vector = ((mode == Mode.Local) ? xform.localEulerAngles : xform.eulerAngles);
			float num = Time.deltaTime * speedFactor;
			vector.x += speed.x * num;
			vector.y += speed.y * num;
			vector.z += speed.z * num;
			if (mode == Mode.Local)
			{
				xform.localEulerAngles = vector;
			}
			else
			{
				xform.eulerAngles = vector;
			}
		}
	}
}
