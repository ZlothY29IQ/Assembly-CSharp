using UnityEngine;

public class MousePositionDrag : MonoBehaviour
{
	private bool m_currFrameHasFocus;

	private bool m_prevFrameHasFocus;

	private Vector3 m_prevMousePosition;

	private void Start()
	{
		m_currFrameHasFocus = false;
		m_prevFrameHasFocus = false;
	}

	private void Update()
	{
		m_currFrameHasFocus = Application.isFocused;
		bool prevFrameHasFocus = m_prevFrameHasFocus;
		m_prevFrameHasFocus = m_currFrameHasFocus;
		if (prevFrameHasFocus || m_currFrameHasFocus)
		{
			Vector3 mousePosition = Input.mousePosition;
			Vector3 prevMousePosition = m_prevMousePosition;
			Vector3 vector = mousePosition - prevMousePosition;
			m_prevMousePosition = mousePosition;
			if (prevFrameHasFocus && Input.GetMouseButton(0))
			{
				base.transform.position += 0.02f * vector;
			}
		}
	}
}
