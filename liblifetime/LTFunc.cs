using System.Numerics;

namespace Mattodev.Lifetime;

public delegate (LTVar?, string?) LTFuncBase(ref LTRuntimeContainer runtimeContainer, LTVarCollection funcParams);

public interface ILifetimeFunc : ILifetimeVar {
	public int AcceptsArgs { get; }
	public bool IgnoreArgCount { get; set; }
	public (LTVarType type, string name)[] AcceptedArgs { get; init; }
	public LTFuncBase? Call { get; init; }
}
public class LTInternalFunc(
	string name, string funcNamespace, string funcClass, LTVarType returnType, LTVarAccess access, (LTVarType type, string name)[] acceptedArgs, bool ignoreArgCount,
	LTFuncBase? executedFunction
) : ILifetimeFunc {
	public string Name { get; set; } = name;
	public string Namespace { get; set; } = funcNamespace;
	public string Class { get; set; } = funcClass;
	public LTVarType Type { get; set; } = returnType;
	public string Value = "[IFunc]";
	public bool Constant { get => true; set => throw new Exception("IFuncs must be constant"); }
	public bool IsNull { get => false; }
	public LTVarAccess Access { get; init; } = access;
	public int AcceptsArgs => AcceptedArgs.Length;
	public bool IgnoreArgCount { get; set; } = ignoreArgCount;
	public (LTVarType type, string name)[] AcceptedArgs { get; init; } = acceptedArgs;
	public Action? OnValueGet { get; set; } = null;
	public Action? OnValueSet { get; set; } = null;
	public LTFuncBase? Call { get; init; } = executedFunction;

	public (LTVar?, string?) CallSafe(ref LTRuntimeContainer runtimeContainer, LTVarCollection funcParams) {
		if (Call == null) return (null, "Executed function is internally null");
		return Call(ref runtimeContainer, funcParams);
	}

	public long? AsInt() => default;
	public ulong? AsUInt() => default;
	public string? AsStr() => null;
	public byte[]? AsRaw() => null;
	public bool? AsBool() => null;
	public void AssignInt(long i) {}
	public void AssignUInt(ulong u) {}
	public void AssignStr(string s) {}
	public void AssignRaw(byte[] v) {}
	public void AssignBool(bool b) {}
	public void AssignNull() {}

	public object Clone() => MemberwiseClone();
}
public class LTDefinedFunc : LTInternalFunc {
	public string[] SourceCode { get; set; }
	public new string Value = "[DFunc]";

	public LTDefinedFunc(
		string name, string funcNamespace, string funcClass, LTVarType returnType, LTVarAccess access, (LTVarType type, string name)[] acceptedArgs, bool ignoreArgCount,
		string[] functionSrcCode, string fileName
	) : base(name, funcNamespace, funcClass, returnType, access, acceptedArgs, ignoreArgCount, null) {
		SourceCode = functionSrcCode;
		Call = (ref LTRuntimeContainer container, LTVarCollection args) => {
			container._namespace = funcNamespace;
			container.tempValuesForInterpreter["class"] = funcClass;
			for (int i = 0; i < args.Count; i++) {
				var arg = args[i];
				if (!ignoreArgCount && arg.Type != acceptedArgs[i].type)
					return (null, $"Type mismatch for argument {acceptedArgs[i].name} (expecting {acceptedArgs[i].type}, got {arg.Type})");
				arg.Namespace = funcNamespace;
				arg.Class = funcClass;
				arg.Name = ignoreArgCount ? arg.Name : acceptedArgs[i].name;
				container.Vars.Add(arg);
			}
			LTInterpreter.Exec(SourceCode, $"{fileName} => !{funcNamespace}->{funcClass}::{name}", ref container, true, true);
			return (container.LastReturnedValue, null);
		};
	}
}
