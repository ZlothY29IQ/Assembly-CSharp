using System;
using BoingKit;
using UnityEngine;

public class CollectibleCoin : MonoBehaviour
{
	public float RespawnTime;

	private bool m_taken;

	private Vector3 m_respawnPosition;

	private float m_respawnTimerStartTime;

	public void Update()
	{
		BoingBehavior component = GetComponent<BoingBehavior>();
		if (m_taken)
		{
			if (Time.time - m_respawnTimerStartTime < RespawnTime)
			{
				return;
			}
			base.transform.position = m_respawnPosition + 0.4f * Vector3.down;
			if (component != null)
			{
				component.Reboot();
			}
			base.transform.position = m_respawnPosition;
			m_taken = false;
		}
		GameObject obj = GameObject.Find("Character");
		GameObject gameObject = GameObject.Find("Coin Icon");
		GameObject gameObject2 = GameObject.Find("Coin Counter");
		if (!((obj.transform.position - base.transform.position).sqrMagnitude > 0.4f))
		{
			m_respawnPosition = base.transform.position;
			if (component != null)
			{
				Vector3Spring positionSpring = component.PositionSpring;
				positionSpring.Reset(base.transform.position, new Vector3(100f, 0f, 0f));
				component.PositionSpring = positionSpring;
			}
			base.transform.position = gameObject.transform.position + new Vector3(-2f, 0.5f, 0f);
			TextMesh component2 = gameObject2.GetComponent<TextMesh>();
			component2.text = (Convert.ToInt32(component2.text) + 1).ToString();
			m_respawnTimerStartTime = Time.time;
			m_taken = true;
		}
	}
}
