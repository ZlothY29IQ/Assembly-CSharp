using UnityEngine;
using UnityEngine.UI;

public class DevInspector : MonoBehaviour
{
	public GameObject pivot;

	public Text outputInfo;

	public Component[] componentToInspect;

	public bool isEnabled;

	public bool autoFind = true;

	public GameObject canvas;

	public int sidewaysOffset;

	private void OnEnable()
	{
		Object.Destroy(base.gameObject);
	}
}
