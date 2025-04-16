using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class RuntimeGizmosPatches
{
	// Fix for out of memory exception:
	// https://discord.com/channels/324229387295653889/1351234563992584315
    [HarmonyPatch(typeof(RuntimeGizmos), "RenderBoxes")]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool RenderBoxes(RuntimeGizmos __instance, List<RuntimeGizmos.BoxData> boxesToRender)
    {
        GL.Begin(1);
		for (int num = boxesToRender.Count - 1; num >= 0; num--)
		{
			RuntimeGizmos.BoxData boxData = boxesToRender[num];
			GL.Color(boxData.color);
			Vector3 extents = boxData.extents;
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(0f - extents.x, 0f - extents.y, 0f - extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(extents.x, 0f - extents.y, 0f - extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(0f - extents.x, 0f - extents.y, 0f - extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(0f - extents.x, 0f - extents.y, extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(0f - extents.x, 0f - extents.y, extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(extents.x, 0f - extents.y, extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(extents.x, 0f - extents.y, 0f - extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(extents.x, 0f - extents.y, extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(0f - extents.x, 0f - extents.y, 0f - extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(0f - extents.x, extents.y, 0f - extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(extents.x, 0f - extents.y, 0f - extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(extents.x, extents.y, 0f - extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(0f - extents.x, 0f - extents.y, extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(0f - extents.x, extents.y, extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(extents.x, 0f - extents.y, extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(extents.x, extents.y, extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(0f - extents.x, extents.y, 0f - extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(extents.x, extents.y, 0f - extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(0f - extents.x, extents.y, 0f - extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(0f - extents.x, extents.y, extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(0f - extents.x, extents.y, extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(extents.x, extents.y, extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(extents.x, extents.y, 0f - extents.z)));
			GL.Vertex(boxData.matrix.MultiplyPoint3x4(boxData.localCenter + new Vector3(extents.x, extents.y, extents.z)));
			
			// TODO: Change this code to just be a transpiler
			boxesToRender.RemoveAtFast(num);
		}
		GL.End();

		return false;
    }
}