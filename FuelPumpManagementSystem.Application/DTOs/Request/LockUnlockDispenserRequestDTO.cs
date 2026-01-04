namespace FuelPumpManagementSystem.Application.DTOs.Request
{
    public class LockUnlockDispenserRequestDTO
    {
        public string ApiEndPoint { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public int DispenserId { get; set; }
    }
}
