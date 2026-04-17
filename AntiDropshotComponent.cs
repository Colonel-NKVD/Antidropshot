using UnityEngine;
using SDG.Unturned;

namespace AntiDropshot
{
    public class AntiDropshotComponent : MonoBehaviour
    {
        private Player player;
        private float _crouchDuration = 0f;
        private bool _isCorrecting = false;
        private EPlayerStance _lastValidStance;

        void Awake()
        {
            player = GetComponent<Player>();
            _lastValidStance = player.stance.stance;
        }

        public void SilentlyAcceptStance(EPlayerStance targetStance)
        {
            _lastValidStance = targetStance;
            if (targetStance == EPlayerStance.CROUCH || targetStance == EPlayerStance.PRONE)
                _crouchDuration = AntiDropshotPlugin.Instance.Configuration.Instance.ProneDelaySeconds; 
            else
                _crouchDuration = 0f;
        }

        void FixedUpdate()
        {
            if (player == null || player.life.isDead) return;
            EPlayerStance currentStance = player.stance.stance;
            var config = AntiDropshotPlugin.Instance.Configuration.Instance;

            // 1. Блокировка в воздухе
            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND) ForceStanceImmediate(EPlayerStance.STAND);
                _lastValidStance = EPlayerStance.STAND;
                _crouchDuration = 0f;
                return;
            }

            // 2. Игнорирование переходов без смены типа или между бегом/ходьбой
            if (currentStance == _lastValidStance || 
                (currentStance == EPlayerStance.SPRINT && _lastValidStance == EPlayerStance.STAND) ||
                (currentStance == EPlayerStance.STAND && _lastValidStance == EPlayerStance.SPRINT))
            {
                if (currentStance == EPlayerStance.CROUCH) _crouchDuration += Time.fixedDeltaTime; 
                else _crouchDuration = 0f;
                _lastValidStance = currentStance;
                return; 
            }

            // 3. Проверка задержки (защита от макросов)
            bool isAllowed = true;
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_lastValidStance != EPlayerStance.CROUCH || _crouchDuration < config.ProneDelaySeconds) 
                    isAllowed = false; 
            }
            else if (currentStance == EPlayerStance.STAND || currentStance == EPlayerStance.SPRINT)
            {
                if (_lastValidStance == EPlayerStance.PRONE || (_lastValidStance == EPlayerStance.CROUCH && _crouchDuration < config.ProneDelaySeconds)) 
                    isAllowed = false; 
            }

            // 4. Применение санкций
            if (!isAllowed)
            {
                ForceStanceImmediate(EPlayerStance.CROUCH);
            }
            else
            {
                // Списание стамины (приведение byte к float)
                float cost = (currentStance == EPlayerStance.PRONE) ? (float)config.StaminaCostProne : (float)config.StaminaCostCrouch;
                
                if (currentStance != EPlayerStance.SPRINT && _lastValidStance != EPlayerStance.SPRINT)
                {
                    if (player.life.stamina >= cost) player.life.serverModifyStamina(-(ushort)cost); 
                }
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

        void OnDestroy() => player = null;
    }
}
