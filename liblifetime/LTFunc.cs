namespace Mattodev.Lifetime;

public interface ILifetimeFunc : ILifetimeVar {
	public int AcceptsArgs { get; }
	public (LTVar?, LTError?) Call(ref LTRuntimeContainer runtimeContainer, ILifetimeVar[] funcParams);
}
public class LTInternalFunc(
	string name, string funcNamespace, string funcClass, string returnType, LTVarAccess access, (string type, string name)[] acceptedArgs,
	Func<LTRuntimeContainer, ILifetimeVar[], (LTVar? ReturnedValue, LTError? Error, LTRuntimeContainer ResultingContainer)> executedFunction
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
	public (string type, string name)[] AcceptedArgs { get; init; } = acceptedArgs;
	string ILifetimeVar.Value { get => Value; set => Value = value; }
	bool ILifetimeVar.Constant { get => Constant; set => Constant = value; }
	bool ILifetimeVar.IsNull { get => IsNull; set => IsNull = value; }

	internal Func<LTRuntimeContainer, ILifetimeVar[], (LTVar? ReturnedValue, LTError? Error, LTRuntimeContainer ResultingContainer)> execedFunc
		= executedFunction;

	public LTInternalFunc(
		string name, string funcNamespace, string funcClass, string returnType, LTVarAccess access, (string type, string name)[] acceptedArgs
	) : this(name, funcNamespace, funcClass, returnType, access, acceptedArgs, (c, _) => (null, null, c)) {}

	public (LTVar?, LTError?) Call(ref LTRuntimeContainer runtimeContainer, ILifetimeVar[] funcParams) {
		var (ret, err, container) = execedFunc(runtimeContainer, funcParams);
		runtimeContainer.Output = container.Output;
		return (ret, err);
	}
}
public class LTDefinedFunc : LTInternalFunc {
	public string[] SourceCode { get; set; }
	public new string Value = "[DFunc]";

	public LTDefinedFunc(
		string name, string funcNamespace, string funcClass, string returnType, LTVarAccess access, (string type, string name)[] acceptedArgs,
		string[] functionSrcCode, string fileName
	) : base(name, funcNamespace, funcClass, returnType, access, acceptedArgs) {
		SourceCode = functionSrcCode;
		execedFunc = (container, args) => {
			LTInterpreter.Exec(SourceCode, $"{fileName} => !{funcNamespace}->{funcClass}::{name}", ref container, true);
			return (container.LastReturnedValue, null, container);
		};
	}
}
