using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class Boss2Cutscene : MonoBehaviour
{
    [SerializeField] private CinemachineSequencerCamera _sequencerCam;
    [SerializeField] private Boss2Controller _boss2Controller;
    [SerializeField] private BreakablePlatform _breakablePlatform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _sequencerCam.Priority = 100;
        ManagerRegistry.Get<InputManager>().DisablePlayerInput();
        StartCoroutine(cutScene());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator cutScene()
    {
        yield return new WaitForSeconds(9f); // 컷씬 지속 시간
        _breakablePlatform._breakDelay = 4;
        ManagerRegistry.Get<InputManager>().EnablePlayerInput();
        _boss2Controller.StartBoss2();

    }
}
