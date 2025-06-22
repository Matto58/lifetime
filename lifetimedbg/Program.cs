namespace Mattodev.Lifetime.CmdLineDebugger;

class Program {
	static bool running = true, debugActive = false;
	static string[] src = [];
	static string filename = "";
	static LTRuntimeContainer rtContainer;
	public static int Main(string[] args) {
		Console.ForegroundColor = ConsoleColor.White;
		Console.WriteLine($"The Lifetime Debugger (Lifetime version {LTInfo.Version})");

		Console.WriteLine("Initializing container");
		rtContainer = LTInterpreter.DefaultContainer();
	
		Console.Write("Enable verbose mode (LTInterpreter.DebugMode)? [y/N]");
		var verbose = Console.ReadKey();
		LTInterpreter.DebugMode = verbose.Key == ConsoleKey.Y;
		Console.WriteLine();

		Console.WriteLine("Creating handlers");
		rtContainer.InputHandler += q => {
			Console.Write(q);
			return Console.ReadLine() ?? "";
		};
		rtContainer.OutputHandler += msg => {
			ConsoleColor old = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(msg);
			Console.ForegroundColor = old;
		};
		rtContainer.ErrOutputHandler += msg => {
			ConsoleColor old = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write(msg);
			Console.ForegroundColor = old;
		};

		foreach (string arg in args) {
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write("dbg > ");
			Console.ForegroundColor = ConsoleColor.DarkGreen;
			Console.WriteLine(arg);
			Console.ForegroundColor = ConsoleColor.White;
			cmd(arg);
		}

		while (running) {
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write("dbg > ");
			Console.ForegroundColor = ConsoleColor.Cyan;
			string? line = Console.ReadLine();
			Console.ForegroundColor = ConsoleColor.White;
			if (line == null) {
				rtContainer.ErrOutputHandler("ERROR: stdin is not available!\n");
				Console.ResetColor();
				return 1;
			}
			cmd(line);
		}
		Console.ResetColor();
		return 0;
	}

	static void cmd(string line) {
		string[] ln = line.Split(' ');

		switch (ln[0]) {
			case "?": {
				Console.WriteLine(
					"? show help\n" +
					"q quit debugger\n" +
					"b <to be implemented> [line] set breakpoint\n" +
					"r run file/continue execution\n" +
					"s <to be implemented> step on the next line\n" +
					"o [filename] open file\n" +
					"l list minified source code");
				break;
			}
			case "q": {
				if (debugActive) {
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.Write("ARE YOU SURE? Debugging is still in progress. [y/N]");
					var conf = Console.ReadKey();
					Console.ForegroundColor = ConsoleColor.White;
					if (conf.Key != ConsoleKey.Y) break;
				}
				debugActive = false;
				running = false;
				break;
			}
			case "o": {
				if (ln.Length < 2) {
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine("Filename not specified.");
					Console.ForegroundColor = ConsoleColor.White;
					break;
				}
				Console.WriteLine("Reading...");
				filename = line[2..];
				var ogSrc = File.ReadAllLines(filename);
				Console.WriteLine("Minifying...");
				src = LTInterpreter.MinifyCode(ogSrc);

				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("Done. Ready to execute.");
				Console.ForegroundColor = ConsoleColor.White;
				break;
			}
			case "r": {
				debugActive = true;
				Console.ForegroundColor = ConsoleColor.DarkGray;
				bool result = LTInterpreter.Exec(src, filename, ref rtContainer, skipMinification: true);
				debugActive = false;
				Console.ForegroundColor = ConsoleColor.DarkCyan;
				Console.WriteLine(result ? "Execution successful." : "Execution failed!");
				break;
			}
			case "l": {
				if (filename == "")
					Console.WriteLine("No file loaded.");
				else
					Console.WriteLine(string.Join('\n', src.Select((s, i) => $"{i+1}:\t{s}"))); // this is terrifying
				break;
			}
			default: {
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("Unknown command.");
				Console.ForegroundColor = ConsoleColor.White;
				break;
			}
		}
	}
}