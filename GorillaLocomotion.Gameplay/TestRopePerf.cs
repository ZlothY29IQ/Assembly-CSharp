using System.Collections;
using UnityEngine;

namespace GorillaLocomotion.Gameplay;

public class TestRopePerf : MonoBehaviour
{
	[SerializeField]
	private GameObject ropesOld;

	[SerializeField]
	private GameObject ropesCustom;

	[SerializeField]
	private GameObject ropesCustomVectorized;

	private IEnumerator Start()
	{
		yield break;
	}
}
