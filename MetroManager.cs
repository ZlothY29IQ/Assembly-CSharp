using UnityEngine;

public class MetroManager : MonoBehaviour
{
	[SerializeField]
	private MetroBlimp[] _blimps = new MetroBlimp[0];

	[SerializeField]
	private MetroSpotlight[] _spotlights = new MetroSpotlight[0];

	[Space]
	[SerializeField]
	private Transform _blimpsRotationAnchor;

	private void Update()
	{
		for (int i = 0; i < _blimps.Length; i++)
		{
			_blimps[i].Tick();
		}
		for (int j = 0; j < _spotlights.Length; j++)
		{
			_spotlights[j].Tick();
		}
	}
}
