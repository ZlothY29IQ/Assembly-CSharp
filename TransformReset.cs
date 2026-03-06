using UnityEngine;

public class TransformReset : MonoBehaviour
{
	private struct OriginalGameObjectTransform(Transform constructionTransform)
	{
		private Transform _thisTransform = constructionTransform;

		private Vector3 _thisPosition = constructionTransform.position;

		private Quaternion _thisRotation = constructionTransform.rotation;

		public Transform thisTransform
		{
			get
			{
				return _thisTransform;
			}
			set
			{
				_thisTransform = value;
			}
		}

		public Vector3 thisPosition
		{
			get
			{
				return _thisPosition;
			}
			set
			{
				_thisPosition = value;
			}
		}

		public Quaternion thisRotation
		{
			get
			{
				return _thisRotation;
			}
			set
			{
				_thisRotation = value;
			}
		}
	}

	private OriginalGameObjectTransform[] transformList;

	private OriginalGameObjectTransform[] tempTransformList;

	private void Awake()
	{
		Transform[] componentsInChildren = GetComponentsInChildren<Transform>();
		transformList = new OriginalGameObjectTransform[componentsInChildren.Length];
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			transformList[i] = new OriginalGameObjectTransform(componentsInChildren[i]);
		}
		ResetTransforms();
	}

	public void ReturnTransforms()
	{
		OriginalGameObjectTransform[] array = tempTransformList;
		for (int i = 0; i < array.Length; i++)
		{
			OriginalGameObjectTransform originalGameObjectTransform = array[i];
			originalGameObjectTransform.thisTransform.position = originalGameObjectTransform.thisPosition;
			originalGameObjectTransform.thisTransform.rotation = originalGameObjectTransform.thisRotation;
		}
	}

	public void SetScale(float ratio)
	{
		OriginalGameObjectTransform[] array = transformList;
		foreach (OriginalGameObjectTransform originalGameObjectTransform in array)
		{
			originalGameObjectTransform.thisTransform.localScale *= ratio;
		}
	}

	public void ResetTransforms()
	{
		tempTransformList = new OriginalGameObjectTransform[transformList.Length];
		for (int i = 0; i < transformList.Length; i++)
		{
			tempTransformList[i] = new OriginalGameObjectTransform(transformList[i].thisTransform);
		}
		OriginalGameObjectTransform[] array = transformList;
		for (int j = 0; j < array.Length; j++)
		{
			OriginalGameObjectTransform originalGameObjectTransform = array[j];
			originalGameObjectTransform.thisTransform.position = originalGameObjectTransform.thisPosition;
			originalGameObjectTransform.thisTransform.rotation = originalGameObjectTransform.thisRotation;
		}
	}
}
