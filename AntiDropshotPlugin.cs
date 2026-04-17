using Rocket.API;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using HarmonyLib;
using Rocket.Core.Logging;

namespace AntiDropshot
{
    public class AntiDropshotPlugin : RocketPlugin<AntiDropshotConfig>
    {
        public static AntiDropshotPlugin Instance { get; private set; }
        private Harmony _harmony;

        protected override void Load()
        {
            Instance = this;

            // Инициализация Harmony
            _harmony = new Harmony("com.project.antidropshot");
            _harmony.PatchAll();

            U.Events.OnPlayerConnected += OnPlayerConnected;
            
            Logger.Log("--- ANTIDROPSHOT v1.0 Loaded ---");
            Logger.Log("Harmony patches applied successfully.");
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            
            if (_harmony != null)
                _harmony.UnpatchAll("com.project.antidropshot");
            
            Instance = null;
            Logger.Log("--- ANTIDROPSHOT Unloaded ---");
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            if (player.Player.gameObject.GetComponent<AntiDropshotComponent>() == null)
            {
                player.Player.gameObject.AddComponent<AntiDropshotComponent>();
            }
        }
    }
}
