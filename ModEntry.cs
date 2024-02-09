using ExtendedFarming.Framework;
using ExtendedFarming.Patches;
using HarmonyLib;
using StardewModdingAPI;

namespace ExtendedFarming
{
	public class ModEntry : Mod
	{
		public override void Entry(IModHelper helper)
		{
			API.api = new(Monitor, Helper);

			helper.Events.GameLoop.GameLaunched += GameLaunched;
		}

		public override object? GetApi()
		{
			return API.api;
		}

		private void GameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
		{
			Patch(new(ModManifest.UniqueID));
		}

		private void Patch(Harmony harmony)
		{
			PreserveType.Apply(harmony, Monitor, Helper);
		}
	}
}
