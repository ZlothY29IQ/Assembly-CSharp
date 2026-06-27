using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public sealed class IndirectMeshInstance : MonoBehaviour
{
	[Tooltip("When true, the transform is tracked and updated each frame instead of baked at registration time.")]
	[SerializeField]
	internal bool dynamic;

	internal MeshRenderer meshRenderer;

	internal MeshFilter meshFilter;

	private bool _registered;

	private void Awake()
	{
		meshRenderer = GetComponent<MeshRenderer>();
		meshFilter = GetComponent<MeshFilter>();
	}

	private void OnEnable()
	{
		if (!_registered)
		{
			_registered = true;
			IndirectMeshGroup componentInParent = GetComponentInParent<IndirectMeshGroup>();
			IndirectMeshRenderer.Register(this, (componentInParent != null) ? componentInParent.GetInstanceID() : 0);
			if (dynamic)
			{
				meshRenderer.enabled = false;
			}
			else
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}
