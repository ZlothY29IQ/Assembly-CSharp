using UnityEngine;

public class BuilderPaintBucket : MonoBehaviour
{
	[SerializeField]
	private BuilderMaterialOptions bucketMaterialOptions;

	[SerializeField]
	private MeshRenderer paintBucketRenderer;

	[SerializeField]
	private string materialId;

	private int materialType = -1;

	private void Awake()
	{
		if (string.IsNullOrEmpty(materialId))
		{
			return;
		}
		materialType = materialId.GetHashCode();
		if (bucketMaterialOptions != null && paintBucketRenderer != null)
		{
			bucketMaterialOptions.GetMaterialFromType(materialType, out var material, out var _);
			if (material != null)
			{
				paintBucketRenderer.material = material;
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (materialType == -1)
		{
			return;
		}
		Rigidbody attachedRigidbody = other.attachedRigidbody;
		if (attachedRigidbody != null)
		{
			BuilderPaintBrush component = attachedRigidbody.GetComponent<BuilderPaintBrush>();
			if (component != null)
			{
				component.SetBrushMaterial(materialType);
			}
		}
	}
}
