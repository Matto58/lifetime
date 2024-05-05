namespace Mattodev.Lifetime.CmdLineTool;

class Program {
	public static int Main(string[] args) {
		string[] source = File.ReadAllLines("test.lt");
		LTRuntimeContainer rtContainer = LTInterpreter.DefaultContainer;
		rtContainer.InputHandler += q => {
			Console.Write(q);
			return Console.ReadLine() ?? "";
		};
		rtContainer.OutputHandler += Console.Write;
		LTInterpreter.DebugMode = args.Contains("-d");
		return LTInterpreter.Exec(source, "test.lt", ref rtContainer) ? 0 : 1;
	}
}
