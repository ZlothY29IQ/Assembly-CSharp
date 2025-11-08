using UnityEngine;

public class PlatformerCollectiblesMain : MonoBehaviour
{
	public GameObject Coin;

	public float CoinGridCount = 5f;

	public float CoinGridSize = 7f;

	public void Start()
	{
		for (int i = 0; (float)i < CoinGridCount; i++)
		{
			float x = -0.5f * CoinGridSize + CoinGridSize * (float)i / (CoinGridCount - 1f);
			for (int j = 0; (float)j < CoinGridCount; j++)
			{
				float z = -0.5f * CoinGridSize + CoinGridSize * (float)j / (CoinGridCount - 1f);
				Object.Instantiate(Coin).transform.position = new Vector3(x, 0.2f, z);
			}
		}
	}
}
