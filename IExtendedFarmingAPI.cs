using StardewValley;

namespace ExtendedFarming
{
	public interface IExtendedFarmingAPI
	{
		/// <summary>Creates a flavored preserve item</summary>
		/// <param name="PreservedItemID">The ID of the ouput item</param>
		/// <param name="PreserveFlavor">The object used as flavoring</param>
		/// <returns></returns>
		public Item? CreateFlavoredItem(string PreservedItemID, StardewValley.Object? PreserveFlavor);
	}
}
