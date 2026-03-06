using UnityEngine;

[CreateAssetMenu(fileName = "MapModeQueueSet", menuName = "Game Settings/Map Mode Queue Set")]
public class MapModeQueueSet : ScriptableObject
{
	public string[] maps;

	public string[] modes;

	public string[] queues;
}
