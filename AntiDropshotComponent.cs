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

        // Константы тайминга (3 секунды в приседе)
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

            // 1. Блокировка стоек в воздухе (Ground Lock)
            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND)
                {
                    ForceStanceImmediate(EPlayerStance.STAND);
                }
                _crouchDuration = 0f;
                return;
            }

            // 2. Логика накопительного таймера приседа
            if (currentStance == EPlayerStance.CROUCH)
            {
                _crouchDuration += TICK_RATE;
            }
            else if (currentStance != EPlayerStance.PRONE)
            {
                // Если игрок встал (STAND/SPRINT) - обнуляем прогресс полностью
                _crouchDuration = 0f;
            }

            // 3. Жесткая блокировка PRONE (Dropshot)
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_crouchDuration < REQUIRED_CROUCH_TIME)
                {
                    // Игрок не выждал 3 секунды - возвращаем в CROUCH мгновенно
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                }
            }
        }

        private void ForceStanceImmediate(EPlayerStance target)
        {
            if (_isCorrecting) return;
            _isCorrecting = true;

            // Используем надежный метод, проверенный в SuppressionSystem.
            // Второй аргумент 'true' (force) заставляет сервер принудительно 
            // отправить пакет синхронизации клиенту.
            player.stance.checkStance(target, true);

            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
        }
    }
}
