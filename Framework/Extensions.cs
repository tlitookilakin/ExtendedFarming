

using HarmonyLib;
using System.Reflection.Emit;

namespace ExtendedFarming.Framework
{
	public static class Extensions
	{
		public static CodeMatcher AddLabels(this CodeMatcher m, params Label[] labels)
			=> m.AddLabels((IEnumerable<Label>)labels);
	}
}
