using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace ImmersalRESTLocalizer
{
    public static class ARImageProcessingUtil
    {
        public static async UniTask<Texture2D> ConvertARCameraImageToTextureAsync(XRCpuImage image,
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

            await UniTask.SwitchToMainThread(cancellationToken);

            texture2d.LoadRawTextureData(conversionTask.GetData<byte>());
            texture2d.Apply();

            return texture2d;
        }
    }
}