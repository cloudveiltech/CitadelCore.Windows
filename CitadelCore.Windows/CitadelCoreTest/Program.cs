﻿/*
* Copyright © 2017 Jesse Nicholson
* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CitadelCore.Logging;
using CitadelCore.Net.Proxy;
using CitadelCore.Windows.Net.Proxy;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace CitadelCoreTest
{
    internal class Program
    {
        private static byte[] s_blockPageBytes;

        private static bool OnFirewallCheck(string binaryAbsPath)
        {
            // Only filter firefox.
            var filtering = binaryAbsPath.IndexOf("firefox", StringComparison.OrdinalIgnoreCase) != -1;

            if(filtering)
            {
                Console.WriteLine("Filtering application {0}", binaryAbsPath);
            }

            return filtering;
        }

        private static void OnMsgBegin(Uri reqUrl, string headers, byte[] body, MessageType msgType, MessageDirection msgDirection, out ProxyNextAction nextAction, out string customBlockResponseContentType, out byte[] customBlockResponse)
        {
            customBlockResponseContentType = string.Empty;
            nextAction = ProxyNextAction.AllowAndIgnoreContent;
            customBlockResponse = null;
            return;

            Console.WriteLine(nameof(OnMsgBegin));

            if(reqUrl.Host.Equals("777.com", StringComparison.OrdinalIgnoreCase))
            {
                nextAction = ProxyNextAction.DropConnection;
                customBlockResponseContentType = "text/html";
                customBlockResponse = s_blockPageBytes;
                return;
            }

            nextAction = ProxyNextAction.AllowAndIgnoreContent;

            if(msgDirection == MessageDirection.Response)
            {
                Console.WriteLine("Got HTTP response.");
                
                if(headers.IndexOf("html") != -1)
                {
                    Console.WriteLine("Requesting to inspect HTML response.");
                    nextAction = ProxyNextAction.AllowButRequestContentInspection;
                }

                //Console.WriteLine(headers);
            }
            
            customBlockResponseContentType = string.Empty;
            customBlockResponse = null;
        }

        private static void OnMsgEnd(Uri reqUrl, string headers, byte[] body, MessageType msgType, MessageDirection msgDirection, out bool shouldBlock, out string customBlockResponseContentType, out byte[] customBlockResponse)
        {
            shouldBlock = false;
            customBlockResponseContentType = string.Empty;
            customBlockResponse = null;

            Console.WriteLine(nameof(OnMsgEnd));

            if(msgDirection == MessageDirection.Response)
            {
                Console.WriteLine("Got http response for inspection.");

                if(body != null)
                {
                    Console.WriteLine("Http HTML or JSON response body is {0} bytes long.", body.Length);

                    Console.Write(headers);

                    // We should check Content-Type for charset=XXXX.
                    var htmlResponse = Encoding.UTF8.GetString(body);

                    Console.WriteLine(htmlResponse);

                    if(htmlResponse.IndexOf("777.com") != -1)
                    {
                        shouldBlock = true;
                    }
                }
            }
        }

        private static void Main(string[] args)
        {
            s_blockPageBytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BlockedPage.html"));
            
            // Let the user decide when to quit with ctrl+c.
            var manualResetEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                manualResetEvent.Set();
                Console.WriteLine("Shutting Down");
            };

            // Hooking into these properties gives us an abstract interface where we may
            // use informational, warning and error messages generated by the internals of
            // the proxy in whatsoever way we see fit, though the design was to allow users
            // to choose logging mechanisms.
            LoggerProxy.Default.OnInfo += (msg) =>
            {
                Console.WriteLine("INFO: {0}", msg);
            };

            LoggerProxy.Default.OnWarning += (msg) =>
            {
                Console.WriteLine("WARN: {0}", msg);
            };

            LoggerProxy.Default.OnError += (msg) =>
            {
                Console.WriteLine("ERRO: {0}", msg);
            };
            
            // Just create the server.
            var proxyServer = new WindowsProxyServer(OnFirewallCheck, OnMsgBegin, OnMsgEnd);
            
            // Give it a kick.
            proxyServer.Start();

            // And you're up and running.
            Console.WriteLine("Proxy Running");

            Console.WriteLine("Listening for IPv4 HTTP connections on port {0}.", proxyServer.V4HttpEndpoint.Port);
            Console.WriteLine("Listening for IPv4 HTTPS connections on port {0}.", proxyServer.V4HttpsEndpoint.Port);
            Console.WriteLine("Listening for IPv6 HTTP connections on port {0}.", proxyServer.V6HttpEndpoint.Port);
            Console.WriteLine("Listening for IPv6 HTTPS connections on port {0}.", proxyServer.V6HttpsEndpoint.Port);

            // Don't exit on me yet fam.
            manualResetEvent.WaitOne();

            Console.WriteLine("Exiting.");

            // Stop if you must.
            proxyServer.Stop();
        }
    }
}