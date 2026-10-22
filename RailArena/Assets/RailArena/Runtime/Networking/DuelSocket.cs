using System;
using System.Net;
using System.Net.Sockets;

namespace RailArena.Networking
{
    /// <summary>Main-thread nonblocking IPv4 UDP. The caller bounds receive work per frame.</summary>
    public sealed class DuelSocket : IDisposable
    {
        private readonly Socket socket;
        private readonly byte[] receiveBuffer = new byte[DuelProtocol.MaximumDatagram + 1];
        public IPEndPoint LocalEndPoint => (IPEndPoint)socket.LocalEndPoint;

        public DuelSocket(int port)
        {
            if (port < 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                socket.Blocking = false;
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            catch { socket.Dispose(); throw; }
        }

        public bool Send(DuelPacket packet, IPEndPoint peer)
        {
            if (peer == null || peer.Port == 0 || peer.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("An IPv4 address and nonzero port are required.", nameof(peer));
            byte[] data = DuelProtocol.Encode(packet);
            try { return socket.SendTo(data, peer) == data.Length; }
            catch (SocketException e) when (Transient(e.SocketErrorCode)) { return false; }
        }

        // True means a datagram was consumed. Invalid traffic returns a null packet and still uses the receive budget.
        public bool Receive(out DuelPacket packet, out IPEndPoint peer)
        {
            packet = null;
            peer = null;
            EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
            int count;
            try { count = socket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref endpoint); }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.MessageSize) { return true; }
            catch (SocketException e) when (Transient(e.SocketErrorCode)) { return false; }
            peer = (IPEndPoint)endpoint;
            if (count > DuelProtocol.MaximumDatagram) return true;
            var data = new byte[count];
            Buffer.BlockCopy(receiveBuffer, 0, data, 0, count);
            DuelProtocol.TryDecode(data, out packet);
            return true;
        }

        private static bool Transient(SocketError error) => error == SocketError.WouldBlock || error == SocketError.IOPending ||
            error == SocketError.NoBufferSpaceAvailable || error == SocketError.ConnectionReset;
        public void Dispose() => socket.Dispose();
    }
}
