using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VGltf;
using VGltf.Unity;

namespace WebglVGltfSample
{
    public sealed class LoaderSample : MonoBehaviour
    {
        sealed class GltfResource : IDisposable
        {
            public IImporterContext Context;
            public GameObject Go;

            public void Dispose()
            {
                if (Go != null)
                {
                    GameObject.Destroy(Go);
                }
                Context?.Dispose();
            }
        }

        [SerializeField] OrbitCamera _camera;
        [SerializeField] DataContainerFromJs _dataContainerFromJs;

        IDisposable _currentModel;

        // Start is called before the first frame update
        void Start()
        {
        }

        void OnDestroy()
        {
            _currentModel?.Dispose();
        }

        // called from index.html
        public void LoadFromContainer()
        {
            var b64 = _dataContainerFromJs.Build();

            async UniTaskVoid Exec()
            {
                try
                {
                    var bytes = Convert.FromBase64String(b64);
                    using(var s = new MemoryStream(bytes))
                    {
                        await LoadAndReplaceModel(s);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            Exec().Forget();
        }

        async UniTask LoadAndReplaceModel(Stream s)
        {
            var d = await LoadGltf(s);
            _currentModel?.Dispose();
            _currentModel = d;

            _camera.mTarget = d.Go;
        }

        static async UniTask<GltfResource> LoadGltf(Stream s)
        {
            var gltfContainer = GltfContainer.FromGlb(s); // Task cannot be used on WebGL platform...

            var res = new GltfResource();
            try
            {
                // Create a GameObject that points to root nodes in the glTF scene.
                // The GameObject of the glTF's child Node will be created under this object.
                var go = new GameObject();
                go.name = "glb";

                res.Go = go;

                // Create a glTF Importer for Unity.
                // The resources will be cached in the internal Context of this Importer.
                // Resources can be released by calling Dispose of the Importer (or the internal Context).
                var timeSlicer = new TimeSlicer();
                using (var gltfImporter = new Importer(gltfContainer, timeSlicer))
                {
                    // Load the Scene.
                    res.Context = await gltfImporter.ImportSceneNodes(System.Threading.CancellationToken.None);
                }

                foreach (var rootNodeIndex in gltfContainer.Gltf.RootNodesIndices)
                {
                    var rootNode = res.Context.Resources.Nodes[rootNodeIndex];
                    rootNode.Value.transform.SetParent(go.transform, false);
                }
            }
            catch (Exception)
            {
                res.Dispose();
                throw;
            }

            return res;
        }
    }
}
