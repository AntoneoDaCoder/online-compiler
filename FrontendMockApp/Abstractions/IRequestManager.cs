namespace FrontendMockApp.Abstractions
{
    public interface IRequestManager
    {
        void ConsumeMessage(string message);
        void RenderFrame();
    }
}
