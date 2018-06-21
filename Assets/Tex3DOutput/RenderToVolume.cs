using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RenderToVolume : MonoBehaviour {
    public ComputeShader SliceCS;

    static ComputeShader _slicer;
    static Mesh _mesh;
    static int _voxelSize = 128;
    static string _volAssetPathName = "Assets/Tex3DOutput/Vol.asset";  // raw
    static string _volAssetPathName1 = "Assets/Tex3DOutput/Vol1.asset";  // raw
    private void Awake() {
        _mesh = GetComponent<MeshFilter>().sharedMesh;
        _slicer = SliceCS;
    }

    static RenderTexture Copy3DSliceToRenderTexture(RenderTexture source, int layer) {
        RenderTexture render = new RenderTexture(_voxelSize, _voxelSize, 0, RenderTextureFormat.ARGB32);
        render.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        render.enableRandomWrite = true;
        render.wrapMode = TextureWrapMode.Clamp;
        render.Create();
        
        int kernelIndex = _slicer.FindKernel("CSMain");
        _slicer.SetTexture(kernelIndex, "voxels", source);
        _slicer.SetInt("layer", layer);
        _slicer.SetTexture(kernelIndex, "Result", render);
        _slicer.Dispatch(kernelIndex, _voxelSize, _voxelSize, 1);

        return render;
    }

    static Texture2D ConvertFromRenderTexture(RenderTexture rt) {
        Texture2D output = new Texture2D(_voxelSize, _voxelSize);
        RenderTexture.active = rt;
        output.ReadPixels(new Rect(0, 0, _voxelSize, _voxelSize), 0, 0);
        output.Apply();
        return output;
    }

    static void Save(RenderTexture rtVol) {
        //ProjectWindowUtil.CreateAsset(rtVol, volAssetPathName1);
        
        RenderTexture[] layers = new RenderTexture[_voxelSize];
        for (int i = 0; i < _voxelSize; i++)
            layers[i] = Copy3DSliceToRenderTexture(rtVol, i);

        Texture2D[] finalSlices = new Texture2D[_voxelSize];
        for (int i = 0; i < _voxelSize; i++)
            finalSlices[i] = ConvertFromRenderTexture(layers[i]);

        Texture3D output = new Texture3D(_voxelSize, _voxelSize, _voxelSize, TextureFormat.ARGB32, false);
        output.filterMode = FilterMode.Trilinear;
        Color[] outputPixels = output.GetPixels();

        for (int k = 0; k < _voxelSize; k++) {
            Color[] layerPixels = finalSlices[k].GetPixels();
            for (int i = 0; i < _voxelSize; i++)
                for (int j = 0; j < _voxelSize; j++) {
                    outputPixels[i + j * _voxelSize + k * _voxelSize * _voxelSize] = layerPixels[i + j * _voxelSize];
                }
        }

        output.SetPixels(outputPixels);
        output.Apply();

        AssetDatabase.CreateAsset(output, _volAssetPathName);
    }

    // Update is called once per frame
    [MenuItem("Assets/Createt Vol Asset")]
    static void ExportVol () {
        RenderTexture rtVol = RenderTexture.GetTemporary(_voxelSize, _voxelSize, 0, RenderTextureFormat.ARGBFloat);
        rtVol.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        rtVol.enableRandomWrite = true;
        rtVol.volumeDepth = _voxelSize;
        Shader.SetGlobalFloat("volumeResolution", _voxelSize);
        Shader.SetGlobalVector("volumeParams", new Vector4(0, 0, 0, 1));

        RenderToVolume go = FindObjectOfType<RenderToVolume>();
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial.SetPass(0);
        if (_mesh == null) {
            _mesh = go.GetComponent<MeshFilter>().sharedMesh;
        }
        if (_slicer == null) {
            _slicer = go.SliceCS;
        }

        Graphics.ClearRandomWriteTargets();
        Graphics.SetRandomWriteTarget(1, rtVol);
        //Graphics.DrawMeshNow(_mesh, Vector3.zero, Quaternion.identity); // This does not work
        Graphics.Blit(null, rtVol, mr.sharedMaterial);
        Graphics.ClearRandomWriteTargets();

        Save(rtVol);
        RenderTexture.ReleaseTemporary(rtVol);
    }
}
