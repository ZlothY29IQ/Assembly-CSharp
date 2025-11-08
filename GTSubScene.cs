using UnityEngine;

public class GTSubScene : ScriptableObject
{
	[DragDropScenes]
	public GTScene[] scenes = new GTScene[0];

	public void SwitchToScene(int index)
	{
		scenes[index].LoadAsync();
	}

	public void SwitchToScene(GTScene scene)
	{
		for (int i = 0; i < scenes.Length; i++)
		{
			GTScene gTScene = scenes[i];
			if (!(scene == gTScene))
			{
				gTScene.UnloadAsync();
			}
		}
		scene.LoadAsync();
	}

	public void LoadAll()
	{
		for (int i = 0; i < scenes.Length; i++)
		{
			scenes[i].LoadAsync();
		}
	}

	public void UnloadAll()
	{
		for (int i = 0; i < scenes.Length; i++)
		{
			scenes[i].UnloadAsync();
		}
	}
}
