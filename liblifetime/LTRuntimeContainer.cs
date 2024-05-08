namespace Mattodev.Lifetime;

public class LTRuntimeContainer {
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

	public LTRuntimeContainer() {
		Output = "";
		Vars = []; DFuncs = []; IFuncs = [];
		Handles = [];
		InputHandler = _ => "";
		OutputHandler = _ => {};
		ErrOutputHandler = _ => {};
	}
}
