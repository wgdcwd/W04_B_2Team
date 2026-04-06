using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class Boss1Cutscene : MonoBehaviour
{
    [Header("Timeline")]
    public PlayableDirector director;

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera _walkPathCam;
    [SerializeField] private CinemachineSequencerCamera _sequencerCam;

    [Header("Settings")]
    [SerializeField] private float _sequencerDuration = 5f; // 인스펙터에서 조절
    [SerializeField] private Transform _player;

    [Header("Boss")]
    [SerializeField] private BossController _controller;

    [Header("Wall")]
    [SerializeField] private GameObject _wall;

    [Header("FadeImage")]
    [SerializeField] private Image _fadeIamge;

    private Vector2 _startPos;
    private bool _triggered = false;
    private PlayerAimer _playerAimer;
    private PlayerHeadRotate _playerHeadRotate;
    private Laser[] _playerLasers;

    void Start()
    {
        _startPos = _player.position;
        _playerAimer = _player != null ? _player.GetComponent<PlayerAimer>() : null;
        _playerHeadRotate = _player != null ? _player.GetComponent<PlayerHeadRotate>() : null;
        _playerLasers = _player != null ? _player.GetComponentsInChildren<Laser>(true) : new Laser[0];
        //_sequencerCam.Priority = 0; // 시작엔 비활성

        director.stopped += OnTimelineFinished;
    }

    void Update()
    {
        if (_triggered) return; // 컷씬 시작 후 Update 중단

        float dist = _player.position.x - _startPos.x;
        float t = Mathf.Clamp01(dist / 40f);
        _walkPathCam.Lens.OrthographicSize = Mathf.Lerp(2f, 8f, t);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (other.CompareTag("Player"))
        {
            ManagerRegistry.Get<InputManager>().DisablePlayerInput();
            other.gameObject.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero; // 플레이어 이동 멈춤
            SetCutscenePointerVisible(false);
            SetPlayerLookRightUp();
            StartCoroutine(FadeOutIn(1.0f)); // 페이드 효과 시작
            _triggered = true;
            _wall.SetActive(true);
        }
    }

    private void OnTimelineFinished(PlayableDirector pd)
    {

    }

    public void StartSequencerCam()
    {
        SetPlayerLookRightUp();
        _walkPathCam.Priority = 0; // 워크 카메라 끄기
        _sequencerCam.Priority = 20; // 시퀀서 카메라 활성화
    }


    public void OnCutsceneAllFinished()
    {
        ReleasePlayerLookLock();
        SetCutscenePointerVisible(true);
        ManagerRegistry.Get<InputManager>().EnablePlayerInput();
        // 여기에 게임 시작 코드 추가
        Debug.Log("컷씬 완료 - 게임 시작!");
        _controller?.StartBoss();

    }

    public void SetPlayerLookRightUp()
    {
        ApplyCutsceneLook(false);
    }

    public void SetPlayerLookLeftUp()
    {
        ApplyCutsceneLook(true);
    }

    public void ReleasePlayerLookLock()
    {
        _playerAimer?.UnlockCutsceneAim();
        _playerHeadRotate?.UnlockHeadRotation();
    }

    void OnDestroy()
    {
    }

    public IEnumerator FadeOutIn(float duration = 0.4f)
    {
        _fadeIamge.gameObject.SetActive(true);
        yield return _fadeIamge.DOFade(1f, 0.1f).WaitForCompletion();
        yield return new WaitForSeconds(0.4f);
        yield return _fadeIamge.DOFade(0f, duration).WaitForCompletion();
        _fadeIamge.gameObject.SetActive(false);

        director.Play();
    }

    void ApplyCutsceneLook(bool isLookingLeft)
    {
        bool shouldLookLeft = isLookingLeft;
        Vector2 aimDirection = Vector2.up;
        if (_player != null && _controller != null)
        {
            Vector2 toBoss = (Vector2)_controller.transform.position - (Vector2)_player.position;
            if (toBoss.sqrMagnitude > 0.001f)
            {
                aimDirection = toBoss.normalized;
                shouldLookLeft = toBoss.x < 0f;
            }
        }

        _playerAimer?.LockCutsceneAim(aimDirection, shouldLookLeft);
        _playerHeadRotate?.FlipHead(shouldLookLeft);
        _playerHeadRotate?.LockHeadToTarget(_controller != null ? _controller.transform : null);
    }

    void SetCutscenePointerVisible(bool visible)
    {
        if (_playerLasers == null)
            return;

        for (int i = 0; i < _playerLasers.Length; i++)
            _playerLasers[i]?.SetCutsceneHidden(!visible);
    }

}
