using UnityEngine;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private void CharacterPicker()
        {
            BeginPage(RefreshMenu);
            Text(menu.transform,"Title","CHOOSE YOUR CONTENDER",64,new Vector2(.1f,.78f),new Vector2(.94f,.9f));
            Text(menu.transform,"Character scope",Session.Running?"NEXT SESSION APPEARANCE / Your current contender stays unchanged.":"OFFLINE APPEARANCE / The other contenders fill your selected bot slots.",22,
                new Vector2(.1f,.70f),new Vector2(.94f,.76f));
            var cards=new UnityEngine.UI.Image[OfflineRoster.CharacterCount];
            var labels=new UnityEngine.UI.Text[OfflineRoster.CharacterCount];
            var status=Text(menu.transform,"Character selection","",24,new Vector2(.1f,.15f),new Vector2(.94f,.20f));status.color=cyan;
            void Refresh()
            {
                for(int i=0;i<cards.Length;i++)
                {
                    bool selected=i==Session.SelectedOfflineCharacter;
                    cards[i].color=selected?new Color(.015f,.12f,.14f):new Color(.025f,.045f,.065f);
                    labels[i].text=OfflineRoster.Label(i)+"\n"+(selected?"SELECTED":"CHOOSE APPEARANCE");
                    labels[i].color=selected?cyan:new Color(.91f,.94f,.96f);
                }
                status.text="SAVED // "+OfflineRoster.Label(Session.SelectedOfflineCharacter)+"     SAME MOVEMENT / SAME HITBOXES";
            }
            for(int i=0;i<cards.Length;i++)
            {
                int character=i;
                var card=new GameObject("Character card "+i,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));
                card.transform.SetParent(menu.transform,false);
                float x=.095f+i*.2075f;Rect(card,new Vector2(x,.23f),new Vector2(x+.1925f,.68f));
                cards[i]=card.GetComponent<UnityEngine.UI.Image>();
                var button=card.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=cards[i];button.onClick.AddListener(InterfaceClick);
                button.onClick.AddListener(()=>{Session.SelectOfflineCharacter(character);Refresh();UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(cards[character].gameObject);});
                var frame=new GameObject("Portrait frame "+i,typeof(RectTransform));frame.transform.SetParent(card.transform,false);
                Rect(frame,new Vector2(.025f,.18f),new Vector2(.975f,.985f));
                var portrait=new GameObject("Portrait "+i,typeof(RectTransform),typeof(UnityEngine.UI.RawImage),typeof(UnityEngine.UI.AspectRatioFitter));
                portrait.transform.SetParent(frame.transform,false);Rect(portrait,Vector2.zero,Vector2.one);
                var image=portrait.GetComponent<UnityEngine.UI.RawImage>();image.texture=Resources.Load<Texture2D>("UI/ContenderPortraits/Character-"+i);image.raycastTarget=false;
                var fit=portrait.GetComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=.8f;
                labels[i]=Text(card.transform,"Character label "+i,"",22,new Vector2(.025f,.015f),new Vector2(.975f,.16f));labels[i].alignment=TextAnchor.MiddleCenter;
            }
            Refresh();UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(cards[Session.SelectedOfflineCharacter].gameObject);float y=.07f;Button("BACK TO SETUP",RefreshMenu,ref y);
        }
    }
}
