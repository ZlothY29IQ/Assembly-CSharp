using TMPro;
using UnityEngine;

public class TextWatcherTMPro : MonoBehaviour
{
	public WatchableStringSO textToCopy;

	private TextMeshPro myText;

	private void Start()
	{
		myText = GetComponent<TextMeshPro>();
		textToCopy.AddCallback(OnTextChanged, shouldCallbackNow: true);
	}

	private void OnDestroy()
	{
		textToCopy.RemoveCallback(OnTextChanged);
	}

	private void OnTextChanged(string newText)
	{
		myText.text = newText;
	}
}
