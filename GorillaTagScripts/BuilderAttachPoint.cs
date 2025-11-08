using UnityEngine;

namespace GorillaTagScripts;

public class BuilderAttachPoint : MonoBehaviour
{
	public Transform center;

	private void Awake()
	{
		if (center == null)
		{
			center = base.transform;
		}
	}
}
