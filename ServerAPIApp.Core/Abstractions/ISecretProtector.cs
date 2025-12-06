namespace ServerAPIApp.Core.Abstractions
{
    public interface ISecretProtector
    {
        string Protect(string plain);
        string Unprotect(string protectedText);
    }
}
