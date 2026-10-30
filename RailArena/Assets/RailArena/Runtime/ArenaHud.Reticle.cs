using UnityEngine;
using RailArena.Simulation;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private sealed class ReticleView
        {
            public RectTransform Root;
            public UnityEngine.UI.Image[] Marks;

            public void Apply(ReticlePreferences preferences, bool locked = false)
            {
                Root.sizeDelta = Vector2.one * preferences.Size;
                var color = preferences.Color;
                color.a = locked ? .55f : 1;
                foreach (var mark in Marks) mark.color = color;
            }
        }

        private ReticleView combatReticle;
        private GameObject illuminatedSight;
        private RectTransform sprintSpreadRoot, sprintBoundaryRoot;
        private readonly UnityEngine.UI.Image[] sprintBoundaryMarks = new UnityEngine.UI.Image[48];
        private float sprintSpreadBlend, lastSpreadAngle;
        private const float SprintSpreadTransitionSeconds = .16f;
        private readonly UnityEngine.UI.Image[] sprintSpreadMarks = new UnityEngine.UI.Image[48];

        private void CreateSprintSpread(Transform parent)
        {
            var root = new GameObject("Sprint spread", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            Rect(root, Vector2.one * .5f, Vector2.one * .5f);
            sprintSpreadRoot = root.GetComponent<RectTransform>();
            for (int i = 0; i < sprintSpreadMarks.Length; i++)
            {
                var segment = Graphic(root.transform, "Spread segment " + i, Vector2.one * .5f, Vector2.one * .5f, Color.white);
                float angle = i * Mathf.PI * 2 / sprintSpreadMarks.Length;
                var rect = segment.rectTransform;
                rect.anchorMin = rect.anchorMax = Vector2.one * .5f;
                rect.pivot = Vector2.one * .5f;
                rect.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .5f;
                rect.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg + 90);
                sprintSpreadMarks[i] = segment;
            }
            root.SetActive(false);
            var boundary = new GameObject("Sprint spread boundary", typeof(RectTransform));
            boundary.transform.SetParent(parent, false);
            Rect(boundary, Vector2.one * .5f, Vector2.one * .5f);
            sprintBoundaryRoot = boundary.GetComponent<RectTransform>();
            for (int i = 0; i < sprintBoundaryMarks.Length; i++)
            {
                var segment = Graphic(boundary.transform, "Boundary segment " + i, Vector2.one * .5f, Vector2.one * .5f, Color.white);
                float angle = i * Mathf.PI * 2 / sprintBoundaryMarks.Length;
                var rect = segment.rectTransform;
                rect.anchorMin = rect.anchorMax = Vector2.one * .5f;
                rect.pivot = Vector2.one * .5f;
                rect.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg + 90);
                sprintBoundaryMarks[i] = segment;
            }
            boundary.SetActive(false);
        }

        private void UpdateSprintSpread(float halfAngle, float deltaTime)
        {
            bool visible = halfAngle > 0;
            if (visible) lastSpreadAngle = halfAngle;
            sprintBoundaryRoot.gameObject.SetActive(visible);
            if (visible)
            {
                float boundaryRadius = SprintFireSpread.ScreenRadius(Session.View.pixelHeight, Session.View.fieldOfView, canvas.GetComponent<Canvas>().scaleFactor, halfAngle);
                sprintBoundaryRoot.sizeDelta = Vector2.one * (boundaryRadius * 2);
                var boundaryColor = Session.Reticle.Color; boundaryColor.a = .8f;
                for (int i = 0; i < sprintBoundaryMarks.Length; i++)
                {
                    float angle = i * Mathf.PI * 2 / sprintBoundaryMarks.Length;
                    var mark = sprintBoundaryMarks[i];
                    mark.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * boundaryRadius;
                    mark.rectTransform.sizeDelta = new Vector2(Mathf.Max(2, boundaryRadius * .13f), 1.5f);
                    mark.color = boundaryColor;
                }
            }
            sprintSpreadBlend = Mathf.MoveTowards(sprintSpreadBlend, visible ? 1f : 0f, Mathf.Max(0, deltaTime) / SprintSpreadTransitionSeconds);
            sprintSpreadRoot.gameObject.SetActive(sprintSpreadBlend > 0);
            if (sprintSpreadBlend <= 0) return;
            var scale = canvas.GetComponent<Canvas>().scaleFactor;
            float fullRadius = SprintFireSpread.ScreenRadius(Session.View.pixelHeight, Session.View.fieldOfView, scale, visible ? halfAngle : lastSpreadAngle);
            float normalRadius = Mathf.Min(Session.Reticle.Size * .5f, fullRadius * .65f);
            float blend = Mathf.SmoothStep(0, 1, sprintSpreadBlend);
            float radius = Mathf.Lerp(normalRadius, fullRadius, blend);
            sprintSpreadRoot.sizeDelta = Vector2.one * (radius * 2);
            var color = Session.Reticle.Color; color.a = .8f * blend;
            for (int i = 0; i < sprintSpreadMarks.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / sprintSpreadMarks.Length;
                var mark = sprintSpreadMarks[i];
                mark.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                mark.rectTransform.sizeDelta = new Vector2(Mathf.Max(2, radius * .13f), 1.5f);
                mark.color = color;
            }
        }
        private void CreateIlluminatedSight(Transform parent)
        {
            illuminatedSight = new GameObject("IlluminatedSight", typeof(RectTransform));
            illuminatedSight.transform.SetParent(parent, false);
            Rect(illuminatedSight, Vector2.one * .5f, Vector2.one * .5f);
            illuminatedSight.GetComponent<RectTransform>().sizeDelta = new Vector2(8, 8);
            Graphic(illuminatedSight.transform, "Sight halo", Vector2.zero, Vector2.one, new Color(0, 1, .85f, .16f));
            Graphic(illuminatedSight.transform, "Sight outline", Vector2.one * .2f, Vector2.one * .8f, new Color(0, 0, 0, .9f));
            Graphic(illuminatedSight.transform, "Sight light", Vector2.one * .32f, Vector2.one * .68f, new Color(.35f, 1, .8f));
            illuminatedSight.SetActive(false);
        }

        private ReticleView CreateReticle(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            Rect(root, Vector2.one * .5f, Vector2.one * .5f);
            var view = new ReticleView { Root = root.GetComponent<RectTransform>(), Marks = new UnityEngine.UI.Image[5] };
            var bounds = new[]
            {
                new Vector4(.47f,.72f,.53f,.98f), new Vector4(.47f,.02f,.53f,.28f),
                new Vector4(.02f,.47f,.28f,.53f), new Vector4(.72f,.47f,.98f,.53f),
                new Vector4(.47f,.47f,.53f,.53f)
            };
            for (int i = 0; i < bounds.Length; i++)
            {
                var b = bounds[i];
                Graphic(root.transform, "Outline" + i, new Vector2(b.x-.025f,b.y-.025f),
                    new Vector2(b.z+.025f,b.w+.025f), new Color(0,0,0,.85f));
                view.Marks[i] = Graphic(root.transform, "Mark" + i, new Vector2(b.x,b.y),new Vector2(b.z,b.w),Session.Reticle.Color);
            }
            view.Apply(Session.Reticle);
            return view;
        }

        private void ReticleSettings()
        {
            BeginPage(Settings);
            Text(menu.transform,"Title","RETICLE",64,new Vector2(.1f,.78f),new Vector2(.8f,.9f));
            Text(menu.transform,"Reticle help","Choose a clear aim point. Changes apply immediately.\nSize uses the HUD's 1080p reference scale.",22,
                new Vector2(.1f,.67f),new Vector2(.91f,.76f));
            var dark = Graphic(menu.transform,"Dark preview",new Vector2(.55f,.40f),new Vector2(.72f,.63f),new Color(.025f,.045f,.06f));
            var light = Graphic(menu.transform,"Light preview",new Vector2(.74f,.40f),new Vector2(.91f,.63f),new Color(.79f,.81f,.75f));
            var previews = new[] { CreateReticle(dark.transform,"ReticlePreviewDark"),CreateReticle(light.transform,"ReticlePreviewLight") };
            void RefreshPreview() { foreach(var preview in previews)preview.Apply(Session.Reticle); }
            Text(menu.transform,"Preview label","DARK / BRIGHT BACKGROUND",18,new Vector2(.55f,.34f),new Vector2(.94f,.39f));
            Slider("Reticle size",Session.Reticle.Size,ReticlePreferences.MinimumSize,ReticlePreferences.MaximumSize,.54f,
                value => Mathf.RoundToInt(value)+" units", value => { Session.Reticle.Size=value; Session.Reticle.Save(); RefreshPreview(); },wholeNumbers:true);
            float y=.41f;
            UnityEngine.UI.Button colorButton=null;
            colorButton=Button("COLOR // "+Session.Reticle.ColorName,() =>
            {
                Session.Reticle.ColorIndex=(Session.Reticle.ColorIndex+1)%ReticlePreferences.ColorCount;
                Session.Reticle.Save(); RefreshPreview();
                colorButton.GetComponentInChildren<UnityEngine.UI.Text>().text="COLOR // "+Session.Reticle.ColorName;
            },ref y);
            Button("RESET RETICLE",() =>
            {
                Session.Reticle.Reset(); Session.Reticle.Save();
                menu.transform.Find("Reticle size").GetComponent<UnityEngine.UI.Slider>().value=Session.Reticle.Size;
                colorButton.GetComponentInChildren<UnityEngine.UI.Text>().text="COLOR // "+Session.Reticle.ColorName;
                RefreshPreview();
            },ref y);
            Text(menu.transform,"Recovery help","Your chosen color stays steady while firing.\nThe rail meter shows weapon recovery.\nAn outline keeps the aim point visible on bright surfaces.",22,
                new Vector2(.55f,.17f),new Vector2(.94f,.30f));
            y=.1f;
            Button("BACK TO SETTINGS",Settings,ref y);
        }
    }
}





