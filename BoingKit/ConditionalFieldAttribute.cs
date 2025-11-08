using System;
using UnityEngine;

namespace BoingKit;

[AttributeUsage(AttributeTargets.Field)]
public class ConditionalFieldAttribute : PropertyAttribute
{
	public string PropertyToCheck;

	public object CompareValue;

	public object CompareValue2;

	public object CompareValue3;

	public object CompareValue4;

	public object CompareValue5;

	public object CompareValue6;

	public string Label;

	public string Tooltip;

	public float Min;

	public float Max;

	public bool ShowRange => Min != Max;

	public ConditionalFieldAttribute(string propertyToCheck = null, object compareValue = null, object compareValue2 = null, object compareValue3 = null, object compareValue4 = null, object compareValue5 = null, object compareValue6 = null)
	{
		PropertyToCheck = propertyToCheck;
		CompareValue = compareValue;
		CompareValue2 = compareValue2;
		CompareValue3 = compareValue3;
		CompareValue4 = compareValue4;
		CompareValue5 = compareValue5;
		CompareValue6 = compareValue6;
		Label = "";
		Tooltip = "";
		Min = 0f;
		Max = 0f;
	}
}
