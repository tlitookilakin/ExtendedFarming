using ExtendedFarming.Framework;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace ExtendedFarming.Patches
{
	internal class PreserveType
	{
		public const int PRESERVE_FLAG = -1;

		public static void Apply(Harmony harmony, IMonitor monitor, IModHelper helper)
		{
			harmony.Patch(
				typeof(ItemQueryResolver.DefaultResolvers)
				.GetMethod(nameof(ItemQueryResolver.DefaultResolvers.FLAVORED_ITEM)),
				transpiler: new(typeof(PreserveType), nameof(ItemQueryTranspiler))
			);
		}

		private static IEnumerable<CodeInstruction> ItemQueryTranspiler(IEnumerable<CodeInstruction> source, ILGenerator gen)
		{
			var il = new CodeMatcher(source, gen);

			var SkipParse = gen.DefineLabel();
			var error = gen.DeclareLocal(typeof(string));
			var skipError = gen.DefineLabel();
			var skipSwap = gen.DefineLabel();

			il
				// find (splitArgs[0], out PreserveType type)
				.MatchStartForward(
					new(OpCodes.Ldloc_S),
					new(OpCodes.Ldc_I4_0),
					new(OpCodes.Ldelem_Ref),
					new(OpCodes.Ldloca_S)
				)

				// insert !ItemQuery_TryCheckFlag(splitArgs, out PreserveType type) &&
				.InsertAndAdvance(
					new(OpCodes.Ldloc_S, il.Operand),
					new(OpCodes.Ldloca_S, il.InstructionAt(3).operand),
					new(OpCodes.Call, typeof(PreserveType).GetMethod(nameof(ItemQuery_TryCheckFlag))),
					new(OpCodes.Brtrue, SkipParse)
				)
				.MatchEndForward(
					new(OpCodes.Call, typeof(ItemQueryResolver.Helpers).GetMethod(nameof(ItemQueryResolver.Helpers.ErrorResult))),
					new(OpCodes.Ret)
				)
				.Advance(1)
				.AddLabels(SkipParse)

				// find second id set
				.MatchEndForward(
					new CodeMatch(OpCodes.Stloc_2)
				)
				.Advance(1)

				// if (type is PRESERVE_FLAG) { (ingredient, base) = (base, ingredient) }
				.AddLabels(skipSwap)
				.InsertAndAdvance(
					new(OpCodes.Ldloc_0),
					new(OpCodes.Ldc_I4_M1),
					new(OpCodes.Ceq),
					new(OpCodes.Brfalse, skipSwap),
					new(OpCodes.Ldloc_1),
					new(OpCodes.Ldloc_2),
					new(OpCodes.Stloc_1),
					new(OpCodes.Stloc_2)
				)

				// find CreateFlavoredItem call
				.MatchStartForward(
					new(OpCodes.Ldloc_S),
					new(OpCodes.Ldloc_0),
					new(OpCodes.Ldloc_S),
					new(OpCodes.Callvirt, typeof(ObjectDataDefinition).GetMethod(nameof(ObjectDataDefinition.CreateFlavoredItem)))
				)

				// add custom preserve call after
				.Advance(5)
				.InsertAndAdvance(
					new(OpCodes.Ldloc, il.InstructionAt(-1).operand),
					new(OpCodes.Ldloc, il.InstructionAt(-3).operand),
					new(OpCodes.Ldloc_2),
					new(OpCodes.Ldloca, error),
					new(OpCodes.Call, typeof(PreserveType).GetMethod(nameof(GetCustomPreserve))),
					new(OpCodes.Stloc, il.InstructionAt(-1).operand)
				)

				// add custom error
				.MatchStartForward(
					new(OpCodes.Ldloca_S),
					new(OpCodes.Call, typeof(DefaultInterpolatedStringHandler).GetMethod(nameof(DefaultInterpolatedStringHandler.ToStringAndClear)))
				)
				.Advance(2)
				.AddLabels(skipError)
				.InsertAndAdvance(
					new(OpCodes.Ldloc, error),
					new(OpCodes.Brfalse, skipError),
					new(OpCodes.Pop),
					new(OpCodes.Ldloc, error)
				);

			return il.InstructionEnumeration();
		}

		public static bool ItemQuery_TryCheckFlag(string[] args, out SObject.PreserveType preserve)
		{
			bool IsSpecialPreserve = args[0].Trim().Equals("other", StringComparison.OrdinalIgnoreCase);
			preserve = IsSpecialPreserve ? (SObject.PreserveType)PRESERVE_FLAG : default;
			return IsSpecialPreserve;
		}

		public static SObject? GetCustomPreserve(SObject? existing, SObject? ingredient, string preserve, out string? err)
		{
			err = null;
			if (existing is not null)
				return existing;

			return ModUtilities.CreateFlavoredItem(ItemRegistry.GetMetadata(preserve), ingredient, out err) as SObject;
		}
	}
}
