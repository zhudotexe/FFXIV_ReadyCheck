using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.Plugin;
using XivCommon;

namespace ReadyCheck {
    public class Plugin : IDalamudPlugin {
        public string Name => "ReadyCheck";

        internal DalamudPluginInterface PluginInterface;
        internal Framework Framework;
        internal CommandManager CommandManager;
        internal ClientState ClientState;
        internal PartyList PartyList;
        internal ChatGui ChatGui;
        internal GameNetwork Network;
        internal XivCommonBase XivCommon;

        internal Configuration Configuration;
        internal PluginUI UI;
        internal PartyState PartyState;

        public Plugin(
            DalamudPluginInterface pluginInterface,
            Framework framework,
            CommandManager commandManager,
            ClientState clientState,
            PartyList partyList,
            ChatGui chatGui,
            GameNetwork network
        ) {
            PluginInterface = pluginInterface;
            Framework = framework;
            CommandManager = commandManager;
            ClientState = clientState;
            PartyList = partyList;
            ChatGui = chatGui;
            Network = network;
            XivCommon = new XivCommonBase();

            Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(this);
            UI = new PluginUI(this);
            pluginInterface.UiBuilder.Draw += DrawUI;
            pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            PartyState = new PartyState(this);

            commandManager.AddHandler("/pcheck", new CommandInfo(OnCommandReady) {
                HelpMessage = "Opens the ReadyCheck party list."
            });
        }

        public void Dispose() {
            UI.Dispose();
            PartyState.Dispose();
            XivCommon.Dispose();
            CommandManager.RemoveHandler("/pcheck");
        }

        private void OnCommandReady(string command, string args) {
            UI.Visible = true;
        }

        private void DrawUI() {
            UI.Draw();
        }

        private void DrawConfigUI() {
            UI.Visible = true;
        }
    }
}
