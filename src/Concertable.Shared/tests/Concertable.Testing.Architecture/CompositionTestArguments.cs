namespace Concertable.Testing.Architecture;

public static class CompositionTestArguments
{
    public static string[] Create() =>
    [
        "--environment=Development",
        "--ConnectionStrings:AuthDb=Server=localhost;Database=AuthDb;User Id=sa;Password=Composition1!;TrustServerCertificate=true",
        "--ConnectionStrings:B2BDb=Server=localhost;Database=B2BDb;User Id=sa;Password=Composition1!;TrustServerCertificate=true",
        "--ConnectionStrings:CustomerDb=Server=localhost;Database=CustomerDb;User Id=sa;Password=Composition1!;TrustServerCertificate=true",
        "--ConnectionStrings:SearchDb=Server=localhost;Database=SearchDb;User Id=sa;Password=Composition1!;TrustServerCertificate=true",
        "--ConnectionStrings:PaymentDb=Server=localhost;Database=PaymentDb;User Id=sa;Password=Composition1!;TrustServerCertificate=true",
        "--ConnectionStrings:asb=Endpoint=sb://localhost/;SharedAccessKeyName=composition;SharedAccessKey=Y29tcG9zaXRpb24=",
        "--ConnectionStrings:blobs=UseDevelopmentStorage=true",
        "--Auth:Authority=https://localhost",
        "--Auth:PublicUrl=https://localhost",
        "--ServiceAuth:ClientId=composition",
        "--ServiceAuth:ClientSecret=composition-secret",
        "--ServiceAuth:AuthClientId=composition-auth",
        "--ServiceAuth:AuthClientSecret=composition-auth-secret",
        "--ServiceAuth:B2BClientSecret=composition-b2b-secret",
        "--ServiceAuth:CustomerClientSecret=composition-customer-secret",
        "--Services:CustomerApiUrl=https://localhost",
        "--services:auth:https:0=https://localhost",
        "--services:payment-web:https:0=https://localhost",
        "--ServiceBus:ServiceName=composition",
        "--ExternalServices:UseRealStripe=false",
        "--Stripe:SecretKey=sk_test_composition",
        "--Stripe:WebhookSecret=whsec_composition",
        "--Functions:Worker:HostEndpoint=http://127.0.0.1:1",
        "--Functions:Worker:WorkerId=composition",
        "--Functions:Worker:RequestId=composition"
    ];
}
