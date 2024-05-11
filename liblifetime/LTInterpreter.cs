using System.Diagnostics;

namespace Mattodev.Lifetime;

public enum LTInterpreterState { Idle, Executing, ExitSuccess, ExitFail, ParsingIf, ParsingFunc }
public partial class LTInterpreter {
	// yes, this variable is readwritable, even by external programs, this is by design
	public static bool DebugMode = false;

	public static bool Exec(string[] source, string fileName, ref LTRuntimeContainer container, bool nested = false) {
		container.interpreterState = LTInterpreterState.Executing;
		var s = MinifyCode(source).Select((l, i) => (l, i)).ToArray();
		Stopwatch? sw = null;
		if (DebugMode) sw = Stopwatch.StartNew();
		foreach ((string line, int i) in s) {
			string[] ln = line.Split(' ');
			if (DebugMode) {
				Console.WriteLine($"Exec: LINE {i+1}: {line}");
				Console.WriteLine($"Exec: PARSED: {string.Join(',', ln)}");
				Console.WriteLine($"Exec: STATE: {container.interpreterState}");
			}

			switch (container.interpreterState) {
				case LTInterpreterState.ParsingFunc:
					if (line == "end") {
						container.interpreterState = (LTInterpreterState)int.Parse(container.tempValuesForInterpreter["fn_prevstate"]);
						var (fnArgs, e) = ParseFuncDefArgs(
							container.tempValuesForInterpreter["fn_args"].Split('\x1'),
							fileName,
							container.tempValuesForInterpreter["fn_defln"],
							int.Parse(container.tempValuesForInterpreter["fn_deflnnum"]));
						if (e != null) {
							LogError(e, ref container);
							return swStop(ref sw, fileName, ref container);
						}

						LTDefinedFunc f = new(
							container.tempValuesForInterpreter["fn_name"],
							container.Namespace,
							container.Class,
							container.tempValuesForInterpreter["fn_type"],
							LTVarAccess.Public,
							fnArgs,
							false,
							// whoops! the following line will make the func source code have an extra empty line
							// (which will get popped by the interpreter anyway when the function gets ran)
							container.tempValuesForInterpreter["fn_src"].Split('\x1'),
							fileName
						);
						container.AppendDFunc(f);
						if (DebugMode)
							Console.WriteLine($"Exec: defined function {f.Name} ({f.SourceCode.Length-1} lines, {f.AcceptsArgs} args)");

						string c = container.Class;
						container.tempValuesForInterpreter.Clear();
						container.tempValuesForInterpreter["class"] = c;
					}
					else {
						container.tempValuesForInterpreter["fn_src"] += line + "\x1";
						if (DebugMode)
							Console.WriteLine($"Exec: adding {line} to source of dfunc {container.tempValuesForInterpreter["fn_name"]}");
					}
					continue;
				default:
					if (line == "end") {
						if (!container.tempValuesForInterpreter.Remove("class")) {
							LogError(new($"Unexpected end keyword", fileName, line, i+1), ref container);
							return swStop(ref sw, fileName, ref container);	
						} else continue;
					}
					break;
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
						return swStop(ref sw, fileName, ref container);
					}
					continue;
			}
			switch (ln[0]) {
				// variable definition: let <type> <variable name> <value>
				case "let":
					if (ln.Length < 3) {
						LogError(new("Missing variable type, name and/or value", fileName, line, i+1), ref container);
						return swStop(ref sw, fileName, ref container);
					}
					// dumb hack numero dos
					string c = container.tempValuesForInterpreter.GetValueOrDefault("class", "");
					string ns = container._namespace;
					if (container.Vars.Where(v => v.Namespace == ns && v.Class == c).Select(v => v.Name).Contains(ln[2])) {
						LogError(new($"Invalid variable redefinition (try doing ${ln[2]} <- {string.Join(' ', ln[3..])})", fileName, line, i+1), ref container);
						return swStop(ref sw, fileName, ref container);
					}
					var (val, e) = ParseFuncArgs(ln[3..], fileName, line, i+1, ref container);
					if (e != null) {
						LogError(e, ref container);
						return swStop(ref sw, fileName, ref container);
					}
					if (val.Count != 1) {
						LogError(new($"Invalid value: {string.Join(' ', ln[3..])}", fileName, line, i+1), ref container);
						return swStop(ref sw, fileName, ref container);
					}
					val[0].Constant = false;
					val[0].Name = ln[2];
					val[0].Type = ln[1];
					val[0].Namespace = container.Namespace;
					val[0].Class = container.Class;
					container.Vars.Add(val[0]);
					break;
				case "fn":
					if (ln.Length < 3) {
						LogError(new($"Missing function type and/or name", fileName, line, i+1), ref container);
						return swStop(ref sw, fileName, ref container);
					}
					Dictionary<string, string> funcProps = new() {
						{ "fn_type", ln[1] },
						{ "fn_name", ln[2] },
						{ "fn_args", ln.Length > 3 ? string.Join('\x01', ln[3..]) : "" },
						{ "fn_src", "" },
						{ "fn_defln", line },
						{ "fn_deflnnum", (i+1).ToString() },
						{ "fn_prevstate", ((int)container.interpreterState).ToString() }
					};
					container.interpreterState = LTInterpreterState.ParsingFunc;
					foreach (var (k, v) in funcProps.Select(kvp => (kvp.Key, kvp.Value)))
						container.tempValuesForInterpreter.Add(k, v);
					break;
				//case "if":
				//
				//	break;
				case "namespace":
					if (ln.Length != 2) {
						LogError(new(ln.Length < 2 ? "Namespace not specified" : $"Too many arguments; passed {ln.Length}, expecting 1", fileName, line, i+1), ref container);
						return swStop(ref sw, fileName, ref container);
					}
					if (container._namespace != "") {
						LogError(new("Namespace already specified", fileName, line, i+1), ref container);
						return swStop(ref sw, fileName, ref container);
					}
					container._namespace = ln[1];
					if (DebugMode) Console.WriteLine($"Exec: container namespace is now {container._namespace}");
					break;
				case "class":
					if (ln.Length != 2) {
						LogError(new(ln.Length < 2 ? "Class not specified" : $"Too many arguments; passed {ln.Length}, expecting 1", fileName, line, i+1), ref container);
						return swStop(ref sw, fileName, ref container);
					}
					container.tempValuesForInterpreter.Add("class", ln[1]);
					break;
				default:
					LogError(new($"Invalid keyword: {ln[0]}", fileName, line, i+1), ref container);
					return swStop(ref sw, fileName, ref container);
			}
		}
		if (!nested) container.interpreterState = LTInterpreterState.ExitSuccess;
		return swStop(ref sw, fileName, ref container, true);
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
		container.ErrOutputHandler(msg);
	}
	public static void LogWarning(string msg, ref LTRuntimeContainer container) {
		string msg2 = $"Warning: {msg}\n";
		container.Output += msg2;
		container.ErrOutputHandler(msg2);
	}
	public static void LogMsg(string msg, ref LTRuntimeContainer container, bool newline = false) {
		msg += newline ? "\n" : "";
		container.Output += msg;
		container.OutputHandler(msg);
	}

	public static LTError? FindAndExecFunc(string id, string[] args, string file, string line, int lineNum, ref LTRuntimeContainer container) {
		string[] s1 = id.Split("::");
		if (s1.Length != 2) {
			if (string.IsNullOrEmpty(s1[0]))
				return new("Missing function identifier", file, line, lineNum);
			if (DebugMode)
				Console.WriteLine($"FindAndExecFunc: looking for !{s1[0]} in namespace '{container.Namespace}', class '{container.Class}'...");

			ILifetimeFunc? f = GetFunc(container.Namespace, container.Class, s1[0], container.DFuncs, container.IFuncs);
			if (f == null) return new($"Invalid function identifier: {id}", file, line, lineNum);

			var e = ExecFunc(f, args, file, line, lineNum, ref container);
			container.nestedFuncExitedFine = e == null;
			return e;
		}
		string[] s2 = s1[0].Split("->");
		if (s2.Length > 2) return new($"Invalid function identifier: {id}", file, line, lineNum);

		ILifetimeFunc? func = null;
		string funcClass, funcName;
		if (s2.Length == 1) {
			funcClass = s2[0];
			funcName = s1[1];
			foreach (string ns in container.bindedNamespaces) {
				if (DebugMode) Console.WriteLine($"FindAndExecFunc: looking for !{funcClass}::{funcName} in binded namespace {ns}...");
				func = GetFunc(ns, funcClass, funcName, container.DFuncs, container.IFuncs);
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
		if (DebugMode) Console.WriteLine($"FindAndExecFunc: looking for !{funcNamespace}->{funcClass}::{funcName}...");
		func = GetFunc(funcNamespace, funcClass, funcName, container.DFuncs, container.IFuncs);
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
		if (e != null)
			return e;
		if (func.AcceptsArgs != args2.Count && !func.IgnoreArgCount)
			return new($"Incorrect amount of args passed; passed {args2.Count}, expecting {func.AcceptsArgs}", file, line, lineNum);

		var (v, e2) = func.Call(ref container, [.. args2]);
		container.LastReturnedValue = v;
		return e2 != null ? new(e2, file, line, lineNum) : null;
	}
	public static (List<LTVar> Vars, LTError? Error) ParseFuncArgs(string[] args, string file, string line, int lineNum, ref LTRuntimeContainer container) {
		List<LTVar> parsed = [];
		bool doingString = false;
		foreach ((string arg, int i) in args.Select((a, b) => (a, b))) {
			if (DebugMode) Console.WriteLine("ParseFuncArgs: parsing " + arg);
			if (arg[0] == '"' && !doingString) {
				doingString = true;
				string s = "";
				if (arg[^1] == '"') {
					s = arg[1..^1];
					doingString = false;
				}
				else s = arg.Length > 1 ? arg[1..] : "";
				parsed.Add(LTVar.SimpleMut("str", "arg" + parsed.Count, s, container.Namespace, container.Class));
				continue;
			}
			else if (arg[0] == '$') {
				// dumb ass hack
				string ns = container.Namespace;
				string c = container.Class;

				// yes im aware i practically ripped this from FindAndExecFunc, not my fault its great
				string s0 = arg[1..];
				if (string.IsNullOrEmpty(s0))
					return ([], new("Missing variable identifier", file, line, lineNum));

				string[] s1 = s0.Split("::");
				if (s1.Length != 2) {
					var v = container.Vars.Where(v => v.Name == arg[1..] && v.Namespace == ns && v.Class == c);
					if (v.Any()) {
						parsed.Add(v.First());
						continue;
					}
					return ([], new($"Referenced variable not found: {arg}", file, line, lineNum));
				}
				string[] s2 = s1[0].Split("->");
				if (s2.Length > 2) return ([], new($"Invalid variable identifier: {arg}", file, line, lineNum));

				var v2 = container.Vars.Where(v => v.Name == s1[1] && v.Namespace == s2[0] && v.Class == s2[1]);
				if (v2.Any()) parsed.Add(v2.First());
				else return ([], new($"Referenced variable not found: {arg}", file, line, lineNum));
				continue;
			}
			else if (arg[0] == '!') {
				var e = FindAndExecFunc(arg[1..], args.Length >= i ? args[(i+1)..] : [], file, line, lineNum, ref container);
				parsed.Add(container.LastReturnedValue!); // todo: scary to put a bang there
				return (parsed, e);
			}
			else if (DebugMode && !doingString)
				Console.WriteLine($"ParseFuncArgs: unknown arg prefix {arg[0]}, gonna parse differently");

			if (doingString)
				if (arg[^1] == '"') {
					parsed[^1].Value += " " + arg[..^1];
					doingString = false;
				}
				else parsed[^1].Value += " " + arg;
			else if (int.TryParse(arg, out int n))
				parsed.Add(LTVar.SimpleMut("int32", "arg" + parsed.Count, n.ToString(), container.Namespace, container.Class));
			else if (bool.TryParse(arg, out bool b))
				parsed.Add(LTVar.SimpleMut("bool", "arg" + parsed.Count, b.ToString(), container.Namespace, container.Class));
			else {
				LogWarning($"Unable to parse {arg} as a str, returning it as an obj", ref container);
				parsed.Add(LTVar.SimpleMut("obj", "arg" + parsed.Count, arg, container.Namespace, container.Class));
			}
		}
		if (DebugMode) Console.WriteLine("ParseFuncArgs: parsed " + parsed.Count + " args");
		return (parsed, null);
	}

	// todo: this function relies heavily on linq-based syntactical sugar, no idea if that's a good thing or not
	public static ((string Type, string Name)[] Args, LTError? Error) ParseFuncDefArgs(string[] args, string file, string line, int lineNum) {
		args = args.Where(a => a.Length != 0).ToArray();
		if (args.Length == 0) return ([], null);

		var a = args.Select(arg => arg.Split(':'));
		var a2 = a.Where(arg => arg.Length != 2);
		if (a2.Any())
			return ([], new($"Missing argument type and/or name from function: {string.Join(':', a2.First())}", file, line, lineNum));
		return ([..a.Select(a => (a[0], a[1]))], null);
	}
	public static ILifetimeFunc? GetFunc(string funcNs, string funcClass, string funcName, Dictionary<string, LTDefinedFunc> indexedDFuncs, Dictionary<string, LTInternalFunc> indexedIFuncs) {
		if (DebugMode) Console.WriteLine($"GetFunc: passed {funcNs},{funcClass},{funcName}");
		if (indexedDFuncs.TryGetValue(funcNs + "/" + funcClass + "/" + funcName, out var dFunc)) return dFunc;

		if (DebugMode) Console.WriteLine("GetFunc: not found in dfuncs, trying ifuncs");
		if (indexedIFuncs.TryGetValue(funcNs + "/" + funcClass + "/" + funcName, out var iFunc)) return iFunc;

		return null;
	}
	/*
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
	*/

	internal static bool swStop(ref Stopwatch? s, string f, ref LTRuntimeContainer c, bool v = false) {
		c.interpreterState = v ? LTInterpreterState.ExitSuccess : LTInterpreterState.ExitFail;
		s?.Stop();
		c.Handles.ForEach(h => h?.Close());
		if (DebugMode) {
			Console.WriteLine($"swStop: exited {f} in {s?.ElapsedMilliseconds}ms, now listing dfuncs:");
			c.DFuncs
				.Select(a => a.Value)
				.ToList()
				.ForEach(f => Console.WriteLine($"\t{f.Type}\t!{f.Namespace}->{f.Class}::{f.Name} ({f.SourceCode.Length-1} lines)"));

			Console.WriteLine("swStop: now ifuncs:");
			c.IFuncs
				.Select(a => a.Value)
				.ToList()
				.ForEach(f => Console.WriteLine($"\t{f.Type}\t!{f.Namespace}->{f.Class}::{f.Name}"));

			Console.WriteLine("swStop: now vars:");
			c.Vars
				.ForEach(f => Console.WriteLine($"\t{f.Type}\t${f.Namespace}->{f.Class}::{f.Name}\t= {f.Value}"));

			Console.WriteLine("swStop: binded namespaces: " + string.Join(", ", c.bindedNamespaces));
		}
		return v;
	}
}
