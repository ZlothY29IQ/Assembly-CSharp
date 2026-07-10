using UnityEngine;

public class XSceneInvoker_Send : MonoBehaviour
{
	[SerializeField]
	private XSceneRef target;

	public void Invoke()
	{
		if (target.TryResolve(out XSceneInvoker_Receive result))
		{
			result.Invoke();
		}
	}
}
