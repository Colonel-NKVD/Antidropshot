using UnityEngine;
using SDG.Unturned;
using Rocket.Unturned.Player;
using System.Collections;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private EPlayerStance lastValidStance;
        private float crouchStartTime;
        private bool isInternalChanging = false;

        // Константы баланса
        private const float PRONE_DELAY = 3.0f; // Обязательное время в приседе (сек)

        void Awake()
        {
            player = GetComponent<Player>();
            lastValidStance = player.stance.stance;
            player.stance.onStanceUpdated += OnStanceUpdated;
        }

        private void OnStanceUpdated()
        {
            if (isInternalChanging) return;

            EPlayerStance currentStance = player.stance.stance;

            // 1. Блокировка любых прыжков/падений (Grounded check)
            if (!player.movement.isGrounded && currentStance != EPlayerStance.STAND)
            {
                ExecuteForcedStance(EPlayerStance.STAND);
                return;
            }

            // 2. Логика входа в состояние PRONE (Лежа)
            if (currentStance == EPlayerStance.PRONE)
            {
                // Если игрок НЕ был в приседе до этого момента
                if (lastValidStance != EPlayerStance.CROUCH)
                {
                    // Прямой переход Stand -> Prone запрещен. Сажаем силой.
                    crouchStartTime = Time.time;
                    ExecuteForcedStance(EPlayerStance.CROUCH);
                    return;
                }

                // Если был в приседе, но не выждал 3 секунды
                float timeInCrouch = Time.time - crouchStartTime;
                if (timeInCrouch < PRONE_DELAY)
                {
                    // Отбрасываем обратно в присед "секунда в секунду"
                    ExecuteForcedStance(EPlayerStance.CROUCH);
                    return;
                }
            }

            // 3. Фиксация начала приседа
            if (currentStance == EPlayerStance.CROUCH && lastValidStance != EPlayerStance.CROUCH)
            {
                crouchStartTime = Time.time;
            }

            lastValidStance = currentStance;
        }

        private void ExecuteForcedStance(EPlayerStance target)
        {
            isInternalChanging = true;
            // Используем нативный метод с приоритетом сервера (reliable = true)
            // Аналогично логике из вашего SuppressionSystem
            player.stance.checkStance(target, true); 
            lastValidStance = target;
            isInternalChanging = false;
        }

        void OnDestroy()
        {
            if (player != null && player.stance != null)
                player.stance.onStanceUpdated -= OnStanceUpdated;
        }
    }
}
