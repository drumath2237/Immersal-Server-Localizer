using ImmersalRESTLocalizer.Types;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine.XR.ARFoundation;

namespace ImmersalRESTLocalizer
{
    public class ImmersalServerLocalizer : MonoBehaviour
    {
        [SerializeField] private ImmersalRESTConfiguration configuration;

        [SerializeField] private TextMeshProUGUI logText;

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
            arSpace.rotation = quaternion.identity;

            var cameraMatrix = cameraTransform.localToWorldMatrix;

            if (!cameraManager.TryAcquireLatestCpuImage(out var image))
            {
                logText.text += "cannot acquire image\n";
                return;
            }

            var cameraTexture = ARImageProcessingUtil.ConvertARCameraImageToTexture(image);
            image.Dispose();

            if (!cameraManager.TryGetIntrinsics(out var intrinsics))
            {
                logText.text += "cannot acquire intrinsics\n";
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