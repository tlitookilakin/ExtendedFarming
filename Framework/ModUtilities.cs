using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;
using StardewValley;
using System.Reflection;

namespace ExtendedFarming.Framework
{
	internal static class ModUtilities
	{
		public static readonly BindingFlags BINDING_ALL = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		public static readonly BindingFlags BINDING_ALL_DECLARED = BINDING_ALL | BindingFlags.DeclaredOnly;

		internal static Item? CreateFlavoredItem(ItemMetadata metadata, SObject? PreserveFlavor, out string? error)
		{
			error = null;

			if (metadata is null)
			{
				error = "Invalid flavored item: item does not exist!";
				return null;
			}

			if (!metadata.Exists())
			{
				error = $"Invalid flavored item: item with id '{metadata.QualifiedItemId}' does not exist!";
				return null;
			}

			var result = metadata.CreateItem();
			if (result is not SObject output || PreserveFlavor is null)
				return result;

			output.preservedParentSheetIndex.Value = PreserveFlavor.QualifiedItemId;

			if (metadata.GetParsedData().RawData is not ObjectData data)
				return output;

			if (data.CustomFields.TryGetValue(DataKeys.PRESERVE_NAME, out var preserveName))
				output.displayNameFormat = preserveName;

			float mul = 0f;
			int add = 0;

			if (data.CustomFields.TryGetValue(DataKeys.PRESERVE_PRICE_MUL, out var smul))
				float.TryParse(smul, out mul);

			if (data.CustomFields.TryGetValue(DataKeys.PRESERVE_PRICE_ADD, out var sadd))
				int.TryParse(sadd, out add);

			if (add is not 0 || mul is not 0f)
				output.Price = (int)(PreserveFlavor.Price * mul) + add;

			return output;
		}

		internal static string? GetDugItem(FarmAnimal animal)
		{
			var data = animal.GetAnimalData();

			if (data is null || data.CustomFields is null)
				return null;

			if (!data.CustomFields.TryGetValue(DataKeys.TRUFFLE_ID, out var truffle_id))
				return null;

			return truffle_id;
		}
	}
}
