using UnityEngine;
using UnityEngine.SceneManagement;

namespace BuildSafe;

public class SceneBakeExampleTask : SceneBakeTask
{
	public override void OnSceneBake(Scene scene, SceneBakeMode mode)
	{
		for (int i = 0; i < 10; i++)
		{
			DuplicateAndRecolor(base.gameObject);
		}
		if (mode != SceneBakeMode.OnBuildPlayer)
		{
			_ = 2;
		}
	}

	private static void DuplicateAndRecolor(GameObject target)
	{
		GameObject obj = Object.Instantiate(target);
		obj.transform.position = Random.insideUnitSphere * 4f;
		MeshRenderer component = obj.GetComponent<MeshRenderer>();
		component.material = new Material(component.sharedMaterial)
		{
			color = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f, 1f, 1f)
		};
	}
}
