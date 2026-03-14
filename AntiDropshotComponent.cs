using UnityEngine;
using SDG.Unturned;
using Rocket.Unturned.Player;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private float _crouchDuration = 0f;
        private bool _isCorrecting = false;

        // Константы тайминга
        private const float REQUIRED_CROUCH_TIME = 3.0f; 
        private const float TICK_RATE = 0.02f;

        void Awake()
        {
            player = GetComponent<Player>();
        }

        void FixedUpdate()
        {
            if (player == null || player.life.isDead) return;

            EPlayerStance currentStance = player.stance.stance;

            // 1. Блокировка в воздухе
            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND)
                {
                    ForceStanceImmediate(EPlayerStance.STAND);
                }
                _crouchDuration = 0f;
                return;
            }

            // 2. Таймер приседа
            if (currentStance == EPlayerStance.CROUCH)
            {
                _crouchDuration += TICK_RATE;
            }
            else if (currentStance != EPlayerStance.PRONE)
            {
                _crouchDuration = 0f;
            }

            // 3. Блокировка дропшота (Prone)
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_crouchDuration < REQUIRED_CROUCH_TIME)
                {
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                }
            }
        }

        private void ForceStanceImmediate(EPlayerStance target)
        {
            if (_isCorrecting) return;
            _isCorrecting = true;

            // ШАГ 1: Серверная валидация (как в вашем SuppressionSystem)
            player.stance.checkStance(target, true);

            // ШАГ 2: Мгновенное сетевое уведомление. 
            // Это заставляет сервер разослать пакет об изменении стойки "прямо сейчас",
            // что прерывает интерполяцию анимации на стороне клиента.
            player.stance.askStance(target);

            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
        }
    }
}
