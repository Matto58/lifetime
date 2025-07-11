namespace Mattodev.Lifetime;

public enum LTVarAccess { Public, Private }
public interface ILifetimeVar : ICloneable {
	public string Name { get; set; }
	public string Namespace { get; set; }
	public string Class { get; set; }
	public string Type { get; set; }
	public string Value { get; set; }
	public bool Constant { get; set; }
	public bool IsNull { get; set; }
	public LTVarAccess Access { get; init; }
	public Action? OnValueGet { get; set; }
	public Action? OnValueSet { get; set; }
}

public class LTVar(string name, string varNamespace, string varClass, string varType, string? varValue, bool isConstant, LTVarAccess access) : ILifetimeVar {
	public string Name { get; set; } = name;
	public string Namespace { get; set; } = varNamespace;
	public string Class { get; set; } = varClass;
	public string Type { get; set; } = varType;
	public string Value { get; set; } = varValue ?? "";
	public bool Constant { get; set; } = isConstant;
	public bool IsNull { get; set; } = varValue is null;
	public LTVarAccess Access { get; init; } = access;
	public Action? OnValueGet { get; set; } = null;
	public Action? OnValueSet { get; set; } = null;

	public static LTVar SimpleConst(string type, string name, string? value, string _namespace, string _class)
		=> new(name, _namespace, _class, type, value, true, LTVarAccess.Public);
	public static LTVar SimpleMut(string type, string name, string? value, string _namespace, string _class)
		=> new(name, _namespace, _class, type, value, false, LTVarAccess.Public);

	public object Clone() => MemberwiseClone();
}
