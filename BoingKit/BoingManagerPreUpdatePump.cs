using UnityEngine;

namespace BoingKit;

public class BoingManagerPreUpdatePump : MonoBehaviour
{
	private int m_lastPumpedFrame = -1;

	private void FixedUpdate()
	{
		TryPump();
	}

	private void Update()
	{
		TryPump();
	}

	private void TryPump()
	{
		if (m_lastPumpedFrame < Time.frameCount)
		{
			if (m_lastPumpedFrame >= 0)
			{
				DoPump();
			}
			m_lastPumpedFrame = Time.frameCount;
		}
	}

	private void DoPump()
	{
		BoingManager.RestoreBehaviors();
		BoingManager.RestoreReactors();
		BoingManager.RestoreBones();
		BoingManager.DispatchReactorFieldCompute();
	}
}
