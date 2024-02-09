using StardewModdingAPI;
using StardewValley;

namespace ExtendedFarming.Framework
{
	public class API : IExtendedFarmingAPI
	{
		#nullable disable
		public static API api;
		#nullable enable

		private IMonitor monitor;
		private IModHelper helper;

		internal API(IMonitor monitor, IModHelper helper)
		{
			this.monitor = monitor;
			this.helper = helper;
		}

		public Item? CreateFlavoredItem(string PreservedItemID, SObject? PreserveFlavor)
		{
			PreservedItemID = int.TryParse(PreservedItemID, out _) ? "(O)" + PreservedItemID : PreservedItemID;
			var objectData = ItemRegistry.GetObjectTypeDefinition();
			string? err = null;

			var result = PreservedItemID switch
			{
				"(O)447" => objectData.CreateFlavoredAgedRoe(PreserveFlavor),
				"(O)340" => objectData.CreateFlavoredHoney(PreserveFlavor),
				"(O)344" => objectData.CreateFlavoredJelly(PreserveFlavor),
				"(O)350" => objectData.CreateFlavoredJuice(PreserveFlavor),
				"(O)342" => objectData.CreateFlavoredPickle(PreserveFlavor),
				"(O)812" => objectData.CreateFlavoredRoe(PreserveFlavor),
				"(O)348" => objectData.CreateFlavoredWine(PreserveFlavor),
				_ => ModUtilities.CreateFlavoredItem(ItemRegistry.GetMetadata(PreservedItemID), PreserveFlavor, out err)
			};

			if (err is not null)
				monitor.Log(err, LogLevel.Error);

			return result;
		}
	}
}
