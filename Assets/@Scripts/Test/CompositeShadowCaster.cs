using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CompositeCollider2D))]
public class TilemapShadowGenerator : MonoBehaviour
{
    private CompositeCollider2D _compositeCollider;
    private List<GameObject> _shadowObjects = new List<GameObject>();

    void Start()
    {
        _compositeCollider = GetComponent<CompositeCollider2D>();
        GenerateShadows();
    }

    public void GenerateShadows()
    {
        // 기존에 생성된 그림자 오브젝트 삭제
        foreach (var obj in _shadowObjects)
        {
            if (obj != null) Destroy(obj);
        }
        _shadowObjects.Clear();

        // Composite Collider에서 분리된 경로(섬)의 개수만큼 반복
        for (int i = 0; i < _compositeCollider.pathCount; i++)
        {
            Vector2[] pathVertices = new Vector2[_compositeCollider.GetPathPointCount(i)];
            _compositeCollider.GetPath(i, pathVertices);

            // 각 경로마다 새로운 Shadow Caster 2D 오브젝트 생성
            GameObject shadowObj = new GameObject($"ShadowCaster_{i}");
            shadowObj.transform.SetParent(transform, false);
            _shadowObjects.Add(shadowObj);

            ShadowCaster2D caster = shadowObj.AddComponent<ShadowCaster2D>();
            caster.selfShadows = false; // 타일맵 내부가 검게 변하는 것 방지

            // 리플렉션을 이용해 경로 데이터 주입
            UpdateShadowPath(caster, pathVertices);
        }
    }

    private void UpdateShadowPath(ShadowCaster2D caster, Vector2[] vertices)
    {
        var field = typeof(ShadowCaster2D).GetField("m_ShapePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Vector3[] v3Path = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++) v3Path[i] = vertices[i];
        field.SetValue(caster, v3Path);
    }
}