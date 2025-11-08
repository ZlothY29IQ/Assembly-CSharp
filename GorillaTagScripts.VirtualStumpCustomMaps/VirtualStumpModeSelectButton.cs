using GorillaExtensions;
using GorillaGameModes;
using GorillaNetworking;

namespace GorillaTagScripts.VirtualStumpCustomMaps;

public class VirtualStumpModeSelectButton : ModeSelectButton
{
	public override void ButtonActivationWithHand(bool isLeftHand)
	{
		if (warningScreen.ShouldShowWarning)
		{
			warningScreen.Show();
		}
		else
		{
			GorillaComputer.instance.SetGameModeWithoutButton(gameMode);
		}
		if (GorillaComputer.instance.IsPlayerInVirtualStump() && RoomSystem.JoinedRoom && NetworkSystem.Instance.LocalPlayer.IsMasterClient && NetworkSystem.Instance.SessionIsPrivate)
		{
			if (GameMode.ActiveGameMode.IsNull())
			{
				GameMode.ChangeGameMode(gameMode);
			}
			else if (GameMode.ActiveGameMode.GameType().ToString().ToLower() != gameMode.ToLower())
			{
				GameMode.ChangeGameMode(gameMode);
			}
		}
	}
}
