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

        // Константы тайминга (3 секунды обязательного приседа)
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

            // 1. Блокировка любых действий в воздухе (Ground Lock)
            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND)
                {
                    ForceStanceImmediate(EPlayerStance.STAND);
                }
                _crouchDuration = 0f;
                return;
            }

            // 2. Накопительный таймер приседа
            if (currentStance == EPlayerStance.CROUCH)
            {
                _crouchDuration += TICK_RATE;
            }
            else if (currentStance != EPlayerStance.PRONE)
            {
                // Сброс прогресса, если игрок встал в полный рост или побежал
                _crouchDuration = 0f;
            }

            // 3. Жесткая блокировка PRONE (Dropshot)
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_crouchDuration < REQUIRED_CROUCH_TIME)
                {
                    // Игрок попытался лечь раньше времени — возвращаем в присед
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                }
            }
        }

        private void ForceStanceImmediate(EPlayerStance target)
        {
            if (_isCorrecting) return;
            _isCorrecting = true;

            // Метод 1: Серверная валидация (как в вашем SuppressionSystem)
            player.stance.checkStance(target, true);

            // Метод 2: Прямая сетевая команда клиенту
            // Это заставляет сервер немедленно отправить пакет 'tellStance' игроку,
            // что мгновенно прерывает анимацию падения на стороне клиента.
            player.stance.askStance(target);

            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
        }
    }
}
