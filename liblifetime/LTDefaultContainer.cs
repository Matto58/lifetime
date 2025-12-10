namespace Mattodev.Lifetime;
public partial class LTInterpreter {
	private class _ {
		internal static (LTVar?, string?) sys_io_print(ref LTRuntimeContainer c, LTVarCollection a) {
			foreach (var v in a.Values) LogMsg(v.Value, ref c);
			return (null, null);
		}
		internal static (LTVar?, string?) sys_io_print_line(ref LTRuntimeContainer c, LTVarCollection a) {
			foreach (var v in a.Values) LogMsg(v.Value, ref c, true);
			return (null, null);
		}
		internal static (LTVar?, string?) sys_io_read_line(ref LTRuntimeContainer c, LTVarCollection a)
			=> (LTVar.SimpleMut("str", "_answer", c.InputHandler(a[0].Value), "sys", "io"), null);

		internal static (LTVar?, string?) sys_tools_is_null(ref LTRuntimeContainer c, LTVarCollection a)
			=> (LTVar.SimpleConst("bool", "_ret", a[0].IsNull ? "true" : "false", "sys", "tools"), null);
		internal static (LTVar?, string?) sys_tools_split_str(ref LTRuntimeContainer c, LTVarCollection a) // why is this unimplemented
			=> (LTVar.SimpleMut("str_array", "_arr", "", "sys", "tools"), null);
		
		internal static (LTVar?, string?) sys_dev_bindns(ref LTRuntimeContainer c, LTVarCollection a) {
			c.bindedNamespaces.Add(a[0].Value);
			return (null, null);
		}
		internal static (LTVar?, string?) sys_dev_unbindns(ref LTRuntimeContainer c, LTVarCollection a) {
			if (c.bindedNamespaces.Contains(a[0].Value))
				c.bindedNamespaces.Remove(a[0].Value);
			return (null, null);
		}

		// todo: avoid copypaste of open_r, open_w and open_rw
		internal static (LTVar?, string?) sys_fl_open_r(ref LTRuntimeContainer c, LTVarCollection a) {
			if (!File.Exists(a[0].Value))
				return (null, "File not found: " + a[0].Value);

			c.Handles.Add(File.Open(a[0].Value, FileMode.Open, FileAccess.Read));
			return (LTVar.SimpleConst("int32", "_handleinx", (c.Handles.Count-1).ToString(), "sys", "fl"), null);
		}
		internal static (LTVar?, string?) sys_fl_open_w(ref LTRuntimeContainer c, LTVarCollection a) {
			if (!File.Exists(a[0].Value))
				return (null, "File not found: " + a[0].Value);

			c.Handles.Add(File.Open(a[0].Value, FileMode.Open, FileAccess.Write));
			return (LTVar.SimpleConst("int32", "_handleinx", (c.Handles.Count-1).ToString(), "sys", "fl"), null);
		}
		internal static (LTVar?, string?) sys_fl_open_rw(ref LTRuntimeContainer c, LTVarCollection a) {
			if (!File.Exists(a[0].Value))
				return (null, "File not found: " + a[0].Value);

			c.Handles.Add(File.Open(a[0].Value, FileMode.Open, FileAccess.ReadWrite));
			return (LTVar.SimpleConst("int32", "_handleinx", (c.Handles.Count-1).ToString(), "sys", "fl"), null);
		}
		internal static (LTVar?, string?) sys_fl_close(ref LTRuntimeContainer c, LTVarCollection a) {
			int handleInx = int.Parse(a[0].Value); // ooo scary!!
			if (handleInx >= c.Handles.Count)
				return (null, $"Invalid handle index: {a[0].Value} (>= {c.Handles.Count} handles in container)");
			if (c.Handles[handleInx] == null)
				return (null, "Handle is closed already");

			c.Handles[handleInx]!.Close();
			c.Handles[handleInx] = null;
			return (null, null);
		}
		internal static (LTVar?, string?) sys_fl_read_as_str(ref LTRuntimeContainer c, LTVarCollection a) {
			int handleInx = int.Parse(a[0].Value);
			if (handleInx >= c.Handles.Count)
				return (null, $"Invalid handle index: {a[0].Value} (>= {c.Handles.Count} handles in container)");

			// misnomer to call a FileStream a handle but imo it kinda makes sense
			FileStream? handle = c.Handles[handleInx];
			if (handle == null)
				return (null, "Handle is closed");
			if (!handle.CanRead)
				return (null, "Can't read from nonreadable handle");

			string content;
			long currentPos = handle.Position;
			handle.Position = 0;
			using (StreamReader s = new(handle, leaveOpen: true))
				content = s.ReadToEnd();
			handle.Position = currentPos;

			return (LTVar.SimpleConst("str", "_filecontent", content, "sys", "fl"), null);
		}
		internal static (LTVar?, string?) sys_fl_enum_dir(ref LTRuntimeContainer c, LTVarCollection a) {
			if (!Directory.Exists(a[0].Value))
				return (null, $"Directory '{a[0].Value}' does not exist");

			return (LTVar.SimpleConst("str_array", "_files", string.Join('\x1',
				Directory.EnumerateFiles(a[0].Value).Select(f => Path.GetFileName(f))
			), "sys", "fl"), null);
		}
		
