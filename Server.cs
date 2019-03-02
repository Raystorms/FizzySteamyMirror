using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Steamworks;
using System.Threading.Tasks;

namespace FizzySteam
{

    internal class SteamClient
    {
        public enum ConnectionState
        {
            CONNECTED,
            DISCONNECTING,
        }

        public CSteamID steamID;
        public ConnectionState state;
        public int connectionID;
        public float timeIdle = 0;

        public SteamClient(ConnectionState state, CSteamID steamID, int connectionID)
        {
            this.state = state;
            this.steamID = steamID;
            this.connectionID = connectionID;
            this.timeIdle = 0;
        }
    }

    internal class SteamConnectionMap : IEnumerable<KeyValuePair<int, SteamClient>>
    {
        public readonly Dictionary<CSteamID, SteamClient> fromSteamID = new Dictionary<CSteamID, SteamClient>();
        public readonly Dictionary<int, SteamClient> fromConnectionID = new Dictionary<int, SteamClient>();

        public SteamConnectionMap()
        {
        }

        public int Count
        {
            get { return fromSteamID.Count; }
        }

        public SteamClient Add(CSteamID steamID, int connectionID, SteamClient.ConnectionState state)
        {
            var newClient = new SteamClient(state, steamID, connectionID);
            fromSteamID.Add(steamID, newClient);
            fromConnectionID.Add(connectionID, newClient);

            return newClient;
        }

        public void Remove(SteamClient steamClient)
        {
            fromSteamID.Remove(steamClient.steamID);
            fromConnectionID.Remove(steamClient.connectionID);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<int, SteamClient>> GetEnumerator()
        {
            return fromConnectionID.GetEnumerator();
        }
    }

    public class Server : Common
    {

        enum ConnectionState : byte
        {
            OFFLINE,
            LISTENING
        }

        public event Action<int> OnConnected;
        public event Action<int, byte[]> OnReceivedData;
        public event Action<int> OnDisconnected;
        public event Action<int, Exception> OnReceivedError;

        private ConnectionState state = ConnectionState.OFFLINE;
        private SteamConnectionMap steamConnectionMap;
        private int nextConnectionID;
        private int maxConnections;

        public bool Listening { get { return state == ConnectionState.LISTENING; } private set { if (value) state = ConnectionState.LISTENING; } }
        public bool Offline
        {
            get { return state == ConnectionState.OFFLINE; }
            private set
            {
                if (value)
                {
                    state = ConnectionState.OFFLINE;

                    deinitialise();
                }
            }
        }

        public async void Listen(int maxConnections = int.MaxValue)
        {
            Debug.Log("Listen Start");
            //todo check we are not already listening ?

            initialise();
            Listening = true;
            this.maxConnections = maxConnections;

            await InternalReceiveLoop();

            await ReceiveLoop();

            Debug.Log("Listen Stop");
        }

        protected override void OnNewConnectionInternal(P2PSessionRequest_t result)
        {
            Debug.Log("OnNewConnectionInternal in server");

            SteamNetworking.AcceptP2PSessionWithUser(result.m_steamIDRemote);
        }


        //start a async loop checking for internal messages and processing them. This includes internal connect negotiation and disconnect requests so runs outside "connected"
        private async Task InternalReceiveLoop()
        {
            Debug.Log("InternalReceiveLoop Start");

            uint readPacketSize;
            CSteamID clientSteamID;

            try
            {
                while (!Offline)
                {

                    while (ReceiveInternal(out readPacketSize, out clientSteamID))
                    {
                        Debug.Log("InternalReceiveLoop - data");
                        if (readPacketSize != 1)
                        {
                            continue;
                        }
                        Debug.Log("InternalReceiveLoop - received " + receiveBufferInternal[0]);
                        switch (receiveBufferInternal[0])
                        {
                            //requesting to connect to us
                            case (byte)InternalMessages.CONNECT:
                                if(steamConnectionMap.Count >= maxConnections)
                                {
                                    SendInternal(clientSteamID, disconnectMsgBuffer);
                                    continue;
                                    //too many connections, reject
                                }
                                SendInternal(clientSteamID, acceptConnectMsgBuffer);

                                int connectionId = nextConnectionID++;
                                steamConnectionMap.Add(clientSteamID, connectionId, SteamClient.ConnectionState.CONNECTED);
                                OnConnected?.Invoke(connectionId);
                                break;

                            //asking us to disconnect
                            case (byte)InternalMessages.DISCONNECT:
                                try
                                {
                                    SteamClient steamClient = steamConnectionMap.fromSteamID[clientSteamID];
                                    steamConnectionMap.Remove(steamClient);
                                    OnDisconnected?.Invoke(steamClient.connectionID);
                                    CloseP2PSessionWithUser(steamClient.steamID);
                                }
                                catch (KeyNotFoundException)
                                {
                                    //we have no idea who this connection is
                                    Debug.LogError("Trying to disconnect a client thats not known SteamID " + clientSteamID);
                                }

                                break;
                        }
                    }

                    //not got a message - wait a bit more
                    await Task.Delay(TimeSpan.FromSeconds(secondsBetweenPolls));
                }
            }
            catch (ObjectDisposedException) { }

            Debug.Log("InternalReceiveLoop Stop");
        }

