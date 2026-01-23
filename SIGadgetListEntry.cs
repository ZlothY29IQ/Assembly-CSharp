using TMPro;
using UnityEngine;

public class SIGadgetListEntry : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI gadgetText;

	[SerializeField]
	private SITouchscreenButtonContainer buttonContainer;

	public ObjectHierarchyFlattener imageFlattener;

	public ObjectHierarchyFlattener textFlattener;

	public GameObject selectionIndicator;

	public SITouchscreenButtonContainer ButtonContainer => buttonContainer;

	public int Id { get; private set; } = -1;

	public void Configure(ITouchScreenStation station, SITechTreePage page, Transform imageTarget, Transform textTarget, SITouchscreenButton.SITouchscreenButtonType buttonType = SITouchscreenButton.SITouchscreenButtonType.Select, int index = 0, float positionInterval = 0f, int listSize = 0)
	{
		string text = (gadgetText.text = page.nickName);
		base.name = text;
		SITouchscreenButton button = buttonContainer.button;
		button.buttonType = buttonType;
		Id = (button.data = (int)page.pageId);
		button.buttonPressed.RemoveAllListeners();
		button.buttonPressed.AddListener(station.TouchscreenButtonPressed);
		station.AddButton(button);
		float num = (float)Mathf.Max(listSize - 1, 0) * (0f - positionInterval / 2f);
		base.transform.localPosition += new Vector3(0f, num + (float)index * positionInterval, 0f);
		imageFlattener.overrideParentTransform = imageTarget;
		textFlattener.overrideParentTransform = textTarget;
		imageFlattener.enabled = true;
		textFlattener.enabled = true;
		buttonContainer.SetUsable(page.IsAllowed);
	}
}
