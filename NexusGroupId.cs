using UnityEngine;

[CreateAssetMenu(fileName = "NexusGroupId", menuName = "Nexus/NexusGroupId")]
public class NexusGroupId : ScriptableObject
{
	[SerializeField]
	private string code;

	[SerializeField]
	private string sandboxCode;

	public string Code => code;
}
