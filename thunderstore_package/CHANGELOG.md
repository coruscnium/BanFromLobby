# Changelog

## 1.1.1 (2026-07-19)

### Changed
- Ban rejection message now includes the mod name: "This player has banned you from their lobbies via the BanFromLobby mod."
- Cleaned up leftover BanPlayersMod.dll from thunderstore package.
- Reformatted README and CHANGELOG to match other mods' style.

## 1.0.1 (2026-07-19)

### Fixed
- Icon dimensions to 256x256.

## 1.0.0 (2026-07-19)

### Added
- Initial release as BanFromLobby (renamed from BanPlayers)
- F7 UI window listing connected players and banned players
- Click [BAN] to add players to ban list, [UNBAN] to remove
- Harmony prefix on GameManager.OnJoinRequested rejects banned players at connection
- Kicks already-connected players via NetworkManager.DisconnectClient
- Ban list stored in BepInEx config (PlayerName|SteamID)
- FileSystemWatcher hot-reload for ModSettingsMenu (F10) edits
- Configurable toggle key via ModSettingsMenu
