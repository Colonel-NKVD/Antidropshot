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
        private EPlayerStance _lastFrameStance;
        private EPlayerStance _lastValidStance;

        // Настройки баланса
        private const float TRANSITION_REQUIRED_TIME = 3.0f; 
        private const byte STAMINA_COST = 10;
        private const float TICK_RATE = 0.02f;

        void Awake()
        {
            player = GetComponent<Player>();
            _lastFrameStance = player.stance.stance;
            _lastValidStance = _lastFrameStance;
        }

        void FixedUpdate()
        {
            if (player == null || player.life.isDead) return;

            EPlayerStance currentStance = player.stance.stance;

            // 1. СПИСАНИЕ СТАМИНЫ (Профессиональный метод)
            if (currentStance != _lastFrameStance)
            {
                // consumeStamina — это публичный метод, который безопасно 
                // вычитает стамину и синхронизирует состояние.
                player.life.consumeStamina(STAMINA_COST);
                _lastFrameStance = currentStance;
            }

            // 2. БЛОКИРОВКА В ВОЗДУХЕ
            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND)
                {
                    ForceStanceImmediate(EPlayerStance.STAND);
                }
                _crouchDuration = 0f;
                return;
            }

            // 3. ОБРАБОТКА ТАЙМЕРА ПРИСЕДА
            if (currentStance == EPlayerStance.CROUCH)
            {
                _crouchDuration += TICK_RATE;
            }
            else if (currentStance != EPlayerStance.PRONE)
            {
                _crouchDuration = 0f;
            }

            // 4. ЛОГИКА "ШЛЮЗА" (МЕДЛЕННЫЙ ПОДЪЕМ И ПАДЕНИЕ)
            
            // Защита от дропшота (вход в PRONE)
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_lastValidStance != EPlayerStance.CROUCH || _crouchDuration < TRANSITION_REQUIRED_TIME)
                {
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                    return;
                }
            }

            // Защита от быстрого вставания (выход в STAND)
            if (currentStance == EPlayerStance.STAND)
            {
                // Если игрок лежал или еще не "отсидел" 3 секунды в приседе
                if (_lastValidStance == EPlayerStance.PRONE || (_lastValidStance == EPlayerStance.CROUCH && _crouchDuration < TRANSITION_REQUIRED_TIME))
                {
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                    return;
                }
            }

            _lastValidStance = currentStance;
        }

        private void ForceStanceImmediate(EPlayerStance target)
        {
            if (_isCorrecting) return;
            _isCorrecting = true;

            // Нативный метод из вашего SuppressionSystem
            player.stance.checkStance(target, true);

            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
        }
    }
}
