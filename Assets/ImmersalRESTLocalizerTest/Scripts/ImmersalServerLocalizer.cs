using System;
using System.Text;
using ImmersalRESTLocalizer.Types;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Serialization;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace ImmersalRESTLocalizerTest
{
    public class ImmersalServerLocalizer : MonoBehaviour
    {
        [SerializeField] private ImmersalRESTConfiguration _configuration;

        [SerializeField] private TextMeshPro _logText;

        [SerializeField] private Texture2D _sampleTexture;

        [SerializeField] private ARCameraManager _cameraManager;

        [SerializeField] private GameObject _plane;


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
            if (!TryGetCameraImageTexture(out var cameraImageTexture))
            {
                _logText.text = "cannot acquire image";
                return;
            }

            var immersalRes = await SendRequestAsync(cameraImageTexture);
            _logText.text = immersalRes;
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

            var buffer = texture2D.GetRawTextureData<byte>();

            unsafe
            {
                image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);
            }

            texture2D.Apply();

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
                _logText.text = request.error;
                return request.error;
            }
        }
    }
}