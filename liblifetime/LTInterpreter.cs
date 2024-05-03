using System.ComponentModel;

namespace Mattodev.Lifetime;

public enum LTInterpreterState { Idle, Executing, ParsingIf, ParsingFunc }
public class LTInterpreter {
	private static LTInterpreterState state = LTInterpreterState.Idle;
	public static LTInterpreterState State => state;
	public static readonly LTRuntimeContainer DefaultContainer = new() {
		IFuncs = [
			// class: !sys->io
			new("print", "sys", "io", "null", LTVarAccess.Public, [("str", "string")], (c, a) => {
				c.Output += a[0];
				return (null, null, c);
			}),
			new("print_line", "sys", "io", "null", LTVarAccess.Public, [("str", "string")], (c, a) => {
				c.Output += a[0] + "\n";
				return (null, null, c);
			}),
			// class: !sys->dev
			new("is_null", "sys", "dev", "bool", LTVarAccess.Public, [("obj", "object")], (c, a) => {
				return (LTVar.SimpleConst("bool", "_ret", a[0].IsNull ? "true" : "false"), null, c);
			})
		]
	};

	public static void Exec(string[] source, ref LTRuntimeContainer container, bool nested = false) {
		state = LTInterpreterState.Executing;
		var s = MinifyCode(source).Select((l, i) => (l, i));
		foreach ((string line, int i) in s) {
			string[] ln = line.Split(' ');
			Console.WriteLine($"LINE {i+1}: {line}");
			Console.WriteLine($"PARSED: {string.Join(',', ln)}");

			switch (ln[0][0]) {
				case '!':
					
					break;
				default:
					break;
			}
			switch (ln[0]) {
				case "":
					break;
			}
		}
		if (!nested) state = LTInterpreterState.Idle;
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

	public static LTError? FindAndExecFunc(string id, string[] args, string file, string line, int lineNum, ref LTRuntimeContainer container) {
		string[] s1 = id.Split("::");
		if (s1.Length != 2) return new($"Invalid function identifier: {id}", file, line, lineNum);
		string[] s2 = s1[0].Split("->");
		if (s2.Length > 2) return new($"Invalid function identifier: {id}", file, line, lineNum);

		string funcNamespace = s2[0], funcClass = s2[1], funcName = s1[1];
		var indexedDFuncs = indexDFuncs(container);
		var indexedIFuncs = indexIFuncs(container);

		ILifetimeFunc? func = null;
		if (s2.Length == 1) {
			foreach (string ns in container.bindedNamespaces) {
				func = GetFunc(ns, funcClass, funcName, indexedDFuncs, indexedIFuncs);
				if (func != null) return ExecFunc(func, args, file, line, lineNum, ref container);
			}
			if (func == null) return new($"Function not found: {id}", file, line, lineNum);
		}
		func = GetFunc(funcNamespace, funcClass, funcName, indexedDFuncs, indexedIFuncs);
		if (func != null) return ExecFunc(func, args, file, line, lineNum, ref container);
		return new($"Function not found: {id}", file, line, lineNum);
	}
	public static LTError? ExecFunc(ILifetimeFunc func, string[] args, string file, string line, int lineNum, ref LTRuntimeContainer container) {
		if (func.AcceptsArgs != args.Length) return new($"", file, line, lineNum);
		var (v, e) = func.Call(ref container, ParseFuncArgs(args));
		container.LastReturnedValue = v;
		return e;
	}
	public static LTVar[] ParseFuncArgs(string[] args) {
		return []; // todo: temporary thing
	}

	public static ILifetimeFunc? GetFunc(string funcNs, string funcClass, string funcName, LTVarInx<LTDefinedFunc> indexedDFuncs, LTVarInx<LTInternalFunc> indexedIFuncs) {
		if (indexedDFuncs.TryGetValue(funcNs, out var dFuncClasses)
			&& dFuncClasses.TryGetValue(funcClass, out var dFuncs)
			&& dFuncs.TryGetValue(funcName, out var dFunc)) return dFunc;

		if (indexedIFuncs.TryGetValue(funcNs, out var iFuncClasses)
			&& iFuncClasses.TryGetValue(funcClass, out var iFuncs)
			&& iFuncs.TryGetValue(funcName, out var iFunc)) return iFunc;

		return null;
	}
	internal static LTVarInx<LTDefinedFunc> indexDFuncs(LTRuntimeContainer container) {
		LTVarInx<LTDefinedFunc> d = [];
		foreach (LTDefinedFunc func in container.DFuncs)
			d[func.Namespace][func.Class][func.Name] = func;
		return d;
	}
	internal static LTVarInx<LTInternalFunc> indexIFuncs(LTRuntimeContainer container) {
		LTVarInx<LTInternalFunc> d = [];
		foreach (LTInternalFunc func in container.IFuncs)
			d[func.Namespace][func.Class][func.Name] = func;
		return d;
	}
}
