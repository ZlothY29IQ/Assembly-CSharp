using UnityEngine;

public class MainCamera : MonoBehaviourStatic<MainCamera>
{
	public Camera camera;

	public static implicit operator Camera(MainCamera mc)
	{
		return mc.camera;
	}
}
