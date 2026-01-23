using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class GREntityDebugCanvas : MonoBehaviour
{
	[SerializeField]
	public TMP_Text text;

	public GameObject textPanelPrefab;

	public Vector3 prefabAttachOffset = new Vector3(0f, 0.5f, 0f);

	public float fontSize = 100f;

	private StringBuilder builder;

	private void Awake()
	{
		builder = new StringBuilder(50);
	}

	private void Start()
	{
		if (text == null && textPanelPrefab != null)
		{
			GameObject gameObject = Object.Instantiate(textPanelPrefab, base.transform.position + prefabAttachOffset, Quaternion.identity, base.transform);
			text = gameObject.GetComponent<TMP_Text>();
		}
		if (text != null)
		{
			text.fontSize = fontSize;
			text.gameObject.SetActive(value: false);
		}
	}

	private bool UpdateActive()
	{
		bool entityDebugEnabled = GhostReactorManager.entityDebugEnabled;
		if (text != null)
		{
			text.gameObject.SetActive(entityDebugEnabled);
		}
		return entityDebugEnabled;
	}

	private void Update()
	{
	}

	private void UpdateText()
	{
		if (!text)
		{
			return;
		}
		builder.Clear();
		List<IGameEntityDebugComponent> list = new List<IGameEntityDebugComponent>();
		GetComponents(list);
		foreach (IGameEntityDebugComponent item in list)
		{
			List<string> strings = new List<string>();
			item.GetDebugTextLines(out strings);
			foreach (string item2 in strings)
			{
				builder.AppendLine(item2);
			}
		}
		text.text = builder.ToString();
	}
}
