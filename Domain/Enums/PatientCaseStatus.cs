namespace ClinicOps.Domain.Enums
{
    public enum PatientCaseStatus
    {
        Waiting = 1,
        InConsultation = 2,
        Finished = 3,
        /// <summary>Closed by nurse in Reports after doctor finished the visit.</summary>
        Mbyllur = 4,
    }
}
