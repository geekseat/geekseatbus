namespace GeekseatBus
{
    public interface IGsCommandSender
    {
        void Send<T>(T command);
    }
}