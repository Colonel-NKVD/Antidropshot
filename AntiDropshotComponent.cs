using UnityEngine;
using SDG.Unturned;

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
            if (isProcessing) return;

            EPlayerStance currentStance = player.stance.stance;
            var config = AntiDropshotPlugin.Instance.Configuration.Instance;

            // Логика перехода в ПРИСЯД
            if (currentStance == EPlayerStance.CROUCH && IsStandingOrRunning(lastStance))
            {
                if (player.life.stamina >= config.StaminaCostCrouch)
                {
                    player.life.askTire(config.StaminaCostCrouch);
                    lastCrouchTime = Time.realtimeSinceStartup;
                }
                else
                {
                    RevertStance(EPlayerStance.STAND);
                    return;
                }
            }
            // Логика перехода в ПОЛОЖЕНИЕ ЛЕЖА
            else if (currentStance == EPlayerStance.PRONE)
            {
                bool cameDirectlyFromStand = IsStandingOrRunning(lastStance);
                bool isMacroOrTooFast = (Time.realtimeSinceStartup - lastCrouchTime) < config.ProneDelaySeconds;

                // Если игрок падает прямо из стойки стоя/на бегу, или не выждал паузу в приседе
                if (cameDirectlyFromStand || (lastStance == EPlayerStance.CROUCH && isMacroOrTooFast))
                {
                    RevertStance(EPlayerStance.CROUCH);

                    // Если прыгнул напрямую из Stand, нужно списать стамину за пропущенный Crouch
                    if (cameDirectlyFromStand)
                    {
                        if (player.life.stamina >= config.StaminaCostCrouch)
                        {
                            player.life.askTire(config.StaminaCostCrouch);
                            lastCrouchTime = Time.realtimeSinceStartup;
                        }
                        else
                        {
                            RevertStance(EPlayerStance.STAND);
                        }
                    }
                    return; // Прерываем дальнейшую обработку, так как стойка изменена
                }
                // Легальный переход из приседа
                else if (lastStance == EPlayerStance.CROUCH && !isMacroOrTooFast)
                {
                    if (player.life.stamina >= config.StaminaCostProne)
                    {
                        player.life.askTire(config.StaminaCostProne);
                    }
                    else
                    {
                        RevertStance(EPlayerStance.CROUCH);
                        return;
                    }
                }
            }

            lastStance = currentStance;
        }

        private bool IsStandingOrRunning(EPlayerStance stance)
        {
            return stance == EPlayerStance.STAND || 
                   stance == EPlayerStance.SPRINT || 
                   stance == EPlayerStance.RUN || 
                   stance == EPlayerStance.JUMP;
        }

        private void RevertStance(EPlayerStance fallbackStance)
        {
            isProcessing = true;
            player.stance.checkStance(fallbackStance, true);
            lastStance = fallbackStance;
            isProcessing = false;
        }
    }
}
