using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GameGrabbable))]
[RequireComponent(typeof(GameSnappable))]
public class SIGadgetHolster : SIGadget, I_SIDisruptable
{
	private enum State
	{
		Unequipped,
		Equipped
	}

	[SerializeField]
	private Image imageMask;

	public List<SuperInfectionSnapPoint> snapPoints;

	private State state;

	private GTPlayer gtPlayer;

	private void Start()
	{
		gtPlayer = GTPlayer.Instance;
	}

	public void Disrupt(float disruptTime)
	{
	}
}
