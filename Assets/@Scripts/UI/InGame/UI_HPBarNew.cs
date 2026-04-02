using UnityEngine;
using UnityEngine.UI;

public class UI_HPBarNew : UI_Base
{
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private Slider _playerHpSlider;

    private int _currentHp;
    private int _maxHp;

    // 슬라이더
    // maxhp로나눈다
    // 개별로 관리
    // 애니메이션은 무적시간동안 깜빡이다가 이후에 줄어듦

    protected override void BindEvents()
    {

    }
}
