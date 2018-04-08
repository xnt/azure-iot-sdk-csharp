// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class AmqpClientConnection
    {
        readonly AmqpSettings _amqpSettings;
        readonly Uri _uri;

        internal AmqpClientConnection(Uri uri, AmqpSettings amqpSettings)
        {
            _uri = uri;
            _amqpSettings = amqpSettings;

            AmqpConnectionSettings = new AmqpConnectionSettings
            {
                ContainerId = Guid.NewGuid().ToString(),
                HostName = _uri.Host
            };
        }

        public AmqpConnection AmqpConnection { get; private set; }

        public AmqpConnectionSettings AmqpConnectionSettings { get; private set; }

        public TlsTransportSettings TransportSettings { get; private set; }

        public AmqpClientSession AmqpSession { get; private set; }

        public bool IsConnectionClosed => _isConnectionClosed;

        private bool _isConnectionClosed;

        private TaskCompletionSource<TransportBase> _tcs;

        private ProtocolHeader _sentHeader;

        public async Task OpenAsync(TimeSpan timeout, bool useWebSocket, X509Certificate2 clientCert)
        {
            var hostName = _uri.Host;

            var tcpSettings = new TcpTransportSettings { Host = hostName, Port = _uri.Port != -1 ? _uri.Port : AmqpConstants.DefaultSecurePort };
            TransportSettings = new TlsTransportSettings(tcpSettings)
            {
                TargetHost = hostName,
                Certificate = clientCert
            };

            TransportBase transport;

            if (useWebSocket)
            {
                transport = await CreateClientWebSocketTransportAsync(timeout).ConfigureAwait(false);
                SaslTransportProvider provider = _amqpSettings.GetTransportProvider<SaslTransportProvider>();
                if (provider != null)
                {
                    _sentHeader = new ProtocolHeader(provider.ProtocolId, provider.DefaultVersion);
                    ByteBuffer buffer = new ByteBuffer(new byte[AmqpConstants.ProtocolHeaderSize]);
                    _sentHeader.Encode(buffer);

                    var args = new TransportAsyncCallbackArgs();
                    args.SetBuffer(buffer.Buffer, buffer.Offset, buffer.Length);
                    args.CompletedCallback = OnWriteHeaderComplete;
                    args.Transport = transport;
                    transport.WriteAsync(args);

                    _tcs = new TaskCompletionSource<TransportBase>();
                    transport = await _tcs.Task.ConfigureAwait(false);
                    await transport.OpenAsync(timeout).ConfigureAwait(false);
                }
            }
            else
            {
                var tcpInitiator = new AmqpTransportInitiator(_amqpSettings, TransportSettings);
                transport = await tcpInitiator.ConnectTaskAsync(timeout).ConfigureAwait(false);
            }

            AmqpConnection = new AmqpConnection(transport, _amqpSettings, AmqpConnectionSettings);
            AmqpConnection.Closed += OnConnectionClosed;
            await AmqpConnection.OpenAsync(timeout).ConfigureAwait(false);
            _isConnectionClosed = false;
        }

        public async Task CloseAsync(TimeSpan timeout)
        {
            var connection = AmqpConnection;
            if (connection != null)
            {
                await connection.CloseAsync(timeout).ConfigureAwait(false);
            }
        }

        public void Close()
        {
            var connection = AmqpConnection;
            if (connection != null)
            {
                connection.Close();
            }
        }

        public AmqpClientSession CreateSession()
        {
            AmqpSession = new AmqpClientSession(this);

            return AmqpSession;
        }

        void OnConnectionClosed(object o, EventArgs args)
        {
            _isConnectionClosed = true;
        }

        async Task<TransportBase> CreateClientWebSocketTransportAsync(TimeSpan timeout)
        {
            UriBuilder webSocketUriBuilder = new UriBuilder
            {
                Scheme = WebSocketConstants.Scheme,
                Host = _uri.Host,
                Port = _uri.Port
            };
            var websocket = await CreateClientWebSocketAsync(webSocketUriBuilder.Uri, timeout).ConfigureAwait(false);
            return new ClientWebSocketTransport(
                websocket,
                null,
                null);
        }

        async Task<ClientWebSocket> CreateClientWebSocketAsync(Uri websocketUri, TimeSpan timeout)
        {
            var websocket = new ClientWebSocket();
            // Set SubProtocol to AMQPWSB10
            websocket.Options.AddSubProtocol(WebSocketConstants.SubProtocols.Amqpwsb10);
            websocket.Options.KeepAliveInterval = WebSocketConstants.KeepAliveInterval;
            websocket.Options.SetBuffer(WebSocketConstants.BufferSize, WebSocketConstants.BufferSize);

            // TODO: expose the Proxy setting as public API on the transport layer
            //Check if we're configured to use a proxy server
            try
            {
                IWebProxy webProxy = WebRequest.DefaultWebProxy;
                Uri proxyAddress = webProxy != null ? webProxy.GetProxy(websocketUri) : null;
                if (!websocketUri.Equals(proxyAddress))
                {
                    // Configure proxy server
                    websocket.Options.Proxy = webProxy;
                }
            }
            catch (PlatformNotSupportedException)
            {
                // .NET Core doesn't support WebProxy configuration - ignore this setting.
            }

            if (TransportSettings.Certificate != null)
            {
                websocket.Options.ClientCertificates.Add(TransportSettings.Certificate);
            }

            using (var cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                await websocket.ConnectAsync(websocketUri, cancellationTokenSource.Token).ConfigureAwait(false);
            }

            return websocket;
        }

        private void OnWriteHeaderComplete(TransportAsyncCallbackArgs args)
        {
            if (args.Exception != null)
            {
                CompleteOnException(args);
                return;
            }

            byte[] headerBuffer = new byte[AmqpConstants.ProtocolHeaderSize];
            args.SetBuffer(headerBuffer, 0, headerBuffer.Length);
            args.CompletedCallback = OnReadHeaderComplete;
            args.Transport.ReadAsync(args);
        }

        private void OnReadHeaderComplete(TransportAsyncCallbackArgs args)
        {
            if (args.Exception != null)
            {
                CompleteOnException(args);
                return;
            }

            try
            {
                ProtocolHeader receivedHeader = new ProtocolHeader();
                receivedHeader.Decode(new ByteBuffer(args.Buffer, args.Offset, args.Count));
                if (!receivedHeader.Equals(_sentHeader))
                {
                    throw new AmqpException(AmqpErrorCode.NotImplemented, $"The requested protocol version {_sentHeader} is not supported. The supported version is {receivedHeader}");
                }

                SaslTransportProvider provider = _amqpSettings.GetTransportProvider<SaslTransportProvider>();
                var transport = provider.CreateTransport(args.Transport, true);
                _tcs.TrySetResult(transport);
            }
            catch (Exception ex)
            {
                args.Exception = ex;
                CompleteOnException(args);
            }
        }

        private void CompleteOnException(TransportAsyncCallbackArgs args)
        {
            if (args.Exception != null && args.Transport != null)
            {
                args.Transport.SafeClose(args.Exception);
                args.Transport = null;
                _tcs.TrySetException(args.Exception);
            }
        }
    }
}
