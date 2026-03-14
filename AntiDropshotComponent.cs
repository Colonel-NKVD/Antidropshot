using UnityEngine;
using SDG.Unturned;
using Rocket.Unturned.Player;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private EPlayerStance lastValidStance;
        private float lastCrouchStartTime;
        private bool isInternalChanging = false;

        // Настройка задержки: 0.5 - 0.8 сек считается стандартом для тактических серверов
        private const float MIN_TIME_IN_CROUCH = 0.6f; 

        void Awake()
        {
            player = GetComponent<Player>();
            lastValidStance = player.stance.stance;
            
            // Подписка на нативное событие Unturned
            player.stance.onStanceUpdated += OnStanceUpdated;
        }

        private void OnStanceUpdated()
        {
            // Защита от рекурсии при вызове checkStance самим плагином
            if (isInternalChanging) return;

            EPlayerStance currentStance = player.stance.stance;

            // 1. Блокировка дропшота в воздухе (самая частая лазейка)
            if (!player.movement.isGrounded && currentStance != EPlayerStance.STAND)
            {
                ForceStance(EPlayerStance.STAND);
                return;
            }

            // 2. СТРОГИЙ ФИЛЬТР ПЕРЕХОДА В PRONE
            if (currentStance == EPlayerStance.PRONE)
            {
                // Если попытка лечь была НЕ из приседа
                if (lastValidStance != EPlayerStance.CROUCH)
                {
                    // Мгновенно сажаем игрока. Пакет перехватывается "секунда в секунду"
                    ForceStance(EPlayerStance.CROUCH);
                    return;
                }

                // Если игрок в приседе, но нажал "лечь" слишком быстро (защита от макросов)
                if (Time.time - lastCrouchStartTime < MIN_TIME_IN_CROUCH)
                {
                    ForceStance(EPlayerStance.CROUCH);
                    return;
                }
            }

            // Фиксируем время входа в присед для проверки задержки
            if (currentStance == EPlayerStance.CROUCH && lastValidStance != EPlayerStance.CROUCH)
            {
                lastCrouchStartTime = Time.time;
            }

            // Если проверка пройдена, обновляем последнюю валидную стойку
            lastValidStance = currentStance;
        }

        private void ForceStance(EPlayerStance targetStance)
        {
            isInternalChanging = true;
            // Использование метода из SuppressionSystem для надежной смены стойки
            player.stance.checkStance(targetStance, true);
            lastValidStance = targetStance;
            isInternalChanging = false;
        }

        void OnDestroy()
        {
            if (player != null && player.stance != null)
            {
                player.stance.onStanceUpdated -= OnStanceUpdated;
            }
        }
    }
}
