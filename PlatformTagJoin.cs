using UnityEngine;

[CreateAssetMenu(fileName = "PlatformTagJoin", menuName = "ScriptableObjects/PlatformTagJoin", order = 0)]
public class PlatformTagJoin : ScriptableObject
{
	public string PlatformTag = " ";

	public override string ToString()
	{
		return PlatformTag;
	}
}
