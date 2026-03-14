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

            // 1. Блокировка в воздухе (Ground Lock)
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

            // 3. Блокировка PRONE
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

            // ШАГ 1: Серверная смена стойки (надежный канал)
            // Метод из вашего SuppressionSystem
            player.stance.checkStance(target, true);

            // ШАГ 2: Принудительный запрос на перепроверку стойки. 
            // Это заставляет сервер немедленно обработать изменение и отправить пакет клиенту.
            player.stance.askCheckStance(target);

            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
        }
    }
}
