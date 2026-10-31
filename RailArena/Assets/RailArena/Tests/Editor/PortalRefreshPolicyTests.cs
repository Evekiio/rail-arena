using NUnit.Framework;
using UnityEngine;
namespace RailArena.Tests
{
    public sealed class PortalRefreshPolicyTests
    {
        [Test] public void CloseScopedAndLargeAperturesNeverDropRefreshes()
        {
            var tiny=new Rect(.4f,.4f,.01f,.01f);
            Assert.That(PortalRefreshPolicy.Interval(tiny,1920,1080,144,false),Is.EqualTo(1));
            Assert.That(PortalRefreshPolicy.Interval(tiny,1920,1080,1600,true),Is.EqualTo(1));
            Assert.That(PortalRefreshPolicy.Interval(new Rect(.4f,.4f,.05f,.01f),1920,1080,1600,false),Is.EqualTo(1));
            Assert.That(PortalRefreshPolicy.Interval(PortalViewport.Full,1920,1080,1600,false),Is.EqualTo(1));
        }
        [Test] public void OnlyDistantSmallAperturesUseTwoOrFourFrames()
        {
            Assert.That(PortalRefreshPolicy.Interval(new Rect(.4f,.4f,.01f,.04f),1920,1080,1600,false),Is.EqualTo(2));
            Assert.That(PortalRefreshPolicy.Interval(new Rect(.4f,.4f,.005f,.01f),1920,1080,1600,false),Is.EqualTo(4));
        }
        [Test] public void CachedCropKeepsActualApertureAndClippedScreenEdgesInside()
        {
            var original=new Rect(.4f,.4f,.02f,.05f);Assert.That(PortalRefreshPolicy.FitsCachedProjection(original,original,1920,1080),Is.True);
            Assert.That(PortalRefreshPolicy.FitsCachedProjection(original,new Rect(.401f,.401f,.02f,.05f),1920,1080),Is.True);
            Assert.That(PortalRefreshPolicy.FitsCachedProjection(original,new Rect(.404f,.4f,.02f,.05f),1920,1080),Is.False);
            Assert.That(PortalRefreshPolicy.FitsCachedProjection(new Rect(0,0,.9995f,1),PortalViewport.Full,1920,1080),Is.False);
            Assert.That(PortalRefreshPolicy.FitsCachedProjection(new Rect(.0005f,0,.9995f,1),PortalViewport.Full,1920,1080),Is.False);
        }
        [Test] public void DistantCameraPhasesDistributeTheMeasuredCombatWorkload()
        {
            int[] slots={1,0,3,4},intervals={2,4,2,4};int total=0;
            for(int frame=0;frame<12;frame++)
            {int renders=0;for(int i=0;i<slots.Length;i++)if(PortalRefreshPolicy.Scheduled(frame,slots[i],intervals[i]))renders++;Assert.That(renders,Is.InRange(1,2));total+=renders;}
            Assert.That(total,Is.EqualTo(18));
        }
    }
}
