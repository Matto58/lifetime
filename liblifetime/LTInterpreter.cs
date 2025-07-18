using System.Diagnostics;

namespace Mattodev.Lifetime;

public enum LTInterpreterState { Idle, Executing, ExitSuccess, ExitFail, ParsingIf, ParsingFunc, BreakpointHit }
public partial class LTInterpreter {
	// yes, this variable is readwritable, even by external programs, this is by design
	public static bool DebugMode = false;

	public static bool Exec(string[] source, string fileName, ref LTRuntimeContainer container, bool nested = false, bool skipMinification = false) {
		if (container.interpreterState == LTInterpreterState.BreakpointHit)
			container.interpreterStateStack.RemoveAt(container.interpreterStateStack.Count-1);
		container.interpreterStateStack.Add(LTInterpreterState.Executing);
		if (!nested) container.nestedFuncExitedFine = true;
		var s = (skipMinification ? source : MinifyCode(source)).Select((l, i) => (l, i)).ToArray();
		Stopwatch? sw = null;
		if (DebugMode) sw = Stopwatch.StartNew();
		foreach ((string line, int i) in s) {
			if (i < container.continueFromInx-1) continue;
			string[] ln = line.Split(' ');
			if (DebugMode) {
				Console.WriteLine($"Exec: LINE {i+1}: {line}");
				Console.WriteLine($"Exec: PARSED: {string.Join(',', ln)}");
				Console.WriteLine($"Exec: STATE: {container.interpreterState}");
				Console.WriteLine("Exec: Scanning for breakpoints...");
			}

			// todo: optimize
			var applicableBpsEnum = container.Breakpoints.Where(b => b.fileName == fileName && b.lineNum == i+1);
			if (applicableBpsEnum.Any()) {
				var applicableBps = applicableBpsEnum.ToList();
				applicableBps.ForEach(a => a.onBreak?.Invoke(line, i+1));
				for (int j = 0; j < container.Breakpoints.Count; j++)
					if (container.Breakpoints[j].fileName == fileName && container.Breakpoints[j].lineNum == i+1)
						container.Breakpoints.RemoveAt(j);
				container.continueFromInx = i+1;
				if (DebugMode)
					Console.WriteLine($"Exec: Found {applicableBps.Count} applicable breakpoint(s).");
				return swStop(ref sw, fileName, ref container, true, true);
			}
			else if (DebugMode) Console.WriteLine("Exec: No applicable breakpoints found.");

			switch (container.interpreterState) {
				case LTInterpreterState.ParsingFunc:
					if (line == "end") {
						container.interpreterStateStack.RemoveAt(container.interpreterStateStack.Count - 1);
						var (fnArgs, e) = ParseFuncDefArgs(
							container.tempValuesForInterpreter["fn_args"].Split('\x1'),
							fileName,
							container.tempValuesForInterpreter["fn_defln"],
							int.Parse(container.tempValuesForInterpreter["fn_deflnnum"]));

						if (e != null && !ThrowError(e, ref container, fileName, ref sw))
							return false;

						LTDefinedFunc f = new(
							container.tempValuesForInterpreter["fn_name"],
							container.Namespace,
							container.Class,
							container.tempValuesForInterpreter["fn_type"],
							LTVarAccess.Public,
							fnArgs,
							false,
							container.tempValuesForInterpreter["fn_src"].Split('\x1')[..^1],
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
				case LTInterpreterState.ParsingIf:
					if (line == "end") {
						container.interpreterStateStack.RemoveAt(container.interpreterStateStack.Count - 1);
						container.tempValuesForInterpreter.Remove("if_exprres");
						continue;
					}
					else if (container.tempValuesForInterpreter["if_exprres"] == "false") continue;
					break;
				default:
					if (line == "end") {
						if (container.tryingForError) {
							container.tryingForError = false;
							continue;
						}
						else if (!container.tempValuesForInterpreter.Remove("class")) {
							if (!ThrowError(new($"Unexpected end keyword", fileName, line, i+1), ref container, fileName, ref sw))
								return false;
						} else continue;
					}
					break;
			}

			// ofc when an error is caught, the rest of the try statement shouldn't be executed
			if (container.tryingForError && container.caughtError != null) {
				if (DebugMode)
					Console.WriteLine("Exec: SKIPPING this line because an error has been caught within this try statement");
				continue;
			}

			switch (ln[0][0]) {
				case '!':
					var e = FindAndExecFunc(ln[0][1..], ln.Length > 1 ? ln[1..] : [], fileName, line, i+1, ref container);
					if (e != null) {
						// LogError shouldnt run if a function that ran within this one exited with an error and already logged it
						if (container.nestedFuncExitedFine)
							LogError(e, ref container);
						container.interpreterStateStack.Add(LTInterpreterState.ExitFail);
						container.nestedFuncExitedFine = false;
						if (!container.IgnoreErrs) return swStop(ref sw, fileName, ref container);
					}
					continue;
			}
			switch (ln[0]) {
				// variable definition: let <type> <variable name> <value>
				case "let":
					if (ln.Length < 3) {
						if (!ThrowError(new("Missing variable type, name and/or value", fileName, line, i+1), ref container, fileName, ref sw))
							return false;
					}
					// dumb hack numero dos
					string c = container.tempValuesForInterpreter.GetValueOrDefault("class", "");
					string ns = container._namespace;
					if (container.Vars.Contains(ns, c, ln[2])) {
						if (!ThrowError(new($"Invalid variable redefinition (try doing ${ln[2]} <- {string.Join(' ', ln[3..])})", fileName, line, i+1), ref container, fileName, ref sw))
							return false;
					}
					var (val, e) = ParseFuncArgs(ln[3..], fileName, line, i+1, ref container);
					if (e != null && !ThrowError(e, ref container, fileName, ref sw))
						return false;
					if (val.Count != 1) {
						if (!ThrowError(new($"Invalid value: {string.Join(' ', ln[3..])}", fileName, line, i+1), ref container, fileName, ref sw))
							return false;
					}
					val[0].Constant = false;
					val[0].Name = ln[2];
					val[0].Type = ln[1];
					val[0].Namespace = container.Namespace;
					val[0].Class = container.Class;
					container.Vars.Add(val[0]);
					if (val[0].OnValueSet != null) val[0].OnValueSet();
					break;
				case "fn":
					if (ln.Length < 3) {
						if (!ThrowError(new("Missing function type and/or name", fileName, line, i+1), ref container, fileName, ref sw))
							return false;
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
					container.interpreterStateStack.Add(LTInterpreterState.ParsingFunc);
					foreach (var (k, v) in funcProps.Select(kvp => (kvp.Key, kvp.Value)))
						container.tempValuesForInterpreter.Add(k, v);
					break;
				case "if":
					string expression = Between(line, "if ", " then")?.Trim() ?? "";
					if (expression.Length == 0) {
						if (!ThrowError(new("Missing if expression", fileName, line, i+1), ref container, fileName, ref sw))
							return false;
					}
					LTRuntimeContainer containerClone = (LTRuntimeContainer)container.Clone();
					if (!Exec([expression], fileName + " (if expression)", ref containerClone))
						if (!container.IgnoreErrs) return swStop(ref sw, fileName, ref container);

					container.tempValuesForInterpreter["if_exprres"] = containerClone.LastReturnedValue?.Value ?? "false";
					container.interpreterStateStack.Add(LTInterpreterState.ParsingIf);
					break;
				case "ret":
					if (ln.Length < 2)
						return swStop(ref sw, fileName, ref container, true);

					(var vals, e) = ParseFuncArgs(ln[1..], fileName, line, i+1, ref container);
					if (e != null && !container.IgnoreErrs) return swStop(ref sw, fileName, ref container);
					if (vals.Count != 1) {
						if (!ThrowError(new("Too many returned values, only one can be returned", fileName, line, i+1), ref container, fileName, ref sw))
							return false;
					}
					container.LastReturnedValue = vals[0];
					return swStop(ref sw, fileName, ref container, true);
				case "namespace":
					if (ln.Length != 2) {
						if (!ThrowError(new(ln.Length < 2 ? "Namespace not specified" : $"Too many arguments; passed {ln.Length}, expecting 1", fileName, line, i+1), ref container, fileName, ref sw))
							return false;
					}
					if (container._namespace != "") {
						if (!ThrowError(new("Namespace already specified", fileName, line, i+1), ref container, fileName, ref sw))
							return false;
					}
					container._namespace = ln[1];
					if (DebugMode) Console.WriteLine($"Exec: container namespace is now {container._namespace}");
					break;
				case "class":
					if (ln.Length != 3) {
						if (!ThrowError(
							new(ln.Length < 3 ? "Class not specified" : $"Too many arguments; passed {ln.Length}, expecting 1", fileName, line, i+1),
							ref container, fileName, ref sw)) return false;
					}
					container.tempValuesForInterpreter.Add("class", ln[1]);
					break;
				case "throw":
					LTError throwed;
					if (ln.Length < 2)
						throwed = new("(no message specified)", fileName, line, i+1);
					else {
						(var varList, var err) = ParseFuncArgs(ln[1..], fileName, line, i+1, ref container);
						if (err != null) throwed = err;
						else if (varList == null) throw new Exception("varList should not be null here (please file an issue on the github)");
						else throwed = new(string.Join("", varList.Select(v => v.Value)), fileName, line, i+1);
					}
					if (!ThrowError(throwed, ref container, fileName, ref sw)) return false;
					break;
				case "try":
					if (container.tryingForError && !ThrowError(new("Cannot use try in a try statement", fileName, line, i+1), ref container, fileName, ref sw, true))
						return false;
					container.tryingForError = true;
					break;
				default:
					if (!ThrowError(new($"Invalid keyword: {ln[0]}", fileName, line, i+1), ref container, fileName, ref sw))
						return false;
					break;
			}
		}
		if (!nested) container.interpreterStateStack.Add(LTInterpreterState.ExitSuccess);
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
	public static bool ThrowError(LTError e, ref LTRuntimeContainer container, string fileName, ref Stopwatch? sw, bool forceThrow = false) {
		if (container.tryingForError && !forceThrow) {
			container.caughtError = e;
			if (DebugMode)
				Console.WriteLine($"Exec: Caught error! {e.File}:{e.Line.Number} | {e.Line.Content} # {e.Message}");
			return true;
		}
		LogError(e, ref container);
		if (!container.IgnoreErrs || forceThrow) return swStop(ref sw, fileName, ref container);
		return true;
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
		if (func.AcceptsArgs < args2.Count && !func.IgnoreArgCount)
			return new($"Too many args passed; passed {args2.Count}, expecting {func.AcceptsArgs}", file, line, lineNum);
		// fill nongiven args with null: breaking change!
		for (int i = args2.Count; i < func.AcceptsArgs; i++)
			args2.Add(LTVar.SimpleConst(func.AcceptedArgs[i].type, "_arg" + i, null, container.Namespace, container.Class));

		var (v, e2) = func.Call(ref container, new(args2));
		container.LastReturnedValue = v;
		return e2 != null ? new(e2, file, line, lineNum) : null;
	}
	public static (List<LTVar> Vars, LTError? Error) ParseFuncArgs(string[] args, string file, string line, int lineNum, ref LTRuntimeContainer container) {
		List<LTVar> parsed = [];
		bool doingString = false;
		bool terminatedStrProperly = false;
		foreach ((string arg, int i) in args.Select((a, b) => (a, b))) {
			if (DebugMode) Console.WriteLine("ParseFuncArgs: parsing " + arg);
			if (arg[0] == '"' && !doingString) {
				doingString = true;
				terminatedStrProperly = false;
				string s = "";
				if (arg[^1] == '"') {
					s = arg[1..^1];
					doingString = false;
					terminatedStrProperly = true;
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
					if (container.Vars.TryGetValue(ns, c, arg[1..], out var v)) {
						parsed.Add(v);
						v.OnValueGet?.Invoke();
						terminatedStrProperly = true;
						continue;
					}
					return ([], new($"Referenced variable not found: {arg}", file, line, lineNum));
				}
				string[] s2 = s1[0].Split("->");
				if (s2.Length > 2) return ([], new($"Invalid variable identifier: {arg}", file, line, lineNum));

				if (container.Vars.TryGetValue(ns, c, arg[1..], out var v2)) {
					parsed.Add(v2);
					terminatedStrProperly = true;
				}
				else return ([], new($"Referenced variable not found: {arg}", file, line, lineNum));
				v2.OnValueGet?.Invoke();
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
					terminatedStrProperly = true;
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
			
		if (parsed.Count != 0 && !terminatedStrProperly && parsed[^1].Type == "str") {
			return (parsed, new("Unterminated string", file, line, lineNum));
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

	// adapted from https://stackoverflow.com/a/17252672
	// yknow what they say, even the most senior of programmers still actively use stackoverflow
	public static string? Between(string s, string sideA, string sideB) {
		int inx = s.IndexOf(sideA);
		int b = s.LastIndexOf(sideB);
		if (inx == -1 || b == -1) return null;
		int a = inx + sideA.Length;
		return s[a..b];
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

	internal static bool swStop(ref Stopwatch? s, string f, ref LTRuntimeContainer c, bool v = false, bool bp = false) {
		c.interpreterStateStack.Add(
			bp
			? LTInterpreterState.BreakpointHit
			: (v ? LTInterpreterState.ExitSuccess : LTInterpreterState.ExitFail));
		s?.Stop();
		if (bp) {
			if (DebugMode) Console.WriteLine($"swStop: hit breakpoint in {s?.ElapsedMilliseconds}ms in file {f}");
			return v;
		}
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
			
			Console.WriteLine("swStop: now interpreter state stack:");
			c.interpreterStateStack
				.ForEach(s => Console.WriteLine($"\t{s}"));

			Console.WriteLine("swStop: binded namespaces: " + string.Join(", ", c.bindedNamespaces));
		}
		return v;
	}

	public class Breakpoint(string fileName, int lineNum, Action<string, int>? onBreak) {
		// string line, int lineNum
		public Action<string, int>? onBreak = onBreak;
		public int lineNum = lineNum;
		public string fileName = fileName;
	}
}
