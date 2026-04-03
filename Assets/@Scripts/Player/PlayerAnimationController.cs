using UnityEngine;

[RequireComponent(typeof(PlayerMove))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] Animator _animator;
    [SerializeField] SpriteRenderer _spriteRenderer;

    bool _isLookingLeft;
    bool _isMovingLeft;

    public void GetGoundCheck(bool _isGrounded)
    {
        _animator.SetBool("isJumping", !_isGrounded);
    }

    public void SetWalkByMovment(float xSpeed)
    {
        float absSpeed = Mathf.Abs(xSpeed);
        _animator.SetFloat("speed", absSpeed);

        if (absSpeed > 0.1) return;

        if (xSpeed > 0.1f)
        {
            _isMovingLeft = false;
        }
        else if (xSpeed < -0.1f)
        {
            _isMovingLeft = true;
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
}
