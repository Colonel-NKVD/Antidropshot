using UnityEngine;
using SDG.Unturned;
using Rocket.Unturned.Player;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private float lastStanceChangeTime;
        private const float MIN_CROUCH_TIME = 0.5f; // Минимум 0.5 сек в приседе перед тем как лечь

        void Awake()
        {
            player = GetComponent<Player>();
            player.stance.onStanceUpdated += OnStanceUpdated;
        }

        private void OnStanceUpdated()
        {
            EPlayerStance currentStance = player.stance.stance;

            // ГАЙКА №1: Запрет на падение в прыжке/падении
            // Если игрок в воздухе и пытается лечь или сесть
            if (!player.movement.isGrounded && currentStance != EPlayerStance.STAND)
            {
                ForceStance(EPlayerStance.STAND);
                return;
            }

            // ГАЙКА №2: Контроль последовательности (Stand -> Crouch -> Prone)
            if (currentStance == EPlayerStance.PRONE)
            {
                // Если игрок попытался лечь прямо из стойки стоя/спринта
                // Или если он пробыл в приседе меньше установленного времени (защита от спама Z)
                if (Time.time - lastStanceChangeTime < MIN_CROUCH_TIME)
                {
                    ForceStance(EPlayerStance.CROUCH);
                }
            }
            
            // Фиксируем время последнего изменения на присед, чтобы начать отсчет для Prone
            if (currentStance == EPlayerStance.CROUCH)
            {
                lastStanceChangeTime = Time.time;
            }
        }

        private void ForceStance(EPlayerStance stance)
        {
            // Используем проверенный метод из вашего SuppressionSystem
            player.stance.checkStance(stance, true);
        }

        void OnDestroy()
        {
            if (player != null) player.stance.onStanceUpdated -= OnStanceUpdated;
        }
    }
}
