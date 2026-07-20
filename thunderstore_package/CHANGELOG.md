# Changelog

## 1.0.0
- Initial release as BanFromLobby (renamed from BanPlayers)
- F7 UI window listing connected players and banned players
- Click [BAN] to add players to ban list, [UNBAN] to remove
- Harmony prefix on GameManager.OnJoinRequested rejects banned players at connection
- Kicks already-connected players via NetworkManager.DisconnectClient
- Ban list stored in BepInEx config (PlayerName|SteamID)
- FileSystemWatcher hot-reload for ModSettingsMenu (F10) edits
- Configurable toggle key via ModSettingsMenu