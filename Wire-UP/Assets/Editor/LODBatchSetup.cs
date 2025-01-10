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
            // LOD Group �߰�
            LODGroup lodGroup = go.GetComponent<LODGroup>();
            if (lodGroup == null)
                lodGroup = go.AddComponent<LODGroup>();

            Renderer[] childRenderers = go.GetComponentsInChildren<Renderer>();
            if (childRenderers == null || childRenderers.Length == 0)
            {
                // Renderer�� ���� ���ٸ� �Ѿ
                continue;
            }

            // LOD �迭 ���� (LOD 0 �ϳ� + Culled)
            LOD[] lods = new LOD[1];

            // (A) LOD0 ����
            lods[0] = new LOD(
                /* screenRelativeTransitionHeight: */ 0.1f,
                /* renderers: */ childRenderers
            );

            // 4) LOD �迭 ����
            lodGroup.SetLODs(lods);

            // 5) Bounds ���� (�� ũ�⡤��ġ �ݿ�)
            lodGroup.RecalculateBounds();
        }
    }
}
