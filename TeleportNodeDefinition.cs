using UnityEngine;

[CreateAssetMenu(fileName = "New TeleportNode Definition", menuName = "Teleportation/TeleportNode Definition", order = 1)]
public class TeleportNodeDefinition : ScriptableObject
{
	[SerializeField]
	private TeleportNode forward;

	[SerializeField]
	private TeleportNode backward;

	public TeleportNode Forward => forward;

	public TeleportNode Backward => backward;

	public void SetForward(TeleportNode node)
	{
		Debug.Log("registered fwd node " + node.name);
		forward = node;
	}

	public void SetBackward(TeleportNode node)
	{
		Debug.Log("registered bkwd node " + node.name);
		backward = node;
	}
}
