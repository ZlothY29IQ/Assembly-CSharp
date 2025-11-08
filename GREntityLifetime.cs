using UnityEngine;

public class GREntityLifetime : MonoBehaviour
{
	public float Lifetime = 3f;

	private GameEntity entity;

	private void Start()
	{
		entity = GetComponent<GameEntity>();
		Invoke("DestroySelf", Lifetime);
	}

	private void Update()
	{
	}

	private void DestroySelf()
	{
		if (entity != null)
		{
			entity.manager.RequestDestroyItem(entity.id);
		}
	}
}
