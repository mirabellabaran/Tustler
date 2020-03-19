using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tustler.Models;
using TustlerServicesLib;

namespace Tustler.Helpers
{
    /// <summary>
    /// Modified from: https://www.codeproject.com/Questions/149630/Playing-video-from-a-memory-stream Solution 4
    /// </summary>
    public static class AudioStreamer
    {
        ///// <summary>
        ///// Creates a TCP listener on port 12. When a client connects, it starts streaming the obj stream.
        ///// </summary>
        ///// <remarks>The parameter is type object for compatibility with ParameterizedThreadStart</remarks>
        ///// <param name="obj">A stream type (typically a MemoryStream)</param>
        //private static void StreamAudioImpl(object obj)
        //{
        //    Stream source = (Stream) obj;
        //    TcpListener listener = new TcpListener(IPAddress.Loopback, 12);
        //    listener.Start();

        //    TcpClient client = listener.AcceptTcpClient();
        //    // HTTP Headers
        //    client.Client.Send(Encoding.Default.GetBytes("HTTP/1.1 200 OK\nDate: Tue, 02 Aug 2011 22:24:05 GMT\nServer: Apache/2.2.8 (Win32) PHP/5.2.5\nLast-Modified: Tue, 02 Aug 2011 22:21:13 GMT\nETag: \"1000000009896-1743f-4a98d2a63ee22\"\nAccept-Ranges: bytes\nContent-Length: 95295\nKeep-Alive: timeout=5, max=100\nConnection: Keep-Alive\nContent-Type: text/plain"));

        //    int length = 0;
        //    byte[] buffer = new byte[1024];

        //    while (source.CanRead)
        //    {
        //        try
        //        {
        //            length = source.Read(buffer, 0, 1024);
        //            client.GetStream().Write(buffer, 0, length);
        //        }
        //        finally
        //        {
        //            listener.Stop();
        //        }
        //    }
        //}

        //public static void StreamAudio(MemoryStream stream)
        //{
        //    Thread th = new Thread(StreamAudioImpl);
        //    th.Start(stream);
        //}

        public static async Task StreamAudioAsync(MemoryStream stream, string contentType, string prefix, NotificationsList notifications)
        {
            if (!HttpListener.IsSupported)
            {
                notifications.ShowMessage("Audio streaming service is not available", "The HttpListener class is built on top of HTTP.sys, which is the kernel mode listener that handles HTTP traffic on Windows.");
                return;
            }

            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(prefix);

                listener.Start();
                listener.TimeoutManager.IdleConnection = TimeSpan.FromSeconds(20.0);
                HttpListenerContext context = await listener.GetContextAsync().ConfigureAwait(true);    //  blocks while waiting for a request

                // incoming client request
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                response.ContentType = contentType;

                //string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
                //byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                //response.ContentLength64 = buffer.Length;

                response.ContentLength64 = stream.Length;

                Stream output = response.OutputStream;
                output.Write(stream.GetBuffer(), 0, (int) stream.Length);
                output.Close();

                listener.Stop();
            }
        }

    }
}
