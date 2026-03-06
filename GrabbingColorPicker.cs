using System;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class GrabbingColorPicker : MonoBehaviour, IGorillaSliceableSimple
{
	[SerializeField]
	private bool setPlayerColor = true;

	[SerializeField]
	private PushableSlider R_PushSlider;

	[SerializeField]
	private PushableSlider G_PushSlider;

	[SerializeField]
	private PushableSlider B_PushSlider;

	[SerializeField]
	private AudioSource R_SliderAudio;

	[SerializeField]
	private AudioSource G_SliderAudio;

	[SerializeField]
	private AudioSource B_SliderAudio;

	[SerializeField]
	private TextMeshPro textR;

	[SerializeField]
	private TextMeshPro textG;

	[SerializeField]
	private TextMeshPro textB;

	[SerializeField]
	private GameObject ColorSwatch;

	[SerializeField]
	private UnityEvent<Vector3> UpdateColor;

	private int Segment1;

	private int Segment2;

	private int Segment3;

	private bool hasUpdated;

	private void Start()
	{
		if (setPlayerColor)
		{
			float r = PlayerPrefs.GetFloat("redValue", 0f);
			float g = PlayerPrefs.GetFloat("greenValue", 0f);
			float b = PlayerPrefs.GetFloat("blueValue", 0f);
			LoadPlayerColor(r, g, b);
		}
	}

	private void LoadPlayerColor(float r, float g, float b)
	{
		Segment1 = Mathf.RoundToInt(Mathf.Lerp(0f, 9f, r));
		Segment2 = Mathf.RoundToInt(Mathf.Lerp(0f, 9f, g));
		Segment3 = Mathf.RoundToInt(Mathf.Lerp(0f, 9f, b));
		R_PushSlider.SetProgress(r);
		G_PushSlider.SetProgress(g);
		B_PushSlider.SetProgress(b);
		UpdateDisplay();
	}

	public void OnEnable()
	{
		GorillaSlicerSimpleManager.RegisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		if (setPlayerColor)
		{
			CosmeticsController.OnPlayerColorSet = (Action<float, float, float>)Delegate.Combine(CosmeticsController.OnPlayerColorSet, new Action<float, float, float>(LoadPlayerColor));
			if ((bool)GorillaTagger.Instance && (bool)GorillaTagger.Instance.offlineVRRig)
			{
				GorillaTagger.Instance.offlineVRRig.OnColorChanged += HandleLocalColorChanged;
			}
		}
	}

	public void OnDisable()
	{
		GorillaSlicerSimpleManager.UnregisterSliceable(this, GorillaSlicerSimpleManager.UpdateStep.Update);
		if (setPlayerColor)
		{
			CosmeticsController.OnPlayerColorSet = (Action<float, float, float>)Delegate.Remove(CosmeticsController.OnPlayerColorSet, new Action<float, float, float>(LoadPlayerColor));
			if ((bool)GorillaTagger.Instance && (bool)GorillaTagger.Instance.offlineVRRig)
			{
				GorillaTagger.Instance.offlineVRRig.OnColorChanged -= HandleLocalColorChanged;
			}
		}
	}

	public void SliceUpdate()
	{
		float num = Vector3.Distance(base.transform.position, GTPlayer.Instance.transform.position);
		hasUpdated = false;
		if (!(num < 5f))
		{
			return;
		}
		int segment = Segment1;
		int segment2 = Segment2;
		int segment3 = Segment3;
		Segment1 = Mathf.RoundToInt(Mathf.Lerp(0f, 9f, R_PushSlider.GetProgress()));
		Segment2 = Mathf.RoundToInt(Mathf.Lerp(0f, 9f, G_PushSlider.GetProgress()));
		Segment3 = Mathf.RoundToInt(Mathf.Lerp(0f, 9f, B_PushSlider.GetProgress()));
		if (segment != Segment1 || segment2 != Segment2 || segment3 != Segment3)
		{
			hasUpdated = true;
			if (setPlayerColor)
			{
				SetPlayerColor();
			}
			UpdateDisplay();
			UpdateColor.Invoke(new Vector3((float)Segment1 / 9f, (float)Segment2 / 9f, (float)Segment3 / 9f));
			if (segment != Segment1)
			{
				R_SliderAudio.transform.position = R_PushSlider.transform.position;
				R_SliderAudio.GTPlay();
			}
			if (segment2 != Segment2)
			{
				G_SliderAudio.transform.position = G_PushSlider.transform.position;
				G_SliderAudio.GTPlay();
			}
			if (segment3 != Segment3)
			{
				B_SliderAudio.transform.position = B_PushSlider.transform.position;
				B_SliderAudio.GTPlay();
			}
		}
	}

	private void SetPlayerColor()
	{
		PlayerPrefs.SetFloat("redValue", (float)Segment1 / 9f);
		PlayerPrefs.SetFloat("greenValue", (float)Segment2 / 9f);
		PlayerPrefs.SetFloat("blueValue", (float)Segment3 / 9f);
		GorillaTagger.Instance.UpdateColor((float)Segment1 / 9f, (float)Segment2 / 9f, (float)Segment3 / 9f);
		GorillaComputer.instance.UpdateColor((float)Segment1 / 9f, (float)Segment2 / 9f, (float)Segment3 / 9f);
		PlayerPrefs.Save();
		if (NetworkSystem.Instance.InRoom)
		{
			GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, (float)Segment1 / 9f, (float)Segment2 / 9f, (float)Segment3 / 9f);
		}
	}

	private void SetSliderColors(float r, float g, float b)
	{
		if (!hasUpdated)
		{
			Segment1 = Mathf.RoundToInt(Mathf.Lerp(0f, 9f, r));
			Segment2 = Mathf.RoundToInt(Mathf.Lerp(0f, 9f, g));
			Segment3 = Mathf.RoundToInt(Mathf.Lerp(0f, 9f, b));
			R_PushSlider.SetProgress(r);
			G_PushSlider.SetProgress(g);
			B_PushSlider.SetProgress(b);
			UpdateDisplay();
		}
	}

	private void HandleLocalColorChanged(Color newColor)
	{
		SetSliderColors(newColor.r, newColor.g, newColor.b);
	}

	private void UpdateDisplay()
	{
		textR.text = Segment1.ToString();
		textG.text = Segment2.ToString();
		textB.text = Segment3.ToString();
		Color color = new Color((float)Segment1 / 9f, (float)Segment2 / 9f, (float)Segment3 / 9f);
		Renderer[] componentsInChildren = ColorSwatch.GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Material[] materials = componentsInChildren[i].materials;
			for (int j = 0; j < materials.Length; j++)
			{
				materials[j].color = color;
			}
		}
	}

	public void ResetSliders(Vector3 v)
	{
		SetSliderColors(v.x, v.y, v.z);
	}
}
