using System;
using System.Collections.Generic;
using UnityEngine;

public interface IEyeScannable
{
	int scannableId { get; }

	Vector3 Position { get; }

	Bounds Bounds { get; }

	IList<KeyValueStringPair> Entries { get; }

	event Action OnDataChange;

	void OnEnable();

	void OnDisable();
}
