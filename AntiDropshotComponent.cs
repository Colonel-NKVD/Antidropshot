using UnityEngine;
using SDG.Unturned;
using Rocket.Unturned.Player;
using System.Collections;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private float _crouchDuration = 0f;
        private bool _isCorrecting = false;
        
        // Константы для жесткой настройки
        private const float REQUIRED_CROUCH_TIME = 3.0f; // Обязательно 3 секунды в приседе
        private const float TICK_RATE = 0.02f; // Частота FixedUpdate

        void Awake()
        {
            player = GetComponent<Player>();
        }

        // Мы используем FixedUpdate, чтобы опережать сетевую репликацию Unturned
        void FixedUpdate()
        {
            if (player == null || player.life.isDead) return;

            EPlayerStance currentStance = player.stance.stance;

            // ГАЙКА 1: Проверка нахождения в воздухе (Ground Lock)
            // Если игрок не на земле, он обязан быть в STAND. Любая попытка сменить - мгновенный возврат.
            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND)
                {
                    ForceStanceImmediate(EPlayerStance.STAND);
                }
                _crouchDuration = 0f; // Сбрасываем таймер приседа в воздухе
                return;
            }

            // ГАЙКА 2: Логика накопительного таймера приседа
            if (currentStance == EPlayerStance.CROUCH)
            {
                _crouchDuration += TICK_RATE;
            }
            else if (currentStance != EPlayerStance.PRONE)
            {
                // Если игрок встал (STAND/SPRINT), таймер сгорает мгновенно
                _crouchDuration = 0f;
            }

            // ГАЙКА 3: Жесткая блокировка PRONE (Дропшот)
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_crouchDuration < REQUIRED_CROUCH_TIME)
                {
                    // Игрок не "отсидел" положенные 3 секунды. 
                    // Возвращаем в CROUCH мгновенно, не дожидаясь завершения кадра.
                    ForceStanceImmediate(EPlayerStance.CROUCH);
                }
            }
        }

        /// <summary>
        /// Самый быстрый способ смены стойки в RocketMod/Unturned.
        /// Использование флага 'reliable: true' заставляет сервер приоритетно 
        /// перезаписать состояние на стороне клиента.
        /// </summary>
        private void ForceStanceImmediate(EPlayerStance target)
        {
            if (_isCorrecting) return;
            
            _isCorrecting = true;
            
            // Нативный вызов, используемый в SuppressionSystem
            player.stance.checkStance(target, true);
            
            // Дополнительная мера: сброс скорости прыжка, чтобы нельзя было 
            // использовать "дельфинчика" (прыжок + лечь)
            if (target == EPlayerStance.CROUCH)
            {
                player.movement.jump(); // Это заставляет сервер пересчитать позицию
            }

            _isCorrecting = false;
        }
    }
}
