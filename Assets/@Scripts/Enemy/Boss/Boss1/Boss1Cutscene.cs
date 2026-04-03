using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

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

    private Vector2 _startPos;
    private bool _triggered = false;

    void Start()
    {
        _startPos = _player.position;
        //_sequencerCam.Priority = 0; // 시작엔 비활성

        director.stopped += OnTimelineFinished;
    }

    //void Update()
    //{
    //    if (_triggered) return; // 컷씬 시작 후 Update 중단

    //    float dist = _player.position.x - _startPos.x;
    //    float t = Mathf.Clamp01(dist / 40f);
    //    _walkPathCam.Lens.OrthographicSize = Mathf.Lerp(5f, 15f, t);
    //}

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (other.CompareTag("Player"))
        {
            _triggered = true;
            ManagerRegistry.Get<InputManager>().DisablePlayerInput();
            director.Play();
        }
    }

    private void OnTimelineFinished(PlayableDirector pd)
    {
        _walkPathCam.Priority = 0; // 워크 카메라 끄기
        _sequencerCam.Priority = 20; // 시퀀서 카메라 활성화
        StartCoroutine(WaitForSequencer());
    }

    private System.Collections.IEnumerator WaitForSequencer()
    {
        yield return new WaitForSeconds(_sequencerDuration);
        OnCutsceneAllFinished();
    }

    private void OnCutsceneAllFinished()
    {
        ManagerRegistry.Get<InputManager>().EnablePlayerInput();
        // 여기에 게임 시작 코드 추가
        Debug.Log("컷씬 완료 - 게임 시작!");
    }

    void OnDestroy()
    {
        director.stopped -= OnTimelineFinished;
    }
}