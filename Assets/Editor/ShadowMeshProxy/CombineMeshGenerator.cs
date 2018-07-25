using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class CombineMeshGenerator
{
    List<MeshFilter> meshFilters;
    List<SkinnedMeshRenderer> skinnedRenderers;
    private GameObject asset;
    private string meshPath = "Assets/Resources/ShadowProxyObject.asset";

    private struct CombineInstanceMaterial
    {
        public CombineInstance combine;
        public Material material;
        public Mesh sharedMesh;
    }

    [MenuItem("Tools/CombinedMesh")]
    public static void CombinedMesh()
    {
        CombineMeshGenerator combineMeshGenerator  = new CombineMeshGenerator();
        combineMeshGenerator.GenerateCombinedMesh();
    }

    private void GenerateCombinedMesh()
    {
        AutoPopulateFiltersAndRenderers();
        bool combined = false;
        Mesh combinedMesh = GenerateCombinedMesh(out combined);
        Mesh existingMesh = null;
        if (combined)
        {
            if ((existingMesh = (Mesh)AssetDatabase.LoadAssetAtPath(meshPath, typeof(Mesh))))
            {
                TransferMesh(combinedMesh, existingMesh);
                combinedMesh = existingMesh;
            }
            else
            {
                existingMesh = new Mesh() { name = combinedMesh.name };
                TransferMesh(combinedMesh, existingMesh);
                combinedMesh = existingMesh;
                AssetDatabase.CreateAsset(existingMesh, meshPath);
            }
        }
    }

    private void TransferMesh(Mesh from, Mesh to)
    {
        to.vertices = from.vertices;
        to.subMeshCount = from.subMeshCount;
        for (int i = 0; i < from.subMeshCount; i++)
        {
            to.SetTriangles(from.GetTriangles(i), i);
        }
        to.normals = from.normals;
        to.tangents = from.tangents;
        to.colors = from.colors;
        to.uv = from.uv;
        to.uv2 = from.uv2;
        to.uv3 = from.uv3;
        to.uv4 = from.uv4;
    }

    private void AutoPopulateFiltersAndRenderers()
    {
        asset = GameObject.Find("UnShadowObjects");
        meshFilters = new List<MeshFilter>();
        skinnedRenderers = new List<SkinnedMeshRenderer>();
        MeshFilter[] filtersInPrefab = asset.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < filtersInPrefab.Length; i++)
        {
            if (meshFilters.Contains(filtersInPrefab[i]) == false)
            {
                meshFilters.Add(filtersInPrefab[i]);
            }
        }
        SkinnedMeshRenderer[] renderers = asset.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (skinnedRenderers.Contains(renderers[i]) == false)
            {
                skinnedRenderers.Add(renderers[i]);
            }
        }
    }

    private Mesh GenerateCombinedMesh(out bool combined)
    {
        return GenerateCombinedMesh(meshFilters, skinnedRenderers, out combined);
    }

    private Mesh GenerateCombinedMesh(List<MeshFilter> filters, List<SkinnedMeshRenderer> renderers, out bool combined)
    {
        int totalMeshes = filters.Count + renderers.Count;
        combined = false;
        if (totalMeshes == 1)
        {
            foreach (MeshFilter mf in filters)
            {
                return mf.sharedMesh;
            }
            foreach (SkinnedMeshRenderer sr in renderers)
            {
                return sr.sharedMesh;
            }
        }
        List<Mesh> tempMeshes = new List<Mesh>();
        List<CombineInstanceMaterial> combineInstances = new List<CombineInstanceMaterial>();
        foreach (MeshFilter mf in filters)
        {
            Material[] materials = new Material[0];
            if (mf.GetComponent<MeshRenderer>())
            {
                materials = mf.GetComponent<MeshRenderer>().sharedMaterials.Where(q => q != null).ToArray();
            }
            for (int i = 0; i < mf.sharedMesh.subMeshCount; i++)
            {
                combineInstances.Add(new CombineInstanceMaterial()
                {
                    combine = new CombineInstance()
                    {
                        mesh = mf.sharedMesh,
                        transform = mf.transform.localToWorldMatrix,
                        subMeshIndex = i
                    },
                    material = materials.Length > i ? materials[i] : null,
                    sharedMesh = mf.sharedMesh,
                });
            }
        }
        foreach (SkinnedMeshRenderer sr in renderers)
        {
            Material[] materials = sr.sharedMaterials.Where(q => q != null).ToArray();

            for (int i = 0; i < sr.sharedMesh.subMeshCount; i++)
            {
                Mesh t = new Mesh();
                sr.BakeMesh(t);
                tempMeshes.Add(t);
                var m = sr.transform.localToWorldMatrix;
                Matrix4x4 scaledMatrix = Matrix4x4.TRS(MatrixUtils.GetPosition(m), MatrixUtils.GetRotation(m), Vector3.one);
                combineInstances.Add(new CombineInstanceMaterial()
                {
                    combine = new CombineInstance()
                    {
                        mesh = t,
                        transform = scaledMatrix,
                        subMeshIndex = i
                    },
                    material = materials.Length > i ? materials[i] : null,
                    sharedMesh = sr.sharedMesh,
                });
            }
        }
        Dictionary<Material, Mesh> materialMeshes = new Dictionary<Material, Mesh>();
        Mesh mesh = null;
        while (combineInstances.Count > 0)
        {
            Material cMat = combineInstances[0].material;
            var combines = combineInstances.Where(q => q.material == cMat).Select(q => q.combine).ToArray();
            mesh = new Mesh();
            mesh.CombineMeshes(combines, true, true);
            materialMeshes.Add(cMat, mesh);
            tempMeshes.Add(mesh);
            combineInstances.RemoveAll(q => q.material == cMat);
        }
        CombineInstance[] finalCombines = materialMeshes.Select(q => new CombineInstance() { mesh = q.Value }).ToArray();
        mesh = new Mesh();
        mesh.CombineMeshes(finalCombines, true, false);
        combined = true;
        foreach (Mesh m in tempMeshes)
        {
            UnityEngine.GameObject.DestroyImmediate(m);
        }
        return mesh;
    }
}
