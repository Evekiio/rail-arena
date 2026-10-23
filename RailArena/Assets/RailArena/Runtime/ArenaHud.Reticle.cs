using UnityEngine;

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
            foreach (Transform child in menu.transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
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
