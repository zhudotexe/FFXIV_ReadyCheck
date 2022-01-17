using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Party;
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
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
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

            if (ImGui.BeginTable("partytable", 2, ImGuiTableFlags.SizingFixedFit)) {
                foreach (PartyMember member in plugin.PartyList) {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"{member.Name} ({member.ContentId})");

                    ImGui.TableNextColumn();
                    ImGui.Text(plugin.PartyState.IsMemberReady(member) ? "ready" : "not ready");
                }
                ImGui.EndTable();
            }


            ImGui.Separator();

            if (ImGui.Button("Reset")) {
                plugin.PartyState.Reset();
            }

            ImGui.EndTabItem();
        }

        private void DrawConfigTab() {
            if (!ImGui.BeginTabItem("Configuration")) return;

            // reset on combat start
            bool resetOnCombat = plugin.Configuration.ResetOnCombat;
            if (ImGui.Checkbox("Reset on Combat Start", ref resetOnCombat))
                plugin.Configuration.ResetOnCombat = resetOnCombat;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Reset the ready check state as soon as combat starts.");

            // hide during combat
            bool hideDuringCombat = plugin.Configuration.HideDuringCombat;
            if (ImGui.Checkbox("Hide During Combat", ref hideDuringCombat))
                plugin.Configuration.HideDuringCombat = hideDuringCombat;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Hide the plugin window during combat.");

            // start countdown when all ready
            bool startCountdown = plugin.Configuration.StartCountdownWhenAllReady;
            if (ImGui.Checkbox("Start Countdown When All Ready", ref startCountdown))
                plugin.Configuration.StartCountdownWhenAllReady = startCountdown;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Start a countdown as soon as all players in the party are ready.");

            // countdown duration
            if (plugin.Configuration.StartCountdownWhenAllReady) {
                int countdownDuration = plugin.Configuration.CountdownDuration;
                if (ImGui.InputInt("Countdown Duration", ref countdownDuration))
                    plugin.Configuration.CountdownDuration = Math.Clamp(countdownDuration, 5, 30);
            }

            // run command when all ready
            string runCommand = plugin.Configuration.RunCommandWhenAllReady;
            if (ImGui.InputText("Run Command When All Ready", ref runCommand, 2000))
                plugin.Configuration.RunCommandWhenAllReady = runCommand;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("NOTE: This setting will only run commands added by other Dalamud plugins.");

            ImGui.EndTabItem();
        }
    }
}
