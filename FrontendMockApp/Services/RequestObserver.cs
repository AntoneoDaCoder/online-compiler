using FrontendMockApp.Abstractions;

namespace FrontendMockApp.Services
{
    public class RequestObserver : IRequestObserver
    {
        private List<IRequestManager> _managers = new List<IRequestManager>();

        public void Subscribe(IRequestManager subscriber)
        {
            _managers.Add(subscriber);
        }

        public void Unsubscribe(IRequestManager subscriber)
        {
            _managers.Remove(subscriber);
        }

        public void NotifySubscribers(string message)
        {
            foreach (var manager in _managers)
                manager.ConsumeMessage(message);
        }
    }
}
