namespace NamEcommerce.Application.Contracts.Dtos.Report;

public class DirectShipReportAppDto
{
    public List<DirectShipByVendorAppDto> ByVendor { get; set; } = new();
    public List<DirectShipByCustomerAppDto> ByCustomer { get; set; } = new();
    public List<DirectShipByProductAppDto> ByProduct { get; set; } = new();
    public List<DirectShipPendingAlertAppDto> PendingAlerts { get; set; } = new();
    public DirectShipRejectRateAppDto RejectRate { get; set; } = new();
}

public class DirectShipByVendorAppDto
{
    public string VendorName { get; set; } = "";
    public int AllocationCount { get; set; }
    public decimal TotalQuantity { get; set; }
}

public class DirectShipByCustomerAppDto
{
    public string CustomerName { get; set; } = "";
    public int DeliveryCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class DirectShipByProductAppDto
{
    public string ProductName { get; set; } = "";
    public int AllocationCount { get; set; }
    public decimal TotalQuantity { get; set; }
}

public class DirectShipPendingAlertAppDto
{
    public string DeliveryNoteCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public int DaysPending { get; set; }
}

public class DirectShipRejectRateAppDto
{
    public int TotalDeliveries { get; set; }
    public int TotalConfirmed { get; set; }
    public int TotalRejected { get; set; }
    public int TotalPending { get; set; }
    public double RejectRatePercent => TotalDeliveries == 0 ? 0 : Math.Round((double)TotalRejected / TotalDeliveries * 100, 1);
    public List<DirectShipRejectReasonAppDto> RejectReasons { get; set; } = new();
}

public class DirectShipRejectReasonAppDto
{
    public string Reason { get; set; } = "";
    public int Count { get; set; }
}
