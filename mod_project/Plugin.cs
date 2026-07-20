using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Pigeon.Movement;
using Sparroh.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BanFromLobbyMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInProcess("Mycopunk.exe")]
    [BepInDependency("sparroh.uilibrary")]
    [MycoMod(null, ModFlags.IsClientSide)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "coruscnium.banfromlobby";
        public const string PluginName = "BanFromLobby";
        public const string PluginVersion = "1.0.0.0";

        internal static new ManualLogSource Logger;
        internal static Plugin Instance { get; private set; }

        // ===== Config =====
        private static ConfigEntry<string> _banListConfig;
        private static ConfigEntry<string> _toggleKeyConfig;

        // ===== In-memory ban list =====
        private struct BanEntry
        {
            public string PlayerName;
            public ulong SteamID;
        }
        private List<BanEntry> _bans = new List<BanEntry>();
        private bool _bansDirty = true;
        private const char BanEntrySep = '|';

        // ===== UI =====
        private UIWindow _window;
        private bool _windowVisible;

        // ===== FileSystemWatcher =====
        private FileSystemWatcher _configWatcher;
        private volatile bool _reloadPending;
        private float _reloadCooldown;
        private const float ReloadDebounceSeconds = 0.25f;

        // ===== Key detection =====
        private UnityEngine.InputSystem.Key _toggleKey = UnityEngine.InputSystem.Key.F7;
        private bool _keyCached;

        private const string BanRejectionMessage = "This player has banned you from their lobbies.";

        // ======================================================================
        //  Awake
        // ======================================================================
        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            _banListConfig = Config.Bind("Ban List", "Bans", "",
                "One ban per line. Format: PlayerName|SteamID");
            _toggleKeyConfig = Config.Bind("General", "ToggleKey", "F7",
                "Key to toggle the ban management window.");

            _toggleKeyConfig.SettingChanged += (_, _) => CacheKey();
            Config.Save();

            CacheKey();
            ParseBanList();

            SetupConfigHotReload();

            SceneManager.sceneLoaded += OnSceneLoaded;

            // Hook OnClientConnected to auto-kick banned players on join
            try
            {
                GameManager.OnClientConnected += OnClientConnected;
                Logger.LogInfo("Subscribed to GameManager.OnClientConnected");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not subscribe to OnClientConnected: {ex.Message}");
            }

            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        // ======================================================================
        //  OnClientConnected — fires when any client joins
        //  We delay 1s because Player.UserID (SteamID) is 0 at event time
        // ======================================================================
        private void OnClientConnected(ulong clientID)
        {
            StartCoroutine(DelayedClientCheck(clientID));
        }

        private IEnumerator DelayedClientCheck(ulong clientID)
        {
            yield return new WaitForSeconds(1.0f);

            try
            {
                Player target = GameManager.GetPlayer(clientID);
                if (target == null)
                {
                    Logger.LogWarning($"DelayedCheck: GetPlayer({clientID}) returned null.");
                    yield break;
                }

                ulong steamID = target.UserID;
                if (steamID == 0)
                {
                    Logger.LogWarning($"DelayedCheck: Player still has UserID=0 for clientID={clientID}, cannot check.");
                    yield break;
                }

                if (IsBanned(steamID))
                {
                    Logger.LogInfo($"Auto-disconnecting banned player (SteamID={steamID}, clientID={clientID}).");
                    DoKickPlayer(target);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error in DelayedClientCheck: {ex.Message}");
            }
        }

        // ======================================================================
        //  Config parsing
        // ======================================================================
        private void ParseBanList()
        {
            _bans.Clear();
            string raw = _banListConfig.Value ?? "";
            if (string.IsNullOrWhiteSpace(raw)) return;

            foreach (string line in raw.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                string[] parts = trimmed.Split(BanEntrySep);
                if (parts.Length >= 2 && ulong.TryParse(parts[1], out ulong steamId))
                {
                    _bans.Add(new BanEntry
                    {
                        PlayerName = parts[0],
                        SteamID = steamId
                    });
                }
            }

            Logger.LogInfo($"Loaded {_bans.Count} ban(s) from config.");
            _bansDirty = false;
        }

        private void SerializeBanList()
        {
            var lines = new List<string>();
            foreach (var ban in _bans)
                lines.Add($"{ban.PlayerName}{BanEntrySep}{ban.SteamID}");
            _banListConfig.Value = string.Join("\n", lines);
            Config.Save();
            _bansDirty = false;
        }

        private bool IsBanned(ulong steamId)
        {
            if (_bansDirty) ParseBanList();
            return _bans.Any(b => b.SteamID == steamId);
        }

        private void CacheKey()
        {
            try
            {
                _toggleKey = (UnityEngine.InputSystem.Key)Enum.Parse(
                    typeof(UnityEngine.InputSystem.Key), _toggleKeyConfig.Value, true);
            }
            catch
            {
                _toggleKey = UnityEngine.InputSystem.Key.F7;
            }
            _keyCached = true;
        }

        // ======================================================================
        //  Ban management
        // ======================================================================
        private void BanPlayer(ulong steamId, string playerName)
        {
            if (IsBanned(steamId))
            {
                SendChatMsg($"<color=yellow>{playerName} is already banned.</color>");
                return;
            }

            _bans.Add(new BanEntry { PlayerName = playerName, SteamID = steamId });
            SerializeBanList();

            // Find the player by SteamID and kick immediately
            var allPlayers = Resources.FindObjectsOfTypeAll<Player>();
            Player target = allPlayers.FirstOrDefault(p => p.UserID == steamId);
            if (target != null)
                DoKickPlayer(target);

            SendChatMsg($"<color=red>BANNED: {playerName}</color>");
            Logger.LogInfo($"Banned {playerName} (SteamID={steamId}).");

            RefreshWindow();
        }

        private void UnbanPlayer(ulong steamId)
        {
            string name = _bans.FirstOrDefault(b => b.SteamID == steamId).PlayerName;
            _bans.RemoveAll(b => b.SteamID == steamId);
            SerializeBanList();

            SendChatMsg($"<color=green>UNBANNED: {name}</color>");

            RefreshWindow();
        }

        // ======================================================================
        //  Kick — uses NetworkManager.Singleton.DisconnectClient (same as KickPlayerButton)
        // ======================================================================
        private static void DoKickPlayer(Player target)
        {
            try
            {
                if (target == null) return;

                ulong clientId = target.OwnerClientId;
                NetworkManager.Singleton.DisconnectClient(clientId, BanRejectionMessage);

                Logger.LogInfo($"Disconnected player {target.Name} (SteamID={target.UserID}, clientId={clientId}).");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to disconnect player: {ex.Message}");
            }
        }

        // ======================================================================
        //  Chat messages
        // ======================================================================
        private void SendChatMsg(string message)
        {
            try
            {
                if (Player.LocalPlayer != null && Player.LocalPlayer.PlayerLook != null)
                    Player.LocalPlayer.PlayerLook.AddTextChatMessage(message, Player.LocalPlayer);
            }
            catch { /* ignore */ }
        }

        // ======================================================================
        //  Config hot-reload
        // ======================================================================
        private void SetupConfigHotReload()
        {
            try
            {
                string configPath = Config.ConfigFilePath;
                string directory = Path.GetDirectoryName(configPath);
                string fileName = Path.GetFileName(configPath);

                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                {
                    Logger.LogWarning("Could not set up config hot reload: invalid config path.");
                    return;
                }

                _configWatcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };
                _configWatcher.Changed += OnConfigFileChanged;
                _configWatcher.Created += OnConfigFileChanged;
                _configWatcher.Renamed += OnConfigFileChanged;

                Logger.LogInfo($"Config hot reload enabled for {fileName}");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to set up config hot reload: {ex.Message}");
            }
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            _reloadPending = true;
        }

        // ======================================================================
        //  UI — SparrohUILib
        // ======================================================================
        private void CreateWindow()
        {
            if (_window != null) return;

            try
            {
                _window = UIWindow.Create(
                    "BanFromLobbyWindow",
                    new Vector2(540f, 500f),
                    $"BAN FROM LOBBY ({_bans.Count})",
                    scrollable: true,
                    closeButton: false,
                    sortingOrder: 29999
                );

                _window.OnClose(() => { });
                RefreshWindow();
                Logger.LogInfo("BanFromLobby UI window created.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to create BanFromLobby window: {ex.Message}");
            }
        }

        private void RefreshWindow()
        {
            if (_window == null || _window.Content == null) return;

            foreach (Transform child in _window.Content)
                Destroy(child.gameObject);

            _window.WithTitle($"BAN FROM LOBBY ({_bans.Count})");

            UIWindow.CreateSectionHeader(_window.Content, "CONNECTED PLAYERS");
            ListPlayers(_window.Content);

            UIWindow.CreateSectionHeader(_window.Content, "BANNED PLAYERS");
            ListBans(_window.Content);

            UIButton.Create(_window.Content, "[CLOSE]", () => ToggleWindow(),
                UIButtonStyle.Primary, "CloseBtn");
        }

        private void ListPlayers(Transform content)
        {
            try
            {
                var connected = Resources.FindObjectsOfTypeAll<Player>()
                    .Where(p => p != null && p.UserID != 0 && p != Player.LocalPlayer)
                    .ToList();

                if (connected.Count == 0)
                {
                    UIText.Create(content, "NoPlayersLabel", "No other players connected.",
                        UITheme.ScaledFontBody, UIColors.TextMuted, TextAlignmentOptions.Left);
                    return;
                }

                foreach (var player in connected)
                {
                    ulong steamId = player.UserID;
                    bool alreadyBanned = IsBanned(steamId);

                    var panel = UIPanel.Create(content, "PlayerRow_" + steamId, UIColors.PanelBg, withBorder: false);
                    UIFactory.AddHorizontalLayout(panel.GameObject, 4f, new RectOffset(4, 4, 2, 2),
                        TextAnchor.MiddleLeft);

                    string displayName = string.IsNullOrEmpty(player.Name) ? $"Player-{player.PlayerNumber}" : player.Name;

                    var labelText = UIText.Create(panel.Content, "Label",
                        $"{displayName} [{steamId}]",
                        UITheme.ScaledFontBody,
                        alreadyBanned ? UIColors.TextMuted : UIColors.TextPrimary,
                        TextAlignmentOptions.Left);

                    if (labelText != null)
                    {
                        var layEl = labelText.GameObject.AddComponent<LayoutElement>();
                        layEl.flexibleWidth = 1;
                    }

                    if (!alreadyBanned)
                    {
                        string name = displayName;
                        var banBtn = UIButton.Create(panel.Content, "BAN",
                            () => BanPlayer(steamId, name),
                            UIButtonStyle.Danger, null, 36f);
                        if (banBtn != null)
                        {
                            var le = banBtn.GameObject.AddComponent<LayoutElement>();
                            le.minWidth = 80f;
                        }
                    }
                    else
                    {
                        var bannedLabel = UIText.Create(panel.Content, "BannedLabel", "[BANNED]",
                            UITheme.ScaledFontBody, UIColors.TextMuted, TextAlignmentOptions.Left);
                        if (bannedLabel != null)
                        {
                            var layEl = bannedLabel.GameObject.AddComponent<LayoutElement>();
                            layEl.flexibleWidth = 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UIText.Create(content, "ErrorLabel", $"Error listing players: {ex.Message}",
                    UITheme.ScaledFontBody, UIColors.Rose, TextAlignmentOptions.Left);
            }
        }

        private void ListBans(Transform content)
        {
            if (_bans.Count == 0)
            {
                UIText.Create(content, "NoBansLabel", "No banned players.",
                    UITheme.ScaledFontBody, UIColors.TextMuted, TextAlignmentOptions.Left);
                return;
            }

            foreach (var ban in _bans)
            {
                var panel = UIPanel.Create(content, "BanRow_" + ban.SteamID, UIColors.PanelBg, withBorder: false);
                UIFactory.AddHorizontalLayout(panel.GameObject, 4f, new RectOffset(4, 4, 2, 2),
                    TextAnchor.MiddleLeft);

                var banLabel = UIText.Create(panel.Content, "Label",
                    $"{ban.PlayerName} | {ban.SteamID}",
                    UITheme.ScaledFontBody, UIColors.TextPrimary, TextAlignmentOptions.Left);

                if (banLabel != null)
                {
                    var layEl = banLabel.GameObject.AddComponent<LayoutElement>();
                    layEl.flexibleWidth = 1;
                }

                ulong sid = ban.SteamID;
                var unbanBtn = UIButton.Create(panel.Content, "UNBAN", () => UnbanPlayer(sid),
                    UIButtonStyle.Primary, null, 36f);
                if (unbanBtn != null)
                {
                    var le = unbanBtn.GameObject.AddComponent<LayoutElement>();
                    le.minWidth = 80f;
                }
            }
        }

        private void ToggleWindow()
        {
            if (_window == null)
            {
                CreateWindow();
                if (_window == null) return;
            }

            _windowVisible = !_windowVisible;

            if (_windowVisible)
            {
                RefreshWindow();
                _window.Show();
                LockGameInput(true);
            }
            else
            {
                _window.Hide();
                LockGameInput(false);
            }
        }

        private void LockGameInput(bool locked)
        {
            try
            {
                if (locked)
                {
                    PlayerInput.EnableMenu();
                    if (Player.LocalPlayer != null)
                    {
                        Player.LocalPlayer.PlayerLook.RotationLocksX++;
                        Player.LocalPlayer.PlayerLook.RotationLocksY++;
                        Player.LocalPlayer.LockFiring(true);
                    }
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    PlayerInput.DisableMenu();
                    if (Player.LocalPlayer != null)
                    {
                        Player.LocalPlayer.PlayerLook.RotationLocksX =
                            Math.Max(0, Player.LocalPlayer.PlayerLook.RotationLocksX - 1);
                        Player.LocalPlayer.PlayerLook.RotationLocksY =
                            Math.Max(0, Player.LocalPlayer.PlayerLook.RotationLocksY - 1);
                        Player.LocalPlayer.LockFiring(false);
                    }
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
            catch { /* ignore */ }
        }

        // ======================================================================
        //  Update
        // ======================================================================
        private void Update()
        {
            if (_reloadPending)
            {
                _reloadPending = false;
                _reloadCooldown = ReloadDebounceSeconds;
            }
            if (_reloadCooldown > 0f)
            {
                _reloadCooldown -= Time.unscaledDeltaTime;
                if (_reloadCooldown <= 0f)
                    TryReloadConfig();
            }

            if (UnityEngine.InputSystem.Keyboard.current != null && _keyCached)
            {
                if (UnityEngine.InputSystem.Keyboard.current[_toggleKey].wasPressedThisFrame)
                    ToggleWindow();
            }
        }

        // ======================================================================
        //  Config reload
        // ======================================================================
        private void TryReloadConfig()
        {
            try
            {
                Config.Reload();
                _bansDirty = true;
                Logger.LogInfo("Config reloaded from disk.");
            }
            catch (IOException)
            {
                _reloadPending = true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to reload config: {ex.Message}");
            }
        }

        // ======================================================================
        //  Scene loaded
        // ======================================================================
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_windowVisible)
            {
                _windowVisible = false;
                LockGameInput(false);
            }

            // Re-subscribe to OnClientConnected after scene change
            try
            {
                GameManager.OnClientConnected += OnClientConnected;
            }
            catch { /* ignore */ }
        }

        // ======================================================================
        //  OnDestroy
        // ======================================================================
        private void OnDestroy()
        {
            if (_configWatcher != null)
            {
                _configWatcher.EnableRaisingEvents = false;
                _configWatcher.Changed -= OnConfigFileChanged;
                _configWatcher.Created -= OnConfigFileChanged;
                _configWatcher.Renamed -= OnConfigFileChanged;
                _configWatcher.Dispose();
                _configWatcher = null;
            }

            if (_windowVisible)
                LockGameInput(false);

            try
            {
                if (_window != null)
                {
                    _window.Hide();
                    _window.Destroy();
                    _window = null;
                }
            }
            catch { /* ignore */ }
        }
    }
}