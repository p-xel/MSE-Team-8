using UnityEngine;
using Fusion;

public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;
    private PlayerHand _playerHand;
    private GameManager _roundManager;
    private NetworkBehaviourId _lastShotTargetId;

    void Start()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        _playerHand = GetComponentInParent<PlayerHand>();
        if (_playerHand == null)
        {
            _playerHand = GetComponentInChildren<PlayerHand>();
        }
        if (_playerHand == null)
        {
            _playerHand = GetComponent<PlayerHand>();
        }
    }

    void Update()
    {
        if (_playerHand == null)
        {
            _playerHand = GetComponentInParent<PlayerHand>();
            if (_playerHand == null)
            {
                _playerHand = GetComponentInChildren<PlayerHand>();
            }
            if (_playerHand == null)
            {
                _playerHand = GetComponent<PlayerHand>();
            }
        }

        if (_playerHand == null || _playerHand.Id == default) return;

        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        if (_animator == null) return;

        if (_roundManager == null)
        {
            _roundManager = FindAnyObjectByType<GameManager>();
        }

        if (_roundManager != null)
        {
            NetworkBehaviourId currentShotTarget = _roundManager.shotTargetHandId;
            if (currentShotTarget != _lastShotTargetId)
            {
                _lastShotTargetId = currentShotTarget;
                if (currentShotTarget == _playerHand.Id)
                {
                    TriggerHitAnimation();
                }
            }
        }
    }

    private void TriggerHitAnimation()
    {
        foreach (var param in _animator.parameters)
        {
            if (param.name.Equals("Hit", System.StringComparison.OrdinalIgnoreCase))
            {
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    _animator.SetTrigger(param.name);
                }
                else if (param.type == AnimatorControllerParameterType.Bool)
                {
                    _animator.SetBool(param.name, true);
                    StartCoroutine(ResetHitBool(param.name));
                }
                break;
            }
        }
    }

    private System.Collections.IEnumerator ResetHitBool(string paramName)
    {
        yield return new WaitForSeconds(0.5f);
        if (_animator != null)
        {
            _animator.SetBool(paramName, false);
        }
    }
}