		internal static (LTVar?, string?) sys_test_ret_true(ref LTRuntimeContainer c, LTVarCollection a)
			=> (LTVar.SimpleConst("bool", "_v", "true", "sys", "test"), null);
		internal static (LTVar?, string?) sys_test_ret_false(ref LTRuntimeContainer c, LTVarCollection a)
			=> (LTVar.SimpleConst("bool", "_v", "false", "sys", "test"), null);
		internal static (LTVar?, string?) sys_test_print_line_arr(ref LTRuntimeContainer c, LTVarCollection a) {
			foreach (var v in a[0].Value.Split('\x1')) LogMsg(v, ref c, true);
			return (null, null);
		}

		internal static (LTVar?, string?) sys_rt_lt_ver(ref LTRuntimeContainer c, LTVarCollection a)
			=> (LTVar.SimpleConst("str", "_ver", LTInfo.Version, "sys", "rt"), null);
		
		internal static (LTVar?, string?) sys_error_get_message(ref LTRuntimeContainer c, LTVarCollection a) {
			if (c.caughtError == null) return (null, "Function called outside of catch statement"); 
			return (LTVar.SimpleConst("str", "_msg", c.caughtError.Message, "sys", "error"), null);
		}
		internal static (LTVar?, string?) sys_error_get_line_num(ref LTRuntimeContainer c, LTVarCollection a) {
			if (c.caughtError == null) return (null, "Function called outside of catch statement"); 
			return (LTVar.SimpleConst("str", "_msg", c.caughtError.Line.Number.ToString(), "sys", "error"), null);
		}
		internal static (LTVar?, string?) sys_error_get_line_content(ref LTRuntimeContainer c, LTVarCollection a) {
			if (c.caughtError == null) return (null, "Function called outside of catch statement"); 
			return (LTVar.SimpleConst("str", "_msg", c.caughtError.Line.Content.ToString(), "sys", "error"), null);
		}
	}
	public static LTRuntimeContainer DefaultContainer() => LTRuntimeContainer.Create([
		// class: !sys->io
		new("print", "sys", "io", "obj", LTVarAccess.Public, [], true, _.sys_io_print),
		new("print_line", "sys", "io", "obj", LTVarAccess.Public, [], true, _.sys_io_print_line),
		new("read_line", "sys", "io", "str", LTVarAccess.Public, [("str", "question")], false, _.sys_io_read_line),
		// class: !sys->tools
		new("is_null", "sys", "tools", "bool", LTVarAccess.Public, [("obj", "object")], false, _.sys_tools_is_null),
		new("split_str", "sys", "tools", "str_array", LTVarAccess.Public, [("str", "string"), ("str", "separator")], false, _.sys_tools_split_str),
		new("create_array", "sys", "tools", "str_array", LTVarAccess.Public, [], true, _.sys_tools_split_str),
		// class: !sys->dev
		new("bindns", "sys", "dev", "obj", LTVarAccess.Public, [("str", "namespace")], false, _.sys_dev_bindns),
		new("unbindns", "sys", "dev", "obj", LTVarAccess.Public, [("str", "namespace")], false, _.sys_dev_unbindns),
		// class: !sys->fl
		new("open_r", "sys", "fl", "int32", LTVarAccess.Public, [("str", "filename")], false, _.sys_fl_open_r),
		new("open_w", "sys", "fl", "int32", LTVarAccess.Public, [("str", "filename")], false, _.sys_fl_open_w),
		new("open_rw", "sys", "fl", "int32", LTVarAccess.Public, [("str", "filename")], false, _.sys_fl_open_rw),
		new("close", "sys", "fl", "obj", LTVarAccess.Public, [("int32", "handle")], false, _.sys_fl_close),
		new("read_as_str", "sys", "fl", "str", LTVarAccess.Public, [("int32", "handle")], false,  _.sys_fl_read_as_str),
		new("enum_dir", "sys", "fl", "str_array", LTVarAccess.Public, [("str", "path")], false, _.sys_fl_enum_dir),
		// class: !sys->test
		new("ret_true", "sys", "test", "str", LTVarAccess.Public, [], false, _.sys_test_ret_true),
		new("ret_false", "sys", "test", "str", LTVarAccess.Public, [], false, _.sys_test_ret_false),
		new("print_line_arr", "sys", "test", "str", LTVarAccess.Public, [("str_array", "arr")], false, _.sys_test_print_line_arr),
		// class: !sys->rt
		new("lt_ver", "sys", "rt", "str", LTVarAccess.Public, [], false, _.sys_rt_lt_ver),
		// class: !sys->error
		new("get_message", "sys", "error", "str", LTVarAccess.Public, [], false, _.sys_error_get_message),
		new("get_line_num", "sys", "error", "int32", LTVarAccess.Public, [], false, _.sys_error_get_line_num),
		new("get_line_content", "sys", "error", "str", LTVarAccess.Public, [], false, _.sys_error_get_line_content),
	], []);
}
