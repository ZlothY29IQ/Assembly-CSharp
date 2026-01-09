using TMPro;
using UnityEngine;

public class SIDispenserGadgetListEntry : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI gadgetText;

	[SerializeField]
	private SITouchscreenButtonContainer dispenseButton;

	[SerializeField]
	private SITouchscreenButtonContainer infoButton;

	public ObjectHierarchyFlattener image1;

	public ObjectHierarchyFlattener image2;

	public ObjectHierarchyFlattener text1;

	public ObjectHierarchyFlattener text2;

	public SITouchscreenButtonContainer DispenseButton => dispenseButton;

	public void SetStation(ITouchScreenStation station, Transform imageTarget, Transform textTarget)
	{
		dispenseButton.button.buttonPressed.RemoveAllListeners();
		dispenseButton.button.buttonPressed.AddListener(station.TouchscreenButtonPressed);
		infoButton.button.buttonPressed.RemoveAllListeners();
		infoButton.button.buttonPressed.AddListener(station.TouchscreenButtonPressed);
		station.AddButton(dispenseButton.button);
		station.AddButton(infoButton.button);
		image1.overrideParentTransform = imageTarget;
		image2.overrideParentTransform = imageTarget;
		text1.overrideParentTransform = textTarget;
		text2.overrideParentTransform = textTarget;
		image1.enabled = true;
		image2.enabled = true;
		text1.enabled = true;
		text2.enabled = true;
	}

	public void SetTechTreeNode(SITechTreeNode node)
	{
		string text = (gadgetText.text = node.nickName);
		base.name = text;
		int nodeId = node.upgradeType.GetNodeId();
		ConfigureButton(dispenseButton.button, SITouchscreenButton.SITouchscreenButtonType.Dispense, nodeId);
		ConfigureButton(infoButton.button, SITouchscreenButton.SITouchscreenButtonType.Select, nodeId);
		static void ConfigureButton(SITouchscreenButton button, SITouchscreenButton.SITouchscreenButtonType type, int data)
		{
			button.buttonType = type;
			button.data = data;
		}
	}
}
