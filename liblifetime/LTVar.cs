namespace Mattodev.Lifetime;

public enum LTVarAccess { Public, Private }
public interface ILifetimeVar {
	public string Name { get; init; }
	public string Namespace { get; init; }
	public string Class { get; init; }
	public string Type { get; init; }
	public string Value { get; set; }
	public bool Constant { get; init; }
	public bool Static { get; init; }
	public bool IsNull { get; set; }
	public LTVarAccess Access { get; init; }
}

public class LTVar(string name, string varNamespace, string varClass, string varType, string? varValue, bool isConstant, bool isStatic, LTVarAccess access) : ILifetimeVar {
	public string Name { get; init; } = name;
	public string Namespace { get; init; } = varNamespace;
	public string Class { get; init; } = varClass;
	public string Type { get; init; } = varType;
	public string Value { get; set; } = varValue ?? "";
	public bool Constant { get; init; } = isConstant;
	public bool Static { get; init; } = isStatic;
	public bool IsNull { get; set; } = varValue is null;
	public LTVarAccess Access { get; init; } = access;
}
