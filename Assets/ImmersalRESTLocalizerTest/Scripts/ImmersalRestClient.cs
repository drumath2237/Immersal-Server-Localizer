using System;
using System.Text;
using Cysharp.Threading.Tasks;
using ImmersalRESTLocalizer.Types;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARSubsystems;

namespace ImmersalRESTLocalizer
{
    public class ImmersalRestClient
    {
        private readonly ImmersalRESTConfiguration _configuration;
        private const string URL = "https://api.immersal.com";

        public ImmersalRestClient(ImmersalRESTConfiguration config)
        {
            _configuration = config;
        }

        public async UniTask<string> SendRequestAsync(XRCameraIntrinsics intrinsics, Texture2D cameraTexture)
        {
            var base64 = Convert.ToBase64String(cameraTexture.EncodeToPNG());

            var reqParams = new ImmersalRequestParams
            {
                b64 = base64,
                fx = intrinsics.focalLength.x,
                fy = intrinsics.focalLength.y,
                ox = intrinsics.principalPoint.x,
                oy = intrinsics.principalPoint.y,
                mapIds = _configuration.MapIds,
                token = _configuration.token
            };

            var request = new UnityWebRequest($"{URL}/localizeb64", "POST");
            byte[] byteRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(reqParams));
            request.uploadHandler = new UploadHandlerRaw(byteRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            try
            {
                var res = await request.SendWebRequest();
                return res.downloadHandler.text;
            }
            catch (Exception e)
            {
                return request.error;
            }
        }
    }
}