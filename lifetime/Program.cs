namespace Mattodev.Lifetime.CmdLineTool;

class Program {
	public static void Main(string[] args) {
		string[] source = File.ReadAllLines("test.lt");
		LTRuntimeContainer rtContainer = LTInterpreter.DefaultContainer;
		LTInterpreter.Exec(source, "test.lt", ref rtContainer);
		Console.Write(rtContainer.Output);
	}
}
