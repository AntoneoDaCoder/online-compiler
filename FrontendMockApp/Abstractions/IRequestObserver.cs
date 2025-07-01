namespace FrontendMockApp.Abstractions
{
    public interface IRequestObserver
    {
        void Subscribe(IRequestManager subscriber);
        void Unsubscribe(IRequestManager subscriber);
        void NotifySubscribers(string message);
    }
}
