using UnityEngine;

namespace RailArena
{
    public sealed partial class ArenaHud
    {
        private UnityEngine.UI.Text connectionStatus;
        private string hostAddress = "127.0.0.1";
        private string hostPort = "7777";

        private void NetworkPanel(string title)
        {
            if (menu != null) Destroy(menu);
            menu = new GameObject("Connection menu", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            menu.transform.SetParent(canvas, false);
            Rect(menu, Vector2.zero, Vector2.one);
            menu.GetComponent<UnityEngine.UI.Image>().color = new Color(.015f, .025f, .045f, .96f);
            Text(menu.transform, "Title", title, 64, new Vector2(.095f, .8f), new Vector2(.94f, .92f)).color = cyan;
        }

        private void OnlineMenu()
        {
            connectionStatus = null;
            NetworkPanel("ONLINE DUEL");
            Text(menu.transform, "Help", "Two humans. Host owns the match.\nUse the same game build on both computers.\n\nLAN: use the host's local IPv4 address.\nInternet: forward the selected UDP port to the host.\nAllow RailArena through the host's firewall.", 25,
                new Vector2(.53f, .3f), new Vector2(.94f, .72f));
            Input("HOST IPv4 ADDRESS", hostAddress, .66f, value => hostAddress = value, 45);
            Input("UDP PORT", hostPort, .5f, value => hostPort = value, 5);
            float y = .35f;
            Button("HOST DUEL", () => Session.HostDuel(Port()), ref y);
            Button("JOIN HOST", () => Session.JoinDuel(hostAddress.Trim(), Port()), ref y);
            Button("BACK", RefreshMenu, ref y);
        }

        private int Port() => int.TryParse(hostPort, out int port) ? port : 0;

        private void ConnectionLobby()
        {
            bool results = Session.Match.Phase == Simulation.MatchPhase.Results;
            NetworkPanel(results ? "DUEL COMPLETE" : "DUEL // READY ROOM");
            if (results) Text(menu.transform, "Winner", "WINNER: " + Session.Actors[Session.Match.Winner].DisplayName, 32,
                new Vector2(.095f, .69f), new Vector2(.9f, .77f));
            connectionStatus = Text(menu.transform, "Connection status", Session.ConnectionStatus, 30,
                new Vector2(.095f, .48f), new Vector2(.94f, .66f));
            float y = .35f;
            Button(results ? "READY / CANCEL REMATCH" : "READY / CANCEL READY", Session.ToggleReady, ref y);
            Button("DISCONNECT", Session.MainMenu, ref y);
            Text(menu.transform, "Ready help", results ? "Both players must ready up for another duel." :
                "Both players must ready up.\nThe host then starts the three-second countdown.", 25,
                new Vector2(.53f, .25f), new Vector2(.94f, .4f));
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
            field.textComponent = text;
            field.text = value;
            field.characterLimit = limit;
            field.onValueChanged.AddListener(changed);
        }
    }
}
