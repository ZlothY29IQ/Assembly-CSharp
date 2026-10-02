using GorillaTagScripts.VirtualStumpCustomMaps;
using UnityEngine;

[RequireComponent(typeof(TeleportNode))]
public class StumpReturnRouter : MonoBehaviour
{
	[Tooltip("Where the return node drops the player back into the Custom hallway.")]
	[SerializeField]
	private XSceneRef customDestination;

	[Tooltip("Where the return node drops the player back into the Feature A hallway.")]
	[SerializeField]
	private XSceneRef featureADestination;

	[Tooltip("Where the return node drops the player back into the Feature B hallway.")]
	[SerializeField]
	private XSceneRef featureBDestination;

	private TeleportNode node;

	private VirtualStumpActivateMode appliedMode;

	private bool hasApplied;

	private void Awake()
	{
		node = GetComponent<TeleportNode>();
	}

	private void Update()
	{
		VirtualStumpActivateMode currentActivateMode = CustomMapManager.CurrentActivateMode;
		if ((!hasApplied || currentActivateMode != appliedMode) && GetDestination(currentActivateMode).TryResolve(out Transform result))
		{
			appliedMode = currentActivateMode;
			hasApplied = true;
			if (result == null)
			{
				Debug.LogWarning($"[StumpReturnRouter] No return destination assigned for mode {currentActivateMode}; the " + "return node will fall back to its serialized teleportToRef.", this);
			}
			Debug.LogWarning($"[StumpReturnRouter] on node '{node.gameObject.name}': mode={currentActivateMode} -> destination=" + ((result != null) ? result.name : "NULL"));
			node.SetDestinationOverride(result);
		}
	}

	public void OnReturnedToHallway()
	{
		CustomMapManager.ExitVStump();
	}

	private XSceneRef GetDestination(VirtualStumpActivateMode mode)
	{
		return mode switch
		{
			VirtualStumpActivateMode.FeatureA => featureADestination, 
			VirtualStumpActivateMode.FeatureB => featureBDestination, 
			_ => customDestination, 
		};
	}
}
