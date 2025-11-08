using UnityEngine;

public class DestroyIfNotBeta : MonoBehaviour
{
	public bool m_shouldKeepIfBeta = true;

	public bool m_shouldKeepIfCreatorBuild;

	private void Awake()
	{
		_ = m_shouldKeepIfBeta;
		_ = m_shouldKeepIfCreatorBuild;
		Object.Destroy(base.gameObject);
	}
}
