using System;

public class InvalidType : ProxyType
{
	private Type _self = typeof(InvalidType);

	public override string Name => _self.Name;

	public override string FullName => _self.FullName;

	public override string AssemblyQualifiedName => _self.AssemblyQualifiedName;
}
