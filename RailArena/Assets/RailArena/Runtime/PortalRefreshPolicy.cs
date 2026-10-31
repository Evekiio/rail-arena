using UnityEngine;
namespace RailArena
{
    /// <summary>Bounded reuse for small distant apertures. Close and scoped views always refresh.</summary>
    public static class PortalRefreshPolicy
    {
        public const float MaximumAge=1f/15;
        public static int Interval(Rect viewport,int width,int height,float distanceSquared,bool scoped)
        {
            float pixels=Mathf.Max(viewport.width*Mathf.Max(1,width),viewport.height*Mathf.Max(1,height));
            if(scoped||distanceSquared<=144||pixels>=96)return 1;
            return pixels>=32?2:4;
        }
        public static bool FitsCachedProjection(Rect cached,Rect current,int width,int height)
        {
            // ForBounds includes two screen pixels of padding. Reuse only while the
            // actual aperture remains inside the cached crop; retain its matching shader UVs.
            float x=2f/Mathf.Max(1,width),y=2f/Mathf.Max(1,height);
            float left=current.xMin<=0?0:current.xMin+x,right=current.xMax>=1?1:current.xMax-x;
            float bottom=current.yMin<=0?0:current.yMin+y,top=current.yMax>=1?1:current.yMax-y;
            return left>=cached.xMin&&right<=cached.xMax&&bottom>=cached.yMin&&top<=cached.yMax;
        }
        public static bool Scheduled(int frame,int slot,int interval)=>interval<=1||(frame+(slot+slot/2))%interval==0;
    }
}
