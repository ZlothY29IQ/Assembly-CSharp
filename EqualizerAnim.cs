using System.Collections.Generic;
using UnityEngine;

public class EqualizerAnim : MonoBehaviour
{
	[SerializeField]
	private AnimationCurve redCurve;

	[SerializeField]
	private AnimationCurve greenCurve;

	[SerializeField]
	private AnimationCurve blueCurve;

	[SerializeField]
	private float loopDuration;

	[SerializeField]
	private Material material;

	[SerializeField]
	private string inputColorProperty;

	private int inputColorHash;

	private static int thisFrame;

	private static HashSet<Material> materialsUpdatedThisFrame = new HashSet<Material>();

	private void Start()
	{
		inputColorHash = Shader.PropertyToID(inputColorProperty);
	}

	private void Update()
	{
		if (thisFrame == Time.frameCount)
		{
			if (materialsUpdatedThisFrame.Contains(material))
			{
				return;
			}
		}
		else
		{
			thisFrame = Time.frameCount;
			materialsUpdatedThisFrame.Clear();
		}
		float time = Time.time % loopDuration;
		material.SetColor(inputColorHash, new Color(redCurve.Evaluate(time), greenCurve.Evaluate(time), blueCurve.Evaluate(time)));
		materialsUpdatedThisFrame.Add(material);
	}
}
