using UnityEngine;
using UnityEngine.Events;

public class XSceneInvoker_Receive : MonoBehaviour
{
	[SerializeField]
	private UnityEvent evt;

	public void Invoke()
	{
		evt.Invoke();
	}
}
