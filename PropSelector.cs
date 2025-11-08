using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PropSelector : MonoBehaviour
{
	[SerializeField]
	private List<GameObject> _props = new List<GameObject>();

	[SerializeField]
	private int _desiredActivePropsNum = 1;

	private static readonly System.Random _gRandom = new System.Random();

	private void Start()
	{
		foreach (GameObject item in new List<GameObject>(_props.OrderBy((GameObject x) => _gRandom.Next()).Take(_desiredActivePropsNum)))
		{
			item.SetActive(value: true);
		}
	}
}
