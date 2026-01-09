using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

public static class WaitUtils
{
	private static WaitForSeconds _waitForSeconds = new WaitForSeconds(1f);

	private static ParameterExpression _param = Expression.Parameter(typeof(float));

	private static Action<float> _waitForSecondsSetter = Expression.Lambda<Action<float>>(Expression.Assign(Expression.Field(Expression.Constant(_waitForSeconds, typeof(WaitForSeconds)), typeof(WaitForSeconds).GetField("m_Seconds", BindingFlags.Instance | BindingFlags.NonPublic)), _param), new ParameterExpression[1] { _param }).Compile();

	public static WaitForSeconds WaitForSeconds(float seconds)
	{
		_waitForSecondsSetter(seconds);
		return _waitForSeconds;
	}
}
