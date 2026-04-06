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

    void Start()
    {
        _startPos = _player.position;
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
        _walkPathCam.Priority = 0; // 워크 카메라 끄기
        _sequencerCam.Priority = 20; // 시퀀서 카메라 활성화
    }


    public void OnCutsceneAllFinished()
    {
        ManagerRegistry.Get<InputManager>().EnablePlayerInput();
        // 여기에 게임 시작 코드 추가
        Debug.Log("컷씬 완료 - 게임 시작!");
        _controller?.StartBoss();

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

}