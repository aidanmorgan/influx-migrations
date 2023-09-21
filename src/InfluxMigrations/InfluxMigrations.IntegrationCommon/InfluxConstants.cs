namespace InfluxMigrations.IntegrationCommon;

public interface InfluxConstants
{
    const string Username = "test-admin";
    const string Password = "test-password";

    const string Bucket = "test-bucket";
    const string Organisation = "test-organisation";
    const string AdminToken = "test-admin-token";
    const string Retention = "1d";

    const string HistoryBucket = "migration-history";
    const string HistoryMeasurement = "migration";
}