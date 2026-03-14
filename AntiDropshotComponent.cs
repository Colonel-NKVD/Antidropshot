using UnityEngine;
using SDG.Unturned;
using Rocket.Unturned.Player;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private EPlayerStance lastStance;
        private float lastCrouchTime;
        private bool isProcessing = false;

        void Awake()
        {
            player = GetComponent<Player>();
            lastStance = player.stance.stance;
            
            // Подписка на событие изменения стойки через нативный API Unturned
            player.stance.onStanceUpdated += OnStanceUpdated;
        }

        void OnDestroy()
        {
            if (player != null && player.stance != null)
            {
                player.stance.onStanceUpdated -= OnStanceUpdated;
            }
        }

        private void OnStanceUpdated()
        {
            // Блокировка рекурсии при принудительной смене стойки плагином
            if (isProcessing) return;

            EPlayerStance currentStance = player.stance.stance;
            var config = AntiDropshotPlugin.Instance.Configuration.Instance;

            // 1. Игнорируем проверку, если игрок в воде или в транспорте (защита от багов)
            if (currentStance == EPlayerStance.DRIVING || currentStance == EPlayerStance.SWIM)
            {
                lastStance = currentStance;
                return;
            }

            // 2. Логика перехода в ПРИСЯД (Crouch)
            if (currentStance == EPlayerStance.CROUCH && IsStandingOrRunning(lastStance))
            {
                if (player.life.stamina >= config.StaminaCostCrouch)
                {
                    player.life.askTire(config.StaminaCostCrouch);
                    lastCrouchTime = Time.realtimeSinceStartup;
                }
                else
                {
                    // Если стамины нет, не даем присесть (возвращаем в Stand)
                    RevertStance(EPlayerStance.STAND);
                    return;
                }
            }
            
            // 3. Логика перехода в положение ЛЕЖА (Prone)
            else if (currentStance == EPlayerStance.PRONE)
            {
                bool cameDirectlyFromStand = IsStandingOrRunning(lastStance);
                float timeInCrouch = Time.realtimeSinceStartup - lastCrouchTime;
                bool isTooFast = timeInCrouch < config.ProneDelaySeconds;

                // Защита: нельзя лечь сразу из Stand или слишком быстро после Crouch (анти-макрос)
                if (cameDirectlyFromStand || (lastStance == EPlayerStance.CROUCH && isTooFast))
                {
                    // Принудительно сажаем игрока
                    RevertStance(EPlayerStance.CROUCH);

                    // Если это был прыжок или бег (прямой дропшот), списываем стамину за присед
                    if (cameDirectlyFromStand)
                    {
                        if (player.life.stamina >= config.StaminaCostCrouch)
                        {
                            player.life.askTire(config.StaminaCostCrouch);
                            lastCrouchTime = Time.realtimeSinceStartup;
                        }
                        else
                        {
                            // Если стамины совсем нет, возвращаем в полный рост
                            RevertStance(EPlayerStance.STAND);
                        }
                    }
                    return;
                }
                // Легальный переход: игрок уже сидел и выждал задержку
                else if (lastStance == EPlayerStance.CROUCH && !isTooFast)
                {
                    if (player.life.stamina >= config.StaminaCostProne)
                    {
                        player.life.askTire(config.StaminaCostProne);
                    }
                    else
                    {
                        // Недостаточно стамины чтобы лечь — оставляем сидеть
                        RevertStance(EPlayerStance.CROUCH);
                        return;
                    }
                }
            }

            lastStance = currentStance;
        }

        /// <summary>
        /// Проверка, находится ли игрок в вертикальном положении (стоя или бег).
        /// В Unturned прыжки также происходят в этих состояниях.
        /// </summary>
        private bool IsStandingOrRunning(EPlayerStance stance)
        {
            return stance == EPlayerStance.STAND || 
                   stance == EPlayerStance.SPRINT;
        }

        /// <summary>
        /// Безопасная принудительная смена стойки игрока сервером.
        /// </summary>
        private void RevertStance(EPlayerStance fallbackStance)
        {
            isProcessing = true;
            player.stance.checkStance(fallbackStance, true);
            lastStance = fallbackStance;
            isProcessing = false;
        }
    }
}
