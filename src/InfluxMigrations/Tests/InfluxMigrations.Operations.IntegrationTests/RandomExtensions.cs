namespace InfluxMigrations.Operations.IntegrationTests;

public static class RandomExtensions
{
    public static string RandomString(this Random random, int bytesLength = 8)
    {
        var bytes = new byte[bytesLength];
        random.NextBytes(bytes);

        return Convert.ToHexString(bytes);
    }
}