using System;
using CustomMapSupport;
using GT_CustomMapSupportRuntime;
using GorillaExtensions;
using GorillaLocomotion.Climbing;
using GorillaLocomotion.Gameplay;
using UnityEngine;

public class CustomMapsGorillaZipline : GorillaZipline
{
	public bool GenerateZipline(CustomMapSupport.BezierSpline splineRef)
	{
		spline = GetComponent<BezierSpline>();
		if (spline.IsNull())
		{
			return false;
		}
		spline.BuildSplineFromPoints(splineRef.GetControlPoints(), ConvertControlPointModes(splineRef.GetControlPointModes()), splineRef.Loop);
		if (segmentsRoot == null)
		{
			return false;
		}
		ziplineDistance = 0f;
		float t = 0f;
		int num = 0;
		Transform transform = null;
		while (t < 1f)
		{
			float num2 = segmentDistance;
			if (num == 0)
			{
				num2 /= 2f;
			}
			FindTFromDistance(ref t, num2, 5000);
			if (t < 1f || spline.Loop)
			{
				Vector3 point = spline.GetPoint(t);
				GameObject gameObject = UnityEngine.Object.Instantiate(segmentPrefab);
				gameObject.transform.SetParent(segmentsRoot);
				gameObject.transform.position = point;
				gameObject.transform.LookAt(point + spline.GetDirection(t));
				gameObject.transform.position -= gameObject.transform.forward * 0.5f;
				if (num > 0)
				{
					transform.LookAt(gameObject.transform);
				}
				gameObject.GetComponent<GorillaClimbableRef>().climb = slideHelper;
				ziplineDistance += segmentDistance;
				transform = gameObject.transform;
			}
			num++;
		}
		return true;
	}

	protected override void OnBeforeClimb(GorillaHandClimber hand, GorillaClimbableRef climbRef)
	{
		slideHelper.gameObject.SetActive(value: true);
		base.OnBeforeClimb(hand, climbRef);
	}

	private BezierControlPointMode[] ConvertControlPointModes(CustomMapSupport.BezierControlPointMode[] refModes)
	{
		BezierControlPointMode[] array = new BezierControlPointMode[refModes.Length];
		for (int i = 0; i < refModes.Length; i++)
		{
			switch (refModes[i])
			{
			case CustomMapSupport.BezierControlPointMode.Free:
				array[i] = BezierControlPointMode.Free;
				break;
			case CustomMapSupport.BezierControlPointMode.Aligned:
				array[i] = BezierControlPointMode.Aligned;
				break;
			case CustomMapSupport.BezierControlPointMode.Mirrored:
				array[i] = BezierControlPointMode.Mirrored;
				break;
			}
		}
		return array;
	}

	protected override void Start()
	{
		GorillaClimbable gorillaClimbable = slideHelper;
		gorillaClimbable.onBeforeClimb = (Action<GorillaHandClimber, GorillaClimbableRef>)Delegate.Combine(gorillaClimbable.onBeforeClimb, new Action<GorillaHandClimber, GorillaClimbableRef>(OnBeforeClimb));
	}

	public void Init(GTObjectPlaceholder ziplinePlaceholder)
	{
		if (ziplinePlaceholder.PlaceholderObject == GTObject.ZipLine)
		{
			segmentDistance = ziplinePlaceholder.ziplineSegmentGenerationOffset;
			spline = base.gameObject.GetComponent<BezierSpline>();
			if (spline == null)
			{
				spline = base.gameObject.AddComponent<BezierSpline>();
			}
			spline.BuildSplineFromPoints(ziplinePlaceholder.spline.GetControlPoints(), ConvertControlPointModes(ziplinePlaceholder.spline.GetControlPointModes()), ziplinePlaceholder.spline.Loop);
			for (int i = 0; i < ziplinePlaceholder.ziplineSegments.Count; i++)
			{
				ziplinePlaceholder.ziplineSegments[i].transform.SetParent(segmentsRoot, worldPositionStays: true);
				ziplinePlaceholder.ziplineSegments[i].AddComponent<GorillaClimbableRef>().climb = slideHelper;
				ziplineDistance += segmentDistance;
			}
		}
	}
}
