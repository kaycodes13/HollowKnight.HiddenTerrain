using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HiddenTerrain;

/// <summary>
/// Miscellaneous utility functions and extensions.
/// </summary>
internal static class Utils {
	#region Assembly

	/// <summary>
	/// Static reference to this assembly.
	/// </summary>
	internal static readonly Assembly asm = Assembly.GetExecutingAssembly();

	/// <summary>
	/// Static reference to this assembly's version.
	/// </summary>
	internal static readonly Version version = asm.GetName().Version;

	#endregion
	#region Assets

	/// <summary>
	/// Streams the embedded resource at <paramref name="path"/>,
	/// invokes <paramref name="action"/>, disposes of the stream.
	/// </summary>
	internal static void ReadAsset(string path, Action<Stream> action) {
		if (!path.StartsWith(nameof(HiddenTerrain)))
			path = $"{(nameof(HiddenTerrain))}.Assets.{path}";
		using Stream stream = asm.GetManifestResourceStream(path);
		action.Invoke(stream);
	}

	/// <summary>
	/// Deserializes the embedded json file at <paramref name="path"/>
	/// to data of type <typeparamref name="T"/>.
	/// </summary>
	internal static T ReadJsonAsset<T>(string path) {
		if (!path.StartsWith(nameof(HiddenTerrain)))
			path = $"{(nameof(HiddenTerrain))}.Assets.{path}";
		T value;
		using (StreamReader reader = new(asm.GetManifestResourceStream(path))) {
			value = JsonConvert.DeserializeObject<T>(reader.ReadToEnd())!;
		}
		return value;
	}

	#endregion
	#region Iterators

	/// <summary>
	/// Enumerates the Transforms of all GameObjects in <paramref name="roots"/>
	/// and all their descendants.
	/// </summary>
	internal static IEnumerable<Transform> WalkHierarchy(IEnumerable<GameObject> roots) {
		foreach (Transform t in roots.SelectMany(x => SelfAndWalkHierarchy(x)))
			yield return t;
	}

	/// <summary>
	/// Enumerates the Transforms of a GameObject and all its descendants.
	/// </summary>
	internal static IEnumerable<Transform> SelfAndWalkHierarchy(GameObject go) {
		yield return go.transform;
		foreach (Transform descendant in WalkHierarchy(go))
			yield return descendant;
	}

	/// <summary>
	/// Enumerates the Transforms of all the descendants of a GameObject.
	/// </summary>
	internal static IEnumerable<Transform> WalkHierarchy(GameObject go) {
		foreach (Transform t in go.transform) {
			yield return t;
			foreach (Transform descendant in WalkHierarchy(t.gameObject))
				yield return descendant;
		}
	}

	#endregion
	#region Extensions

	extension(Transform t) {
		internal bool TrueOfSelfOrAncestor(Func<Transform, bool> predicate) {
			if (predicate.Invoke(t))
				return true;

			while (t.parent) {
				if (predicate.Invoke(t.parent))
					return true;
				t = t.parent;
			}

			return false;
		}
	}

	#endregion
}
