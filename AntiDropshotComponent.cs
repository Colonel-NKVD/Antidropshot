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

        private const float TRANSITION_TIME = 2.0f; 
        private const float STAMINA_COST = 10f;     

        void Awake()
        {
            player = GetComponent<Player>();
            _lastValidStance = player.stance.stance;
        }

        public void SilentlyAcceptStance(EPlayerStance targetStance)
        {
            _lastValidStance = targetStance;
            if (targetStance == EPlayerStance.CROUCH || targetStance == EPlayerStance.PRONE)
                _crouchDuration = TRANSITION_TIME; 
            else
                _crouchDuration = 0f;
        }

        void FixedUpdate()
        {
            if (player == null || player.life.isDead) return;
            EPlayerStance currentStance = player.stance.stance;

            if (!player.movement.isGrounded)
            {
                if (currentStance != EPlayerStance.STAND) ForceStanceImmediate(EPlayerStance.STAND);
                _lastValidStance = EPlayerStance.STAND;
                _crouchDuration = 0f;
                return;
            }

            if (currentStance == _lastValidStance || 
                (currentStance == EPlayerStance.SPRINT && _lastValidStance == EPlayerStance.STAND) ||
                (currentStance == EPlayerStance.STAND && _lastValidStance == EPlayerStance.SPRINT))
            {
                if (currentStance == EPlayerStance.CROUCH) _crouchDuration += Time.fixedDeltaTime; 
                else _crouchDuration = 0f;
                _lastValidStance = currentStance;
                return; 
            }

            bool isTransitionAllowed = true;
            if (currentStance == EPlayerStance.PRONE)
            {
                if (_lastValidStance != EPlayerStance.CROUCH || _crouchDuration < TRANSITION_TIME) isTransitionAllowed = false; 
            }
            else if (currentStance == EPlayerStance.STAND || currentStance == EPlayerStance.SPRINT)
            {
                if (_lastValidStance == EPlayerStance.PRONE || (_lastValidStance == EPlayerStance.CROUCH && _crouchDuration < TRANSITION_TIME)) isTransitionAllowed = false; 
            }

            if (!isTransitionAllowed) ForceStanceImmediate(EPlayerStance.CROUCH);
            else
            {
                if (currentStance != EPlayerStance.SPRINT && _lastValidStance != EPlayerStance.SPRINT)
                {
                    if (STAMINA_COST > 0 && player.life.stamina >= STAMINA_COST) player.life.serverModifyStamina(-STAMINA_COST); 
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
