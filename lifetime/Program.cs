namespace Mattodev.Lifetime.CmdLineTool;

class Program {
	public static void Main(string[] args) {
		string[] source = File.ReadAllLines("test.lt");
		LTRuntimeContainer rtContainer = new();
		LTInterpreter.Exec(source, ref rtContainer);
		Console.Write(rtContainer.Output);
	}
}
