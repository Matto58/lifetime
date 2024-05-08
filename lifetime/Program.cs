using System.Diagnostics;

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
		rtContainer.ErrOutputHandler += msg => {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write(msg);
			Console.ResetColor();
		};
		LTInterpreter.DebugMode = args.Contains("-d") || Debugger.IsAttached;
		return LTInterpreter.Exec(source, "test.lt", ref rtContainer) ? 0 : 1;
	}
}
