using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HiddenTerrain.Structs;

[Serializable]
record struct NamedTraps(
	HashSet<string> Objects,
	List<string> ObjectsRegex,
	HashSet<string> Parents,
	List<string> ParentsRegex
) {
	public readonly bool Includes(Transform t) {
		if (t.parent) {
			string parentName = CleanName(t.parent.gameObject);
			if (
				Parents.Contains(parentName)
				|| ParentsRegex.Any(x => Regex.IsMatch(parentName, x))
			)
				return true;
		}

		string name = CleanName(t.gameObject);
		return Objects.Contains(name) || ObjectsRegex.Any(x => Regex.IsMatch(name, x));
	}

	/// <summary>
	/// Removes the '(Clone)' or ' (4)' or '_3' or '47' from the end of an object's name.
	/// </summary>
	static string CleanName(GameObject go)
		=> Regex.Replace(Regex.Replace(go.name,
				@"\s?\([a-zA-Z0-9]+\)", ""),
				@"[\s_]?[0-9]+$", "")
				.Trim();
}
