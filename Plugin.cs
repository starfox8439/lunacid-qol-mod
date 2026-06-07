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

        internal static ConfigEntry<float>  HudAlpha         = null!;
        internal static ConfigEntry<bool>   UltrawideFix     = null!;
        internal static ConfigEntry<bool>   RespawnOnDeath   = null!;
        internal static ConfigEntry<bool>   JumpDuringAttack = null!;
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

        // ── HUD canvas helpers ────────────────────────────────────────────────────

        internal static void ApplyHudAlpha()
        {
            if (HudCanvasGroup == null)
                HudCanvasGroup = FindHudCanvasGroup();
            if (HudCanvasGroup != null)
                HudCanvasGroup.alpha = HudAlpha.Value;
        }

        internal static CanvasGroup? FindHudCanvasGroup()
        {
            // Search by canvas name first
            foreach (var canvas in Object.FindObjectsOfType<Canvas>())
            {
                if (canvas.name.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0)
                    return GetOrAddCanvasGroup(canvas.gameObject);
            }
            // Fallback: first canvas that contains a Slider (health/stamina bars)
            foreach (var slider in Object.FindObjectsOfType<Slider>())
            {
                var canvas = slider.GetComponentInParent<Canvas>();
                if (canvas != null)
                    return GetOrAddCanvasGroup(canvas.gameObject);
            }
            return null;
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            return go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
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
    }

    // ─── Patch 1: Respawn on death ────────────────────────────────────────────────
    // Skips the death screen and reloads the current scene instead.

    [HarmonyPatch(typeof(Player_Control_scr), "Die")]
    static class Patch_PlayerDie
    {
        [HarmonyPrefix]
        static bool Prefix(Player_Control_scr __instance)
        {
            if (!Plugin.RespawnOnDeath.Value) return true;

            // Clear death state so the game doesn't treat the player as dead
            // after the scene reloads.
            Traverse.Create(__instance).Field("ded").SetValue(false);

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return false;
        }
    }

    // ─── Patch 2: Allow jumping while attacking ───────────────────────────────────
    // Clears the Freeze flag before NawJump checks it, so the jump is never blocked
    // by an ongoing weapon-attack animation.

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
    // The flash GO has an AspectRatioFitter that clamps it to 16:9 on ultra-wide;
    // we disable it and anchor the rect to fill the full screen instead.

    [HarmonyPatch(typeof(Flash_scr), "Flash")]
    static class Patch_FlashAspect
    {
        [HarmonyPrefix]
        static void Prefix(Flash_scr __instance)
        {
            if (!Plugin.IsUltrawide()) return;

            var rt = __instance.GetComponent<RectTransform>();
            if (rt != null)
                Plugin.StretchRect(rt, __instance.gameObject);
        }
    }

    // ─── Patch 4: HUD transparency + ultrawide canvas scaling on scene load ───────
    // Runs after LoadMenu so every new scene has correct alpha and canvas scale mode.

    [HarmonyPatch(typeof(Menus), "LoadMenu")]
    static class Patch_MenusLoadMenu
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            // Drop the cached group so we re-discover the HUD in the new scene.
            Plugin.HudCanvasGroup = null;
            Plugin.ApplyHudAlpha();
            ApplyUltrawideCanvasScaling();
        }

        static void ApplyUltrawideCanvasScaling()
        {
            if (!Plugin.IsUltrawide()) return;

            foreach (var canvas in Object.FindObjectsOfType<Canvas>())
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) continue;
                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
        }
    }

    // ─── Patch 5: Re-apply HUD alpha every Update ─────────────────────────────────
    // The game resets CanvasGroup.alpha each frame via its own Update loop.

    [HarmonyPatch(typeof(Menus), "Update")]
    static class Patch_MenusUpdate
    {
        [HarmonyPostfix]
        static void Postfix() => Plugin.ApplyHudAlpha();
    }

    // ─── Patch 6: Re-apply HUD alpha after SET calls ──────────────────────────────

    [HarmonyPatch(typeof(Menus), "SET")]
    static class Patch_MenusSET
    {
        [HarmonyPostfix]
        static void Postfix() => Plugin.ApplyHudAlpha();
    }

    // ─── Patch 7: Ultrawide popup text positioning ────────────────────────────────
    // Floating damage / notification text uses positions baked for 16:9.
    // Scale the X coordinate so it stays proportionally correct on wider displays.

    [HarmonyPatch(typeof(POP_text_scr), "POP")]
    static class Patch_PopText
    {
        [HarmonyPostfix]
        static void Postfix(POP_text_scr __instance)
        {
            if (!Plugin.IsUltrawide()) return;

            var rt = __instance.GetComponent<RectTransform>();
            if (rt == null) return;

            float aspect = (float)Screen.width / Screen.height;
            // Map the 16:9 X coordinate into the actual wider canvas space.
            float xScale = aspect / (16f / 9f);
            var pos = rt.anchoredPosition;
            pos.x *= xScale;
            rt.anchoredPosition = pos;
        }
    }

    // ─── Patch 8: Ultrawide poison overlay ───────────────────────────────────────
    // Player_Poison.IMG is the fullscreen poison-vignette image.
    // Remove its AspectRatioFitter and stretch to fill the screen.

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
            if (rt != null)
                Plugin.StretchRect(rt, img.gameObject);
        }
    }

    // ─── Patches 9a / 9b: Load input override JSON via Unity InputSystem ──────────
    // Both CONTROL and Menu_Control_scr run Start(); we hook both so the overrides
    // are applied regardless of which initialises first. A static flag prevents
    // double-loading.

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
                var json          = File.ReadAllText(path);
                var overrideAsset = InputActionAsset.FromJson(json);

                var playerInput = Object.FindObjectOfType<PlayerInput>();
                if (playerInput?.actions == null) return;

                // Convert the full asset's bindings to an overrides JSON and apply.
                playerInput.actions.LoadBindingOverridesFromJson(
                    overrideAsset.SaveBindingOverridesAsJson());

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
