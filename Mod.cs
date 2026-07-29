using GlobalEnums;
using HiddenTerrain.Structs;
using Modding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace HiddenTerrain;

public class Mod : Modding.Mod, IGlobalSettings<ModSettings>, ITogglableMod, IMenuMod {

	#region Boilerplate

	/// <summary>
	/// Instance of the mod.
	/// </summary>
	internal static Mod Inst {
		get {
			if (field == null)
				throw new InvalidOperationException($"An instance of {nameof(Mod)} was never constructed");
			return field;
		}
		private set {
			if (field != null)
				throw new InvalidOperationException($"An instance of {nameof(Mod)} has already been constructed");
			field = value;
		}
	}

	public override string GetVersion() => $"{Utils.version}";

	public Mod() : base("Hidden Terrain") {
		Inst = this;
		#if DEBUG
		Log("--- This is a development build ---");
		#endif
	}

	#endregion

	#region Settings/Menu

	internal static ModSettings Settings { get; private set; } = new();

	public void OnLoadGlobal(ModSettings settings) => Settings = settings;

	public ModSettings OnSaveGlobal() => Settings;

	public bool ToggleButtonInsideMenu => true;

	public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry) => [
		(IMenuMod.MenuEntry)toggleButtonEntry!,
		new IMenuMod.MenuEntry {
			Name = "Show Traps & Water",
			Description = "Spikes, acid pools, etc",
			Values = ["Off", "On"],
			Saver = opt => {
				Settings.showTraps = opt == 1;
				if (Settings.modEnabled)
					knownTraps.ForEach(x => x.enabled = Settings.showTraps);
			},
			Loader = () => Settings.showTraps ? 1 : 0
		}
	];

	#endregion

	#region Internals

	static float
		clipNear = 25f,
		clipFar = 7f;

	static readonly int[] SHOW_LAYERS = [.. new PhysLayers[] {
		PhysLayers.ENEMIES,
		PhysLayers.ENEMY_ATTACK,
		PhysLayers.PROJECTILES,
		PhysLayers.CORPSE,
		PhysLayers.ITEM,
		PhysLayers.PLAYER,
		PhysLayers.HERO_DETECTOR,
	}.Cast<int>()];

	static readonly NamedTraps
		namedTraps = Utils.ReadJsonAsset<NamedTraps>("trap_names.json");

	static readonly List<Renderer>
		knownTerrain = [],
		knownTraps = [];

	#endregion

	#region Init/Unload

	public override void Initialize() {
		Log("Applying hooks...");

		USceneManager.activeSceneChanged += SceneChangedHook;

		knownTerrain.ForEach(x => x.enabled = false);
		if (!Settings.showTraps)
			knownTraps.ForEach(x => x.enabled = false);

		Settings.modEnabled = true;

		Log("Initialized!");
	}

	public void Unload() {
		Log("Undoing hooks...");

		Settings.modEnabled = false;

		USceneManager.activeSceneChanged -= SceneChangedHook;

		knownTerrain.ForEach(x => x.enabled = true);
		knownTraps.ForEach(x => x.enabled = true);

		Log("Mod Disabled!");
	}

	#endregion

	#region Hooks

	static void SceneChangedHook(Scene _, Scene scene) {
		knownTerrain.Clear();
		knownTraps.Clear();
		if (Settings.modEnabled && GameManager.instance)
			GameManager.instance.StartCoroutine(HideTerrainCoro(scene));
	}

	static IEnumerator HideTerrainCoro(Scene scene) {
		for (int i = 0; i < 2; i++)
			yield return null;
		if (!scene.isLoaded || GameManager.instance.IsNonGameplayScene())
			yield break;

		float heroZ = HeroController.instance.transform.position.z,
			nearZ = heroZ - clipNear,
			farZ = heroZ + clipFar;

		foreach (Transform t in Utils.WalkHierarchy(scene.GetRootGameObjects())) {
			if (
				!t.gameObject.activeInHierarchy
				|| (t.position.z < nearZ || t.position.z > farZ)
				|| SHOW_LAYERS.Contains(t.gameObject.layer)
				|| t.GetComponent<PlayMakerFSM>()
				|| t.GetComponent<RestBench>()
				|| t.GetComponent<BlurPlane>()
				|| !t.TryGetComponent<Renderer>(out var renderer)
				|| !renderer.enabled
			)
				continue;

			if (
				t.TrueOfSelfOrAncestor(x => x.gameObject.layer == (int)PhysLayers.HERO_ATTACK)
				|| namedTraps.Includes(t)
			) {
				knownTraps.Add(renderer);
				if (Settings.showTraps)
					continue;
			} else
				knownTerrain.Add(renderer);

			renderer.enabled = false;
		}
	}

	#endregion
}
