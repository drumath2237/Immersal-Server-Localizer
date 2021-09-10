using System;
using System.Linq;
using System.Text;
using System.Threading;
using ImmersalRESTLocalizer.Types;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

            var cameraTexture = ConvertARCameraImageToTexture(image);
            
            image.Dispose();

            var resText = await SendRequestAsync(cameraTexture);

            var immersalResponse = JsonUtility.FromJson<ImmersalResponseParams>(resText);

            var immersalCameraMatrix = CalcImmersalCameraMatrix(immersalResponse);
            var mapMatrix = cameraMatrix * immersalCameraMatrix.inverse * arSpace.localToWorldMatrix;

            arSpace.position = mapMatrix.GetColumn(3);
            arSpace.rotation = mapMatrix.rotation;
        }

        private static unsafe Texture2D ConvertARCameraImageToTexture(XRCpuImage image)
        {
            var conversionParams = new XRCpuImage.ConversionParams
            {
                transformation = XRCpuImage.Transformation.MirrorX,
                outputFormat = TextureFormat.RGBA32,
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width, image.height),
            };

            var size = image.GetConvertedDataSize(conversionParams);
            using var buffer = new NativeArray<byte>(size, Allocator.Temp);
            image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);

            var texture = new Texture2D(
                conversionParams.outputDimensions.x,
                conversionParams.outputDimensions.y,
                conversionParams.outputFormat,
                false
            );
            texture.LoadRawTextureData(buffer);
            texture.Apply();

            return texture;
        }

        private static async UniTask<Texture2D> ConvertARCameraImageToTextureAsync(XRCpuImage image,
            CancellationToken cancellationToken)
        {
            var conversionParams = new XRCpuImage.ConversionParams
            {
                transformation = XRCpuImage.Transformation.MirrorX,
                outputFormat = TextureFormat.RGBA32,
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width, image.height),
            };

            using var conversionTask = image.ConvertAsync(conversionParams);

            await UniTask.WaitWhile(() => !conversionTask.status.IsDone(),
                PlayerLoopTiming.Update, cancellationToken);

            if (conversionTask.status != XRCpuImage.AsyncConversionStatus.Ready)
            {
                Debug.LogError("conversion task failed");
                return null;
            }

            var texture2d = new Texture2D(
                conversionTask.conversionParams.outputDimensions.x,
                conversionTask.conversionParams.outputDimensions.y,
                conversionTask.conversionParams.outputFormat,
                false);

            texture2d.LoadRawTextureData(conversionTask.GetData<byte>());
            texture2d.Apply();

            return texture2d;
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