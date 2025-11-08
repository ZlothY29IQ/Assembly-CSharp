using UnityEngine;

public class GorillaLightmapData : MonoBehaviour
{
	[SerializeField]
	public Texture2D[] dirTextures;

	[SerializeField]
	public Texture2D[] lightTextures;

	public Color[][] lights;

	public Color[][] dirs;

	public bool done;

	public void Awake()
	{
		lights = new Color[lightTextures.Length][];
		dirs = new Color[dirTextures.Length][];
		for (int i = 0; i < dirTextures.Length; i++)
		{
			float value = Random.value;
			Debug.Log(value + " before load " + Time.realtimeSinceStartup);
			dirs[i] = dirTextures[i].GetPixels();
			lights[i] = lightTextures[i].GetPixels();
			Debug.Log(value + " after load " + Time.realtimeSinceStartup);
		}
	}
}
