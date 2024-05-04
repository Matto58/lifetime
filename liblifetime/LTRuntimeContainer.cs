namespace Mattodev.Lifetime;

public class LTRuntimeContainer {
	public string Output;
	public Func<string, string> InputHandler;
	public Action<string> OutputHandler;
	public List<LTVar> Vars;
	public List<LTDefinedFunc> DFuncs;
	public List<LTInternalFunc> IFuncs;
	public LTVar? LastReturnedValue;
	internal List<string> bindedNamespaces = []; 

	public LTRuntimeContainer() {
		Output = "";
		Vars = []; DFuncs = []; IFuncs = [];
		InputHandler = _ => "";
		OutputHandler = _ => {};
	}
}
