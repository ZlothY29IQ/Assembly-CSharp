using ExitGames.Client.Photon;
using Photon.Pun;

namespace GorillaNetworking.ScheduledEvents;

public class TestScheduledEventStateCycleButton : GorillaPressableButton
{
	private string lastRenderedState;

	public override void ButtonActivation()
	{
		if (PhotonNetwork.CurrentRoom == null)
		{
			SetText("(no room)");
			return;
		}
		string text = NextState(ReadState());
		Hashtable propertiesToSet = new Hashtable { { "scheduledEventState", text } };
		PhotonNetwork.CurrentRoom.SetCustomProperties(propertiesToSet);
		RenderState(text);
	}

	private void Update()
	{
		string text = ReadState();
		if (text != lastRenderedState)
		{
			RenderState(text);
		}
	}

	private static string ReadState()
	{
		if (PhotonNetwork.CurrentRoom == null)
		{
			return null;
		}
		PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("scheduledEventState", out var value);
		return value as string;
	}

	private static string NextState(string current)
	{
		if (current == "regular")
		{
			return "event-in-progress";
		}
		if (current == "event-in-progress")
		{
			return "post-event";
		}
		return "regular";
	}

	private void RenderState(string state)
	{
		lastRenderedState = state;
		SetText(state ?? "(unset)");
	}
}
