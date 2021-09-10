using System;
using System.Text;
using ImmersalRESTLocalizer.Types;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ImmersalRESTLocalizerTest
{
    public class ImmersalServerLocalizer : MonoBehaviour
    {
        [SerializeField] private ImmersalRESTConfiguration configuration;

        [SerializeField] private TextMeshProUGUI logText;

        [SerializeField] private ARCameraManager cameraManager;

        [SerializeField] private Transform arSpace;

        [SerializeField] private Transform cameraTransform;

        private Texture2D lastARCameraImage;

        private string _token;

        private MapId[] _mapIds;

        private void Start()
        {
            if (configuration != null)
            {
                _token = configuration.token;
                _mapIds = configuration.MapIds;
            }

        }

        private void OnEnable()
        {
            cameraManager.frameReceived += OnFrameReceived;
        }

        private void OnDisable()
        {
            cameraManager.frameReceived -= OnFrameReceived;
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
            
            var resText = await SendRequestAsync(lastARCameraImage);

            var immersalResponse = JsonUtility.FromJson<ImmersalResponseParams>(resText);

            var immersalCameraMatrix = CalcImmersalCameraMatrix(immersalResponse);
            var mapMatrix = cameraMatrix * immersalCameraMatrix.inverse * arSpace.localToWorldMatrix;

            arSpace.position = mapMatrix.GetColumn(3);
            arSpace.rotation = mapMatrix.rotation;
        }

        private void OnFrameReceived(ARCameraFrameEventArgs args)
        {
            // TryGetCameraImageTextureAsync().ContinueWith(result =>
            // {
            //     var (isSuccess, texture) = result;
            //     if (!isSuccess)
            //     {
            //         logText.text += "failed get camera image\n";
            //         return;
            //     }
            //
            //     lastARCameraImage = texture;
            // });
            

            if (!cameraManager.TryAcquireLatestCpuImage(out var image))
            {
                logText.text += "failed to get image\n";
                return;
            }

            logText.text += "success get image\n";
            
            image.Dispose();


        }

        bool TryAcquireCameraImageTextureSync(out Texture2D texture)
        {
            if (cameraManager.TryAcquireLatestCpuImage(out var image))
            {
                texture = null;
                return false;
            }

            texture = null;
            return true;
        }

        private async UniTask<(bool, Texture2D)> TryGetCameraImageTextureAsync()
        {
            if (!cameraManager.TryAcquireLatestCpuImage(out var image))
            {
                return (false, null);
            }

            var conversionParams = new XRCpuImage.ConversionParams
            {
                transformation = XRCpuImage.Transformation.MirrorX,
                outputFormat = TextureFormat.RGBA32,
                inputRect = new RectInt(0,0,image.width, image.height),
                outputDimensions = new Vector2Int(image.width, image.height),
            };

            using var conversionTask = image.ConvertAsync(conversionParams);

            await UniTask.WaitWhile(() => !conversionTask.status.IsDone(),
                PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());

            if (conversionTask.status != XRCpuImage.AsyncConversionStatus.Ready)
            {
                Debug.LogError("conversion task failed");
                return (false, null);
            }

            var texture2d = new Texture2D(
                conversionTask.conversionParams.outputDimensions.x,
                conversionTask.conversionParams.outputDimensions.y,
                conversionTask.conversionParams.outputFormat,
                false);

            texture2d.LoadRawTextureData(conversionTask.GetData<byte>());
            texture2d.Apply();

            return (true, texture2d);
        }

        private async UniTask<string> SendRequestAsync(Texture2D texture)
        {
            var base64 = Convert.ToBase64String(texture.EncodeToPNG());

            if (!cameraManager.TryGetIntrinsics(out var intrinsics))
            {
                logText.text += "cannot get intrinsics\n";
                return "";
            }

            var reqParams = new ImmersalRequestParams
            {
                b64 = base64,
                fx = intrinsics.focalLength.x,
                fy = intrinsics.focalLength.y,
                ox = intrinsics.principalPoint.x,
                oy = intrinsics.principalPoint.y,
                mapIds = _mapIds,
                token = _token
            };

            var request = new UnityWebRequest("https://api.immersal.com/localizeb64", "POST");
            byte[] byteRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(reqParams));
            request.uploadHandler = new UploadHandlerRaw(byteRaw);

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            try
            {
                var res = await request.SendWebRequest();
                logText.text = res.downloadHandler.text;

                return res.downloadHandler.text;
            }
            catch (Exception e)
            {
                logText.text = e.ToString();
                return request.error;
            }
        }

        private static Matrix4x4 CalcImmersalCameraMatrix(ImmersalResponseParams iParams)
        {
            var mat = new Matrix4x4
            {
                m00 = iParams.r00,
                m01 = iParams.r01,
                m02 = iParams.r02,
                m10 = iParams.r10,
                m11 = iParams.r11,
                m12 = iParams.r12,
                m20 = iParams.r20,
                m21 = iParams.r21,
                m22 = iParams.r22,

                m03 = iParams.px,
                m13 = iParams.py,
                m23 = iParams.pz,

                m33 = 1
            };

            return mat;
        }
    }
}