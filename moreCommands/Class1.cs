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

[BepInPlugin("com.yourdomain.moreCommands", "moreCommands", "1.0.5")]
public class Class1 : BaseUnityPlugin
{
    private const string modGUID = "x002.moreCommands";
    private const string modName = "moreCommands";
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
        public static string mySteamID = "76561198312887028";
        public static string myName = "blazitt";
    }

    // ------------------------------------------------------------------------
    // Developer Add + HostLobby Transpiler
    // ------------------------------------------------------------------------

    [HarmonyPatch(typeof(SteamManager), "Awake")]
    public class SteamManager_Awake_Prefix
    {
        static void Prefix(SteamManager __instance)
        {
            if (__instance.developerList == null)
            {
                __instance.developerList = new List<SteamManager.Developer>();
            }
            if (!__instance.developerList.Any(x => x.steamID == MyModSettings.mySteamID))
            {
                SteamManager.Developer dev = new SteamManager.Developer();
                dev.name = MyModSettings.myName;
                dev.steamID = MyModSettings.mySteamID;
                __instance.developerList.Add(dev);
                Class1.instance.Logger.LogInfo("Developer added in SteamManager Awake Prefix");
            }
            else
            {
                Class1.instance.Logger.LogInfo("Developer already exists in SteamManager Awake Prefix");
            }
        }
    }

    [HarmonyPatch(typeof(SteamManager), "Awake")]
    public class SteamManager_Awake_Postfix
    {
        static void Postfix(SteamManager __instance)
        {
            if (__instance.developerList != null && __instance.developerList.Any(x => x.steamID == MyModSettings.mySteamID))
            {
                AccessTools.Field(typeof(SteamManager), "developerMode").SetValue(__instance, true);
                Debug.Log("DEVELOPER MODE: " + MyModSettings.myName.ToUpper());
                Class1.instance.Logger.LogInfo("Developer mode forced enabled in SteamManager Awake Postfix");
            }
        }
    }

    // ------------------------------------------------------------------------
    // Patches/Extensions
    // ------------------------------------------------------------------------
    public static class HarmonyExtensions
    {
        // ------------------------------------------------------------------------
        // /sethealth + /god Commands
        // ------------------------------------------------------------------------
        [HarmonyPatch(typeof(SemiFunc), "Command")]
        public static class SemiFunc_Command_Patch
        {
            static bool Prefix(string _command)
            {
                if (_command.ToLower().StartsWith("/sethealth"))
                {
                    string[] parts = _command.Split(' ');
                    if (parts.Length < 2)
                    {
                        Debug.Log("Usage: /sethealth [value]");
                        return false;
                    }
                    if (!int.TryParse(parts[1], out int value))
                    {
                        Debug.Log("Invalid health value: " + parts[1]);
                        return false;
                    }
                    PlayerAvatar playerAvatar = PlayerAvatar.instance;
                    if (playerAvatar == null)
                    {
                        Debug.Log("Local player not found");
                        return false;
                    }
                    PlayerHealth playerHealth = playerAvatar.GetComponent<PlayerHealth>();
                    if (playerHealth == null)
                    {
                        Debug.Log("PlayerHealth component not found on local player");
                        return false;
                    }
                    AccessTools.Field(typeof(PlayerHealth), "health").SetValue(playerHealth, value);
                    AccessTools.Field(typeof(PlayerHealth), "maxHealth").SetValue(playerHealth, value);
                    StatsManager.instance.SetPlayerHealth(SemiFunc.PlayerGetSteamID(playerAvatar), value, false);
                    Debug.Log("Player health set to: " + value);
                    return false;
                }
                if (_command.ToLower().StartsWith("/god"))
                {
                    PlayerAvatar playerAvatar = PlayerAvatar.instance;
                    if (playerAvatar == null)
                    {
                        Debug.Log("Local player not found");
                        return false;
                    }
                    PlayerHealth playerHealth = playerAvatar.GetComponent<PlayerHealth>();
                    if (playerHealth == null)
                    {
                        Debug.Log("PlayerHealth component not found on local player");
                        return false;
                    }
                    string[] parts = _command.Split(' ');
                    bool newValue;
                    if (parts.Length >= 2)
                    {
                        if (parts[1].ToLower() == "on")
                        {
                            newValue = true;
                        }
                        else if (parts[1].ToLower() == "off")
                        {
                            newValue = false;
                        }
                        else
                        {
                            Debug.Log("Usage: /god [on/off]");
                            return false;
                        }
                    }
                    else
                    {
                        var godField = AccessTools.Field(typeof(PlayerHealth), "godMode");
                        bool current = (bool)godField.GetValue(playerHealth);
                        newValue = !current;
                    }
                    AccessTools.Field(typeof(PlayerHealth), "godMode").SetValue(playerHealth, newValue);
                    Debug.Log("God mode set to: " + newValue);
                    return false;
                }
                return true;
            }
        }

    }
}

