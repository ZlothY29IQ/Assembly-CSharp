using UnityEngine;
using UnityEngine.UI;

public class TextWatcher : MonoBehaviour
{
	public WatchableStringSO textToCopy;

	private Text myText;

	private void Start()
	{
		myText = GetComponent<Text>();
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
