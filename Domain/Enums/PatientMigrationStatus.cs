namespace ClinicOps.Domain.Enums
{
    public enum PatientMigrationStatus
    {
        Uploaded = 0,
        Previewed = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5
    }
}
