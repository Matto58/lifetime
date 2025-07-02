namespace Mattodev.Lifetime;

public class LTRuntimeContainer : ICloneable {
	public string Output;
	public Func<string, string> InputHandler;
	public Action<string> OutputHandler, ErrOutputHandler;
	public LTVarCollection Vars;
	public Dictionary<string, LTDefinedFunc> DFuncs;
	public Dictionary<string, LTInternalFunc> IFuncs;
	public List<FileStream?> Handles;
	public LTVar? LastReturnedValue;
	internal List<string> bindedNamespaces = [];
	internal LTInterpreterState interpreterState => interpreterStateStack.Count > 0 ? interpreterStateStack[^1] : LTInterpreterState.Idle;
	internal List<LTInterpreterState> interpreterStateStack = [LTInterpreterState.Idle];
	internal bool nestedFuncExitedFine = true;
	internal bool tryingForError = false;
	internal LTError? caughtError = null;
	internal bool ignoreErrs = false;
	public bool IgnoreErrs { get => tryingForError || ignoreErrs; set => ignoreErrs = value; }
	internal Dictionary<string, string> tempValuesForInterpreter = [];
	public List<LTInterpreter.Breakpoint> Breakpoints = [];
	internal int continueFromInx = 0;
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

	public static LTRuntimeContainer Create(List<LTInternalFunc> iFuncs, List<LTDefinedFunc> dFuncs) {
		LTRuntimeContainer c = new();
		iFuncs.ForEach(c.AppendIFunc);
		dFuncs.ForEach(c.AppendDFunc);
		return c;
	}
	public void AppendDFunc(LTDefinedFunc f) => DFuncs.Add($"{f.Namespace}/{f.Class}/{f.Name}", f);
	public void AppendIFunc(LTInternalFunc f) => IFuncs.Add($"{f.Namespace}/{f.Class}/{f.Name}", f);

	public object Clone() {
		LTRuntimeContainer clone = (LTRuntimeContainer)MemberwiseClone();
		clone.Vars = [..Vars];
		clone.DFuncs = [];
		DFuncs.ToList().ForEach(f => clone.AppendDFunc((LTDefinedFunc)f.Value.Clone()));
		clone.IFuncs = [];
		IFuncs.ToList().ForEach(f => clone.AppendIFunc((LTInternalFunc)f.Value.Clone()));
		clone.tempValuesForInterpreter = tempValuesForInterpreter.ToDictionary(a => a.Key, a => a.Value);
		return clone;
	}
	public LTInterpreterState GetInterpreterState() => interpreterState;
}