        private async Task ReceiveLoop()
        {
            Debug.Log("ReceiveLoop Start");

            uint readPacketSize;
            CSteamID clientSteamID;

            try
            {
                byte[] receiveBuffer;
                while (!Offline)
                {
                    for (int i = 0; i < channels.Length; i++) {
                        while (Receive(out readPacketSize, out clientSteamID, out receiveBuffer, i)) {
                            if (readPacketSize == 0) {
                                continue;
                            }

                            try {
                                int connectionId = steamConnectionMap.fromSteamID[clientSteamID].connectionID;
                                // we received some data,  raise event
                                OnReceivedData?.Invoke(connectionId, receiveBuffer);
                            } catch (KeyNotFoundException) {
                                CloseP2PSessionWithUser(clientSteamID);
                                //we have no idea who this connection is
                                Debug.LogError("Data received from steam client thats not known " + clientSteamID);
                            }
                        }
                    }
                    //not got a message - wait a bit more
                    await Task.Delay(TimeSpan.FromSeconds(secondsBetweenPolls));
                }
            }
            catch (ObjectDisposedException) { }

            Debug.Log("ReceiveLoop Stop");
        }

        // check if the server is running
        public bool Active
        {
            get { return Listening; }
        }

        public void Stop()
        {
            Debug.LogWarning("Server Stop");
            // only if started
            if (!Active) return;

            Offline = true;

            deinitialise();
            Debug.Log("Server Stop Finished");
        }

        // disconnect (kick) a client
        public bool Disconnect(int connectionId)
        {
            try
            {
                SteamClient steamClient = steamConnectionMap.fromConnectionID[connectionId];
                Disconnect(steamClient);
                return true;
            }
            catch (KeyNotFoundException)
            {
                //we have no idea who this connection is
                Debug.LogError("Tryign to disconnect a connection thats not known " + connectionId);
            }
            Debug.LogWarning("Tryign to disconnect a connection thats not known " + connectionId);
            return false;
        }

        private async void Disconnect(SteamClient steamClient)
        {
            if(steamClient.state != SteamClient.ConnectionState.CONNECTED)
            {
                return;
            }

            SendInternal(steamClient.steamID, disconnectMsgBuffer);
            steamClient.state = SteamClient.ConnectionState.DISCONNECTING;

            //Wait a short time before calling steams disconnect function so the message has time to go out
            await Task.Delay(100);
            CloseP2PSessionWithUser(steamClient.steamID);
        }

        public bool Send(int connectionId, byte[] data, int channelId = 0)
        {
            try
            {
                SteamClient steamClient = steamConnectionMap.fromConnectionID[connectionId];
                //will default to reliable at channel 0 if sent on an unknown channel
                Send(steamClient.steamID, data, channelToSendType(channelId), channelId >= channels.Length ? 0 : channelId);
                return true;
            }
            catch (KeyNotFoundException)
            {
                //we have no idea who this connection is
                Debug.LogError("Tryign to Send on a connection thats not known " + connectionId);
                return false;
            }

        }

        public string ServerGetClientAddress(int connectionId)
        {
            try
            {
                SteamClient steamClient = steamConnectionMap.fromConnectionID[connectionId];
                return steamClient.steamID.ToString();
            }
            catch (KeyNotFoundException)
            {
                //we have no idea who this connection is
                Debug.LogError("Trying to get info on an unknown connection " + connectionId);
            }

            return null;
        }

        protected override void initialise()
        {
            base.initialise();

            nextConnectionID = 1;
            steamConnectionMap = new SteamConnectionMap();
        }
    }
}
