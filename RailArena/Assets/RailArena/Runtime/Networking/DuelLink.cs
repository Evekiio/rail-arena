using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace RailArena.Networking
{
    public enum DuelLinkState { Listening, Connecting, Connected, Closed, Failed }

    /// <summary>One-peer connection, compatibility handshake, ready state and liveness over UDP.</summary>
    public sealed class DuelLink : IDisposable
    {
        public const double ConnectionTimeout = 8;
        private const double RetryInterval = .25;
        private readonly DuelSocket socket;
        private readonly ulong build;
        private readonly ulong nonce;
        private readonly byte[] cookieKey = new byte[32];
        private IPEndPoint peer;
        private ulong session;
        private uint sendSequence;
        private uint lastSequence;
        private bool receivedSequence;
        private bool challenged;
        private double lastReceive;
        private double lastSend = double.NegativeInfinity;
        private double started = double.NaN;
        private double previousTime = double.NegativeInfinity;
        public bool IsHost { get; }
        public DuelLinkState State { get; private set; }
        public string Error { get; private set; } = "";
        public bool LocalReady { get; private set; }
        public bool RemoteReady { get; private set; }
        public int Round { get; private set; }
        public DuelSnapshot LatestSnapshot { get; private set; }
        public IPEndPoint LocalEndPoint => socket.LocalEndPoint;
        public event Action<DuelInput[]> InputsReceived;

        private DuelLink(bool host, int port, IPEndPoint endpoint, ulong build)
        {
            if (build == 0) throw new ArgumentOutOfRangeException(nameof(build));
            if (!host && (endpoint == null || endpoint.Port == 0 || endpoint.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork))
                throw new ArgumentException("An IPv4 host and nonzero port are required.");
            IsHost = host;
            this.build = build;
            peer = endpoint;
            using var random = RandomNumberGenerator.Create();
            random.GetBytes(cookieKey);
            nonce = BitConverter.ToUInt64(cookieKey, 0) | 1UL;
            socket = new DuelSocket(port);
            State = host ? DuelLinkState.Listening : DuelLinkState.Connecting;
        }

        public static DuelLink Host(int port, ulong build) => new(true, port, null, build);
        public static DuelLink Join(IPEndPoint endpoint, ulong build) => new(false, 0, endpoint, build);

        public void SetReady(bool ready) { LocalReady = ready; lastSend = double.NegativeInfinity; }

        public void SetRound(int round)
        {
            if (!IsHost) throw new InvalidOperationException("Only the host advances rounds.");
            if (round <= Round) throw new ArgumentOutOfRangeException(nameof(round));
            Round = round;
            LocalReady = RemoteReady = false;
        }

        public void Tick(double now)
        {
            if (double.IsNaN(now) || double.IsInfinity(now) || now < 0 || now < previousTime)
                throw new ArgumentOutOfRangeException(nameof(now), "Use a monotonic seconds clock.");
            previousTime = now;
            if (State == DuelLinkState.Closed || State == DuelLinkState.Failed) return;
            if (double.IsNaN(started)) { started = now; lastReceive = now; }
            try
            {
                // Malformed and foreign traffic consume the same budget as valid messages.
                for (int i = 0; i < 64 && socket.Receive(out var packet, out var sender); i++)
                    if (packet != null && sender != null) Process(packet, sender, now);
                if (State == DuelLinkState.Connecting && now - started >= ConnectionTimeout)
                    Fail("Connection timed out. Check the host address, UDP port and firewall.");
                if (State == DuelLinkState.Connected && now - lastReceive >= ConnectionTimeout)
                    Fail("The other player stopped responding.");
                if (now - lastSend < RetryInterval) return;
                if (State == DuelLinkState.Connecting)
                    Send(new DuelPacket { Kind = challenged ? DuelMessage.Connect : DuelMessage.Hello,
                        Nonce = nonce, Build = build }, peer);
                else if (State == DuelLinkState.Connected)
                    Send(new DuelPacket { Kind = DuelMessage.Ready, Round = Round, Ready = LocalReady }, peer);
                lastSend = now;
            }
            catch (System.Net.Sockets.SocketException e) { Fail("Network socket error: " + e.SocketErrorCode); }
        }

        private void Process(DuelPacket packet, IPEndPoint sender, double now)
        {
            if (IsHost && (packet.Kind == DuelMessage.Hello || packet.Kind == DuelMessage.Connect))
            {
                if (packet.Build != build)
                { Send(new DuelPacket { Kind = DuelMessage.Reject, Rejection = RejectReason.Incompatible }, sender); return; }
                if (State == DuelLinkState.Connected)
                {
                    if (sender.Equals(peer) && packet.Kind == DuelMessage.Connect && packet.Session == session)
                        SendWelcome(packet.Nonce);
                    else Send(new DuelPacket { Kind = DuelMessage.Reject, Rejection = RejectReason.Full }, sender);
                    return;
                }
                if (State != DuelLinkState.Listening) return;
                if (packet.Kind == DuelMessage.Hello)
                {
                    ulong challenge = Cookie(sender, packet.Nonce, (long)(now / 4));
                    Send(new DuelPacket { Kind = DuelMessage.Challenge, Session = challenge,
                        Nonce = packet.Nonce, Build = build }, sender, false);
                }
                else if (packet.Session != 0 && (packet.Session == Cookie(sender, packet.Nonce, (long)(now / 4)) ||
                    packet.Session == Cookie(sender, packet.Nonce, (long)(now / 4) - 1)))
                {
                    peer = sender;
                    session = packet.Session;
                    State = DuelLinkState.Connected;
                    lastReceive = now;
                    SendWelcome(packet.Nonce);
                }
                return;
            }
            if (!sender.Equals(peer)) return;
            if (!IsHost && State == DuelLinkState.Connecting)
            {
                if (packet.Kind == DuelMessage.Reject)
                { Fail(packet.Rejection == RejectReason.Incompatible ? "Incompatible game builds. Both players need the same build." : "The host already has a player."); return; }
                if (packet.Nonce != nonce || packet.Build != build || packet.Session == 0) return;
                if (packet.Kind == DuelMessage.Challenge && !challenged)
                { session = packet.Session; challenged = true; lastSend = double.NegativeInfinity; }
                else if (packet.Kind == DuelMessage.Welcome && challenged && packet.Session == session)
                { State = DuelLinkState.Connected; lastReceive = now; }
                return;
            }
            if (State != DuelLinkState.Connected || packet.Session != session || !Fresh(packet.Sequence)) return;
            // A correctly authenticated but inappropriate message cannot refresh the timeout.
            switch (packet.Kind)
            {
                case DuelMessage.Ready when packet.Round == Round:
                    RemoteReady = packet.Ready;
                    break;
                case DuelMessage.Input when IsHost && packet.Round == Round:
                    InputsReceived?.Invoke(packet.Inputs);
                    break;
                case DuelMessage.Snapshot when !IsHost && packet.Snapshot.Round >= Round:
                    if (packet.Snapshot.Round > Round)
                    { Round = packet.Snapshot.Round; LocalReady = RemoteReady = false; }
                    LatestSnapshot = packet.Snapshot;
                    break;
                case DuelMessage.Leave:
                    State = DuelLinkState.Closed;
                    Error = "The other player disconnected.";
                    break;
                default: return;
            }
            receivedSequence = true;
            lastSequence = packet.Sequence;
            lastReceive = now;
        }

        public void SendInputs(DuelInput[] inputs)
        {
            if (IsHost || State != DuelLinkState.Connected) return;
            Send(new DuelPacket { Kind = DuelMessage.Input, Round = Round, Inputs = inputs }, peer);
        }

        public void SendSnapshot(DuelSnapshot snapshot)
        {
            if (!IsHost || State != DuelLinkState.Connected) return;
            if (snapshot == null || snapshot.Round != Round) throw new ArgumentException("Snapshot round must match the connection.");
            Send(new DuelPacket { Kind = DuelMessage.Snapshot, Snapshot = snapshot }, peer);
        }

        private bool Fresh(uint sequence) => !receivedSequence || unchecked((int)(sequence - lastSequence)) > 0;
        private void SendWelcome(ulong clientNonce) => Send(new DuelPacket { Kind = DuelMessage.Welcome, Nonce = clientNonce, Build = build }, peer);
        private void Send(DuelPacket packet, IPEndPoint destination, bool useSession = true)
        {
            if (useSession) packet.Session = session;
            packet.Sequence = ++sendSequence;
            socket.Send(packet, destination);
        }

        private ulong Cookie(IPEndPoint endpoint, ulong clientNonce, long timeBucket)
        {
            using var hmac = new HMACSHA256(cookieKey);
            byte[] digest = hmac.ComputeHash(Encoding.ASCII.GetBytes(endpoint + "/" + clientNonce + "/" + timeBucket));
            return BitConverter.ToUInt64(digest, 0) | 1UL;
        }

        private void Fail(string error) { State = DuelLinkState.Failed; Error = error; }
        public void Dispose()
        {
            if (State == DuelLinkState.Connected)
            {
                try { Send(new DuelPacket { Kind = DuelMessage.Leave }, peer); }
                catch (System.Net.Sockets.SocketException) { }
            }
            State = DuelLinkState.Closed;
            socket.Dispose();
        }
    }
}
