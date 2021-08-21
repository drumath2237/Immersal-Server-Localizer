using System;
using System.Text;
using ImmersalRESTLocalizer.Types;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
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
            _ = LocalizeAsync();
        }

        private async Task LocalizeAsync()
        {
            _arSpace.position = Vector3.zero;
            _arSpace.rotation = quaternion.identity;
            
            var cameraMatrix = _cameraTransform.localToWorldMatrix;

            if (!TryGetCameraImageTexture(out var cameraImageTexture))
            {
                _logText.text = "cannot acquire image";
                return;
            }

            var resText = await SendRequestAsync(cameraImageTexture);

            var immersalResponse = JsonUtility.FromJson<ImmersalResponseParams>(resText);

            var immersalCameraMatrix = CalcImmersalCameraMatrix(immersalResponse);
            var mapMatrix = cameraMatrix * immersalCameraMatrix.inverse * _arSpace.localToWorldMatrix;

            _arSpace.position = mapMatrix.GetColumn(3);
            _arSpace.rotation = mapMatrix.rotation;
        }

        private bool TryGetCameraImageTexture(out Texture2D texture2D)
        {
            if (!_cameraManager.TryAcquireLatestCpuImage(out var image))
            {
                texture2D = null;
                return false;
            }

            var conversionParams = new XRCpuImage.ConversionParams(
                image, TextureFormat.RGBA32, XRCpuImage.Transformation.MirrorX);

            texture2D = new Texture2D(
                conversionParams.outputDimensions.x,
                conversionParams.outputDimensions.y,
                conversionParams.outputFormat, false);

            using (var buffer = texture2D.GetRawTextureData<byte>())
            {
                unsafe
                {
                    image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);
                }
            }

            texture2D.Apply();
            
            _cameraManager.

            return true;
        }

        private async Task<string> SendRequestAsync(Texture2D texture)
        {
            var base64 = Convert.ToBase64String(texture.EncodeToPNG());

            if (!_cameraManager.TryGetIntrinsics(out var intrinsics))
            {
                _logText.text = "cannot get intrinsics";
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