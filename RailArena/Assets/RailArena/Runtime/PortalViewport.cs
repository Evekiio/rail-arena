using UnityEngine;

namespace RailArena
{
    /// <summary>Conservative aperture bounds and the matching clip-space transform.</summary>
    public static class PortalViewport
    {
        public static readonly Rect Full=new(0,0,1,1);
        public static Rect ForBounds(Camera viewer,Bounds bounds)
        {
            float left=1,right=0,bottom=1,top=0;
            var minimum=bounds.min;var maximum=bounds.max;
            for(int corner=0;corner<8;corner++)
            {
                var point=new Vector3((corner&1)==0?minimum.x:maximum.x,(corner&2)==0?minimum.y:maximum.y,(corner&4)==0?minimum.z:maximum.z);
                var projected=viewer.WorldToViewportPoint(point);
                // Near-plane intersections need the full projection; dividing a crossing bound is unstable.
                if(projected.z<=viewer.nearClipPlane+.01f||!Finite(projected.x)||!Finite(projected.y))return Full;
                left=Mathf.Min(left,projected.x);right=Mathf.Max(right,projected.x);bottom=Mathf.Min(bottom,projected.y);top=Mathf.Max(top,projected.y);
            }
            float padX=2f/Mathf.Max(1,viewer.pixelWidth),padY=2f/Mathf.Max(1,viewer.pixelHeight);
            left=Mathf.Clamp01(left-padX);right=Mathf.Clamp01(right+padX);bottom=Mathf.Clamp01(bottom-padY);top=Mathf.Clamp01(top+padY);
            if(right-left<.00001f||top-bottom<.00001f)return Full;
            return Rect.MinMaxRect(left,bottom,right,top);
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        public static Matrix4x4 CropProjection(Matrix4x4 projection,Rect viewport)
        {
            var crop=Matrix4x4.identity;crop.m00=1/viewport.width;crop.m11=1/viewport.height;
            crop.m03=(1-2*viewport.center.x)/viewport.width;crop.m13=(1-2*viewport.center.y)/viewport.height;
            return crop*projection;
        }
        public static int TextureDimension(int fullSize,float fraction,int currentSize)
        {
            const int bucket=64;
            int required=Mathf.Clamp(Mathf.CeilToInt(fullSize*fraction/bucket)*bucket,Mathf.Min(128,fullSize),fullSize);
            // Grow immediately; leave headroom when shrinking so camera jitter does not churn render textures.
            return currentSize>=required&&currentSize<=fullSize&&required>=currentSize-bucket*2?currentSize:required;
        }
    }
}
