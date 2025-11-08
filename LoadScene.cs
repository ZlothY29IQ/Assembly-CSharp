using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
	[SerializeField]
	private float _delay;

	[SerializeField]
	private string _sceneName;

	public IEnumerator Start()
	{
		yield return new WaitForSecondsRealtime(_delay);
		AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(_sceneName, LoadSceneMode.Single);
		while (asyncOperation.progress < 0.99f)
		{
			yield return null;
		}
		asyncOperation.allowSceneActivation = true;
	}
}
