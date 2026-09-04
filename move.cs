using UnityEngine;

public class move : MonoBehaviour
{
	private int direction = 1;

	private int cnt;

	private bool bounce = true;

	private void Update()
	{
		if (bounce)
		{
			cnt++;
			if (cnt % 50 == 0)
			{
				direction = ((direction <= 0) ? 1 : (-1));
				cnt = 1;
			}
			Vector3 translation = base.gameObject.transform.forward * ((float)direction * 1f);
			base.gameObject.transform.Translate(translation);
		}
	}

	public void BounceState(string state)
	{
		bounce = state == "1";
	}
}
