namespace Mattodev.Lifetime;

public class LTRuntimeContainer {
	public string Output;
	public Dictionary<string, LTVar> Vars;
	public Dictionary<string, LTDefinedFunc> DFuncs;
	public Dictionary<string, LTInternalFunc> IFuncs;
	public LTVar? LastReturnedValue;

	public LTRuntimeContainer() {
		Output = "";
		Vars = []; DFuncs = []; IFuncs = [];
	}
}
