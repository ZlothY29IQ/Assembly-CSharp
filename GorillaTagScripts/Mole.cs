using System.Collections.Generic;
using UnityEngine;

namespace GorillaTagScripts;

public class Mole : Tappable
{
	public delegate void MoleTapEvent(MoleTypes moleType, Vector3 position, bool isLocalTap, bool isLeft);

	public enum MoleState
	{
		Reset,
		Ready,
		TransitionToVisible,
		Visible,
		TransitionToHidden,
		Hidden
	}

	public float positionOffset = 0.2f;

	public MoleTypes[] moleTypes;

	private float showMoleDuration;

	private Vector3 visiblePosition;

	private Vector3 hiddenPosition;

	private float currentTime;

	private float animStartTime;

	private float travelTime;

	private float normalTravelTime = 0.3f;

	private float hitTravelTime = 0.2f;

	private AnimationCurve animCurve;

	private AnimationCurve normalAnimCurve;

	private AnimationCurve hitAnimCurve;

	private MoleState currentState;

	private Vector3 origin;

	private Vector3 target;

	private int randomMolePickedIndex;

	public CallLimiter rpcCooldown;

	private int moleScore;

	private List<int> safeMoles = new List<int>();

	private List<int> hazardMoles = new List<int>();

	public bool IsLeftSideMole { get; set; }

	public event MoleTapEvent OnTapped;

	private void Awake()
	{
		currentState = MoleState.Hidden;
		Vector3 position = base.transform.position;
		origin = (target = position);
		visiblePosition = new Vector3(position.x, position.y + positionOffset, position.z);
		hiddenPosition = new Vector3(position.x, position.y - positionOffset, position.z);
		travelTime = normalTravelTime;
		animCurve = (normalAnimCurve = AnimationCurves.EaseInOutQuad);
		hitAnimCurve = AnimationCurves.EaseOutBack;
		for (int i = 0; i < moleTypes.Length; i++)
		{
			if (moleTypes[i].isHazard)
			{
				hazardMoles.Add(i);
			}
			else
			{
				safeMoles.Add(i);
			}
		}
		randomMolePickedIndex = -1;
	}

	public void InvokeUpdate()
	{
		if (currentState == MoleState.Ready)
		{
			return;
		}
		switch (currentState)
		{
		case MoleState.TransitionToVisible:
		case MoleState.TransitionToHidden:
		{
			float num = animCurve.Evaluate(Mathf.Clamp01((Time.time - animStartTime) / travelTime));
			base.transform.position = Vector3.Lerp(origin, target, num);
			if (num >= 1f)
			{
				currentState++;
			}
			break;
		}
		case MoleState.Reset:
		case MoleState.Hidden:
			currentState = MoleState.Ready;
			break;
		}
		if (Time.time - currentTime >= showMoleDuration && currentState > MoleState.Ready && currentState < MoleState.TransitionToHidden)
		{
			HideMole();
		}
	}

	public bool CanPickMole()
	{
		return currentState == MoleState.Ready;
	}

	public void ShowMole(float _showMoleDuration, int randomMoleTypeIndex)
	{
		if (randomMoleTypeIndex >= moleTypes.Length || randomMoleTypeIndex < 0)
		{
			return;
		}
		randomMolePickedIndex = randomMoleTypeIndex;
		for (int i = 0; i < moleTypes.Length; i++)
		{
			moleTypes[i].gameObject.SetActive(i == randomMoleTypeIndex);
			if (moleTypes[i].monkeMoleDefaultMaterial != null)
			{
				moleTypes[i].MeshRenderer.material = moleTypes[i].monkeMoleDefaultMaterial;
			}
		}
		showMoleDuration = _showMoleDuration;
		origin = base.transform.position;
		target = visiblePosition;
		animCurve = normalAnimCurve;
		currentState = MoleState.TransitionToVisible;
		animStartTime = (currentTime = Time.time);
		travelTime = normalTravelTime;
	}

	public void HideMole(bool isHit = false)
	{
		if (currentState >= MoleState.TransitionToVisible && currentState <= MoleState.Visible)
		{
			origin = base.transform.position;
			target = hiddenPosition;
			animCurve = (isHit ? hitAnimCurve : normalAnimCurve);
			animStartTime = Time.time;
			travelTime = (isHit ? hitTravelTime : normalTravelTime);
			currentState = MoleState.TransitionToHidden;
		}
	}

	public bool CanTap()
	{
		MoleState moleState = currentState;
		return moleState == MoleState.TransitionToVisible || moleState == MoleState.Visible;
	}

	public override bool CanTap(bool isLeftHand)
	{
		return CanTap();
	}

	public override void OnTapLocal(float tapStrength, float tapTime, PhotonMessageInfoWrapped info)
	{
		if (CanTap())
		{
			bool flag = info.Sender.ActorNumber == NetworkSystem.Instance.LocalPlayerID;
			bool isLeft = flag && GorillaTagger.Instance.lastLeftTap >= GorillaTagger.Instance.lastRightTap;
			MoleTypes moleTypes = null;
			if (randomMolePickedIndex >= 0 && randomMolePickedIndex < this.moleTypes.Length)
			{
				moleTypes = this.moleTypes[randomMolePickedIndex];
			}
			if (moleTypes != null)
			{
				this.OnTapped?.Invoke(moleTypes, base.transform.position, flag, isLeft);
			}
		}
	}

	public void ResetPosition()
	{
		base.transform.position = hiddenPosition;
		currentState = MoleState.Reset;
	}

	public int GetMoleTypeIndex(bool useHazardMole)
	{
		if (!useHazardMole)
		{
			return safeMoles[Random.Range(0, safeMoles.Count)];
		}
		return hazardMoles[Random.Range(0, hazardMoles.Count)];
	}
}
