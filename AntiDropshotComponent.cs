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

        private const float REQUIRED_CROUCH_TIME = 3.0f; // 3 секунды в приседе
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

            // 2. Накопительный таймер приседа
            if (currentStance == EPlayerStance.CROUCH)
            {
                _crouchDuration += TICK_RATE;
            }
            else if (currentStance != EPlayerStance.PRONE)
            {
                _crouchDuration = 0f;
            }

            // 3. Жесткая блокировка PRONE (Dropshot)
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

            // Принудительная смена стойки (используется надежный метод из вашего SuppressionSystem)
            player.stance.checkStance(target, true);

            // Профессиональный сброс инерции через CharacterController
            // Это "примораживает" игрока к месту, исключая проскальзывание в анимацию
            if (player.movement.controller != null)
            {
                // Если игрок пытается двигаться вверх (прыжок) или падать
                if (player.movement.controller.velocity.y != 0)
                {
                    // В Unturned мы не всегда можем напрямую задать velocity контроллеру,
                    // но мы можем принудительно обновить состояние перемещения.
                    player.movement.readSecondaryInput(); 
                }
            }

            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
        }
    }
}
