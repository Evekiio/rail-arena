using UnityEngine;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private UnityEngine.UI.Text connectionStatus;
        private GameObject connectionErrorPanel;
        private string hostAddress = "127.0.0.1";
        private string hostPort = "7777";
        private UnityEngine.UI.Button readyButton;

        private void NetworkPanel(string title,System.Action back=null)
        {
            if (menu != null) { menu.SetActive(false); Destroy(menu); }
            connectionErrorPanel = null;
            menu = new GameObject("Connection menu", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            menu.transform.SetParent(canvas, false);
            Rect(menu, Vector2.zero, Vector2.one);
            menu.GetComponent<UnityEngine.UI.Image>().color = new Color(.015f, .025f, .045f, .96f);
            backAction = back;
            readyButton = null;
            BrandChrome();
            Text(menu.transform, "Title", title, 64, new Vector2(.095f, .8f), new Vector2(.94f, .92f)).color = cyan;
        }

        private void OnlineMenu()
        {
            connectionStatus = null;
            NetworkPanel("ONLINE DUEL",() => { ClearFormError(); RefreshMenu(); });
            Text(menu.transform, "Help", "HOST A MATCH\nChoose a port, then share your IPv4 address.\n\nJOIN A MATCH\nEnter the host's address and matching port.\n\nBoth players need the same build.\nFor internet play, the host must allow\nand forward this UDP port.\n127.0.0.1 connects to this PC only.", 22,
                new Vector2(.53f, .39f), new Vector2(.94f, .74f));
            Input("HOST IPv4 ADDRESS", hostAddress, .66f, value => { hostAddress = value; ClearFormError(); }, 45);
            Input("UDP PORT", hostPort, .5f, value => { hostPort = value; ClearFormError(); }, 5);
            float y = .35f;
            Button("HOST DUEL", () => Session.HostDuel(Port()), ref y);
            Button("JOIN HOST", () => Session.JoinDuel(hostAddress.Trim(), Port()), ref y);
            Button("BACK", () => { ClearFormError(); RefreshMenu(); }, ref y);
            if(!string.IsNullOrEmpty(Session.ConnectionError))
            {
                var panel=Graphic(menu.transform,"Connection error panel",new Vector2(.53f,.14f),new Vector2(.94f,.36f),new Color(.16f,.09f,.045f));
                connectionErrorPanel=panel.gameObject;
                Icon(panel.transform,"Error icon","alert",new Vector2(.035f,.75f),new Vector2(.095f,.95f),amber);
                Text(panel.transform,"Error heading","CONNECTION NEEDS ATTENTION",20,new Vector2(.12f,.75f),new Vector2(.96f,.95f)).color=amber;
                Text(panel.transform,"Connection error",Session.ConnectionError,24,new Vector2(.04f,.08f),new Vector2(.96f,.72f)).color=Color.white;
            }
        }

        private void ClearFormError()
        {
            Session.ClearConnectionError();
            if(connectionErrorPanel!=null)connectionErrorPanel.SetActive(false);
        }

        private int Port() => int.TryParse(hostPort, out int port) ? port : 0;

        private void ConnectionLobby()
        {
            bool results = Session.Match.Phase == Simulation.MatchPhase.Results;
            NetworkPanel(results ? "DUEL COMPLETE" : "DUEL // READY ROOM");
            if (results) Text(menu.transform, "Winner", "WINNER: " + Session.Actors[Session.Match.Winner].DisplayName, 32,
                new Vector2(.095f, .69f), new Vector2(.9f, .77f));
            connectionStatus = Text(menu.transform, "Connection status", Session.ConnectionStatus, results?21:30,
                new Vector2(.095f, results ? .54f:.48f), new Vector2(results ? .46f:.94f, .66f));
            float y = .35f;
            readyButton=Button("READY UP",Session.ToggleReady,ref y);
            RefreshReadyAction();
            Button("DISCONNECT", Session.MainMenu, ref y);
            Button("QUIT", Session.QuitGame, ref y);
            if(results)
            {
                var final=CreateStandings(menu.transform,"FinalStandings","FINAL STANDINGS",new Vector2(.50f,.18f),new Vector2(.97f,.66f));
                RefreshStandings(final);
            }
            Text(menu.transform, "Ready help", results ? "Both players must ready up for another duel." :
                "Both players must ready up.\nThe host then starts the three-second countdown.", results?20:25,
                new Vector2(results ? .095f:.53f, results ? .10f:.25f), new Vector2(results ? .46f:.94f, results ? .18f:.4f));
        }

        private void Input(string label, string value, float y, UnityEngine.Events.UnityAction<string> changed, int limit)
        {
            Text(menu.transform, label, label, 23, new Vector2(.095f, y + .065f), new Vector2(.46f, y + .11f));
            var root = new GameObject(label + " entry", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.InputField));
            root.transform.SetParent(menu.transform, false);
            Rect(root, new Vector2(.095f, y), new Vector2(.46f, y + .06f));
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(.08f, .14f, .19f);
            var text = Text(root.transform, "Value", value, 28, new Vector2(.03f, .05f), new Vector2(.97f, .95f));
            text.alignment = TextAnchor.MiddleLeft;
            var field = root.GetComponent<UnityEngine.UI.InputField>();
            field.targetGraphic=root.GetComponent<UnityEngine.UI.Image>();
            field.targetGraphic.color=Color.white;
            var colors=field.colors;colors.normalColor=new Color(.11f,.17f,.21f).linear;colors.selectedColor=new Color(.08f,.32f,.36f).linear;
            colors.highlightedColor=new Color(.12f,.26f,.30f).linear;field.colors=colors;
            field.caretColor=cyan;field.selectionColor=new Color(cyan.r,cyan.g,cyan.b,.3f);
            field.textComponent = text;
            field.text = value;
            field.characterLimit = limit;
            field.onValueChanged.AddListener(changed);
        }

        private void RefreshReadyAction()
        {
            if(readyButton==null)return;
            readyButton.interactable=Session.CanReady;
            bool results=Session.Match?.Phase==Simulation.MatchPhase.Results;
            readyButton.GetComponentInChildren<UnityEngine.UI.Text>().text=!Session.CanReady?"WAITING FOR OPPONENT":
                Session.LocalReady?(results?"CANCEL REMATCH":"CANCEL READY"):(results?"READY FOR REMATCH":"READY UP");
        }
    }
}
