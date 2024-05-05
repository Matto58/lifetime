namespace Mattodev.Lifetime;

public enum LTInterpreterState { Idle, Executing, ExitSuccess, ExitFail, ParsingIf, ParsingFunc }
public class LTInterpreter {
	public static readonly LTRuntimeContainer DefaultContainer = new() {
		IFuncs = [
			// class: !sys->io
			new("print", "sys", "io", "obj", LTVarAccess.Public, [("str", "string")], (c, a) => {
				c.Output += a[0].Value;
				c.OutputHandler(a[0].Value);
				return (null, null, c);
			}),
			new("print_line", "sys", "io", "obj", LTVarAccess.Public, [("str", "string")], (c, a) => {
				c.Output += a[0].Value + "\n";
				c.OutputHandler(a[0].Value + "\n");
				return (null, null, c);
			}),
			new("read_line", "sys", "io", "str", LTVarAccess.Public, [("str", "question")], (c, a) =>
				(LTVar.SimpleMut("str", "_answer", c.InputHandler(a[0].Value)), null, c)),
			// class: !sys->tools
			new("is_null", "sys", "tools", "bool", LTVarAccess.Public, [("obj", "object")], (c, a) => {
				return (LTVar.SimpleConst("bool", "_ret", a[0].IsNull ? "true" : "false"), null, c);
			}),
			// class: !sys->dev
			new("bindns", "sys", "dev", "obj", LTVarAccess.Public, [("str", "namespace")], (c, a) => {
				c.bindedNamespaces.Add(a[0].Value);
				return (null, null, c);
			}),
			new("unbindns", "sys", "dev", "obj", LTVarAccess.Public, [("str", "namespace")], (c, a) => {
				if (c.bindedNamespaces.Contains(a[0].Value))
					c.bindedNamespaces.Remove(a[0].Value);
				return (null, null, c);
			}),
		]
	};

	// yes, this variable is readwritable, even by external programs, this is by design
	public static bool DebugMode = false;

	public static bool Exec(string[] source, string fileName, ref LTRuntimeContainer container, bool nested = false) {
		container.interpreterState = LTInterpreterState.Executing;
		var s = MinifyCode(source).Select((l, i) => (l, i));
		foreach ((string line, int i) in s) {
			string[] ln = line.Split(' ');
			if (DebugMode) {
				Console.WriteLine($"LINE {i+1}: {line}");
				Console.WriteLine($"PARSED: {string.Join(',', ln)}");
			}

			switch (ln[0][0]) {
				case '!':
					var e = FindAndExecFunc(ln[0][1..], ln.Length > 1 ? ln[1..] : [], fileName, line, i+1, ref container);
					if (e != null) {
						// LogError shouldnt run if a function that ran within this one exited with an error and already logged it
						if (!container.nestedFuncExitedFine)
							LogError(e, ref container);
						container.interpreterState = LTInterpreterState.ExitFail;
						container.nestedFuncExitedFine = false;
						return false;
					}
					break;
				default:
					break;
			}
			switch (ln[0]) {
				// variable definition: let <type> <variable name> <value>
				case "let":
					if (ln.Length < 4) {
						LogError(new("Missing variable type, name and/or value", fileName, line, i+1), ref container);
						return false;
					}
					// todo: filter container vars by namespace and class and do the check on that after implementing class and namespace definitions
					if (container.Vars.Select(v => v.Name).Contains(ln[2])) {
						LogError(new($"Invalid variable redefinition (try doing ${ln[2]} <- {string.Join(' ', ln[3..])})", fileName, line, i+1), ref container);
						return false;
					}
					var (val, e) = ParseFuncArgs(ln[3..], fileName, line, i+1, ref container);
					if (e != null) {
						LogError(e, ref container);
						return false;
					}
					if (val.Count != 1) {
						LogError(new($"", fileName, line, i+1), ref container);
						return false;
					}
					val[0].Constant = false;
					val[0].Name = ln[2];
					val[0].Type = ln[1];
					container.Vars.Add(val[0]);
					break;
			}
		}
		if (!nested) container.interpreterState = LTInterpreterState.ExitSuccess;
		return true;
	}

	public static string[] MinifyCode(string[] lines) {
		// trim and remove empty lines
		lines = lines
			.Select(l => l.Trim())
			.Where(l => l.Length != 0)
			.ToArray();

		// remove block comments
		bool canAdd = true;
		List<string> a = [];
		foreach (string line in lines) {
			if (canAdd) a.Add(line);
			if (line.Length < 2) continue;
			if (line[..2] == "#>") canAdd = false;
			else if (line[..2] == "<#") canAdd = true;
			else continue;
		}
		lines = [..a];

		string joinedLines = string.Join('\n', lines);
		//Console.WriteLine(joinedLines);

		// remove inline comments, trim end, remove empty lines, convert to array and return
		return joinedLines
			.Split('\n')
			.Select(l => l
				.Split('#')[0]
				.TrimEnd())
			.Where(l => l.Length != 0)
			.ToArray();
	}

	public static void LogError(LTError error, ref LTRuntimeContainer container) {
		string msg =
			$"ERROR!! {error.Message}\n" +
			$"Occured in: {error.File}\n" +
			$"{error.Line.Number}:\t{error.Line.Content}\n";
		container.Output += msg;
		container.OutputHandler(msg);
	}
	public static void LogWarning(string msg, ref LTRuntimeContainer container) {
		string msg2 = $"Warning: {msg}\n";
		container.Output += msg2;
		container.OutputHandler(msg2);
	}

