using UnityEngine;
using UnityEngine.XR;

public class GameButtonActivatable : MonoBehaviour, IGameActivatable
{
	public enum InputButton
	{
		Trigger,
		ButtonA,
		ButtonB,
		Grip,
		Joystick
	}

	[SerializeField]
	public InputButton inputButton;

	public GameEntity gameEntity;

	public bool CheckInput(XRNode xrNode, float sensitivity = 0.25f)
	{
		return inputButton switch
		{
			InputButton.Trigger => ControllerInputPoller.TriggerFloat(xrNode) > sensitivity, 
			InputButton.ButtonA => ControllerInputPoller.PrimaryButtonPress(xrNode), 
			InputButton.ButtonB => ControllerInputPoller.SecondaryButtonPress(xrNode), 
			InputButton.Grip => ControllerInputPoller.GripFloat(xrNode) > sensitivity, 
			InputButton.Joystick => ControllerInputPoller.TriggerFloat(xrNode) > sensitivity, 
			_ => false, 
		};
	}

	public bool CheckInput(float sensitivity = 0.25f)
	{
		int equippedSlotIndex = gameEntity.EquippedSlotIndex;
		if (equippedSlotIndex == -1 || !gameEntity.IsHeldOrSnappedByLocalPlayer)
		{
			return false;
		}
		GamePlayer gamePlayer = GamePlayerLocal.instance.gamePlayer;
		if (gameEntity.IsSnappedToHand)
		{
			int num = equippedSlotIndex switch
			{
				2 => 0, 
				3 => 1, 
				_ => -1, 
			};
			if (gamePlayer.TryGetSlotEntity(num, out var out_entity) && out_entity.TryGetComponent<IGameActivatable>(out var _))
			{
				return false;
			}
			if (inputButton == InputButton.Trigger && GameTriggerInteractable.LocalInteractableTriggers.Count > 0)
			{
				Vector3 position = gamePlayer.GetHandTransform(num).position;
				for (int i = 0; i < GameTriggerInteractable.LocalInteractableTriggers.Count; i++)
				{
					if (GameTriggerInteractable.LocalInteractableTriggers[i].PointWithinInteractableArea(position))
					{
						return false;
					}
				}
			}
		}
		return CheckInput(gameEntity.EquippedHandXRNode, sensitivity);
	}
}
