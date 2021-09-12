using NUnit.Framework;
using ImmersalRESTLocalizer.Types;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace Tests
{
    public class ResponseParamsTest
    {
        [Test]
        public void ToMatrixTest()
        {
            var theta = Mathf.PI / 3;
            var res = new ImmersalResponseParams()
            {
                error = "none",
                success = true,
                map = 0,
                px = 1f,
                py = 1f,
                pz = 1f,
                r00 = 1f,
                r11 = Mathf.Cos(theta),
                r12 = -Mathf.Sin(theta),
                r21 = Mathf.Sin(theta),
                r22 = Mathf.Cos(theta),
            };

            var matrix = Matrix4x4.TRS(
                new Vector3 { x = 1f, y = 1f, z = 1f },
                Quaternion.AngleAxis(theta * Mathf.Rad2Deg, new Vector3(1, 0, 0)),
                Vector3.one
            );

            Assert.That(res.ToMatrix4().GetRow(0),
                Is.EqualTo(matrix.GetRow(0)).Using(Vector4EqualityComparer.Instance));
                
            Assert.That(res.ToMatrix4().GetRow(1),
                Is.EqualTo(matrix.GetRow(1)).Using(Vector4EqualityComparer.Instance));
                
            Assert.That(res.ToMatrix4().GetRow(2),
                Is.EqualTo(matrix.GetRow(2)).Using(Vector4EqualityComparer.Instance));
                
            Assert.That(res.ToMatrix4().GetRow(3),
                Is.EqualTo(matrix.GetRow(3)).Using(Vector4EqualityComparer.Instance));
        }
    }
}