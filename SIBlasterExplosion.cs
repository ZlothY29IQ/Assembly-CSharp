using UnityEngine;

public class SIBlasterExplosion : MonoBehaviour
{
	private void OnDisable()
	{
		SIGadgetBlasterProjectile.DespawnExplosion(base.gameObject);
	}
}
