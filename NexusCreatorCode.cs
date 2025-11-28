using UnityEngine;

[CreateAssetMenu(fileName = "NexusCreatorCode", menuName = "Nexus/NexusCreatorCode")]
public class NexusCreatorCode : ScriptableObject
{
	[SerializeField]
	private string code;

	[SerializeField]
	private NexusGroupId groupId;

	public string Code => code;

	public NexusGroupId GroupId => groupId;
}
