using UnityEngine;

public class GREnemyXRayVisionEffect : MonoBehaviour
{
	public GameObject enemyXRayEffect;

	public void Awake()
	{
	}

	private void Start()
	{
		InvokeRepeating("UpdateEffect", 0f, 0.5f);
	}

	private bool ShouldShowEffect()
	{
		return GRPlayer.GetLocal().HasXRayVision();
	}

	private void UpdateEffect()
	{
		enemyXRayEffect.SetActive(ShouldShowEffect());
	}
}
