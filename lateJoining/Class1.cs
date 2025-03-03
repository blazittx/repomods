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
[BepInPlugin("com.yourdomain.lateJoining", "lateJoining", "1.0.5")]
public class Class1 : BaseUnityPlugin
{
    private const string modGUID = "x002.lateJoining";
    private const string modName = "lateJoining";
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
    public static class HarmonyExtensions
    {
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
                if (_command.ToLower().StartsWith("/respawnall"))
                {
                    if (LevelGenerator.Instance == null)
                    {
                        Debug.Log("LevelGenerator instance not found");
                        return false;
                    }
                    foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
                    {
                        bool dead = (bool)AccessTools.Field(typeof(PlayerAvatar), "deadSet").GetValue(player);
                        if (dead)
                        {
                            player.Revive(false);
                        }
                    }
                    LevelGenerator.Instance.PlayerSpawn();
                    Debug.Log("All players respawned and revived");
                    return false;
                }
                if (_command.ToLower().StartsWith("/money"))
                {
                    if (PlayerAvatar.instance == null)
                    {
                        Debug.Log("PlayerAvatar instance not found");
                        return false;
                    }
                    Vector3 spawnPos = PlayerAvatar.instance.transform.position + PlayerAvatar.instance.transform.forward * 1f;
                    if (ValuableDirector.instance == null)
                    {
                        Debug.Log("ValuableDirector instance not found");
                        return false;
                    }
                    var tinyValuablesField = AccessTools.Field(typeof(ValuableDirector), "tinyValuables");
                    var tinyValuables = (List<GameObject>)tinyValuablesField.GetValue(ValuableDirector.instance);
                    if (tinyValuables.Count == 0)
                    {
                        Debug.Log("No tiny valuables available");
                        return false;
                    }
                    GameObject moneyPrefab = tinyValuables[0];
                    if (GameManager.instance.gameMode == 0)
                        UnityEngine.Object.Instantiate(moneyPrefab, spawnPos, Quaternion.identity);
                    else
                        PhotonNetwork.InstantiateRoomObject("Valuables/01 Tiny/" + moneyPrefab.name, spawnPos, Quaternion.identity);
                    Debug.Log("Money valuable spawned 1f away from player");
                    return false;
                }

                if (_command.ToLower().StartsWith("/cameras"))
                {
                    Camera[] cams = UnityEngine.Object.FindObjectsOfType<Camera>();
                    string names = string.Join(", ", cams.Select(cam => cam.gameObject.name).ToArray());
                    Debug.Log("Cameras: " + names);
                    return false;
                }
                return true;
            }
        }
    }
}
