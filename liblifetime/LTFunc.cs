namespace Mattodev.Lifetime;

public interface ILifetimeFunc : ILifetimeVar {
	public int AcceptsArgs { get; }
	public bool IgnoreArgCount { get; set; }
	public (LTVar?, string?) Call(ref LTRuntimeContainer runtimeContainer, LTVarCollection funcParams);
}
public class LTInternalFunc(
	string name, string funcNamespace, string funcClass, string returnType, LTVarAccess access, (string type, string name)[] acceptedArgs, bool ignoreArgCount,
	Func<LTRuntimeContainer, LTVarCollection, (LTVar? ReturnedValue, string? Error, LTRuntimeContainer ResultingContainer)> executedFunction
) : ILifetimeFunc {
	public string Name { get; set; } = name;
	public string Namespace { get; set; } = funcNamespace;
	public string Class { get; set; } = funcClass;
	public string Type { get; set; } = returnType;
	public string Value = "[IFunc]";
	public bool Constant = true;
	public bool IsNull = false;
	public LTVarAccess Access { get; init; } = access;
	public int AcceptsArgs => AcceptedArgs.Length;
	public bool IgnoreArgCount { get; set; } = ignoreArgCount;
	public (string type, string name)[] AcceptedArgs { get; init; } = acceptedArgs;
	string ILifetimeVar.Value { get => Value; set => Value = value; }
	bool ILifetimeVar.Constant { get => Constant; set => Constant = value; }
	bool ILifetimeVar.IsNull { get => IsNull; set => IsNull = value; }

	internal Func<LTRuntimeContainer, LTVarCollection, (LTVar? ReturnedValue, string? Error, LTRuntimeContainer ResultingContainer)> execedFunc
		= executedFunction;

	public LTInternalFunc(
		string name, string funcNamespace, string funcClass, string returnType, LTVarAccess access, (string type, string name)[] acceptedArgs, bool ignoreArgCount
	) : this(name, funcNamespace, funcClass, returnType, access, acceptedArgs, ignoreArgCount, (c, _) => (null, null, c)) {}

	public (LTVar?, string?) Call(ref LTRuntimeContainer runtimeContainer, LTVarCollection funcParams) {
		var (ret, err, container) = execedFunc((LTRuntimeContainer)runtimeContainer.Clone(), funcParams);
		runtimeContainer.Output = container.Output;
		return (ret, err);
	}

	public object Clone() => MemberwiseClone();
}
public class LTDefinedFunc : LTInternalFunc {
	public string[] SourceCode { get; set; }
	public new string Value = "[DFunc]";

	public LTDefinedFunc(
		string name, string funcNamespace, string funcClass, string returnType, LTVarAccess access, (string type, string name)[] acceptedArgs, bool ignoreArgCount,
		string[] functionSrcCode, string fileName
	) : base(name, funcNamespace, funcClass, returnType, access, acceptedArgs, ignoreArgCount) {
		SourceCode = functionSrcCode;
		execedFunc = (container, args) => {
			LTRuntimeContainer container2 = (LTRuntimeContainer)container.Clone();
			container2._namespace = funcNamespace;
			container2.tempValuesForInterpreter["class"] = funcClass;
			for (int i = 0; i < args.Count; i++) {
				var arg = args[i];
				if (!ignoreArgCount && arg.Type != acceptedArgs[i].type)
					return (null, $"Type mismatch for argument {acceptedArgs[i].name} (expecting {acceptedArgs[i].type}, got {arg.Type})", container);
				arg.Namespace = funcNamespace;
				arg.Class = funcClass;
				arg.Name = ignoreArgCount ? arg.Name : acceptedArgs[i].name;
				container2.Vars.Add(arg);
			}
			LTInterpreter.Exec(SourceCode, $"{fileName} => !{funcNamespace}->{funcClass}::{name}", ref container2, true, true);
			container.Output = container2.Output;
			return (container2.LastReturnedValue, null, container);
		};
	}
}
