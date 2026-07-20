# Changelog

## 1.1.2 (2026-07-19)

### Fixed
- Removed nested zip from release (stale BanFromLobby-1.0.1.zip was in thunderstore_package/).
- Added lesson #11 to clinerules documenting zip build hygiene.

## 1.1.0 (2026-07-19)

### Changed
- Ban rejection message now includes the mod name: "This player has banned you from their lobbies via the BanFromLobby mod."
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
