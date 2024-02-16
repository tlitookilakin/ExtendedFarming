using ExtendedFarming.Framework;
using HarmonyLib;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using System.Reflection.Emit;

namespace ExtendedFarming.Patches
{
	internal class CustomTruffles
	{
		public static void Apply(Harmony harmony, IMonitor monitor, IModHelper helper)
		{
			harmony.Patch(
				typeof(FarmAnimal).GetMethod("behaviors", ModUtilities.BINDING_ALL),
				transpiler: new(typeof(CustomTruffles), nameof(BehaviorPatch))
			);

			harmony.Patch(
				typeof(FarmAnimal).GetMethod("findTruffle", ModUtilities.BINDING_ALL),
				transpiler: new(typeof(CustomTruffles), nameof(FindTrufflePatch))
			);

			harmony.Patch(
				typeof(FarmAnimal).GetMethod(nameof(FarmAnimal.dayUpdate)),
				postfix: new(typeof(CustomTruffles), nameof(ResetAnimalData))
			);
		}

		private static IEnumerable<CodeInstruction> FindTrufflePatch(IEnumerable<CodeInstruction> codes, ILGenerator gen)
		{
			var il = new CodeMatcher(codes, gen);
			var skip = gen.DefineLabel();

			il
				// find object params just AFTER id (1 stack, false isRecipe, -1 price)
				.MatchStartForward(
					new(OpCodes.Ldc_I4_1),
					new(OpCodes.Ldc_I4_0),
					new(OpCodes.Ldc_I4_M1)
				)
				// add in replacement
				.InsertAndAdvance(
					new(OpCodes.Ldarg_0),
					new(OpCodes.Call, typeof(CustomTruffles).GetMethod(nameof(GetCachedItem)))
				)
				// find produce nuller- this is called when the farm animal is "done" digging for the day
				.MatchStartForward(
					new(OpCodes.Ldarg_0),
					new(OpCodes.Ldfld, typeof(FarmAnimal).GetField(nameof(FarmAnimal.currentProduce))),
					new(OpCodes.Ldnull)
				)
				// if its custom produce, skip the nulling and set our own flag
				.InsertAndAdvance(
					new(OpCodes.Ldarg_0),
					new(OpCodes.Call, typeof(CustomTruffles).GetMethod(nameof(TryFlagAnimalDoneDigging))),
					new(OpCodes.Brtrue, skip)
				)
				.MatchEndForward(
					new CodeMatch(OpCodes.Callvirt, typeof(NetFieldBase<string, NetString>).GetProperty(nameof(NetFieldBase<string, NetString>.Value))!.SetMethod)
				)
				.Advance(1)
				.AddLabels(skip);

			return il.InstructionEnumeration();
		}

		private static IEnumerable<CodeInstruction> BehaviorPatch(IEnumerable<CodeInstruction> codes, ILGenerator gen)
		{
			var il = new CodeMatcher(codes, gen);

			il
				// find pig check
				.MatchEndForward(
					new(OpCodes.Ldstr, "Pig"),
					new(OpCodes.Callvirt, typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) }))
				)
				// check our own data
				.Advance(1)
				.InsertAndAdvance(
					new(OpCodes.Ldarg_0),
					new(OpCodes.Call, typeof(CustomTruffles).GetMethod(nameof(TryGetDugItem)))
				)
				// find null produce check
				.MatchEndBackwards(
					new(OpCodes.Ldfld, typeof(FarmAnimal).GetField(nameof(FarmAnimal.currentProduce))),
					new(OpCodes.Callvirt, typeof(NetFieldBase<string, NetString>).GetProperty(nameof(NetFieldBase<string, NetString>.Value))!.GetMethod)
				)
				// check our "done" flag
				.Advance(1)
				.InsertAndAdvance(
					new(OpCodes.Ldarg_0),
					new(OpCodes.Call, typeof(CustomTruffles).GetMethod(nameof(IsAnimalDoneDigging)))
				);

			return il.InstructionEnumeration();
		}

		public static void ResetAnimalData(FarmAnimal __instance)
		{
			__instance.modData.Remove(DataKeys.TRUFFLE_DONE);
			__instance.modData.Remove(DataKeys.TRUFFLE_CACHE);
		}

		public static bool TryGetDugItem(bool original, FarmAnimal animal)
		{
			if (original)
				return true;

			var item = ModUtilities.GetDugItem(animal);

			if (item is null)
				return false;

			animal.modData[DataKeys.TRUFFLE_CACHE] = item;
			return true;
		}

		public static string GetCachedItem(string original, FarmAnimal animal)
		{
			if (animal.modData.TryGetValue(DataKeys.TRUFFLE_CACHE, out var id))
				return id;

			return original;
		}

		public static bool TryFlagAnimalDoneDigging(FarmAnimal animal)
		{
			if (!animal.modData.ContainsKey(DataKeys.TRUFFLE_CACHE))
				return false;

			animal.modData[DataKeys.TRUFFLE_DONE] = "T";
			return true;
		}

		public static string? IsAnimalDoneDigging(string? original, FarmAnimal animal)
		{
			return original is null || animal.modData.ContainsKey(DataKeys.TRUFFLE_DONE) ? null : original;
		}
	}
}
