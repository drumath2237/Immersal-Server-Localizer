using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace ImmersalRESTLocalizer
{
    public static class ARImageProcessingUtil
    {
        public static unsafe Texture2D ConvertARCameraImageToTexture(XRCpuImage image)
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

            texture2d.LoadRawTextureData(conversionTask.GetData<byte>());
            texture2d.Apply();

            return texture2d;
        }
    }
}