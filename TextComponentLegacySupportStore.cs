using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct TextComponentLegacySupportStore
{
	private Transform _objectReference;

	private TMP_Text _tmpTextReference;

	private Text _legacyTextReference;

	private TextMesh _legacyTextMeshReference;

	public bool IsValid
	{
		get
		{
			if (!_tmpTextReference && !_legacyTextReference)
			{
				return _legacyTextMeshReference;
			}
			return true;
		}
	}

	public float characterSpacing
	{
		get
		{
			if ((bool)_tmpTextReference)
			{
				return _tmpTextReference.characterSpacing;
			}
			return 0f;
		}
		set
		{
			if ((bool)_tmpTextReference)
			{
				_tmpTextReference.characterSpacing = value;
			}
		}
	}

	public string text
	{
		get
		{
			if ((bool)_tmpTextReference)
			{
				return _tmpTextReference.text;
			}
			if ((bool)_legacyTextReference)
			{
				return _legacyTextReference.text;
			}
			if ((bool)_legacyTextMeshReference)
			{
				return _legacyTextMeshReference.text;
			}
			Debug.LogError("[LOCALIZATION::TEXT_COMPONENT_LEGACY_SUPPORT_STORE] Both Legacy Text ref and TMP text ref are null!");
			return "";
		}
		set
		{
			if (_tmpTextReference != null)
			{
				_tmpTextReference.text = value;
			}
			else if (_legacyTextReference != null)
			{
				_legacyTextReference.text = value;
			}
			else if ((bool)_legacyTextMeshReference)
			{
				_legacyTextMeshReference.text = value;
			}
			else
			{
				Debug.LogError("[LOCALIZATION::TEXT_COMPONENT_LEGACY_SUPPORT_STORE] Both Legacy Text ref and TMP text ref are null and cannot be set!", _objectReference);
			}
		}
	}

	public TextComponentLegacySupportStore(Transform objRef)
	{
		_objectReference = objRef;
		_legacyTextReference = null;
		_legacyTextMeshReference = null;
		_tmpTextReference = objRef.GetComponent<TMP_Text>();
		if (_tmpTextReference != null)
		{
			return;
		}
		_legacyTextReference = objRef.GetComponent<Text>();
		if (!_legacyTextReference)
		{
			_legacyTextMeshReference = objRef.GetComponent<TextMesh>();
			if (!_legacyTextMeshReference)
			{
				Debug.LogError("[LOCALIZATION::TEXT_COMPONENT_LEGACY_SUPPORT_STORE] Could not find either a [TMP_Text], Legacy-[Text], or Legacy-[TextMesh] component on object [" + objRef.name + "]", _objectReference);
			}
		}
	}

	public void SetFont(TMP_FontAsset font, Font legacyFont)
	{
		if (font != null && (bool)_tmpTextReference)
		{
			SetFont(font);
		}
		else if (legacyFont != null && ((bool)_legacyTextReference || (bool)_legacyTextMeshReference))
		{
			SetFont(legacyFont);
		}
		else if (!IsValid)
		{
			Debug.LogError("[LOCALIZATION::TEXT_COMPONENT_LEGACY_SUPPORT_STORE] Trying to change font but both text references are NULL.");
		}
	}

	public void SetFont(Font font)
	{
		if ((bool)_legacyTextReference)
		{
			_legacyTextReference.font = font;
		}
		else if ((bool)_legacyTextMeshReference)
		{
			_legacyTextMeshReference.font = font;
		}
		else
		{
			Debug.LogError("[LOCALIZATION::TEXT_COMPONENT_LEGACY_SUPPORT_STORE] Trying to change font for non-legacy reference but passed in a legacy font.", font);
		}
	}

	public void SetFont(TMP_FontAsset font)
	{
		if (!(_tmpTextReference == null))
		{
			_tmpTextReference.font = font;
		}
	}

	public void SetFontSize(float fontSize)
	{
		if ((bool)_tmpTextReference)
		{
			TMP_Text tmpTextReference = _tmpTextReference;
			float fontSize2 = (_tmpTextReference.fontSizeMax = fontSize);
			tmpTextReference.fontSize = fontSize2;
		}
	}

	public void SetText(string newText)
	{
		text = newText;
	}

	public void SetCharSpacing(float spacing)
	{
		characterSpacing = spacing;
	}
}
