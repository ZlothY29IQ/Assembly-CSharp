using UnityEngine;

public class PeriodicFoodTopUpper : MonoBehaviour
{
	private CrittersFood food;

	private float timeFoodEmpty;

	private bool waitingToRefill;

	public float waitToRefill = 10f;

	public GameObject foodObject;

	private void Awake()
	{
		food = GetComponentInParent<CrittersFood>();
	}

	private void Update()
	{
		if (CrittersManager.instance.LocalAuthority())
		{
			if (!waitingToRefill && food.currentFood == 0f)
			{
				waitingToRefill = true;
				timeFoodEmpty = Time.time;
			}
			if (waitingToRefill && Time.time > timeFoodEmpty + waitToRefill)
			{
				waitingToRefill = false;
				food.Initialize();
			}
		}
	}
}
