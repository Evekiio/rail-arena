using UnityEngine;

namespace RailArena
{
    /// <summary>Shared menu feedback; keeps native uGUI navigation and click handling.</summary>
    public sealed class MenuButton : UnityEngine.UI.Button
    {
        [SerializeField] private UnityEngine.UI.Text caption;
        private Transform focusRail;
        public void ConfigureCaption(UnityEngine.UI.Text text)
        {
            caption=text;targetGraphic=GetComponent<UnityEngine.UI.Image>();targetGraphic.color=Color.white;
            focusRail=transform.Find("Focus Rail");
            transition=Transition.ColorTint;
            var palette=colors;
            palette.normalColor=new Color(.11f,.17f,.21f).linear;
            palette.highlightedColor=new Color(.12f,.26f,.30f).linear;
            palette.selectedColor=new Color(.08f,.32f,.36f).linear;
            palette.pressedColor=new Color(.07f,.16f,.19f).linear;
            palette.disabledColor=new Color(.07f,.10f,.12f).linear;
            palette.colorMultiplier=1;palette.fadeDuration=.08f;colors=palette;
            DoStateTransition(currentSelectionState,true);
        }
        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            base.DoStateTransition(state,instant);
            if(focusRail!=null)focusRail.gameObject.SetActive(state==SelectionState.Highlighted||state==SelectionState.Selected||state==SelectionState.Pressed);
            if(caption!=null)caption.color=(state==SelectionState.Disabled?new Color(.42f,.49f,.53f):new Color(.937f,.914f,.851f)).linear;
        }
    }
}
