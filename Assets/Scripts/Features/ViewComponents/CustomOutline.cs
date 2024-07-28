using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Utils.View
{
    [AddComponentMenu("UI/Effects/CustomOutline", 15)]
    public class CustomOutline : Shadow
    {
        [SerializeField] [Range(1, NeededCpacity)]private int _duplicates = 1;

        private const int NeededCpacity = 5;
        
        protected CustomOutline()
        {}

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
                return;

            var verts = ListPool<UIVertex>.Get();
            vh.GetUIVertexStream(verts);

            var neededCpacity = verts.Count * NeededCpacity;
            if (verts.Capacity < neededCpacity)
                verts.Capacity = neededCpacity;

            for (int i = 0; i < _duplicates; i++)
            {
                var start = 0;
                var end = verts.Count;
                ApplyShadowZeroAlloc(verts, effectColor, start, verts.Count, effectDistance.x, effectDistance.y);

                start = end;
                end = verts.Count;
                ApplyShadowZeroAlloc(verts, effectColor, start, verts.Count, effectDistance.x, -effectDistance.y);

                start = end;
                end = verts.Count;
                ApplyShadowZeroAlloc(verts, effectColor, start, verts.Count, -effectDistance.x, effectDistance.y);

                start = end;
                end = verts.Count;
                ApplyShadowZeroAlloc(verts, effectColor, start, verts.Count, -effectDistance.x, -effectDistance.y);
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
            ListPool<UIVertex>.Release(verts);
        }
    }
}