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

        // Настройки
        private const float TRANSITION_TIME = 3.0f; // 3 секунды в приседе
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

            // 1. СПИСАНИЕ СТАМИНЫ (Исправлено для CS0200)
            if (currentStance != _lastFrameStance)
            {
                if (player.life.stamina >= STAMINA_COST)
                    player.life.stamina -= STAMINA_COST;
                else
                    player.life.stamina = 0;

                // Синхронизируем стамину с клиентом
                player.life.sendRevive(); 
                
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

            // 3. ТАЙМЕР ПРИСЕДА
            if (currentStance == EPlayerStance.CROUCH)
            {
                _crouchDuration += TICK_RATE;
            }
            else if (currentStance != EPlayerStance.PRONE)
            {
                _crouchDuration = 0f;
            }

            // 4. КОНТРОЛЬ ПЕРЕХОДОВ (ШЛЮЗ)

            // А) Логика "Приземления" (Вход в PRONE)
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_lastValidStance != EPlayerStance.CROUCH || _crouchDuration < TRANSITION_TIME)
                {
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                    return;
                }
            }

            // Б) Логика "Подъема" (Выход в STAND)
            if (currentStance == EPlayerStance.STAND && _lastValidStance == EPlayerStance.PRONE)
            {
                // Запрещаем мгновенный подъем из лежа в стоя. Только через присед.
                ForceStanceImmediate(EPlayerStance.CROUCH);
                return;
            }

            if (currentStance == EPlayerStance.STAND && _lastValidStance == EPlayerStance.CROUCH)
            {
                // Если мы пытаемся встать, но еще не "отсидели" 3 секунды после того как лежали
                // Примечание: если игрок просто присел и хочет встать, это тоже займет 3 сек.
                if (_crouchDuration < TRANSITION_TIME)
                {
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                    return;
                }
            }

            // Обновляем валидное состояние, если все проверки пройдены
            _lastValidStance = currentStance;
        }

        private void ForceStanceImmediate(EPlayerStance target)
        {
            if (_isCorrecting) return;
            _isCorrecting = true;

            // Используем надежный метод смены стойки сервером
            player.stance.checkStance(target, true);

            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
        }
    }
}
