using UnityEngine;

[DisallowMultipleComponent]
public class EdPositionSync : MonoBehaviour
{
	[Tooltip("The object whose transform this object should match. Its scene must be open for the button to work.")]
	public XSceneRef Target;

	public bool position = true;

	public bool rotation = true;

	public bool scale;

	[Tooltip("Extra rotation applied on top of the target's rotation, in the target's own space.")]
	public Vector3 rotationOffset = Vector3.zero;

	public void UpdatePosition()
	{
	}

	private void SelectTarget()
	{
	}

	private static float SafeDivide(float a, float b)
	{
		if (!Mathf.Approximately(b, 0f))
		{
			return a / b;
		}
		return 0f;
	}
}
