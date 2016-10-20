namespace GeekseatBus
{
    public interface IGsEventPublisher
    {
        void Publish<T>(T eventMessage);
    }
}