namespace ClinicOps.API.DTOs.PatientCase
{
    /// <summary>JSON body for attaching a service: { "serviceId": "..." }</summary>
    public class AttachServiceToCaseRequest
    {
        public Guid? ServiceId { get; set; }
    }
}
