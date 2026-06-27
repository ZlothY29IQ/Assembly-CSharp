using GorillaLocomotion;
using UnityEngine;

public class RadialMaterialFade : MonoBehaviour
{
	[SerializeField]
	private Material material;

	[SerializeField]
	private Transform target;

	[Header("Distance")]
	[SerializeField]
	private float minDistance = 1f;

	[SerializeField]
	private float maxDistance = 10f;

	[Header("Alpha")]
	[SerializeField]
	[Range(0f, 1f)]
	private float alphaAtMinDistance;

	[SerializeField]
	[Range(0f, 1f)]
	private float alphaAtMaxDistance = 1f;

	private static readonly int colorID = Shader.PropertyToID("_Color");

	private void Update()
	{
		if (!(material == null) && !(target == null))
		{
			Camera mainCamera = GTPlayer.Instance.mainCamera;
			if (!(mainCamera == null))
			{
				float value = Vector3.Distance(mainCamera.transform.position, target.position);
				float t = Mathf.InverseLerp(minDistance, maxDistance, value);
				float a = Mathf.Lerp(alphaAtMinDistance, alphaAtMaxDistance, t);
				Color color = material.GetColor(colorID);
				color.a = a;
				material.SetColor(colorID, color);
			}
		}
	}
}