	public static LTError? FindAndExecFunc(string id, string[] args, string file, string line, int lineNum, ref LTRuntimeContainer container) {
		string[] s1 = id.Split("::");
		if (s1.Length != 2) return new($"Invalid function identifier: {id}", file, line, lineNum);
		string[] s2 = s1[0].Split("->");
		if (s2.Length > 2) return new($"Invalid function identifier: {id}", file, line, lineNum);

		var indexedDFuncs = indexDFuncs(container);
		var indexedIFuncs = indexIFuncs(container);

		ILifetimeFunc? func = null;
		string funcClass, funcName;
		if (s2.Length == 1) {
			funcClass = s2[0];
			funcName = s1[1];
			foreach (string ns in container.bindedNamespaces) {
				if (DebugMode) Console.WriteLine($"Looking for !{funcClass}::{funcName} in binded namespace {ns}...");
				func = GetFunc(ns, funcClass, funcName, indexedDFuncs, indexedIFuncs);
				if (func != null) {
					var e = ExecFunc(func, args, file, line, lineNum, ref container);
					container.nestedFuncExitedFine = e == null;
					return e;
				}
			}
			container.nestedFuncExitedFine = func == null;
			if (func == null) return new($"Function not found: {id}", file, line, lineNum);
		}

		string funcNamespace = s2[0];
		funcClass = s2[1];
		funcName = s1[1];
		if (DebugMode) Console.WriteLine($"Looking for !{funcNamespace}->{funcClass}::{funcName}...");
		func = GetFunc(funcNamespace, funcClass, funcName, indexedDFuncs, indexedIFuncs);
		if (func != null) {
			var e = ExecFunc(func, args, file, line, lineNum, ref container);
			container.nestedFuncExitedFine = e == null;
			return e;
		}
		container.nestedFuncExitedFine = false;
		return new($"Function not found: {id}", file, line, lineNum);
	}
	public static LTError? ExecFunc(ILifetimeFunc func, string[] args, string file, string line, int lineNum, ref LTRuntimeContainer container) {
		var (args2, e) = ParseFuncArgs(args, file, line, lineNum, ref container);
		if (func.AcceptsArgs != args2.Count)
			return new($"Incorrect amount of args passed; passed {args2.Count}, expecting {func.AcceptsArgs}", file, line, lineNum);
		if (e != null)
			return e;

		var (v, e2) = func.Call(ref container, [.. args2]);
		container.LastReturnedValue = v;
		return e2;
	}
	public static (List<LTVar> Vars, LTError? Error) ParseFuncArgs(string[] args, string file, string line, int lineNum, ref LTRuntimeContainer container) {
		List<LTVar> parsed = [];
		bool doingString = false;
		foreach (string arg in args) {
			if (DebugMode) Console.WriteLine("ParseFuncArgs: parsing " + arg);
			if (arg[0] == '"') {
				doingString = true;
				string s = "";
				if (arg[^1] == '"') {
					s = arg[1..^1];
					doingString = false;
				}
				else s = arg.Length > 1 ? arg[1..] : "";
				parsed.Add(LTVar.SimpleMut("str", "arg" + parsed.Count, s));
				continue;
			}
			else if (arg[0] == '$') {
				// todo: filter container vars by namespace and class and do the check on that after implementing class and namespace definitions
				var v = container.Vars.Where(v => v.Name == arg[1..]);
				if (v.Any())
					parsed.Add(v.First());
				else
					return ([], new($"Referenced variable not found: {arg}", file, line, lineNum));
				continue;
			}
			if (doingString)
				if (arg[^1] == '"') {
					parsed[^1].Value += " " + arg[..^1];
					doingString = false;
				}
				else parsed[^1].Value += " " + arg;
			else if (int.TryParse(arg, out int n))
				parsed.Add(LTVar.SimpleMut("int32", "arg" + parsed.Count, n.ToString()));
			else if (bool.TryParse(arg, out bool b))
				parsed.Add(LTVar.SimpleMut("bool", "arg" + parsed.Count, b.ToString()));
			else {
				LogWarning($"Unable to parse {arg} as a str, returning it as an obj", ref container);
				parsed.Add(LTVar.SimpleMut("obj", "arg" + parsed.Count, arg));
			}
		}
		if (DebugMode) Console.WriteLine("ParseFuncArgs: parsed " + parsed.Count + " args");
		return (parsed, null);
	}

	public static ILifetimeFunc? GetFunc(string funcNs, string funcClass, string funcName, Dictionary<string, LTDefinedFunc> indexedDFuncs, Dictionary<string, LTInternalFunc> indexedIFuncs) {
		if (DebugMode) Console.WriteLine($"GetFunc: passed {funcNs},{funcClass},{funcName}");
		if (indexedDFuncs.TryGetValue(funcNs + "/" + funcClass + "/" + funcName, out var dFunc)) return dFunc;

		if (DebugMode) Console.WriteLine("Not found in dfuncs, trying ifuncs");
		if (indexedIFuncs.TryGetValue(funcNs + "/" + funcClass + "/" + funcName, out var iFunc)) return iFunc;

		return null;
	}
	internal static Dictionary<string, LTDefinedFunc> indexDFuncs(LTRuntimeContainer container) {
		Dictionary<string, LTDefinedFunc> d = [];
		foreach (LTDefinedFunc func in container.DFuncs)
			d[func.Namespace + "/" + func.Class + "/" + func.Name] = func;
		return d;
	}
	internal static Dictionary<string, LTInternalFunc> indexIFuncs(LTRuntimeContainer container) {
		Dictionary<string, LTInternalFunc> d = [];
		foreach (LTInternalFunc func in container.IFuncs) {
			d[func.Namespace + "/" + func.Class + "/" + func.Name] = func;
		}
		return d;
	}
}
