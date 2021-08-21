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
        [SerializeField] private ImmersalRESTConfiguration _configuration;

        [SerializeField] private TextMeshProUGUI _logText;

        [SerializeField] private ARCameraManager _cameraManager;

        [SerializeField] private Transform _arSpace;

        [SerializeField] private Transform _cameraTransform;


        private string token;

        private void Start()
        {
            if (_configuration != null)
            {
                token = _configuration.token;
            }
        }

        public void Localize()
        {
            LocalizeAsync().Forget();
        }

        private async UniTask LocalizeAsync()
        {
            _arSpace.position = Vector3.zero;
            _arSpace.rotation = quaternion.identity;

            var cameraMatrix = _cameraTransform.localToWorldMatrix;

            var (isSuccess, cameraImageTexture) = await TryGetCameraImageTextureAsync();

            if (!isSuccess)
            {
                _logText.text += "cannot get camera image texture\n";
                return;
            }

            var resText = await SendRequestAsync(cameraImageTexture);

            var immersalResponse = JsonUtility.FromJson<ImmersalResponseParams>(resText);

            var immersalCameraMatrix = CalcImmersalCameraMatrix(immersalResponse);
            var mapMatrix = cameraMatrix * immersalCameraMatrix.inverse * _arSpace.localToWorldMatrix;

            _arSpace.position = mapMatrix.GetColumn(3);
            _arSpace.rotation = mapMatrix.rotation;
        }

        private async UniTask<(bool, Texture2D)> TryGetCameraImageTextureAsync()
        {
            if (!_cameraManager.TryAcquireLatestCpuImage(out var image))
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

            _logText.text += "conversing...\n";

            await UniTask.WaitWhile(() => !conversionTask.status.IsDone(),
                PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());

            if (conversionTask.status != XRCpuImage.AsyncConversionStatus.Ready)
            {
                Debug.LogError("conversion task failed");
                _logText.text += "conversion task failed\n";
                return (false, null);
            }

            var texture2d = new Texture2D(
                conversionTask.conversionParams.outputDimensions.x,
                conversionTask.conversionParams.outputDimensions.y,
                conversionTask.conversionParams.outputFormat,
                false);

            texture2d.LoadRawTextureData(conversionTask.GetData<byte>());
            texture2d.Apply();

            _logText.text += "conversion done\n";

            return (true, texture2d);
        }

        private async UniTask<string> SendRequestAsync(Texture2D texture)
        {
            var base64 = Convert.ToBase64String(texture.EncodeToPNG());

            if (!_cameraManager.TryGetIntrinsics(out var intrinsics))
            {
                _logText.text += "cannot get intrinsics\n";
                return "";
            }

            var reqParams = new ImmersalRequestParams
            {
                b64 = base64,
                fx = intrinsics.focalLength.x,
                fy = intrinsics.focalLength.y,
                ox = intrinsics.principalPoint.x,
                oy = intrinsics.principalPoint.y,
                mapIds = new[] {new MapId {id = 27517}},
                token = token
            };

            var request = new UnityWebRequest("https://api.immersal.com/localizeb64", "POST");
            byte[] byteRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(reqParams));
            request.uploadHandler = new UploadHandlerRaw(byteRaw);

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            try
            {
                var res = await request.SendWebRequest();
                _logText.text = res.downloadHandler.text;

                return res.downloadHandler.text;
            }
            catch (Exception e)
            {
                _logText.text = e.ToString();
                return request.error;
            }
        }

        private Matrix4x4 CalcImmersalCameraMatrix(ImmersalResponseParams iParams)
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