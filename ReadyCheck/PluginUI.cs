using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using ImGuiNET;

namespace ReadyCheck {
    class PluginUI : IDisposable {
        private readonly Plugin plugin;

        private bool visible = false;

        public PluginUI(Plugin plugin) {
            this.plugin = plugin;
        }

        public bool Visible {
            get => visible;
            set => visible = value;
        }

        public void Dispose() {
            // we don't need to do anything here
        }

        public void Draw() {
            if (!Visible
                || plugin.Configuration.HideDuringCombat && (plugin.ClientState.LocalPlayer?.StatusFlags & StatusFlags.InCombat) != 0) {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(450, 210), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("ReadyCheck", ref visible,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                // tab bar
                if (ImGui.BeginTabBar("rctabs")) {
                    DrawCheckStatusTab();
                    DrawConfigTab();
                    ImGui.EndTabBar();
                    ImGui.Separator();
                }
            }
            ImGui.End();
        }

        private void DrawCheckStatusTab() {
            if (!ImGui.BeginTabItem("Status")) return;

            foreach (var member in plugin.PartyList) {
                ImGui.Text($"{member.Name} ({member.ContentId})");
            }
        }

        private static void DrawConfigTab() {
            if (!ImGui.BeginTabItem("Configuration")) return;
            // todo
            ImGui.Text("content goes here");
        }
    }
}
