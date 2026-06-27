using UnityEngine;

public sealed class IndirectMeshGroup : MonoBehaviour
{
	private void OnEnable()
	{
		IndirectMeshRenderer.SetGroupVisible(GetInstanceID(), visible: true);
	}

	private void OnDisable()
	{
		IndirectMeshRenderer.SetGroupVisible(GetInstanceID(), visible: false);
	}
}
