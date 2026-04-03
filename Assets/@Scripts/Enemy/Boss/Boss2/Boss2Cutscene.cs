using UnityEngine;

public class Boss2Cutscene : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ManagerRegistry.Get<InputManager>().DisablePlayerInput();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
