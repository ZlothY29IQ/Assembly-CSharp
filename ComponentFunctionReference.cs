using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public sealed class ComponentFunctionReference<TResult>
{
	[Serializable]
	private struct MethodRef(UnityEngine.Object obj, MethodInfo m)
	{
		public UnityEngine.Object component = obj;

		public string methodName = m.Name;
	}

	[SerializeField]
	private GameObject _target;

	[SerializeField]
	private MethodRef _selection;

	private Func<TResult> _cached;

	public bool IsValid
	{
		get
		{
			if (!_selection.component)
			{
				return !string.IsNullOrEmpty(_selection.methodName);
			}
			return true;
		}
	}

	private IEnumerable<ValueDropdownItem<MethodRef>> GetMethodOptions()
	{
		if (_target == null)
		{
			yield break;
		}
		yield return new ValueDropdownItem<MethodRef>("NONE", default(MethodRef));
		Type type = typeof(GameObject);
		BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		MethodInfo[] methods = type.GetMethods(flags);
		foreach (MethodInfo methodInfo in methods)
		{
			if (methodInfo.GetParameters().Length == 0 && methodInfo.ReturnType == typeof(TResult))
			{
				string text = type.Name + "/" + methodInfo.Name;
				yield return new ValueDropdownItem<MethodRef>(text, new MethodRef(_target, methodInfo));
			}
		}
		Component[] components = _target.GetComponents<Component>();
		foreach (Component comp in components)
		{
			type = comp.GetType();
			methods = type.GetMethods(flags);
			foreach (MethodInfo methodInfo2 in methods)
			{
				if (methodInfo2.GetParameters().Length == 0 && methodInfo2.ReturnType == typeof(TResult))
				{
					string text2 = type.Name + "/" + methodInfo2.Name;
					yield return new ValueDropdownItem<MethodRef>(text2, new MethodRef(comp, methodInfo2));
				}
			}
		}
	}

	public TResult Invoke()
	{
		if (_cached == null)
		{
			Cache();
		}
		if (_cached == null)
		{
			return default(TResult);
		}
		return _cached();
	}

	public void Cache()
	{
		_cached = null;
		if (!(_selection.component == null) && !string.IsNullOrEmpty(_selection.methodName))
		{
			MethodInfo method = _selection.component.GetType().GetMethod(_selection.methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
			if (method != null)
			{
				_cached = (Func<TResult>)Delegate.CreateDelegate(typeof(Func<TResult>), _selection.component, method);
			}
		}
	}
}
