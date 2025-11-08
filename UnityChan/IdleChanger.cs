using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityChan;

public class IdleChanger : MonoBehaviour
{
	private AnimatorStateInfo currentState;

	private AnimatorStateInfo previousState;

	public bool _random;

	public float _threshold = 0.5f;

	public float _interval = 10f;

	public bool isGUI = true;

	public Animator UnityChanA;

	public Animator UnityChanB;

	private Keyboard kb;

	private void Start()
	{
		currentState = UnityChanA.GetCurrentAnimatorStateInfo(0);
		previousState = currentState;
		StartCoroutine("RandomChange");
		kb = Keyboard.current;
	}

	private void Update()
	{
		if (kb.upArrowKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
		{
			UnityChanA.SetBool("Next", value: true);
			UnityChanB.SetBool("Next", value: true);
		}
		if (kb.downArrowKey.wasPressedThisFrame)
		{
			UnityChanA.SetBool("Back", value: true);
			UnityChanB.SetBool("Back", value: true);
		}
		if (UnityChanA.GetBool("Next"))
		{
			currentState = UnityChanA.GetCurrentAnimatorStateInfo(0);
			if (previousState.fullPathHash != currentState.fullPathHash)
			{
				UnityChanA.SetBool("Next", value: false);
				UnityChanB.SetBool("Next", value: false);
				previousState = currentState;
			}
		}
		if (UnityChanA.GetBool("Back"))
		{
			currentState = UnityChanA.GetCurrentAnimatorStateInfo(0);
			if (previousState.fullPathHash != currentState.fullPathHash)
			{
				UnityChanA.SetBool("Back", value: false);
				UnityChanB.SetBool("Back", value: false);
				previousState = currentState;
			}
		}
	}

	private void OnGUI()
	{
		if (isGUI)
		{
			GUI.Box(new Rect(Screen.width - 110, 10f, 100f, 90f), "Change Motion");
			if (GUI.Button(new Rect(Screen.width - 100, 40f, 80f, 20f), "Next"))
			{
				UnityChanA.SetBool("Next", value: true);
				UnityChanB.SetBool("Next", value: true);
			}
			if (GUI.Button(new Rect(Screen.width - 100, 70f, 80f, 20f), "Back"))
			{
				UnityChanA.SetBool("Back", value: true);
				UnityChanB.SetBool("Back", value: true);
			}
		}
	}

	private IEnumerator RandomChange()
	{
		while (true)
		{
			if (_random)
			{
				float num = Random.Range(0f, 1f);
				if (num < _threshold)
				{
					UnityChanA.SetBool("Back", value: true);
					UnityChanB.SetBool("Back", value: true);
				}
				else if (num >= _threshold)
				{
					UnityChanA.SetBool("Next", value: true);
					UnityChanB.SetBool("Next", value: true);
				}
			}
			yield return new WaitForSeconds(_interval);
		}
	}
}
