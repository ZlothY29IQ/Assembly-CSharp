using UnityEngine;
using UnityEngine.UI;

public class GorillaLevelScreen : MonoBehaviour
{
	public string startingText;

	public Material goodMaterial;

	public Material badMaterial;

	public Text myText;

	private void Awake()
	{
		if (myText != null)
		{
			startingText = myText.text;
		}
	}

	public void UpdateText(string newText, bool setToGoodMaterial)
	{
		if (myText != null)
		{
			myText.text = newText;
		}
		Material[] materials = GetComponent<MeshRenderer>().materials;
		materials[0] = (setToGoodMaterial ? goodMaterial : badMaterial);
		GetComponent<MeshRenderer>().materials = materials;
	}
}
