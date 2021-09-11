using NUnit.Framework;
using ImmersalRESTLocalizer.Types;
using UnityEngine;

namespace Tests
{
    public class ResponseParamsTest
    {
        [Test]
        public void ToMatrixTest()
        {
            var res = new ImmersalResponseParams()
            {
                error = "none",
                success = true,
                map = 0,
                px = 1f,
                py = 1f,
                pz = 1f,
            };

            var matrix = new Matrix4x4
            {
                m03 = 1f,
                m13 = 1f,
                m23 = 1f,
                
                m33 = 1f
            };
            
            Assert.That(res.ToMatrix4(), Is.EqualTo(matrix));
        }
    }
}