// Potato Runes - the sign's runic font on Valheim's hover text, written from scratch.
using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace PotatoRunes
{
    // The text that shows when you look at something (names, prompts, timers, chest names) is drawn in the font the
    // game uses on signs instead of its plain one. The font comes from the game's own sign, so nothing is shipped
    // and it follows the game if the sign ever changes. If the hover text already uses that font, nothing is done.
    // The text is wrapped in font tags rather than its font being swapped, so turning it off leaves the game's text as it was.
    // Only your own game changes; nobody else needs the mod.
    [BepInPlugin(Id, "Potato Runes", Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "potato.runes";
        public const string Version = "1.0.1";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled, Bold;
        internal static ConfigEntry<float> TextSize;

        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;
            const string font = "1 - Font";
            Enabled = Config.Bind(font, "Runic hover text", true,
                "Draw the text that shows when you look at something in the font the game uses on signs.");
            TextSize = Config.Bind(font, "Text size", 1.15f,
                new ConfigDescription("Size of the hover text compared with the game's own. Raise it if the runic letters are hard to read.",
                    new AcceptableValueRange<float>(0.8f, 1.6f)));
            Bold = Config.Bind(font, "Bold", false,
                "Draw the runic hover text thicker.");

            harmony = new Harmony(Id);
            try { harmony.CreateClassProcessor(typeof(HoverFont)).Patch(); }
            catch (Exception e) { Log.LogError($"Runic hover text is disabled: {e.GetBaseException().Message}"); }
        }

        private void OnDestroy() => harmony?.UnpatchSelf();
    }

    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
    internal static class HoverFont
    {
        private static bool failed, sameFont;
        private static TMP_FontAsset signFont;
        private static Material signMaterial;
        private static string tag, tagFor;

        // After the game (and other mods) fill in the hover text, the text is wrapped in tags that draw it in the sign's
        // font. The text object itself keeps the game's font and look, so with the setting off nothing is touched at all.
        // Runs late, so text other mods put in is wrapped too, but before anything that measures the text (like Potato
        // Tweaks' chest icons, which run last).
        [HarmonyPriority(Priority.VeryLow)]
        private static void Postfix(Hud __instance)
        {
            if (failed || !Plugin.Enabled.Value) return;
            try { Apply(__instance); }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Runic hover text is disabled: {e.GetBaseException().Message}");
                failed = true;
            }
        }

        private static void Apply(Hud hud)
        {
            var text = hud.m_hoverName;
            if (!text || !ZNetScene.instance) return;
            if (!signFont)
            {
                if (!FindSignFont()) { failed = true; return; }
                sameFont = signFont == text.font;
                if (sameFont) Plugin.Log.LogInfo($"The hover text already uses the sign's font ({signFont.name}), so nothing changes.");
            }
            if (sameFont) return;

            string shown = text.text;
            if (string.IsNullOrEmpty(shown)) return;
            string wanted = Tag();
            if (shown.StartsWith(wanted, StringComparison.Ordinal)) return;
            text.text = wanted + shown;
        }

        // <font> with the sign's outline look, <size> for the text size and <b> for Bold. Rebuilt only when a setting
        // or the font's letter sheet changes.
        private static string Tag()
        {
            int size = Mathf.RoundToInt(Plugin.TextSize.Value * 100f);
            bool bold = Plugin.Bold.Value;
            // The look has to be drawn from the font's current letter sheet, or the letters come out wrong; fall back to
            // the font's own look then.
            var look = signMaterial && signFont.atlasTexture && signMaterial.mainTexture == signFont.atlasTexture ? signMaterial : null;
            string key = $"{size}|{bold}|{(look ? look.GetInstanceID() : 0)}";
            if (tag != null && key == tagFor) return tag;

            string material = "";
            if (look)
            {
                string name = "PotatoRunes" + look.GetInstanceID();
                int hash = TMP_TextUtilities.GetHashCode(name);
                if (!MaterialReferenceManager.TryGetMaterial(hash, out _)) MaterialReferenceManager.AddFontMaterial(hash, look);
                material = $" material=\"{name}\"";
            }
            tag = $"<font=\"{signFont.name}\"{material}><size={size}%>" + (bold ? "<b>" : "");
            tagFor = key;
            return tag;
        }

        private static bool FindSignFont()
        {
            var prefab = ZNetScene.instance.GetPrefab("sign");
            var sign = prefab ? prefab.GetComponent<Sign>() : null;
            if (!sign || !sign.m_textWidget || !sign.m_textWidget.font)
            {
                Plugin.Log.LogWarning("Couldn't find the game's sign or its font, so the hover text keeps the game's own.");
                return false;
            }
            signFont = sign.m_textWidget.font;
            // <font> tags find fonts by name; register the sign's font so the tag finds it.
            if (!MaterialReferenceManager.TryGetFontAsset(signFont.hashCode, out _))
            {
                try { MaterialReferenceManager.AddFontAsset(signFont); }
                catch (Exception e) { Plugin.Log.LogWarning($"Registering the sign's font: {e.GetBaseException().Message}"); }
            }
            TMP_Text.OnFontAssetRequest += (hash, name) => name == signFont.name ? signFont : null;
            // The sign's own material is made for the world. On the screen the text needs an outline like the game's,
            // so use the font's outline look if it has one.
            var looks = Resources.FindObjectsOfTypeAll<Material>().Where(m => m && m.mainTexture == signFont.material.mainTexture).ToList();
            signMaterial = looks.FirstOrDefault(m => m.name == signFont.name + " - Outline")
                ?? looks.FirstOrDefault(m => m.name.IndexOf("Outline", StringComparison.OrdinalIgnoreCase) >= 0);
            Plugin.Log.LogInfo($"Hover text font: {signFont.name}, look: {(signMaterial ? signMaterial.name : signFont.material.name)}.");
            return true;
        }
    }
}
