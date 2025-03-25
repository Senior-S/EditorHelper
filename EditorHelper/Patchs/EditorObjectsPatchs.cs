using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.AccessControl;
using EditorHelper.Editor;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patchs;

[HarmonyPatch]
public class EditorObjectsPatchs
{
    [HarmonyPatch(typeof(EditorObjects), "addSelection")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void addSelection(Transform select)
    {
        if(EditorHelper.Instance.ObjectsManager == null) return;
        EditorHelper.Instance.ObjectsManager.SelectObject(select);
        EditorHelper.Instance.ObjectsManager.UnhighlightAll();
    }
    
    [HarmonyPatch(typeof(EditorObjects), "Update")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void Update()
    {
        // When going back from SP into the editor, this may get called and fuck up things
        if(EditorHelper.Instance.ObjectsManager == null) return;
        EditorHelper.Instance.ObjectsManager.LateUpdate();
    }

    private static void OnObjectCopied(EditorCopy editorCopy, EditorSelection selection)
    {
        if(EditorHelper.Instance.ObjectsManager == null) return;
        EditorHelper.Instance.ObjectsManager.OnObjectCopied(editorCopy, selection);
    }

    private static void OnObjectPasted(EditorCopy editorCopy, Transform transform)
    {
        if(EditorHelper.Instance.ObjectsManager == null) return;
        EditorHelper.Instance.ObjectsManager.OnObjectPasted(editorCopy, transform);
    }
    
    // I told myself I won't use transpilers for this module, but hell, here we go
    [HarmonyPatch(typeof(EditorObjects), "Update")]
    [HarmonyTranspiler]
    [UsedImplicitly]
    static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> ins = [.. instructions];

        MethodInfo getAssetEditorMethodInfo = AccessTools.Method(typeof(LevelObjects), "getAssetEditor");
        MethodInfo addSelectionMethodInfo = AccessTools.Method(typeof(EditorObjects), "addSelection");
        
        MethodInfo genericLastMethod = typeof(System.Linq.Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .First(m => m.Name == "Last" && m.GetParameters().Length == 1);

        MethodInfo lastMethodInfo = genericLastMethod.MakeGenericMethod(typeof(EditorCopy));

        MethodInfo invokeTargetCopy = new Action<EditorCopy, EditorSelection>(OnObjectCopied).Method;
        MethodInfo invokeTargetPaste = new Action<EditorCopy, Transform>(OnObjectPasted).Method;
        
        // Probably this isn't the best future-proof solution, but the chances of Nelson doing anything for the editor are almost 0
        int index = ins.FindIndex(c => c.opcode == OpCodes.Call && c.Calls(getAssetEditorMethodInfo));

        object copiesOperand = null;
        List<CodeInstruction> selectionInstructions = [];
        bool copyDone = false;
        
        List<CodeInstruction> copyGetInstructions = [];
        for (int i = index; i < ins.Count(); i++)
        {
            CodeInstruction instruction = ins[i];
            if (!copyDone)
            {
                if (copiesOperand == null && instruction.opcode == OpCodes.Ldsfld)
                {
                    copiesOperand = instruction.operand;
                
                    selectionInstructions.Add(ins[i + 1]); // selection call
                    selectionInstructions.Add(ins[i + 2]); // i
                    selectionInstructions.Add(ins[i + 3]); // getItem call
                
                    continue;
                }
                if(instruction.opcode != OpCodes.Newobj) continue;

                CodeInstruction[] toAdd = [
                    new CodeInstruction(OpCodes.Ldsfld, copiesOperand),
                    new CodeInstruction(OpCodes.Call, lastMethodInfo),
                    ..selectionInstructions,
                    new CodeInstruction(OpCodes.Call, invokeTargetCopy)
                ];

                ins.InsertRange(i + 2, toAdd);
                i += 6;
                copyDone = true;
            }
            else
            {
                if (copyGetInstructions.Count == 0 && instruction.opcode == OpCodes.Ldsfld && instruction.operand == copiesOperand && ins[i + 1].opcode == OpCodes.Ldloc_S)
                {
                    copyGetInstructions.Add(instruction);
                    copyGetInstructions.Add(ins[i + 1]);
                    copyGetInstructions.Add(ins[i + 2]);
                    continue;
                }

                if (instruction.opcode == OpCodes.Call && instruction.Calls(addSelectionMethodInfo))
                {
                    
                    CodeInstruction[] toAdd = [
                        ..copyGetInstructions,
                        ins[i - 1],
                        new CodeInstruction(OpCodes.Call, invokeTargetPaste)
                    ];
                
                    ins.InsertRange(i + 1, toAdd);
                
                    return ins;   
                }
            }
        }
        
        return instructions;
    }
    
    [HarmonyPatch(typeof(EditorObjects), "calculateHandleOffsets")]
    [HarmonyPostfix]
    [UsedImplicitly]
    static void calculateHandleOffsets()
    {
        if (EditorHelper.Instance.ObjectsManager == null)
        {
            return;
        }
        
        EditorHelper.Instance.ObjectsManager.UpdateSelectedObject();
    }
}