using System;

namespace IotRouter
{
    public delegate void MessageEventHandler(object sender, MessageReceivedEventArgs e);

    public class MessageReceivedEventArgs : EventArgs
    {
        public string Topic { get; private set; }
        public byte[] Payload { get; private set; }

        public MessageReceivedEventArgs(string topic, byte[] payload = null)
        {
            Topic = topic;
            Payload = payload;
        }
    }
}
