using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;

namespace ReadyCheck {
    public class PartyState : IDisposable {
        private readonly Plugin plugin;
        private bool wasInCombatLastUpdate = false;
        private bool wasAllReadyLastUpdate = false;

        // map of content id => is ready
        private Dictionary<long, bool> readyStates = new Dictionary<long, bool>();

        public PartyState(Plugin plugin) {
            this.plugin = plugin;
            this.plugin.ClientState.TerritoryChanged += OnTerritoryChange;
            this.plugin.Framework.Update += OnUpdate;
        }

        public void Reset() {
            readyStates.Clear();
        }

        private void OnCombatStateChange(bool isInCombat) {
            if (plugin.Configuration.ResetOnCombat && isInCombat) {
                Reset();
            }
        }

        private void OnReadyStateChange(bool isAllReady) {
            if (plugin.Configuration.StartCountdownWhenAllReady && isAllReady) {
                // todo
            }

            if (plugin.Configuration.RunCommandWhenAllReady != "" && isAllReady) {
                // todo
            }
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
            bool isAllReady = plugin.PartyList.All(member => readyStates.TryGetValue(member.ContentId, out bool memberIsReady) && memberIsReady);
            if (isAllReady != wasAllReadyLastUpdate) {
                OnReadyStateChange(isAllReady);
            }
            wasAllReadyLastUpdate = isAllReady;
        }

        public void Dispose() {
            plugin.ClientState.TerritoryChanged -= OnTerritoryChange;
            plugin.Framework.Update -= OnUpdate;
        }
    }
}
