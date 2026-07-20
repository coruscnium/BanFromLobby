# BanFromLobby

Have you ever kicked someone from the game only for them to join back? Do you have someone who's just not getting the message? Well BanFromLobby is the perfect fix for those pesky pests. Simply ban them and you won't have to worry about bothering with kicking them again.

## Features

- **Block on join**: Banned players are disconnected at connection — they never reach your lobby
- **In-game UI**: Press F7 (configurable) to open the ban management window
- **Player list**: See every connected player, ban them with one click
- **Ban list**: View and unban all currently banned players
- **Persistent**: Ban list saves to BepInEx config and survives restarts
- **F10 compatible**: Edit the ban list directly via ModSettingsMenu (hot-reloads automatically)
- **Configurable**: Toggle key is editable via ModSettingsMenu

## How to Use

1. Press **F7** (configurable) to open the Ban From Lobby window
2. Click **[BAN]** next to any connected player
3. They'll be disconnected immediately and blocked from re-joining

To unban, scroll down to the **BANNED PLAYERS** section and click **[UNBAN]**.

## Installation

1. Install [BepInExPack_Mycopunk](https://thunderstore.io/c/mycopunk/p/BepInEx/BepInExPack_Mycopunk/)
2. Install [SparrohUILib](https://thunderstore.io/c/mycopunk/p/Sparroh/SparrohUILib/)
3. Install [ModSettingsMenu](https://thunderstore.io/c/mycopunk/p/Sparroh/ModSettingsMenu/) (optional, for F10 editing)
4. Extract this mod into your `BepInEx/plugins/` folder

## Config

The ban list is stored in `BepInEx/config/coruscnium.banfromlobby.cfg`. Format:
```
Bans = PlayerName|SteamID\nAnotherName|SteamID
```

You can edit this file directly or use ModSettingsMenu (F10).

## Changelog

### 1.0.0
- Initial release
- F7 UI with player list and ban/unban
- Harmony prefix on GameManager.OnJoinRequested to block at connection
- Kicks already-connected players via NetworkManager.DisconnectClient
- Config hot-reload support
- Editable toggle key