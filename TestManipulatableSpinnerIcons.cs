using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestManipulatableSpinnerIcons : MonoBehaviour
{
	public ManipulatableSpinner spinner;

	public float rotationScale = 1f;

	public int rollerElementCount = 5;

	public GameObject rollerElementTemplate;

	public GameObject iconCanvas;

	public GameObject iconElementTemplate;

	public float iconOffset = 1f;

	public float rollerElementAngle = 15f;

	private List<Text> visibleIcons = new List<Text>();

	private float currentRotation;

	public int scrollableCount = 50;

	public int selectedIndex;

	private void Awake()
	{
		GenerateRollers();
	}

	private void LateUpdate()
	{
		currentRotation = spinner.angle * rotationScale;
		UpdateSelectedIndex();
		UpdateRollers();
	}

	private void GenerateRollers()
	{
		for (int i = 0; i < rollerElementCount; i++)
		{
			float x = rollerElementAngle * (float)i + rollerElementAngle * 0.5f;
			Object.Instantiate(rollerElementTemplate, base.transform).transform.localRotation = Quaternion.Euler(x, 0f, 0f);
			GameObject gameObject = Object.Instantiate(iconElementTemplate, iconCanvas.transform);
			gameObject.transform.localRotation = Quaternion.Euler(x, 0f, 0f);
			visibleIcons.Add(gameObject.GetComponentInChildren<Text>());
		}
		rollerElementTemplate.SetActive(value: false);
		iconElementTemplate.SetActive(value: false);
		UpdateRollers();
	}

	private void UpdateSelectedIndex()
	{
		float num = currentRotation / rollerElementAngle;
		if (rollerElementCount % 2 == 1)
		{
			num += 0.5f;
		}
		selectedIndex = Mathf.FloorToInt(num);
		selectedIndex %= scrollableCount;
		if (selectedIndex < 0)
		{
			selectedIndex = scrollableCount + selectedIndex;
		}
	}

	private void UpdateRollers()
	{
		float num = currentRotation;
		if (Mathf.Abs(num) > rollerElementAngle / 2f)
		{
			if (num > 0f)
			{
				num += rollerElementAngle / 2f;
				num %= rollerElementAngle;
				num -= rollerElementAngle / 2f;
			}
			else
			{
				num -= rollerElementAngle / 2f;
				num %= rollerElementAngle;
				num += rollerElementAngle / 2f;
			}
		}
		num -= (float)rollerElementCount / 2f * rollerElementAngle;
		base.transform.localRotation = Quaternion.Euler(num, 0f, 0f);
		iconCanvas.transform.localRotation = Quaternion.Euler(num, 0f, 0f);
		int num2 = rollerElementCount / 2;
		for (int i = 0; i < visibleIcons.Count; i++)
		{
			int num3 = selectedIndex - i + num2;
			num3 = ((num3 >= 0) ? (num3 % scrollableCount) : (num3 + scrollableCount));
			visibleIcons[i].text = $"{num3 + 1}";
		}
	}
}
