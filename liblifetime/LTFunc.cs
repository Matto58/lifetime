namespace Mattodev.Lifetime;

public interface ILifetimeFunc : ILifetimeVar {
	public int AcceptsArgs { get; }
	public LTVar? Call(ref LTRuntimeContainer runtimeContainer, ILifetimeVar[] funcParams);
}
public class LTInternalFunc(
	string name, string funcNamespace, string funcClass, string returnType, bool isStatic, LTVarAccess access, (string type, string name)[] acceptedArgs,
	Func<LTRuntimeContainer, ILifetimeVar[], (LTVar? ReturnedValue, LTRuntimeContainer ResultingContainer)> executedFunction
) : ILifetimeFunc {
	public string Name { get; init; } = name;
	public string Namespace { get; init; } = funcNamespace;
	public string Class { get; init; } = funcClass;
	public string Type { get; init; } = returnType;
	public string Value = "[IFunc]";
	public bool Constant = true;
	public bool Static { get; init; } = isStatic;
	public bool IsNull = false;
	public LTVarAccess Access { get; init; } = access;
	public int AcceptsArgs => AcceptedArgs.Length;
	public (string type, string name)[] AcceptedArgs { get; init; } = acceptedArgs;
	string ILifetimeVar.Value { get => Value; set => Value = value; }
	bool ILifetimeVar.Constant { get => Constant; init => Constant = value; }
	bool ILifetimeVar.IsNull { get => IsNull; set => IsNull = value; }

	internal Func<LTRuntimeContainer, ILifetimeVar[], (LTVar? ReturnedValue, LTRuntimeContainer ResultingContainer)> execedFunc = executedFunction;

	public LTInternalFunc(
		string name, string funcNamespace, string funcClass, string returnType, bool isStatic, LTVarAccess access, (string type, string name)[] acceptedArgs
	) : this(name, funcNamespace, funcClass, returnType, isStatic, access, acceptedArgs, (c, _) => (null, c)) {}

	public LTVar? Call(ref LTRuntimeContainer runtimeContainer, ILifetimeVar[] funcParams) {
		var (ret, container) = execedFunc(runtimeContainer, funcParams);
		runtimeContainer = container;
		return ret;
	}
}
public class LTDefinedFunc : LTInternalFunc {
	public string[] SourceCode { get; set; }
	public new string Value = "[DFunc]";

	public LTDefinedFunc(
		string name, string funcNamespace, string funcClass, string returnType, bool isStatic, LTVarAccess access, (string type, string name)[] acceptedArgs,
		string[] functionSrcCode
	) : base(name, funcNamespace, funcClass, returnType, isStatic, access, acceptedArgs) {
		SourceCode = functionSrcCode;
		execedFunc = (container, args) => {
			return (null, container); // todo: temporary code until i implement the interpreter
		};
	}
}
