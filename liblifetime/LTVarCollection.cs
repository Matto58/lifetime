using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Mattodev.Lifetime;

public class LTVarCollection : IDictionary<string, LTVar>, IList<LTVar> {
	private List<LTVar> vars;
	private Dictionary<string, int> nameMap;

	public LTVarCollection() {
		vars = []; nameMap = [];
	}
	public LTVarCollection(List<LTVar> fromVarList) : this() {
		foreach (LTVar var in fromVarList) Add(var);
	}

	public LTVar this[int inx] { get => vars[inx]; set => vars[inx] = value; }
	public LTVar this[string key] {
		get {
			if (!nameMap.TryGetValue(key, out int inx))
				throw new ArgumentOutOfRangeException(nameof(key));
			return vars[inx];
		}
		set {
			if (!nameMap.TryGetValue(key, out int inx)) {
				nameMap.Add(key, vars.Count);
				vars.Add(value);
			}
			else vars[inx] = value;
		}
	}
	public ICollection<string> Keys => nameMap.Keys;
	public ICollection<LTVar> Values => nameMap.Values.Select(i => vars[i]).ToArray();
	public int Count => vars.Count;
	public bool IsReadOnly => false;

	public void Add(string key, LTVar value) {
		if (nameMap.ContainsKey(key)) throw new ArgumentException($"key {key} already exists", nameof(key));
		this[key] = value;
	}
	public void Add(KeyValuePair<string, LTVar> item) => Add(item.Key, item.Value);

	public void Add(LTVar item) => Add($"${item.Namespace}::{item.Class}->{item.Value}", item);

	public void Clear() {
		nameMap.Clear();
		vars.Clear();
	}

	public bool Contains(KeyValuePair<string, LTVar> item)
		=> nameMap.TryGetValue(item.Key, out int inx) && vars[inx] == item.Value;

	public bool Contains(LTVar item) => vars.Contains(item);

	public bool Contains(string @namespace, string @class, string name)
		=> ContainsKey($"${@namespace}::{@class}->{name}");

	public bool ContainsKey(string key) => nameMap.ContainsKey(key);

	public void CopyTo(KeyValuePair<string, LTVar>[] array, int arrayIndex) {
		for (int i = 0; i < array.Length - arrayIndex; i++) {
			var s = nameMap.Where(n => n.Value == i);
			if (!s.Any())
				throw new Exception($"internal error: index {i} has no bound key");
			array[i + arrayIndex] = new(s.First().Key, vars[i]);
		}
	}

	public void CopyTo(LTVar[] array, int arrayIndex) {
		for (int i = 0; i < array.Length - arrayIndex; i++)
			array[i + arrayIndex] = vars[i];
	}

	// weird!!
	public IEnumerator<KeyValuePair<string, LTVar>> GetEnumerator() =>
		nameMap
			.Select(n => new KeyValuePair<string, LTVar>(n.Key, this[n.Key]))
			.GetEnumerator();

	public int IndexOf(LTVar item) => vars.IndexOf(item);

	public void Insert(int index, LTVar item) {
		ArgumentOutOfRangeException.ThrowIfNegative(index);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(index, vars.Count);
		// inserting at this index is identical to adding at that index
		if (index == vars.Count) {
			Add(item);
			return;
		}
		vars.Insert(index, item);
		// shirt indexes to the right
		foreach ((string name, int _) in nameMap.Where(p => p.Value >= index))
			nameMap[name]++;
		nameMap.Add($"${item.Namespace}::{item.Class}->{item.Value}", index);
	}

	public bool Remove(string key) => nameMap.Remove(key);

	public bool Remove(KeyValuePair<string, LTVar> item) {
		// return false if the item is not even real
		if (!nameMap.TryGetValue(item.Key, out int index)) return false;
		// pop from vars and name map
		vars.RemoveAt(index);
		nameMap.Remove(item.Key);
		// shift indexes beyond the value we popped to the left to avoid index out of range exceptions
		var names = nameMap.Where(p => p.Value > index);
		foreach ((string name, int _) in names)
			nameMap[name]--;
		return true;
	}

	public bool Remove(LTVar item) {
		// if the item isn't real, return false
		if (!Contains(item)) return false;
		// find the index and pop from vars
		int inx = vars.IndexOf(item);
		vars.RemoveAt(inx);
		// find the item's key
		string? key = null;
		for (int i = 0; i < nameMap.Count; i++) {
			var e = nameMap.ElementAt(i);
			if (e.Value == i) { key = e.Key; break; }
		}
		// redundant but whatever, just in case
		if (key == null) return false;
		// aaand finally pop from name map
		nameMap.Remove(key);
		// then shift indexes like in Remove(KeyValuePair<string, LTVar>)
		var names = nameMap.Where(p => p.Value > inx);
		foreach ((string name, int _) in names)
			nameMap[name]--;
		return true;
	}

	// TODO: this is stupid
	public void RemoveAt(int index) {
		if (vars.Count >= index) throw new IndexOutOfRangeException();
		Remove(vars[index]);
	}

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out LTVar value) {
		if (!nameMap.TryGetValue(key, out int index)) {
			value = null;
			return false;
		}
		value = vars[index];
		return true;
	}
	public bool TryGetValue(string @namespace, string @class, string name [MaybeNullWhen(false)] out LTVar value)
		=> TryGetValue($"${@namespace}::{@class}->{name}", out value);

	// implementations of some linq functions
	public IEnumerable<T> Select<T>(Func<LTVar, T> value) => vars.Select(value);
	public IEnumerable<LTVar> Where(Func<LTVar, bool> value) => vars.Where(value);
	public void ForEach(Action<LTVar> value) => vars.ForEach(value);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	IEnumerator<LTVar> IEnumerable<LTVar>.GetEnumerator() => vars.GetEnumerator();
}