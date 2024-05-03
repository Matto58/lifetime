namespace Mattodev.Lifetime.CmdLineTool;

class Program {
	public static void Main(string[] args) {
		string[] source = File.ReadAllLines("snippet.lt");
		LTRuntimeContainer rtContainer = new();
		LTInterpreter.Exec(source, ref rtContainer);
	}
}
