using System.Collections;
using UnityEngine;

public class TextureSlideshow : MonoBehaviour
{
	private Renderer _renderer;

	[SerializeField]
	private Texture[] textures;

	[SerializeField]
	private Vector2 minMaxPause;

	[SerializeField]
	private float prePause = 1f;

	private void Awake()
	{
		_renderer = GetComponent<Renderer>();
		_renderer.material.mainTexture = textures[0];
	}

	private void OnEnable()
	{
		StartCoroutine(runSlideshow());
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}

	private IEnumerator runSlideshow()
	{
		yield return new WaitForSecondsRealtime(prePause);
		int i = 0;
		while (true)
		{
			yield return new WaitForSecondsRealtime(Random.Range(minMaxPause.x, minMaxPause.y));
			_renderer.material.mainTexture = textures[i];
			i = (i + 1) % textures.Length;
		}
	}
}
