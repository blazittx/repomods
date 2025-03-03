using BepInEx;
using HarmonyLib;
using System;
using UnityEngine;
using System.Reflection;
using TMPro;
using UnityEngine.UI;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using System.Runtime.CompilerServices;

[BepInPlugin("com.yourdomain.maxPlayers", "maxPlayers", "1.0.5")]
public class Class1 : BaseUnityPlugin
{
    private const string modGUID = "x002.maxPlayers";
    private const string modName = "maxPlayers";
    private const string modVersion = "1.0.5";
    public static Class1 instance;
    private readonly Harmony harmony = new Harmony(modGUID);

    private void Awake()
    {
        instance = this;
        harmony.PatchAll();
        Logger.LogInfo($"{modName} is loaded!");
    }

    public static class MyModSettings
    {
        public static int newLobbySize = 12;
    }

    // ------------------------------------------------------------------------
    // Developer Add + HostLobby Transpiler
    // ------------------------------------------------------------------------

    [HarmonyPatch]
    public class SteamManager_HostLobby_Transpiler
    {
        static MethodBase TargetMethod()
        {
            var method = typeof(SteamManager).GetMethod("HostLobby", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return method.GetStateMachineTarget();
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Class1.instance.Logger.LogInfo("SteamManager HostLobby Transpiler executed");
            foreach (var instruction in instructions)
            {
                if ((instruction.opcode == OpCodes.Ldc_I4_6) || (instruction.operand is int val && val == 6))
                {
                    Class1.instance.Logger.LogInfo("Lobby size constant replaced");
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MyModSettings), nameof(MyModSettings.newLobbySize)));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }


    [HarmonyPatch(typeof(NetworkConnect), "TryJoiningRoom")]
    public class NetworkConnect_TryJoiningRoom_Transpiler
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> newInstructions = new List<CodeInstruction>(instructions);
            bool replaced = false;

            for (int i = 0; i < newInstructions.Count; i++)
            {
                if ((newInstructions[i].opcode == OpCodes.Ldc_I4_6) || (newInstructions[i].operand is int val && val == 6))
                {
                    Class1.instance.Logger.LogInfo($"[Harmony] Replacing default max player count (6) with new value: {Class1.MyModSettings.newLobbySize}");
                    newInstructions[i] = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Class1.MyModSettings), nameof(Class1.MyModSettings.newLobbySize)));
                    replaced = true;
                }
            }

            if (!replaced)
            {
                Class1.instance.Logger.LogWarning("[Harmony] WARNING: Could not find max player count (6) in TryJoiningRoom! Patch might have failed.");
            }
            else
            {
                Class1.instance.Logger.LogInfo("[Harmony] Successfully patched max player count in TryJoiningRoom.");
            }

            return newInstructions;
        }
    }

}

public static class HarmonyExtensions
{
    public static MethodInfo GetStateMachineTarget(this MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<AsyncStateMachineAttribute>();
        if (attribute != null)
        {
            return attribute.StateMachineType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        return method;
    }
}
