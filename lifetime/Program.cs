using System.Diagnostics;

namespace Mattodev.Lifetime.CmdLineTool;

class Program {
	public static int Main(string[] args) {
		LTRuntimeContainer rtContainer = LTInterpreter.DefaultContainer();
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
		rtContainer.IgnoreErrs = args.Contains("-ie");
		if (args.Contains("-v")) {
			Console.WriteLine(LTInfo.Version);
			return 0;
		}
		if (args.Contains("-i")) {
			Console.WriteLine(
				$"Lifetime {LTInfo.Version} ({LTInfo.DevYears}) - {LTInfo.RepoUrl}\n" +
				$"Licensed under the MIT license.");
		}

		switch (args.Length < 1 ? "help" : args[0]) {
			case "help":
				Console.WriteLine(
					"lifetime <command> [filename] [switches]\n" +
					"COMMANDS:\n" +
					"\thelp\tdisplays this help and exits\n" +
					"\trun\truns the specified file\n" +
					"SWITCHES:\n" +
					"\t-d\tdebug mode (use only if you are willing to read a lot of spaghetti)\n" +
					"\t-i\tshows info about Lifetime at the start of the program\n" +
					"\t-v\tshows Lifetime version and exits\n" +
					"\t-ie\tmakes errors not exit the program");
				break;
			case "run":
				if (args.Length < 2) {
					rtContainer.ErrOutputHandler("Filename not specified\n");
					return 1;
				}
				if (!File.Exists(args[1])) {
					rtContainer.ErrOutputHandler($"File not found: {args[1]}\n");
					return 1;
				}
				return LTInterpreter.Exec(File.ReadAllLines(args[1]), args[1], ref rtContainer) ? 0 : 1;
			default:
				rtContainer.ErrOutputHandler($"Invalid command: {args[0]}\n");
				return 1;
		}
		return 0;
	}
}
