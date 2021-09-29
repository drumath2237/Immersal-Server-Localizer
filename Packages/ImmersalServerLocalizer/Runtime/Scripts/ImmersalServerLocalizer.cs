using ImmersalRESTLocalizer.Types;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.XR.ARFoundation;

namespace ImmersalRESTLocalizer
{
    public class ImmersalServerLocalizer : MonoBehaviour
    {
        [SerializeField] private ImmersalRESTConfiguration configuration;

        [SerializeField] private ARCameraManager cameraManager;

        [SerializeField] private Transform arSpace;

        [SerializeField] private Transform cameraTransform;

        private ImmersalRestClient _immersalRestClient;

        private void Start()
        {
            _immersalRestClient = new ImmersalRestClient(configuration);
        }

        public void Localize()
        {
            LocalizeAsync().Forget();
        }

        private async UniTask LocalizeAsync()
        {
            arSpace.position = Vector3.zero;
            arSpace.rotation = Quaternion.identity;

            var cameraMatrix = cameraTransform.localToWorldMatrix;

            if (!cameraManager.TryAcquireLatestCpuImage(out var image))
            {
                Debug.Log("cannot acquire cpu image");
                return;
            }

            var cameraTexture =
                await ARImageProcessingUtil.ConvertARCameraImageToTextureAsync(image,
                    this.GetCancellationTokenOnDestroy());
            image.Dispose();

            if (!cameraManager.TryGetIntrinsics(out var intrinsics))
            {
                Debug.Log("cannot acquire intrinsics");
                return;
            }

            var resText = await _immersalRestClient.SendRequestAsync(intrinsics, cameraTexture);

            var immersalResponse = JsonUtility.FromJson<ImmersalResponseParams>(resText);

            var immersalCameraMatrix = immersalResponse.ToMatrix4();
            var mapMatrix = cameraMatrix * immersalCameraMatrix.inverse * arSpace.localToWorldMatrix;

            arSpace.position = mapMatrix.GetColumn(3);
            arSpace.rotation = mapMatrix.rotation;
        }
    }
}