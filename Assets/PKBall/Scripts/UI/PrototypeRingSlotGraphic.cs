using UnityEngine;
using UnityEngine.UI;

namespace PKBall.Prototype
{
    // A UGUI sector with a matching hit area. All sectors share one ring-sized rect.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PrototypeRingSlotGraphic : MaskableGraphic
    {
        [SerializeField] private float startAngle;
        [SerializeField] private float endAngle;
        [SerializeField] private float innerRadius = 142f;
        [SerializeField] private float outerRadius = 258f;

        protected override void OnEnable()
        {
            // Repair older saved slots before registering them with UGUI raycasters.
            if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }

        public void Configure(float centerAngle, float sweep, float inner, float outer)
        {
            startAngle = centerAngle - sweep * .5f + .6f;
            endAngle = centerAngle + sweep * .5f - .6f;
            innerRadius = inner + 2f;
            outerRadius = outer - 2f;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            const int steps = 12;
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Deg2Rad * Mathf.Lerp(startAngle, endAngle, i / (float)steps);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddVertex(helper, direction * innerRadius);
                AddVertex(helper, direction * outerRadius);
            }
            for (int i = 0; i < steps; i++)
            {
                int baseIndex = i * 2;
                helper.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
                helper.AddTriangle(baseIndex + 1, baseIndex + 3, baseIndex + 2);
            }
        }

        private void AddVertex(VertexHelper helper, Vector2 position)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = position;
            helper.AddVert(vertex);
        }

        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            if (!base.Raycast(screenPoint, eventCamera)) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, screenPoint, eventCamera, out Vector2 point)) return false;
            float radius = point.magnitude;
            if (radius < innerRadius || radius > outerRadius) return false;
            float angle = Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg;
            float offset = Mathf.Repeat(angle - startAngle, 360f);
            return offset <= endAngle - startAngle;
        }
    }
}
