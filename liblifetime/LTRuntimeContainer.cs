namespace Mattodev.Lifetime;

public class LTRuntimeContainer : ICloneable {
	public string Output;
	public Func<string, string> InputHandler;
	public Action<string> OutputHandler, ErrOutputHandler;
	public List<LTVar> Vars;
	public List<LTDefinedFunc> DFuncs;
	public List<LTInternalFunc> IFuncs;
	public List<FileStream?> Handles;
	public LTVar? LastReturnedValue;
	internal List<string> bindedNamespaces = [];
	internal LTInterpreterState interpreterState = LTInterpreterState.Idle;
	internal bool nestedFuncExitedFine = true;
	internal Dictionary<string, string> tempValuesForInterpreter = [];
	internal string _namespace = "";
	public string Namespace => _namespace;
	public string Class => tempValuesForInterpreter.GetValueOrDefault("class", "");

	public LTRuntimeContainer() {
		Output = "";
		Vars = []; DFuncs = []; IFuncs = [];
		Handles = [];
		InputHandler = _ => "";
		OutputHandler = _ => {};
		ErrOutputHandler = _ => {};
	}

	public object Clone() {
		LTRuntimeContainer clone = (LTRuntimeContainer)MemberwiseClone();
		clone.Vars = [..Vars];
		clone.DFuncs = [];
		DFuncs.ForEach(f => clone.DFuncs.Add((LTDefinedFunc)f.Clone()));
		clone.IFuncs = [];
		IFuncs.ForEach(f => clone.IFuncs.Add((LTInternalFunc)f.Clone()));
		clone.tempValuesForInterpreter = tempValuesForInterpreter.ToDictionary(a => a.Key, a => a.Value);
		return clone;
	}
}
