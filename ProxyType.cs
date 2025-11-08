using System;
using System.Globalization;
using System.Reflection;

public class ProxyType : Type
{
	private Type _self = typeof(ProxyType);

	private readonly string _typeName;

	private static readonly string kPrefix = "ProxyType.";

	private static InvalidType kInvalidType = new InvalidType();

	public override string Name => _typeName;

	public override string FullName => kPrefix + _typeName;

	public override Module Module => _self.Module;

	public override string Namespace => _self.Namespace;

	public override Type UnderlyingSystemType => _self.UnderlyingSystemType;

	public override Assembly Assembly => _self.Assembly;

	public override string AssemblyQualifiedName => _self.AssemblyQualifiedName.Replace("ProxyType", FullName);

	public override Type BaseType => _self.BaseType;

	public override Guid GUID => _self.GUID;

	public ProxyType()
	{
	}

	public ProxyType(string typeName)
	{
		_typeName = typeName;
	}

	public static ProxyType Parse(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			throw new ArgumentNullException("input");
		}
		input = input.Trim();
		if (!input.Contains(kPrefix, StringComparison.InvariantCultureIgnoreCase))
		{
			return kInvalidType;
		}
		if (!input.StartsWith(kPrefix, StringComparison.InvariantCultureIgnoreCase))
		{
			return kInvalidType;
		}
		if (input.Contains(','))
		{
			input = input.Split(',')[0];
		}
		string text = input.Split('.')[1].Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return kInvalidType;
		}
		return new ProxyType(text);
	}

	public override string ToString()
	{
		return base.ToString() + "." + _typeName;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return _self.GetCustomAttributes(inherit);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		return _self.GetCustomAttributes(attributeType, inherit);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		return _self.IsDefined(attributeType, inherit);
	}

	protected override TypeAttributes GetAttributeFlagsImpl()
	{
		return TypeAttributes.NotPublic;
	}

	protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		return null;
	}

	public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
	{
		return _self.GetConstructors(bindingAttr);
	}

	public override Type GetElementType()
	{
		return _self.GetElementType();
	}

	public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
	{
		return _self.GetEvent(name, bindingAttr);
	}

	public override EventInfo[] GetEvents(BindingFlags bindingAttr)
	{
		return _self.GetEvents(bindingAttr);
	}

	public override FieldInfo GetField(string name, BindingFlags bindingAttr)
	{
		return _self.GetField(name, bindingAttr);
	}

	public override FieldInfo[] GetFields(BindingFlags bindingAttr)
	{
		return _self.GetFields(bindingAttr);
	}

	public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
	{
		return _self.GetMembers(bindingAttr);
	}

	protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
	{
		return null;
	}

	public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
	{
		return _self.GetMethods(bindingAttr);
	}

	public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
	{
		return _self.GetProperties(bindingAttr);
	}

	public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
	{
		return _self.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
	}

	protected override bool IsArrayImpl()
	{
		return false;
	}

	protected override bool IsByRefImpl()
	{
		return false;
	}

	protected override bool IsCOMObjectImpl()
	{
		return false;
	}

	protected override bool IsPointerImpl()
	{
		return false;
	}

	protected override bool IsPrimitiveImpl()
	{
		return false;
	}

	protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
	{
		return null;
	}

	protected override bool HasElementTypeImpl()
	{
		return false;
	}

	public override Type GetNestedType(string name, BindingFlags bindingAttr)
	{
		return _self.GetNestedType(name, bindingAttr);
	}

	public override Type[] GetNestedTypes(BindingFlags bindingAttr)
	{
		return _self.GetNestedTypes(bindingAttr);
	}

	public override Type GetInterface(string name, bool ignoreCase)
	{
		return _self.GetInterface(name, ignoreCase);
	}

	public override Type[] GetInterfaces()
	{
		return _self.GetInterfaces();
	}
}
