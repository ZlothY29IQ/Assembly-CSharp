using UnityEngine;

public class TransformReset : MonoBehaviour
{
	private struct OriginalGameObjectTransform
	{
		private Transform _thisTransform;

		private Vector3 _thisPosition;

		private Quaternion _thisRotation;

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

		public OriginalGameObjectTransform(Transform constructionTransform)
		{
			_thisTransform = constructionTransform;
			_thisPosition = constructionTransform.position;
			_thisRotation = constructionTransform.rotation;
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
