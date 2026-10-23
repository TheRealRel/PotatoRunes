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
    // Only your own game changes; nobody else needs the mod.
    [BepInPlugin(Id, "Potato Runes", Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "potato.runes";
        public const string Version = "1.0.0";

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
        private static Hud captured;
        private static bool failed, sameFont;
        private static TMP_FontAsset gameFont, signFont;
        private static Material gameMaterial, signMaterial;
        private static float gameSize;
        private static FontStyles gameStyle;

        // Every frame, before the hover text is filled in, so anything that copies its look (like Potato Tweaks'
        // chest icons) gets the new one. It only changes something when the text's look isn't the wanted one yet.
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Hud __instance)
        {
            if (failed) return;
            try { Apply(__instance); }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Runic hover text is disabled: {e.GetBaseException().Message}");
                failed = true;
            }
        }

        private static void Apply(Hud hud)
        {
            if (!ZNetScene.instance || !hud.m_hoverName) return;
            var text = hud.m_hoverName;
            if (captured != hud)
            {
                captured = hud;
                gameFont = text.font;
                gameMaterial = text.fontSharedMaterial;
                gameSize = text.fontSize;
                gameStyle = text.fontStyle;
                if (!signFont && !FindSignFont()) { failed = true; return; }
                sameFont = signFont == gameFont;
                if (sameFont) Plugin.Log.LogInfo($"The hover text already uses the sign's font ({signFont.name}), so nothing changes.");
            }
            if (sameFont) return;

            bool on = Plugin.Enabled.Value;
            var font = on ? signFont : gameFont;
            var material = Matching(on ? signMaterial : gameMaterial, font);
            float size = on ? gameSize * Plugin.TextSize.Value : gameSize;
            var style = on && Plugin.Bold.Value ? gameStyle | FontStyles.Bold : gameStyle;
            if (text.font == font && text.fontSharedMaterial == material && Mathf.Approximately(text.fontSize, size) && text.fontStyle == style) return;

            text.font = font;
            text.fontSharedMaterial = material;
            text.fontSize = size;
            text.fontStyle = style;
            // The text's letters were looked up in the old font; read them again from the new one, or they come out
            // garbled until the text changes.
            text.ForceMeshUpdate(true, true);
        }

        // The fonts fill their letter sheet while the game runs; a look that isn't drawn from the font's current sheet
        // would show the wrong letters, so fall back to the font's own look then.
        private static Material Matching(Material material, TMP_FontAsset font) =>
            material && font.atlasTexture && material.mainTexture == font.atlasTexture ? material : font.material;

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
            // The sign's own material is made for the world. On the screen the text needs an outline like the game's,
            // so use the font's outline look if it has one.
            var looks = Resources.FindObjectsOfTypeAll<Material>().Where(m => m && m.mainTexture == signFont.material.mainTexture).ToList();
            signMaterial = looks.FirstOrDefault(m => m.name == signFont.name + " - Outline")
                ?? looks.FirstOrDefault(m => m.name.IndexOf("Outline", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? signFont.material;
            Plugin.Log.LogInfo($"Hover text font: {signFont.name}, look: {signMaterial.name}.");
            return true;
        }
    }
}
