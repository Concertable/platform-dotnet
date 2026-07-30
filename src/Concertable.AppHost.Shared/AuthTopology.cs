using Concertable.Shared.Email.Application;

public static class AuthTopology
{
    public static AsbTopology AddAuthTopology(this AsbTopology topology) =>
        topology
            .Queue<SendEmailCommand>(AppHostConstants.ServiceNames.Auth)
            .Queue<SendVerificationEmailCommand>(AppHostConstants.ServiceNames.Auth);
}
