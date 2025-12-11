using System.Numerics;
using System.Text;

namespace Mattodev.Lifetime;

public enum LTVarType {
	obj, str, boolean,
	i8, i16, i32, i64,
	u8, u16, u32, u64,
}

public enum LTVarAccess { Public, Private }
public interface ILifetimeVar : ICloneable {
	public string Name { get; set; }
	public string Namespace { get; set; }
	public string Class { get; set; }
	public LTVarType Type { get; set; }
	public bool Constant { get; set; }
	public bool IsNull { get; }
	public LTVarAccess Access { get; init; }
	public Action? OnValueGet { get; set; }
	public Action? OnValueSet { get; set; }
	public long? AsInt();
	public ulong? AsUInt();
	public string? AsStr();
	public byte[]? AsRaw();
	public bool? AsBool();
	public void AssignInt(long i);
	public void AssignUInt(ulong u);
	public void AssignStr(string s);
	public void AssignRaw(byte[] v);
	public void AssignBool(bool b);
	public void AssignNull();
}

public class LTVar(string name, string varNamespace, string varClass, LTVarType varType, bool isConstant, LTVarAccess access) : ILifetimeVar {
	public string Name { get; set; } = name;
	public string Namespace { get; set; } = varNamespace;
	public string Class { get; set; } = varClass;
	public LTVarType Type { get; set; } = varType;
	public bool Constant { get; set; } = isConstant;
	public bool IsNull { get => valueBin.Count == 0; }
	public LTVarAccess Access { get; init; } = access;
	public Action? OnValueGet { get; set; } = null;
	public Action? OnValueSet { get; set; } = null;
	internal List<byte> valueBin = [];

	public static LTVar SimpleMut(LTVarType type, string name, string value, string _namespace, string _class) {
		LTVar v = new(name, _namespace, _class, type, false, LTVarAccess.Public);
		v.AssignStr(value);
		return v;
	}
	public static LTVar SimpleMut(LTVarType type, string name, long value, string _namespace, string _class) {
		LTVar v = new(name, _namespace, _class, type, false, LTVarAccess.Public);
		v.AssignInt(value);
		return v;
	}
	public static LTVar SimpleMut(LTVarType type, string name, ulong value, string _namespace, string _class) {
		LTVar v = new(name, _namespace, _class, type, false, LTVarAccess.Public);
		v.AssignUInt(value);
		return v;
	}
	public static LTVar SimpleMut(LTVarType type, string name, byte[] value, string _namespace, string _class) {
		LTVar v = new(name, _namespace, _class, type, false, LTVarAccess.Public);
		v.AssignRaw(value);
		return v;
	}
	public static LTVar SimpleMut(LTVarType type, string name, bool value, string _namespace, string _class) {
		LTVar v = new(name, _namespace, _class, type, false, LTVarAccess.Public);
		v.AssignBool(value);
		return v;
	}

	public static LTVar SimpleConst(LTVarType type, string name, string value, string _namespace, string _class) {
		LTVar v = SimpleMut(type, name, value, _namespace, _class);
		v.Constant = true;
		return v;
	}
	public static LTVar SimpleConst(LTVarType type, string name, long value, string _namespace, string _class) {
		LTVar v = SimpleMut(type, name, value, _namespace, _class);
		v.Constant = true;
		return v;
	}
	public static LTVar SimpleConst(LTVarType type, string name, byte[] value, string _namespace, string _class) {
		LTVar v = SimpleMut(type, name, value, _namespace, _class);
		v.Constant = true;
		return v;
	}
	public static LTVar SimpleConst(LTVarType type, string name, bool value, string _namespace, string _class) {
		LTVar v = SimpleMut(type, name, value, _namespace, _class);
		v.Constant = true;
		return v;
	}

	public long? AsInt() => IsNull ? null : BitConverter.ToInt64(valueBin.ToArray());
	public void AssignInt(long i) => valueBin = BitConverter.GetBytes(i).ToList();
	public ulong? AsUInt() => IsNull ? null : BitConverter.ToUInt64(valueBin.ToArray());
	public void AssignUInt(ulong u) => valueBin = BitConverter.GetBytes(u).ToList();
	public string? AsStr() => IsNull ? null : Encoding.UTF8.GetString(valueBin.ToArray());
	public void AssignStr(string s) => valueBin = Encoding.UTF8.GetBytes(s).ToList();
	public byte[]? AsRaw() => IsNull ? null : valueBin.ToArray();
	public void AssignRaw(byte[] v) => valueBin = v.ToList();
	public bool? AsBool() => IsNull ? null : valueBin[0] != 0;
	public void AssignBool(bool b) => valueBin = [b ? (byte)1 : (byte)0];

	public void AssignNull() => valueBin = [];

	public object Clone() => MemberwiseClone();
}
