using UnityEngine;
using SDG.Unturned;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private float _crouchDuration = 0f;
        private bool _isCorrecting = false;
        
        // Храним только ту стойку, которая была официально "одобрена" плагином
        private EPlayerStance _lastValidStance;

        // Настройки баланса
        private const float TRANSITION_TIME = 3.0f; 
        private const byte STAMINA_COST = 10;

        void Awake()
        {
            player = GetComponent<Player>();
            _lastValidStance = player.stance.stance;
        }

        void FixedUpdate()
        {
            if (player == null || player.life.isDead) return;

            EPlayerStance currentStance = player.stance.stance;

            // 1. БЛОКИРОВКА В ВОЗДУХЕ (Ground Lock)
            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND) ForceStanceImmediate(EPlayerStance.STAND);
                _lastValidStance = EPlayerStance.STAND;
                _crouchDuration = 0f;
                return;
            }

            // 2. ОПТИМИЗАЦИЯ: Если игрок не меняет позу, просто считаем время (если он сидит)
            if (currentStance == _lastValidStance)
            {
                if (currentStance == EPlayerStance.CROUCH)
                {
                    // Используем системное время Unity для точности вместо жесткого TICK_RATE
                    _crouchDuration += Time.fixedDeltaTime; 
                }
                return; // Прерываем выполнение, лишние проверки не нужны
            }

            // === 3. ЛОГИКА ИЗМЕНЕНИЯ ПОЗЫ ===
            bool isTransitionAllowed = true;

            // А) Игрок пытается лечь (PRONE)
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_lastValidStance != EPlayerStance.CROUCH || _crouchDuration < TRANSITION_TIME)
                {
                    isTransitionAllowed = false; // Не отсидел 3 секунды в приседе
                }
            }
            // Б) Игрок пытается встать (STAND)
            else if (currentStance == EPlayerStance.STAND)
            {
                if (_lastValidStance == EPlayerStance.PRONE || (_lastValidStance == EPlayerStance.CROUCH && _crouchDuration < TRANSITION_TIME))
                {
                    isTransitionAllowed = false; // Пытается вскочить из положения лежа или не досидел 3 секунды
                }
            }

            // === 4. ПРИМЕНЕНИЕ РЕЗУЛЬТАТА ===
            if (!isTransitionAllowed)
            {
                // Если переход запрещен, жестко возвращаем в предыдущую легальную позу (в CROUCH)
                ForceStanceImmediate(EPlayerStance.CROUCH);
            }
            else
            {
                // Переход разрешен!

                // Списываем стамину ТОЛЬКО за успешную смену позы
                if (STAMINA_COST > 0 && player.life.stamina >= STAMINA_COST)
                {
                    // askTire — стандартный метод Unturned для "усталости". Работает безотказно.
                    player.life.askTire(STAMINA_COST); 
                }

                // Обновляем валидную стойку и сбрасываем таймер для нового состояния
                _lastValidStance = currentStance;
                _crouchDuration = 0f; 
            }
        }

        private void ForceStanceImmediate(EPlayerStance target)
        {
            if (_isCorrecting) return;
            _isCorrecting = true;
            player.stance.checkStance(target, true);
            _isCorrecting = false;
        }

        void OnDestroy()
        {
            player = null;
        }
    }
}
