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

	public bool CheckInput(bool checkHeld = true, bool checkSnapped = true, float sensitivity = 0.25f, bool checkHeldActivatable = true)
	{
		int num = -1;
		if (checkHeld && GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			num = out_gamePlayer.FindHandIndex(gameEntity.id);
		}
		if (num == -1 && checkSnapped && GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out var out_gamePlayer2))
		{
			num = out_gamePlayer2.FindSnapIndex(gameEntity.id);
		}
		if (num == -1)
		{
			return false;
		}
		if (checkHeldActivatable && gameEntity.IsSnappedByLocalPlayer() && GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out var out_gamePlayer3))
		{
			GameEntity grabbedGameEntity = out_gamePlayer3.GetGrabbedGameEntity(num);
			if (grabbedGameEntity != null && grabbedGameEntity.GetComponent<IGameActivatable>() != null)
			{
				return false;
			}
		}
		XRNode xrNode = (GamePlayer.IsLeftHand(num) ? XRNode.LeftHand : XRNode.RightHand);
		return CheckInput(xrNode, sensitivity);
	}

	private float GetFloatInput(XRNode xrNode, float sensitivity = 0.25f)
	{
		float num = inputButton switch
		{
			InputButton.Trigger => ControllerInputPoller.TriggerFloat(xrNode), 
			InputButton.ButtonA => ControllerInputPoller.PrimaryButtonPress(xrNode) ? 1 : 0, 
			InputButton.ButtonB => ControllerInputPoller.SecondaryButtonPress(xrNode) ? 1 : 0, 
			InputButton.Grip => ControllerInputPoller.GripFloat(xrNode), 
			InputButton.Joystick => ControllerInputPoller.TriggerFloat(xrNode), 
			_ => 0f, 
		};
		if (!(num >= sensitivity))
		{
			return 0f;
		}
		return num;
	}

	public float GetFloatInput(bool checkHeld = true, bool checkSnapped = true, float sensitivity = 0.25f, bool checkHeldActivatable = true)
	{
		int num = -1;
		if (checkHeld && GamePlayer.TryGetGamePlayer(gameEntity.heldByActorNumber, out var out_gamePlayer))
		{
			num = out_gamePlayer.FindHandIndex(gameEntity.id);
		}
		if (num == -1 && checkSnapped && GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out var out_gamePlayer2))
		{
			num = out_gamePlayer2.FindSnapIndex(gameEntity.id);
		}
		if (num == -1)
		{
			return 0f;
		}
		if (checkHeldActivatable && gameEntity.IsSnappedByLocalPlayer() && GamePlayer.TryGetGamePlayer(gameEntity.snappedByActorNumber, out var out_gamePlayer3))
		{
			GameEntity grabbedGameEntity = out_gamePlayer3.GetGrabbedGameEntity(num);
			if (grabbedGameEntity != null && grabbedGameEntity.GetComponent<IGameActivatable>() != null)
			{
				return 0f;
			}
		}
		XRNode xrNode = (GamePlayer.IsLeftHand(num) ? XRNode.LeftHand : XRNode.RightHand);
		return GetFloatInput(xrNode, sensitivity);
	}

	protected bool IsEquippedLocal()
	{
		if (!gameEntity.IsHeldByLocalPlayer())
		{
			return gameEntity.IsSnappedByLocalPlayer();
		}
		return true;
	}
}
