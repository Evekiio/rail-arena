using NUnit.Framework;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class PortalViewportTests
    {
        [TestCase(70f,4f/3f)] [TestCase(100f,16f/9f)] [TestCase(110f,21f/9f)]
        public void BoundsContainProjectedApertureAtPhysicalLensSettings(float fov,float aspect)
        {
            var root=new GameObject("Owned physical crop camera");var camera=root.AddComponent<Camera>();
            try
            {
                camera.usePhysicalProperties=true;camera.aspect=aspect;camera.lensShift=new Vector2(.12f,-.08f);camera.focalLength=Camera.FieldOfViewToFocalLength(fov,camera.sensorSize.y);
                camera.transform.SetPositionAndRotation(new Vector3(1.1f,.4f,-6),Quaternion.Euler(3,-7,0));
                var bounds=new Bounds(Vector3.zero,new Vector3(2.6f,3.4f,.02f));var rect=PortalViewport.ForBounds(camera,bounds);
                Assert.That(rect.width,Is.InRange(.01f,.9f));Assert.That(rect.height,Is.InRange(.01f,.9f));
                for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)
                {
                    var projected=camera.WorldToViewportPoint(new Vector3(x*1.3f,y*1.7f,0));
                    Assert.That(projected.x,Is.InRange(rect.xMin,rect.xMax));Assert.That(projected.y,Is.InRange(rect.yMin,rect.yMax));
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void NearPlaneCrossingUsesStableFullProjection()
        {
            var root=new GameObject("Owned near crop camera");var camera=root.AddComponent<Camera>();
            try{camera.nearClipPlane=.1f;Assert.That(PortalViewport.ForBounds(camera,new Bounds(new Vector3(0,0,.1f),new Vector3(2,3,.2f))),Is.EqualTo(PortalViewport.Full));}
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void TextureBucketsNeverUndersampleAndAvoidBoundaryChurn()
        {
            foreach(int full in new[]{320,540,960})for(int fraction=1;fraction<=100;fraction++)
            {
                int size=PortalViewport.TextureDimension(full,fraction/100f,0);Assert.That(size,Is.GreaterThanOrEqualTo(Mathf.CeilToInt(full*fraction/100f)));Assert.That(size,Is.LessThanOrEqualTo(full));
            }
            Assert.That(PortalViewport.TextureDimension(960,.13f,256),Is.EqualTo(256));
            Assert.That(PortalViewport.TextureDimension(960,.27f,256),Is.GreaterThan(256));
            Assert.That(PortalViewport.TextureDimension(960,.1f,960),Is.LessThan(960));
        }
    }
}
