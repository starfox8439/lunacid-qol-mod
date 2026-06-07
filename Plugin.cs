using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LunacidQoLMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid    = "crycode4650.lunacid.qolmod";
        public const string PluginName    = "LunacidQoLMod";
        public const string PluginVersion = "1.0.0";

        internal static Plugin          Instance      = null!;
        internal static ManualLogSource Log           = null!;
        internal static CanvasGroup?    HudCanvasGroup;

        internal static ConfigEntry<float>  HudAlpha          = null!;
        internal static ConfigEntry<bool>   UltrawideFix      = null!;
        internal static ConfigEntry<bool>   RespawnOnDeath    = null!;
        internal static ConfigEntry<bool>   JumpDuringAttack  = null!;
        internal static ConfigEntry<string> InputOverridePath = null!;

        private void Awake()
        {
            Instance = this;
            Log      = Logger;

            HudAlpha = Config.Bind(
                "HUD", "HudAlpha", 0.8f,
                new ConfigDescription("Overall HUD transparency (0 = invisible, 1 = fully opaque)",
                    new AcceptableValueRange<float>(0f, 1f)));

            UltrawideFix = Config.Bind(
                "Display", "UltrawideFix", true,
                "Enable AspectRatioFitter patches for ultrawide monitors");

            RespawnOnDeath = Config.Bind(
                "Gameplay", "RespawnOnDeath", true,
                "Respawn on death instead of showing the death screen");

            JumpDuringAttack = Config.Bind(
                "Gameplay", "JumpDuringAttack", true,
                "Allow jumping while performing a weapon attack");

            InputOverridePath = Config.Bind(
                "Input", "InputOverridePath", "",
                "Absolute path to an InputActionAsset JSON file to override game input bindings");

            new Harmony(PluginGuid).PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }

        // ── HUD canvas group ──────────────────────────────────────────────────────

        internal static void ApplyHudAlpha(Menus? menus = null)
        {
            if (HudCanvasGroup == null)
                HudCanvasGroup = menus != null ? FindHudGroupFromMenus(menus) : FindHudGroupFallback();
            if (HudCanvasGroup != null)
                HudCanvasGroup.alpha = HudAlpha.Value;
        }

        // Find the specific MENUS[] panel that is the HUD, without using a
        // positional index. Priority: name match → parents a Slider → canvas root.
        internal static CanvasGroup? FindHudGroupFromMenus(Menus menus)
        {
            if (menus.MENUS != null)
            {
                // 1. Name heuristic
                foreach (var go in menus.MENUS)
                {
                    if (go == null) continue;
                    if (go.name.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Log.LogInfo($"HUD panel found by name: {go.name}");
                        return go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
                    }
                }

                // 2. Find the MENUS element that parents any of the HP/MP sliders
                if (menus.Sliders != null)
                {
                    foreach (var go in menus.MENUS)
                    {
                        if (go == null) continue;
                        foreach (var slider in menus.Sliders)
                        {
                            if (slider != null && ((Component)slider).transform.IsChildOf(go.transform))
                            {
                                Log.LogInfo($"HUD panel found via Slider parent: {go.name}");
                                return go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
                            }
                        }
                    }
                }
            }

            // 3. Last resort: canvas that parents the first HP/MP slider
            if (menus.Sliders?.Length > 0 && menus.Sliders[0] != null)
            {
                var canvas = ((Component)menus.Sliders[0]).GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Log.LogInfo($"HUD canvas found via Slider root: {canvas.name}");
                    return canvas.gameObject.GetComponent<CanvasGroup>() ?? canvas.gameObject.AddComponent<CanvasGroup>();
                }
            }

            Log.LogWarning("HUD CanvasGroup not found.");
            return null;
        }

        // Generic fallback used by Update/SET when no Menus instance is handy.
        internal static CanvasGroup? FindHudGroupFallback()
        {
            foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
            {
                if (canvas.name.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0)
                    return canvas.gameObject.GetComponent<CanvasGroup>() ?? canvas.gameObject.AddComponent<CanvasGroup>();
            }
            foreach (var slider in UnityEngine.Object.FindObjectsOfType<Slider>())
            {
                var canvas = slider.GetComponentInParent<Canvas>();
                if (canvas != null)
                    return canvas.gameObject.GetComponent<CanvasGroup>() ?? canvas.gameObject.AddComponent<CanvasGroup>();
            }
            return null;
        }

        // ── Ultrawide helpers ─────────────────────────────────────────────────────

        internal static bool IsUltrawide()
            => UltrawideFix.Value && (float)Screen.width / Screen.height > 16f / 9f + 0.01f;

        internal static void StretchRect(RectTransform rt, GameObject go)
        {
            var fitter = go.GetComponent<AspectRatioFitter>();
            if (fitter != null) fitter.enabled = false;

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        internal static void ApplyUltrawideCanvasScaling()
        {
            if (!IsUltrawide()) return;
            foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
                // Match height so the canvas expands horizontally on ultrawide —
                // same as the original mod: matchWidthOrHeight=1, MatchWidthOrHeight mode.
                scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
            }
        }
    }

    // ─── Patch 1: Respawn on death ────────────────────────────────────────────────

    [HarmonyPatch(typeof(Player_Control_scr), "Die")]
    static class Patch_PlayerDie
    {
        [HarmonyPrefix]
        static bool Prefix(Player_Control_scr __instance)
        {
            if (!Plugin.RespawnOnDeath.Value) return true;

            Traverse.Create(__instance).Field("ded").SetValue(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return false;
        }
    }

    // ─── Patch 2: Allow jumping while attacking ───────────────────────────────────

    [HarmonyPatch(typeof(Player_Control_scr), "NawJump")]
    static class Patch_NawJump
    {
        [HarmonyPrefix]
        static void Prefix(Player_Control_scr __instance)
        {
            if (Plugin.JumpDuringAttack.Value)
                Traverse.Create(__instance).Field("Freeze").SetValue(false);
        }
    }

    // ─── Patch 3: Fix AspectRatioFitter on screen-flash overlay ──────────────────

    [HarmonyPatch(typeof(Flash_scr), "Flash")]
    static class Patch_FlashAspect
    {
        [HarmonyPrefix]
        static void Prefix(Flash_scr __instance)
        {
            if (!Plugin.IsUltrawide()) return;
            var rt = __instance.GetComponent<RectTransform>();
            if (rt != null) Plugin.StretchRect(rt, __instance.gameObject);
        }
    }

    // ─── Patch 4: HUD transparency + ultrawide on LoadMenu ───────────────────────
    // LoadMenu is called when the HUD/menu canvas is first built and on every
    // scene transition — serialized fields (MENUS, Sliders) are live by then.

    [HarmonyPatch(typeof(Menus), "LoadMenu")]
    static class Patch_MenusLoadMenu
    {
        [HarmonyPostfix]
        static void Postfix(Menus __instance)
        {
            Plugin.HudCanvasGroup = null;
            Plugin.ApplyHudAlpha(__instance);
            Plugin.ApplyUltrawideCanvasScaling();
        }
    }

    // ─── Patch 6: Re-apply HUD alpha every Update ─────────────────────────────────
    // Guard: game may fight the alpha value each frame.

    [HarmonyPatch(typeof(Menus), "Update")]
    static class Patch_MenusUpdate
    {
        [HarmonyPostfix]
        static void Postfix(Menus __instance) => Plugin.ApplyHudAlpha(__instance);
    }

    // ─── Patch 7: Re-apply HUD alpha after SET calls ──────────────────────────────

    [HarmonyPatch(typeof(Menus), "SET")]
    static class Patch_MenusSET
    {
        [HarmonyPostfix]
        static void Postfix(Menus __instance) => Plugin.ApplyHudAlpha(__instance);
    }

    // ─── Patch 8: Ultrawide popup text positioning ────────────────────────────────

    [HarmonyPatch(typeof(POP_text_scr), "POP")]
    static class Patch_PopText
    {
        [HarmonyPostfix]
        static void Postfix(POP_text_scr __instance)
        {
            if (!Plugin.IsUltrawide()) return;
            var rt = __instance.GetComponent<RectTransform>();
            if (rt == null) return;
            float xScale = ((float)Screen.width / Screen.height) / (16f / 9f);
            var pos = rt.anchoredPosition;
            pos.x *= xScale;
            rt.anchoredPosition = pos;
        }
    }

    // ─── Patch 9: Ultrawide poison overlay ───────────────────────────────────────

    [HarmonyPatch(typeof(Player_Poison), "Harm")]
    static class Patch_PoisonHarm
    {
        [HarmonyPostfix]
        static void Postfix(Player_Poison __instance)
        {
            if (!Plugin.IsUltrawide()) return;
            var img = Traverse.Create(__instance).Field<Image>("IMG").Value;
            if (img == null) return;
            var rt = img.GetComponent<RectTransform>();
            if (rt != null) Plugin.StretchRect(rt, img.gameObject);
        }
    }

    // ─── Patches 10a / 10b: Load input override JSON ─────────────────────────────

    [HarmonyPatch(typeof(CONTROL), "Start")]
    static class Patch_ControlStart
    {
        [HarmonyPostfix]
        static void Postfix() => InputOverrideLoader.TryLoad();
    }

    [HarmonyPatch(typeof(Menu_Control_scr), "Start")]
    static class Patch_MenuControlStart
    {
        [HarmonyPostfix]
        static void Postfix() => InputOverrideLoader.TryLoad();
    }

    static class InputOverrideLoader
    {
        private static bool _loaded;

        internal static void TryLoad()
        {
            if (_loaded) return;
            var path = Plugin.InputOverridePath.Value;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            try
            {
                var playerInput = UnityEngine.Object.FindObjectOfType<PlayerInput>();
                if (playerInput?.actions == null) return;
                playerInput.actions = InputActionAsset.FromJson(File.ReadAllText(path));
                _loaded = true;
                Plugin.Log.LogInfo($"Input overrides loaded from: {path}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to load input overrides from \"{path}\": {ex}");
            }
        }
    }
}
