using UnityEngine;

namespace GorillaTag.Rendering;

public class LocalSkyboxRotationDriver : MonoBehaviour
{
	private static readonly int _LocalSkyboxRotation = Shader.PropertyToID("_LocalSkyboxRotation");

	[Tooltip("The sky's rotation mirrors this Transform's world rotation. Only rotation is used - position and scale are ignored.")]
	[SerializeField]
	private Transform rotationSource;

	private void LateUpdate()
	{
		if (!(rotationSource == null))
		{
			Shader.SetGlobalMatrix(_LocalSkyboxRotation, Matrix4x4.Rotate(rotationSource.rotation));
		}
	}

	private void OnDisable()
	{
		Shader.SetGlobalMatrix(_LocalSkyboxRotation, Matrix4x4.identity);
	}
}
