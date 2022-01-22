using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Network;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;

namespace ReadyCheck {
    public class PartyState : IDisposable {
        private readonly Plugin plugin;
        private bool wasInCombatLastUpdate = false;
        private bool wasAllReadyLastUpdate = false;
        private readonly Regex readyPattern = new Regex(@"^r((ea)?dy)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex unreadyPattern = new Regex(@"^unr((ea)?dy)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // map of content id => is ready
        private readonly Dictionary<long, bool> readyStates = new Dictionary<long, bool>();

        public PartyState(Plugin plugin) {
            this.plugin = plugin;
            this.plugin.ClientState.TerritoryChanged += OnTerritoryChange;
            this.plugin.Framework.Update += OnUpdate;
            this.plugin.ChatGui.ChatMessage += OnChatMessage;
            this.plugin.Network.NetworkMessage += OnNetworkMessage;
        }

        public void Reset() {
            PluginLog.Debug("Resetting ready states");
            readyStates.Clear();
        }

        public bool IsMemberReady(PartyMember member) {
            return readyStates.TryGetValue(member.ContentId, out bool memberIsReady) && memberIsReady;
        }

        private void OnCombatStateChange(bool isInCombat) {
            PluginLog.Debug($"Combat state changed: inCombat={isInCombat}");
            if (plugin.Configuration.ResetOnCombat && isInCombat) {
                Reset();
            }
        }

        private void OnReadyStateChange(bool isAllReady) {
            PluginLog.Debug($"Ready state changed: isReady={isAllReady}");
            if (plugin.Configuration.StartCountdownWhenAllReady && isAllReady) {
                plugin.XivCommon.Functions.Chat.SendMessage($"/cd {plugin.Configuration.CountdownDuration}");
            }

            if (plugin.Configuration.RunCommandWhenAllReady != "" && isAllReady) {
                plugin.CommandManager.ProcessCommand(plugin.Configuration.RunCommandWhenAllReady);
            }
        }

        private void OnPartyMemberReady(long memberContentId) {
            PluginLog.Debug($"Member ready: {memberContentId}");
            readyStates[memberContentId] = true;
        }

        private void OnPartyMemberUnready(long memberContentId) {
            PluginLog.Debug($"Member unready: {memberContentId}");
            readyStates[memberContentId] = false;
        }

        // ==== utils ====
        private long ResolvePartyMemberFromSenderName(SeString sender) {
            // if there is only a single RawText node and its value is our name (plus possibly the party number indicator), it's probably the local player
            if (plugin.ClientState.LocalPlayer is not null
                && sender.Payloads.Count == 1
                && sender.Payloads[0].Type == PayloadType.RawText
                && sender.TextValue.EndsWith(plugin.ClientState.LocalPlayer.Name.TextValue)) {
                return (long)plugin.ClientState.LocalContentId;
            }

            // otherwise find the player payload in the sestring
            PlayerPayload playerPayload = ResolvePlayerPayload(sender);
            if (playerPayload == null) return -1;

            // and match it to someone in the party with the same name
            foreach (PartyMember member in plugin.PartyList) {
                if (member.Name.TextValue == playerPayload.PlayerName && member.World.Id == playerPayload.World.RowId) {
                    return member.ContentId;
                }
            }
            PluginLog.Warning($"Unable to resolve PartyMember from SeString: {sender}");
            return -1;
        }

        private static PlayerPayload ResolvePlayerPayload(SeString seString) {
            return seString.Payloads.Find(p => p.Type == PayloadType.Player) as PlayerPayload;
        }

        // ==== hooks ====
        private void OnTerritoryChange(object _, ushort territoryId) {
            Reset();
        }

        private void OnUpdate(Framework f) {
            // combat state change tracking
            bool isInCombat = (plugin.ClientState.LocalPlayer?.StatusFlags & StatusFlags.InCombat) != 0;
            if (isInCombat != wasInCombatLastUpdate) {
                OnCombatStateChange(isInCombat);
            }
            wasInCombatLastUpdate = isInCombat;

            // all ready state change tracking
            bool isAllReady = plugin.PartyList.All(IsMemberReady) && plugin.PartyList.Length > 0;
            if (isAllReady != wasAllReadyLastUpdate) {
                OnReadyStateChange(isAllReady);
            }
            wasAllReadyLastUpdate = isAllReady;
        }

        private void OnChatMessage(XivChatType chatType, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
            // check for party chat
            if (chatType != XivChatType.Party) return;

            if (readyPattern.IsMatch(message.ToString())) {
                // if the sender is ready
                long member = ResolvePartyMemberFromSenderName(sender);
                if (member != -1) OnPartyMemberReady(member);
            } else if (unreadyPattern.IsMatch(message.ToString())) {
                // if the sender is unready
                long member = ResolvePartyMemberFromSenderName(sender);
                if (member != -1) OnPartyMemberUnready(member);
            }
        }

        private void OnNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
            if (!plugin.Configuration.ListenToGameReadyCheck) return;
            if (direction != NetworkMessageDirection.ZoneDown) return;
            if (opCode != Opcodes.EventPlay32) return;

            // get party member by actor id and update their ready state
            long actorId = Marshal.ReadInt64(dataPtr);
            int eventId = Marshal.ReadInt32(dataPtr + 0x8);
            PluginLog.Debug($"Got EventPlay32 from {actorId}, eventId={eventId}");

            if (eventId == Opcodes.EventIdReadyCheckReady) {
                OnPartyMemberReady(actorId);
            } else if (eventId == Opcodes.EventIdReadyCheckNotReady) {
                OnPartyMemberUnready(actorId);
            }
        }

        public void Dispose() {
            plugin.ClientState.TerritoryChanged -= OnTerritoryChange;
            plugin.Framework.Update -= OnUpdate;
            plugin.ChatGui.ChatMessage -= OnChatMessage;
            plugin.Network.NetworkMessage -= OnNetworkMessage;
        }
    }
}
