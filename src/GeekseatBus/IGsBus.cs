using System;

namespace GeekseatBus
{
    public interface IGsBus : IGsCommandSender, IGsEventPublisher, IDisposable
    {
        bool IsConnected { get; }

        void Connect();
        void Disconnect();
    }
}