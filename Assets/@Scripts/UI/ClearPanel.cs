using System;
using UnityEngine;

public class ClearPanel : MonoBehaviour
{
    [SerializeField] private GameObject _panel;

    private void Start()
    {
        Init();
    }
    private void Init()
    {
        _panel.gameObject.SetActive(GameManager.Instance.isClear);
    }
}
