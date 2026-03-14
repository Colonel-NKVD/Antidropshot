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

            // 2. Логика накопительного таймера
            if (currentStance == EPlayerStance.CROUCH)
            {
                _crouchDuration += TICK_RATE;
            }
            else if (currentStance != EPlayerStance.PRONE)
            {
                _crouchDuration = 0f;
            }

            // 3. Блокировка PRONE (Дропшот)
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

            // Самый надежный способ из вашего SuppressionSystem
            // Флаг 'force = true' заставляет сервер принудительно обновить состояние у клиента.
            player.stance.checkStance(target, true);

            // Если анимация все еще "проскакивает", мы принудительно сбрасываем 
            // локальный таймер обновления стойки в компоненте.
            // Это гарантированно публичное поле в классе PlayerStance.
            player.stance.lastStance = Time.time;

            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
        }
    }
}
