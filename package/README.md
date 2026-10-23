Potato Runes draws Valheim's hover text, the words that show when you look at something, in the runic font the game uses on signs. Names, prompts, timers and chest names all switch over. It only changes your own game: nobody else needs it, and it works on any server, modded or not.

![A named chest and its contents in runic hover text](https://raw.githubusercontent.com/TheRealRel/PotatoRunes/main/docs/images/chest.jpg)

![A smelter's timers in runic hover text](https://raw.githubusercontent.com/TheRealRel/PotatoRunes/main/docs/images/smelter.jpg)

**Nothing is shipped.** The font is read from the game's own sign when you load in, so the mod is a single small file and follows the game if Iron Gate ever changes the sign. Text added by other mods (Potato Tweaks' chest icons and station timers, for example) picks up the same font.

**Settings.** In `BepInEx/config/potato.runes.cfg`, or in game with Configuration Manager (F1). Changes show at once.

| Setting | What it does | Default |
|---|---|---|
| Runic hover text | Switches the runic font on or off | On |
| Text size | Size compared with the game's own text, from 0.8 to 1.6. Raise it if the letters are hard to read | 1.15 |
| Bold | Draws the letters thicker | Off |

If a Valheim update breaks something the mod relies on, it switches itself off, writes why in the BepInEx log, and the game keeps its normal text.

Bug reports go to [GitHub Issues](https://github.com/TheRealRel/PotatoRunes/issues), where the source lives too. Questions are welcome on Discord: [relanebeis](https://discord.com/users/373574370267889674).

Needs [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).

The code is under the MIT License: use it, change it and share it freely. The icon and screenshots are © TheRealRel under [CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/): free to share and adapt with credit, but not for commercial use. See LICENSE.
