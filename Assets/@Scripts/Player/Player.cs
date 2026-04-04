using System;
using UnityEngine;

public enum LocomotionState { Idle, Jumping, Falling, Land } // 여기서의 Jumping은 오르고 있는 상태를 의미, Run 추가 예정.
public enum RecoilState { None, Recoiling }
public enum SkillState { None, Slow, Deadeye }

// 원래는 해당 상태가 되면 해당 함수 실행 (HandleStateChanged) 이런 식으로 해야 하는데 현재는 그냥 이렇게 둠.


[RequireComponent(typeof(PlayerAimer))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerMove))]
[RequireComponent(typeof(PlayerJump))]
[RequireComponent(typeof(Rigidbody2D))]


public class Player : MonoBehaviour
{

    public float moveSpeed = 5f;
    public float gravityOffDuration = 0.5f;
    public float dampingDuration = 0.1f;
    public float dampingValue = 8f;
    public float OriginalGravity { get; private set; }

    // 반동 뒤 점프 막기 위함
    public bool CanJump { get; set; } = true;

    // Temp
    [Header("Land")]
    public float landDuration = 0.1f;

    public LocomotionState CurrentLocomotion { get; private set; } = LocomotionState.Idle;
    public RecoilState CurrentRecoil { get; private set; } = RecoilState.None;
    public SkillState CurrentSkill { get; private set; } = SkillState.None;

    public void SetLocomotionState(LocomotionState state)
    {
        if (CurrentLocomotion == state) return;

        if (CurrentLocomotion == LocomotionState.Land && state != LocomotionState.Idle) return;

        CurrentLocomotion = state;

        OnLocomotionChanged?.Invoke(state);
    }

    public void SetRecoilState(RecoilState state)
    {
        if (CurrentRecoil == state) return;
        CurrentRecoil = state;
        OnRecoilStateChanged?.Invoke(state);
    }

    public void SetSkillState(SkillState state)
    {
        if (CurrentSkill == state) return;
        CurrentSkill = state;
        OnSkillStateChanged?.Invoke(state);
    }

    // 편의 프로퍼티
    public bool IsGrounded => CurrentLocomotion == LocomotionState.Idle || CurrentLocomotion == LocomotionState.Land;
    public bool IsRecoiling => CurrentRecoil == RecoilState.Recoiling;

    // metaUI 추가
    public bool IsSlowAirborne => !IsGrounded && CurrentSkill == SkillState.Slow;
    public bool IsMetaLensActive => IsSlowAirborne || CurrentSkill == SkillState.Deadeye;

    public PlayerAttack playerAttack{ get; private set; }
    public PlayerMove playerMove{ get; private set; }
    public PlayerAimer playerAimer { get; private set; }
    public PlayerJump playerJump { get; private set; }
    public PlayerHealth playerHealth { get; private set; }
    public DeadeyeSkill deadeyeSkill { get; private set; }

    Rigidbody2D _rb;

    // 이벤트
    public event Action<LocomotionState> OnLocomotionChanged;
    public event Action<RecoilState> OnRecoilStateChanged;
    public event Action<SkillState> OnSkillStateChanged;

    void Awake()
    {
        playerAttack= GetComponent<PlayerAttack>(); // 샷건, 주무기 반동 담당 (반동 시 중력 제어 추가 됨)
        playerMove= GetComponent<PlayerMove>(); // 좌우 이동 + 땅 여부 판단 담당 + Locomotion
        playerAimer = GetComponent<PlayerAimer>(); 
        playerJump = GetComponent<PlayerJump>(); // 점프 + 올라갈 때, 내려갈 때의 중력 담당
        playerHealth = GetComponent<PlayerHealth>();
        deadeyeSkill = GetComponent<DeadeyeSkill>();
        _rb = GetComponent<Rigidbody2D>();
        OriginalGravity = _rb.gravityScale;
    }

    public void ResetState()
    {
        SetLocomotionState(LocomotionState.Idle);
        SetRecoilState(RecoilState.None);
        SetSkillState(SkillState.None);
        CanJump = true;
    }
}
