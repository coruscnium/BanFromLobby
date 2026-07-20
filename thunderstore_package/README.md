# BanFromLobby

Lobby ban management for Mycopunk. Block unwanted players from joining your lobby with a simple in-game UI. Banned players get disconnected immediately and can't re-join.

## Key Features

### Block on Join
Banned players are disconnected at connection — they never reach your lobby. Uses `NetworkManager.DisconnectClient` with a clear rejection message.

### In-Game UI
Press **F7** (configurable) to open the ban management window. See every connected player, ban them with one click, or unban from the banned list below.

### Persistent Ban List
Your ban list saves to `BepInEx/config/coruscnium.banfromlobby.cfg` and survives restarts. Format: `PlayerName|SteamID` per line.

### F10 Compatible
Edit the ban list directly via ModSettingsMenu's F10 GUI. Changes hot-reload automatically via FileSystemWatcher.

## Installation

### Thunderstore
1. Install [BepInExPack_Mycopunk](https://thunderstore.io/c/mycopunk/p/BepInEx/BepInExPack_Mycopunk/).
2. Install [SparrohUILib](https://thunderstore.io/c/mycopunk/p/Sparroh/SparrohUILib/) by Sparroh.
3. Install [ModSettingsMenu](https://thunderstore.io/c/mycopunk/p/Sparroh/ModSettingsMenu/) by Sparroh (optional, for F10 editing).
4. Install this mod through your mod manager.
5. Launch the game.

### Manual
1. Install BepInExPack_Mycopunk, SparrohUILib, and ModSettingsMenu as above.
2. Extract `BanFromLobbyMod.dll` into `BepInEx/plugins/` in your Mycopunk directory.
3. Launch the game.

## Usage

1. Press **F7** (configurable) to open the Ban From Lobby window.
2. Click **[BAN]** next to any connected player.
3. They'll be disconnected immediately and blocked from re-joining.

To unban, scroll down to the **BANNED PLAYERS** section and click **[UNBAN]**.

## Configuration

Press **F10** in-game to open ModSettingsMenu, find **BanFromLobby**, and edit any setting. Changes apply instantly.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Bans` | string | *(empty)* | One ban per line: `PlayerName\|SteamID` |
| `ToggleKey` | string | `F7` | Key to open/close the ban window |

## Requirements

- Mycopunk, obviously
- [BepInExPack_Mycopunk](https://thunderstore.io/c/mycopunk/p/BepInEx/BepInExPack_Mycopunk/)
- [SparrohUILib](https://thunderstore.io/c/mycopunk/p/Sparroh/SparrohUILib/) by Sparroh
- [ModSettingsMenu](https://thunderstore.io/c/mycopunk/p/Sparroh/ModSettingsMenu/) by Sparroh (optional, for F10 editing)

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Credits

Icon: [Gavel icon](https://stock.adobe.com/images/Black-gavel-with-a-curved-line-on-transparent-background-law-judge-court-legal-j/1864599014) by Yevgen Romanenko from stock.adobe.com.
