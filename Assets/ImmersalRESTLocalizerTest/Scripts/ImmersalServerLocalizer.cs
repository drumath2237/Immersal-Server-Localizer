using System;
using System.Text;
using ImmersalRESTLocalizer.Types;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;


namespace ImmersalRESTLocalizerTest
{
    public class ImmersalServerLocalizer : MonoBehaviour
    {
        [SerializeField] private ImmersalRESTConfiguration _configuration;

        [SerializeField] private TextMeshPro _textMeshPro;

        [SerializeField] private Texture2D _sampleTexture;


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
            SendRequestAsync().ContinueWith(resTask => { Debug.Log(resTask.Result); });
        }

        private async Task<string> SendRequestAsync()
        {
            var base64 = Convert.ToBase64String(_sampleTexture.EncodeToPNG());

            var reqParams = new ImmersalRequestParams
            {
                b64 = base64,
                fx = 1455.738159f,
                fy = 1455.738159f,
                ox = 962.615967f,
                oy = 694.292175f,
                mapIds = new[] {new MapId {id = 4054}, new MapId {id = 4065}},
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
                Debug.Log(res.downloadHandler.text);

                return res.downloadHandler.text;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}