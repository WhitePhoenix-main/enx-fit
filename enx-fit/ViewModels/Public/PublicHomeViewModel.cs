namespace enx_fit.ViewModels.Public;

public sealed record PublicHomeViewModel(
    bool SignedIn,
    string StartUrl,
    string SubscriptionUrl)
{
    public string StartLabel => SignedIn ? "Открыть приложение" : "Начать бесплатно";
}
