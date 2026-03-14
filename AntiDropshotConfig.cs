using Rocket.API;

namespace AntiDropshot
{
    public class AntiDropshotConfig : IRocketPluginConfiguration
    {
        public byte StaminaCostCrouch;
        public byte StaminaCostProne;
        public float ProneDelaySeconds; // Защита от макросов: минимальное время в приседе перед падением

        public void LoadDefaults()
        {
            StaminaCostCrouch = 10;
            StaminaCostProne = 10;
            ProneDelaySeconds = 0.4f; // 400 мс достаточно, чтобы разорвать тайминги макроса
        }
    }
}
