using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMove))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] Animator _animator;
    [SerializeField] SpriteRenderer _spriteRenderer;

    bool _isLookingLeft;
    bool _isMovingLeft;

    public bool IsLookingLeft => _isLookingLeft;

    public void GetGoundCheck(bool _isGrounded)
    {
        _animator.SetBool("isJumping", !_isGrounded);
    }

    public void SetWalkByMovment(float xSpeed)
    {
        float absSpeed = Mathf.Abs(xSpeed);
        _animator.SetFloat("speed", absSpeed);

        if (absSpeed < 0.1) return;

        if (xSpeed > 0.1f)
        {
            _isMovingLeft = true;
        }
        else if (xSpeed < -0.1f)
        {
            _isMovingLeft = false;
        }

        bool _isFacingSame = (_isMovingLeft == _isLookingLeft);
        _animator.SetBool("isFacingSame", _isFacingSame);
    }

    public void FlipSpriteByInput(float xInput)
    {
        if (Mathf.Abs(xInput) < 0.1f) return;

        if (xInput > 0.1f)
        {
            _isLookingLeft = false;
        }
        else if (xInput < -0.1f)
        {
            _isLookingLeft = true;
        }

        _spriteRenderer.flipX = _isLookingLeft;
    }

    public void FlipSprite(bool isLookingLeft)
    {
        _isLookingLeft = isLookingLeft;
        _spriteRenderer.flipX = _isLookingLeft;
    }
}
