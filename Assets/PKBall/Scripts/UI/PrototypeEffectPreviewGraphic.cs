using UnityEngine;
using UnityEngine.UI;
namespace PKBall.Prototype
{
    public enum PrototypePreviewShape { Circle, Star, Wave, Beam, Volley, None }
    public sealed class PrototypeEffectPreviewGraphic : MaskableGraphic
    {
        public PrototypePreviewShape shape;public bool animate=true;private float next;
        private void Update(){if(animate && Time.unscaledTime>=next){next=Time.unscaledTime+.033f;SetVerticesDirty();}}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=rectTransform.rect;var center=rect.center;float radius=Mathf.Min(rect.width,rect.height)*.39f;
            float t=animate?(Time.unscaledTime*.9f)%1:.6f;var tint=color;
            if(shape==PrototypePreviewShape.None){Line(vh,center-new Vector2(radius,radius),center+new Vector2(radius,radius),3,tint);Line(vh,center+new Vector2(-radius,radius),center+new Vector2(radius,-radius),3,tint);return;}
            if(shape==PrototypePreviewShape.Beam){Line(vh,center-new Vector2(radius,radius*.5f),center+new Vector2(radius,radius*.5f),5,tint);return;}
            if(shape==PrototypePreviewShape.Volley){for(int i=0;i<5;i++){float a=(i/4f-.5f)*Mathf.PI;var start=center+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*radius;var end=Vector2.Lerp(start,center,t);Line(vh,end,end+(center-start).normalized*radius*.4f,3,tint);}return;}
            float r=radius*(shape==PrototypePreviewShape.Circle?1:.3f+.7f*t);if(shape!=PrototypePreviewShape.Circle)tint.a*=1-t*.7f;
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI*2/32,b=(i+1)*Mathf.PI*2/32;
                float ra=shape==PrototypePreviewShape.Star&&i%2==1?r*.45f:r,rb=shape==PrototypePreviewShape.Star&&(i+1)%2==1?r*.45f:r;
                var v1=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*ra;var v2=center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*rb;
                if(shape==PrototypePreviewShape.Circle){int n=vh.currentVertCount;vh.AddVert(center,tint,Vector2.zero);vh.AddVert(v1,tint,Vector2.zero);vh.AddVert(v2,tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
                else Line(vh,v1,v2,3,tint);
            }
        }
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {
            var delta=(b-a).normalized;var normal=new Vector2(-delta.y,delta.x)*width*.5f;int n=vh.currentVertCount;
            vh.AddVert(a-normal,tint,Vector2.zero);vh.AddVert(a+normal,tint,Vector2.zero);vh.AddVert(b+normal,tint,Vector2.zero);vh.AddVert(b-normal,tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}
