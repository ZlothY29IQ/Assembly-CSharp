using GorillaTagScripts.VirtualStumpCustomMaps;
using UnityEngine;

[RequireComponent(typeof(TeleportNode))]
public class StumpReturnRouter : MonoBehaviour
{
	[Tooltip("Where the return node drops the player back into the Custom hallway.")]
	[SerializeField]
	private Transform customDestination;

	[Tooltip("Where the return node drops the player back into the Feature A hallway.")]
	[SerializeField]
	private Transform featureADestination;

	[Tooltip("Where the return node drops the player back into the Feature B hallway.")]
	[SerializeField]
	private Transform featureBDestination;

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
		if (!hasApplied || currentActivateMode != appliedMode)
		{
			appliedMode = currentActivateMode;
			hasApplied = true;
			Transform destination = GetDestination(currentActivateMode);
			if (destination == null)
			{
				Debug.LogWarning($"[StumpReturnRouter] No return destination assigned for mode {currentActivateMode}; the " + "return node will fall back to its serialized teleportToRef.", this);
			}
			Debug.LogWarning($"[StumpReturnRouter] on node '{node.gameObject.name}': mode={currentActivateMode} -> destination=" + ((destination != null) ? destination.name : "NULL"));
			node.SetDestinationOverride(destination);
		}
	}

	public void OnReturnedToHallway()
	{
		CustomMapManager.Deactivate();
	}

	private Transform GetDestination(VirtualStumpActivateMode mode)
	{
		return mode switch
		{
			VirtualStumpActivateMode.FeatureA => featureADestination, 
			VirtualStumpActivateMode.FeatureB => featureBDestination, 
			_ => customDestination, 
		};
	}
}
