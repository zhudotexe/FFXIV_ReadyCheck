using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace ReadyCheck {
    [Serializable]
    public class Configuration : IPluginConfiguration {
        [NonSerialized]
        private Plugin plugin;

        public int Version { get; set; } = 1;

        public bool ResetOnCombat { get; set; } = true;
        public bool HideDuringCombat { get; set; } = true;
        public bool StartCountdownWhenAllReady { get; set; } = true;
        public int CountdownDuration { get; set; } = 15;
        public string RunCommandWhenAllReady { get; set; } = "";

        public void Initialize(Plugin plugin) {
            this.plugin = plugin;
        }

        public void Save() {
            plugin.PluginInterface.SavePluginConfig(this);
        }
    }
}
