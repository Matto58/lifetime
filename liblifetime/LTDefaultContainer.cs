namespace Mattodev.Lifetime;
public partial class LTInterpreter {
	public static readonly LTRuntimeContainer DefaultContainer = new() {
		IFuncs = [
			// class: !sys->io
			new("print", "sys", "io", "obj", LTVarAccess.Public, [], true, (c, a) => {
				a.ToList().ForEach(a => LogMsg(a.Value, ref c));
				return (null, null, c);
			}),
			new("print_line", "sys", "io", "obj", LTVarAccess.Public, [], true, (c, a) => {
				a.ToList().ForEach(a => LogMsg(a.Value, ref c, true));
				return (null, null, c);
			}),
			new("read_line", "sys", "io", "str", LTVarAccess.Public, [("str", "question")], false, (c, a) =>
				(LTVar.SimpleMut("str", "_answer", c.InputHandler(a[0].Value)), null, c)),
			// class: !sys->tools
			new("is_null", "sys", "tools", "bool", LTVarAccess.Public, [("obj", "object")], false, (c, a) => {
				return (LTVar.SimpleConst("bool", "_ret", a[0].IsNull ? "true" : "false"), null, c);
			}),
			// class: !sys->dev
			new("bindns", "sys", "dev", "obj", LTVarAccess.Public, [("str", "namespace")], false, (c, a) => {
				c.bindedNamespaces.Add(a[0].Value);
				return (null, null, c);
			}),
			new("unbindns", "sys", "dev", "obj", LTVarAccess.Public, [("str", "namespace")], false, (c, a) => {
				if (c.bindedNamespaces.Contains(a[0].Value))
					c.bindedNamespaces.Remove(a[0].Value);
				return (null, null, c);
			}),
			// class: !sys->fl
			// todo: avoid copypaste of open_r, open_w and open_rw
			new("open_r", "sys", "fl", "int32", LTVarAccess.Public, [("str", "filename")], false, (c, a) => {
				if (!File.Exists(a[0].Value))
					return (null, "File not found: " + a[0].Value, c);

				c.Handles.Add(File.Open(a[0].Value, FileMode.Open, FileAccess.Read));
				return (LTVar.SimpleConst("int32", "_handleinx", (c.Handles.Count-1).ToString()), null, c);
			}),
			new("open_w", "sys", "fl", "int32", LTVarAccess.Public, [("str", "filename")], false, (c, a) => {
				if (!File.Exists(a[0].Value))
					return (null, "File not found: " + a[0].Value, c);

				c.Handles.Add(File.Open(a[0].Value, FileMode.Open, FileAccess.Write));
				return (LTVar.SimpleConst("int32", "_handleinx", (c.Handles.Count-1).ToString()), null, c);
			}),
			new("open_rw", "sys", "fl", "int32", LTVarAccess.Public, [("str", "filename")], false, (c, a) => {
				if (!File.Exists(a[0].Value))
					return (null, "File not found: " + a[0].Value, c);

				c.Handles.Add(File.Open(a[0].Value, FileMode.Open, FileAccess.ReadWrite));
				return (LTVar.SimpleConst("int32", "_handleinx", (c.Handles.Count-1).ToString()), null, c);
			}),
			new("close", "sys", "fl", "obj", LTVarAccess.Public, [("int32", "handle")], false, (c, a) => {
				int handleInx = int.Parse(a[0].Value); // ooo scary!!
				if (handleInx >= c.Handles.Count)
					return (null, $"Invalid handle index: {a[0].Value} (>= {c.Handles.Count} handles in container)", c);
				if (c.Handles[handleInx] == null)
					return (null, "Handle is closed already", c);

				c.Handles[handleInx]!.Close();
				c.Handles[handleInx] = null;
				return (null, null, c);
			}),
			new("read_as_str", "sys", "fl", "str", LTVarAccess.Public, [("int32", "handle")], false, (c, a) => {
				int handleInx = int.Parse(a[0].Value);
				if (handleInx >= c.Handles.Count)
					return (null, $"Invalid handle index: {a[0].Value} (>= {c.Handles.Count} handles in container)", c);

				// misnomer to call a FileStream a handle but imo it kinda makes sense
				FileStream? handle = c.Handles[handleInx];
				if (handle == null)
					return (null, "Handle is closed", c);
				if (!handle.CanRead)
					return (null, "Can't read from nonreadable handle", c);

				string content;
				long currentPos = handle.Position;
				handle.Position = 0;
				using (StreamReader s = new(handle, leaveOpen: true))
					content = s.ReadToEnd();
				handle.Position = currentPos;

				return (LTVar.SimpleConst("str", "_filecontent", content), null, c);
			}),
		]
	};
}