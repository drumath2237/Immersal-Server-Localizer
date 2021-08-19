using System;
using System.Text;
using ImmersalRESTLocalizer.Types;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace ImmersalRESTLocalizerTest
{
    public class ImmersalServerLocalizer : MonoBehaviour
    {
        [SerializeField] private ImmersalRESTConfiguration _configuration;

        [SerializeField] private TextMeshPro _textMeshPro;

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

        public void SendRequest()
        {
            // SendRequestAsync().ContinueWith(resTask => { Debug.Log(resTask.Result); });
            if (!_cameraManager.TryAcquireLatestCpuImage(out var image))
            {
                _textMeshPro.text = "cannot aquire image";
                return;
            }


            var conversionParams = new XRCpuImage.ConversionParams(
                image, TextureFormat.RGBA32, XRCpuImage.Transformation.MirrorX);

            var texture2d = new Texture2D(
                conversionParams.outputDimensions.x,
                conversionParams.outputDimensions.y,
                conversionParams.outputFormat, false);

            var buffer = texture2d.GetRawTextureData<byte>();

            unsafe
            {
                image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);
            }

            texture2d.Apply();

            _plane.GetComponent<MeshRenderer>().material.mainTexture = texture2d;

            _ = SendRequestAsync(texture2d);

        }

        private async Task<string> SendRequestAsync(Texture2D texture)
        {
            var base64 = Convert.ToBase64String(texture.EncodeToPNG());

            if (!_cameraManager.TryGetIntrinsics(out var intrinsics))
            {
                _textMeshPro.text = "cannot get intrinsics";
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
                _textMeshPro.text = res.downloadHandler.text;

                return res.downloadHandler.text;
            }
            catch (Exception e)
            {
                _textMeshPro.text = request.ToString();
                return e.ToString();
            }
        }
    }
}