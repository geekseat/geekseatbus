namespace GeekseatBus
{
    public interface IGsHandler<in T>
    {
        void Handle(T message);
    }
}