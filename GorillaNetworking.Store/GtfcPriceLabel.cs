using TMPro;
using UnityEngine;

namespace GorillaNetworking.Store;

public sealed class GtfcPriceLabel : MonoBehaviour
{
	[SerializeField]
	private TMP_Text _label;

	public void SetText(string text)
	{
		_label.text = text;
	}
}
