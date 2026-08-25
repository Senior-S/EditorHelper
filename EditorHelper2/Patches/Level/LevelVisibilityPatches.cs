using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper2.Patches.Level;

// Fix for vanilla bug :)
[HarmonyPatch(typeof(LevelVisibility), nameof(LevelVisibility.load))]
public static class LevelVisibilityPatches
{
    [HarmonyTranspiler]
    [UsedImplicitly]
    private static IEnumerable<CodeInstruction> LoadTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> patchedInstructions = instructions.ToList();
        MethodInfo readBoolean = AccessTools.DeclaredMethod(typeof(River), nameof(River.readBoolean), Type.EmptyTypes);
        MethodInfo setNodesVisible = AccessTools.DeclaredPropertySetter(typeof(LevelVisibility), nameof(LevelVisibility.nodesVisible));
        int discardedNodeVisibilityIndex = -1;

        for (int index = 0; index < patchedInstructions.Count - 1; index++)
        {
            if (!patchedInstructions[index].Calls(readBoolean) || patchedInstructions[index + 1].opcode != OpCodes.Pop)
            {
                continue;
            }

            // Just in case :>
            if (discardedNodeVisibilityIndex >= 0)
            {
                throw new InvalidOperationException("Found multiple discarded booleans in LevelVisibility.load; the node visibility patch is ambiguous.");
            }

            discardedNodeVisibilityIndex = index;
        }

        if (discardedNodeVisibilityIndex < 0)
        {
            throw new InvalidOperationException("Could not find the discarded node visibility value in LevelVisibility.load.");
        }

        CodeInstruction discardedValue = patchedInstructions[discardedNodeVisibilityIndex + 1];
        discardedValue.opcode = OpCodes.Call;
        discardedValue.operand = setNodesVisible;
        return patchedInstructions;
    }
}