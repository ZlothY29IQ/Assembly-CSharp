using System;
using UnityEngine;

[CreateAssetMenu(fileName = "JoinTriggerUITemplate", menuName = "ScriptableObjects/JoinTriggerUITemplate")]
public class JoinTriggerUITemplate : ScriptableObject
{
	[Serializable]
	public struct FormattedString
	{
		[TextArea]
		[SerializeField]
		private string formatText;

		[NonSerialized]
		private StringFormatter formatter;

		public string GetText(string oldZone, string newZone, string oldGameType, string newGameType)
		{
			if (formatter == null)
			{
				formatter = StringFormatter.Parse(formatText);
			}
			return formatter.Format(oldZone, newZone, oldGameType, newGameType);
		}

		public string GetText(Func<string> oldZone, Func<string> newZone, Func<string> oldGameType, Func<string> newGameType)
		{
			if (formatter == null)
			{
				formatter = StringFormatter.Parse(formatText);
			}
			return formatter.Format(oldZone, newZone, oldGameType, newGameType);
		}
	}

	public Material Milestone_Error;

	public Material Milestone_AlreadyInRoom;

	public Material Milestone_InPrivateRoom;

	public Material Milestone_NotConnectedSoloJoin;

	public Material Milestone_LeaveRoomAndSoloJoin;

	public Material Milestone_LeaveRoomAndGroupJoin;

	public Material Milestone_AbandonPartyAndSoloJoin;

	public Material Milestone_ChangingGameModeSoloJoin;

	public Material ScreenBG_Error;

	public Material ScreenBG_AlreadyInRoom;

	public Material ScreenBG_InPrivateRoom;

	public Material ScreenBG_NotConnectedSoloJoin;

	public Material ScreenBG_LeaveRoomAndSoloJoin;

	public Material ScreenBG_LeaveRoomAndGroupJoin;

	public Material ScreenBG_AbandonPartyAndSoloJoin;

	public Material ScreenBG_ChangingGameModeSoloJoin;

	public string ScreenText_Error;

	public bool showFullErrorMessages;

	public FormattedString ScreenText_AlreadyInRoom;

	public FormattedString ScreenText_InPrivateRoom;

	public FormattedString ScreenText_NotConnectedSoloJoin;

	public FormattedString ScreenText_LeaveRoomAndSoloJoin;

	public FormattedString ScreenText_LeaveRoomAndGroupJoin;

	public FormattedString ScreenText_AbandonPartyAndSoloJoin;

	public FormattedString ScreenText_ChangingGameModeSoloJoin;
}
