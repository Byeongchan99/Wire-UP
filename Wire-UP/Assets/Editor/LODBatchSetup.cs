using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LODBatchSetup : MonoBehaviour
{
    [MenuItem("Tools/Batch LOD Setup")]
    static void SetupLODs()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        foreach (GameObject go in selectedObjects)
        {
            // LOD Group 추가
            LODGroup lodGroup = go.GetComponent<LODGroup>();
            if (lodGroup == null)
                lodGroup = go.AddComponent<LODGroup>();

            Renderer[] childRenderers = go.GetComponentsInChildren<Renderer>();
            if (childRenderers == null || childRenderers.Length == 0)
            {
                // Renderer가 전혀 없다면 넘어감
                continue;
            }

            // LOD 배열 생성 (LOD 0 하나 + Culled)
            LOD[] lods = new LOD[1];

            // (A) LOD0 설정
            lods[0] = new LOD(
                /* screenRelativeTransitionHeight: */ 0.1f,
                /* renderers: */ childRenderers
            );

            // 4) LOD 배열 적용
            lodGroup.SetLODs(lods);

            // 5) Bounds 재계산 (모델 크기·위치 반영)
            lodGroup.RecalculateBounds();
        }
    }
}
