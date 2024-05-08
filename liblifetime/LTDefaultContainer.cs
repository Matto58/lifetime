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
			new("open_r", "sys", "fl", "handle", LTVarAccess.Public, [("str", "filename")], false, (c, a) => {
				if (!File.Exists(a[0].Value))
					return (null, "", c);
				c.Handles.Add(File.OpenRead(a[0].Value));
				return (LTVar.SimpleConst("int32", "_handleinx", (c.Handles.Count-1).ToString()), null, c);
			}),
			new("open_w", "sys", "fl", "handle", LTVarAccess.Public, [("str", "filename")], false, (c, a) => {
				return (null, null, c);
			}),
			new("open_rw", "sys", "fl", "handle", LTVarAccess.Public, [("str", "filename")], false, (c, a) => {
				return (null, null, c);
			}),
		]
	};
}
