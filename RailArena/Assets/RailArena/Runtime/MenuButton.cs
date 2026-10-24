using UnityEngine;

namespace RailArena
{
    /// <summary>Shared menu feedback; keeps native uGUI navigation and click handling.</summary>
    public sealed class MenuButton : UnityEngine.UI.Button
    {
        [SerializeField] private UnityEngine.UI.Text caption;
        public void ConfigureCaption(UnityEngine.UI.Text text)
        {
            caption=text;targetGraphic=GetComponent<UnityEngine.UI.Image>();targetGraphic.color=Color.white;
            transition=Transition.ColorTint;
            var palette=colors;
            palette.normalColor=new Color(.055f,.10f,.15f);
            palette.highlightedColor=new Color(.07f,.20f,.25f);
            palette.selectedColor=new Color(.045f,.26f,.31f);
            palette.pressedColor=new Color(.025f,.12f,.16f);
            palette.disabledColor=new Color(.025f,.035f,.045f);
            palette.colorMultiplier=1;palette.fadeDuration=.08f;colors=palette;
            DoStateTransition(currentSelectionState,true);
        }
        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            base.DoStateTransition(state,instant);
            if(caption!=null)caption.color=state==SelectionState.Disabled?new Color(.42f,.49f,.53f):new Color(.91f,.94f,.96f);
        }
    }
}
