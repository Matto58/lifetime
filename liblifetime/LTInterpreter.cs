using System.ComponentModel;

namespace Mattodev.Lifetime;

public enum LTInterpreterState { Idle, Executing, ExitSuccess, ExitFail, ParsingIf, ParsingFunc }
public class LTInterpreter {
	private static LTInterpreterState state = LTInterpreterState.Idle;
	public static LTInterpreterState State => state;
	public static readonly LTRuntimeContainer DefaultContainer = new() {
		IFuncs = [
			// class: !sys->io
			new("print", "sys", "io", "obj", LTVarAccess.Public, [("str", "string")], (c, a) => {
				c.Output += a[0].Value;
				return (null, null, c);
			}),
			new("print_line", "sys", "io", "obj", LTVarAccess.Public, [("str", "string")], (c, a) => {
				c.Output += a[0].Value + "\n";
				return (null, null, c);
			}),
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

	public static void Exec(string[] source, string fileName, ref LTRuntimeContainer container, bool nested = false) {
		state = LTInterpreterState.Executing;
		var s = MinifyCode(source).Select((l, i) => (l, i));
		foreach ((string line, int i) in s) {
			string[] ln = line.Split(' ');
			Console.WriteLine($"LINE {i+1}: {line}");
			Console.WriteLine($"PARSED: {string.Join(',', ln)}");

			switch (ln[0][0]) {
				case '!':
					var e = FindAndExecFunc(ln[0][1..], ln.Length > 1 ? ln[1..] : [], fileName, line, i+1, ref container);
					if (e != null) {
						LogError(e, ref container);
						state = LTInterpreterState.ExitFail;
						return;
					}
					break;
				default:
					break;
			}
			switch (ln[0]) {
				/*
				case "":
					break;
				*/
			}
		}
		if (!nested) state = LTInterpreterState.ExitSuccess;
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
		container.Output +=
			$"ERROR!! {error.Message}\n" +
			$"Occured in: {error.File}\n" +
			$"{error.Line.Number}:\t{error.Line.Content}\n";
	}
	public static void LogWarning(string msg, ref LTRuntimeContainer container) {
		container.Output += $"Warning: {msg}";
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
				Console.WriteLine($"Looking for !{funcClass}::{funcName} in binded namespace {ns}...");
				func = GetFunc(ns, funcClass, funcName, indexedDFuncs, indexedIFuncs);
				if (func != null) return ExecFunc(func, args, file, line, lineNum, ref container);
			}
			if (func == null) return new($"Function not found: {id}", file, line, lineNum);
		}

		string funcNamespace = s2[0];
		funcClass = s2[1];
		funcName = s1[1];
		Console.WriteLine($"Looking for !{funcNamespace}->{funcClass}::{funcName}...");
		func = GetFunc(funcNamespace, funcClass, funcName, indexedDFuncs, indexedIFuncs);
		if (func != null) return ExecFunc(func, args, file, line, lineNum, ref container);
		return new($"Function not found: {id}", file, line, lineNum);
	}
	public static LTError? ExecFunc(ILifetimeFunc func, string[] args, string file, string line, int lineNum, ref LTRuntimeContainer container) {
		var args2 = ParseFuncArgs(args, ref container);
		if (func.AcceptsArgs != args2.Count) return new($"Incorrect amount of args passed; passed {args2.Count}, expecting {func.AcceptsArgs}", file, line, lineNum);
		var (v, e) = func.Call(ref container, [.. args2]);
		container.LastReturnedValue = v;
		return e;
	}
	public static List<LTVar> ParseFuncArgs(string[] args, ref LTRuntimeContainer container) {
		List<LTVar> parsed = [];
		bool doingString = false;
		foreach (string arg in args) {
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
		return parsed;
	}

	public static ILifetimeFunc? GetFunc(string funcNs, string funcClass, string funcName, Dictionary<string, LTDefinedFunc> indexedDFuncs, Dictionary<string, LTInternalFunc> indexedIFuncs) {
		Console.WriteLine($"GetFunc: passed {funcNs},{funcClass},{funcName}");
		if (indexedDFuncs.TryGetValue(funcNs + "/" + funcClass + "/" + funcName, out var dFunc)) return dFunc;

		Console.WriteLine("Not found in dfuncs, trying ifuncs");
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
