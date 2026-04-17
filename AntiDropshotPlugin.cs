using Rocket.API;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using HarmonyLib;
using Logger = Rocket.Core.Logging.Logger;

namespace AntiDropshot
{
    public class AntiDropshotPlugin : RocketPlugin<AntiDropshotConfig>
    {
        public static AntiDropshotPlugin Instance { get; private set; }
        private Harmony _harmony;

        protected override void Load()
        {
            Instance = this;

            // Инициализация Harmony для работы патчей
            _harmony = new Harmony("com.project.antidropshot");
            _harmony.PatchAll();

            U.Events.OnPlayerConnected += OnPlayerConnected;
            
            Logger.Log("--- ANTIDROPSHOT v1.0 Loaded ---");
            Logger.Log("Harmony patches applied successfully.");
            Logger.Log($"Crouch Cost: {Configuration.Instance.StaminaCostCrouch} | Prone Cost: {Configuration.Instance.StaminaCostProne}");
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            
            // Снимаем патчи при выгрузке
            _harmony.UnpatchAll("com.project.antidropshot");
            
            Instance = null;
            Logger.Log("--- ANTIDROPSHOT Unloaded ---");
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            // Навешиваем компонент контроля стоек на каждого нового игрока
            if (player.Player.gameObject.GetComponent<AntiDropshotComponent>() == null)
            {
                player.Player.gameObject.AddComponent<AntiDropshotComponent>();
            }
        }
    }
}
