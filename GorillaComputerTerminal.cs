using System.Collections;
using GorillaNetworking;
using TMPro;
using UnityEngine;

public class GorillaComputerTerminal : MonoBehaviour, IBuildValidation
{
	public TextMeshPro myScreenText;

	public TextMeshPro myFunctionText;

	public MeshRenderer monitorMesh;

	public bool BuildValidationCheck()
	{
		if (myScreenText == null || myFunctionText == null || monitorMesh == null)
		{
			Debug.LogErrorFormat(base.gameObject, "gorilla computer terminal {0} is missing screen text, function text, or monitor mesh. this will break lots of computer stuff", base.gameObject.name);
			return false;
		}
		return true;
	}

	private void OnEnable()
	{
		if (GorillaComputer.instance == null)
		{
			StartCoroutine(OnEnable_Local());
		}
		else
		{
			Init();
		}
		IEnumerator OnEnable_Local()
		{
			yield return new WaitUntil(() => GorillaComputer.instance != null);
			yield return null;
			Init();
		}
	}

	private void Init()
	{
		GameEvents.ScreenTextChangedEvent.AddListener(OnScreenTextChanged);
		GameEvents.FunctionSelectTextChangedEvent.AddListener(OnFunctionTextChanged);
		GameEvents.ScreenTextMaterialsEvent.AddListener(OnMaterialsChanged);
		GameEvents.LanguageEvent.AddListener(OnLanguageChanged);
		myScreenText.text = GorillaComputer.instance.screenText.currentText;
		myFunctionText.text = GorillaComputer.instance.functionSelectText.currentText;
		if (GorillaComputer.instance.screenText.currentMaterials != null)
		{
			monitorMesh.sharedMaterials = GorillaComputer.instance.screenText.currentMaterials;
		}
	}

	private void OnDisable()
	{
		GameEvents.ScreenTextChangedEvent.RemoveListener(OnScreenTextChanged);
		GameEvents.FunctionSelectTextChangedEvent.RemoveListener(OnFunctionTextChanged);
		GameEvents.ScreenTextMaterialsEvent.RemoveListener(OnMaterialsChanged);
	}

	public void OnScreenTextChanged(string text)
	{
		myScreenText.text = text;
	}

	public void OnFunctionTextChanged(string text)
	{
		myFunctionText.text = text;
	}

	private void OnMaterialsChanged(Material[] materials)
	{
		monitorMesh.sharedMaterials = materials;
	}

	private void OnLanguageChanged()
	{
		if (LocalisationManager.GetFontAssetForCurrentLocale(out var result))
		{
			myScreenText.font = result.fontAsset;
			myFunctionText.font = result.fontAsset;
		}
		myScreenText.characterSpacing = result.charSpacing;
	}
}
