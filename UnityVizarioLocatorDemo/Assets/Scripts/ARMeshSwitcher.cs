using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using TMPro;


public class ARMeshSwitcher : MonoBehaviour
{
    [SerializeField]
    private Material invisibleMeshMaterial = null;

    [SerializeField]
    private Material normalMeshMaterial = null;

    // [SerializeField]
    // private Material OcclusionMaterial = null;

    private bool IsVisible { get; set; } = true;

    private ARMeshManager _meshManager;
    public ARPlaneManager _arpm = null;
    public ARPlaneMeshVisualizer _arpmv = null;

    public void ToggleARMesh()
    {
        if (_meshManager)
        {
            var meshes = _meshManager.meshes;
            var meshPrefabRenderer = _meshManager.meshPrefab.GetComponent<MeshRenderer>();

            if (IsVisible)
            {
                meshPrefabRenderer.material = invisibleMeshMaterial;

                foreach (var mesh in meshes)
                {
                    mesh.GetComponent<MeshRenderer>().material = invisibleMeshMaterial;
                }
            }
            else
            {
                meshPrefabRenderer.material = normalMeshMaterial;
                foreach (var mesh in meshes)
                {
                    mesh.GetComponent<MeshRenderer>().material = normalMeshMaterial;
                }
            }
        }

        //plane manager
        if (_arpm != null)
        {
            var mr = _arpm.planePrefab.GetComponent<Renderer>();
            var mat = !IsVisible ? normalMeshMaterial : invisibleMeshMaterial;
            mr.material = mat;
            mr.materials[0] = mat;

            foreach(var plane in _arpm.trackables)
            {
                foreach(var r in plane.GetComponents<Renderer>())
                {
                    r.material = mat;
                }
                //plane.GetComponent<Renderer>().material = mat;
            }
        }

        if(_arpmv != null)
        {
            //_arpmv.enabled = !_arpmv.enabled;  
        }
        IsVisible = !IsVisible;
    }



    void Awake()
    {
        _meshManager = GetComponent<ARMeshManager>();
    }
}
