namespace Mattodev.Lifetime;

public enum LTInterpreterState { Idle, Executing, ParsingIf, ParsingFunc }
public class LTInterpreter {
	private static LTInterpreterState state = LTInterpreterState.Idle;
	public static LTInterpreterState State => state;
	public static void Exec(string[] source, ref LTRuntimeContainer container, bool nested = false) {
		state = LTInterpreterState.Executing;
		var s = MinifyCode(source).Select((l, i) => (l, i));
		foreach ((string line, int i) in s) {
			string[] ln = line.Split(' ');
			Console.WriteLine($"LINE {i+1}: {line}");
			Console.WriteLine($"PARSED: {string.Join(',', ln)}");

			// todo: gonna do this later
			//switch (ln[0]) {
			//
			//}
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
}
