using Rocket.API;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Logger = Rocket.Core.Logging.Logger;

namespace AntiDropshot
{
    public class AntiDropshotPlugin : RocketPlugin<AntiDropshotConfig>
    {
        public static AntiDropshotPlugin Instance { get; private set; }

        protected override void Load()
        {
            Instance = this;
            U.Events.OnPlayerConnected += OnPlayerConnected;
            
            Logger.Log("--- ANTIDROPSHOT v1.0 Loaded ---");
            Logger.Log($"Crouch Cost: {Configuration.Instance.StaminaCostCrouch} | Prone Cost: {Configuration.Instance.StaminaCostProne}");
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
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
