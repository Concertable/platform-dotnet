public static class AuthTopology
{
    public static AsbTopology AddAuthTopology(this AsbTopology topology) =>
        topology
            .Queue("command-concertable-auth-sendemailcommand")
            .Queue("command-concertable-auth-sendverificationemailcommand");
}
