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
[BepInPlugin("com.yourdomain.wideScreenSupport", "wideScreenSupport", "1.0.5")]
public class Class1 : BaseUnityPlugin
{
    private const string modGUID = "x002.wideScreenSupport";
    private const string modName = "wideScreenSupport";
    private const string modVersion = "1.0.5";
    public static Class1 instance;
    private readonly Harmony harmony = new Harmony(modGUID);
    private void Awake()
    {
        instance = this;
        harmony.PatchAll();
        Logger.LogInfo($"{modName} is loaded!");
    }
}
[HarmonyPatch(typeof(GraphicsManager), "Update")]
public class AwakePatch
{

    public static GameObject val;
    public static GameObject val2;

    public static void Prefix()
    {
        float timer = (float)AccessTools.Field(typeof(GraphicsManager), "fullscreenCheckTimer").GetValue(GraphicsManager.instance);
        if (timer > 0f)
        {
            return;
        }

        if (!val || !val2)
        {
            val = GameObject.Find("Render Texture Overlay");
            val2 = GameObject.Find("Render Texture Main");
        }

        if (val == null || val2 == null)
        {
            return;
        }

        RectTransform component = val.GetComponent<RectTransform>();
        RectTransform component2 = val2.GetComponent<RectTransform>();
        if (component != null && component2 != null)
        {
            float num = (float)Screen.width / (float)Screen.height;
            if (num > 1.7777778f)
            {
                component.sizeDelta = new Vector2(428f * num, 428f);
                component2.sizeDelta = new Vector2(428f * num, 428f);
            }
            else
            {
                component.sizeDelta = new Vector2(750f, 750f / num);
                component2.sizeDelta = new Vector2(750f, 750f / num);
            }
        }
    }
}
