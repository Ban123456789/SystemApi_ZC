namespace Accura_MES.Controllers.Models.PO
{
    public class CreatePORequestBody
    {
        public string? Number { get; set; } = "tempNumber";
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Client { get; set; }
        public string BuildSource { get; set; } = "owner";
        public DateTime? StartTime { get; set; }
        public DateTime? FinishTime { get; set; }
        public long? SourcePOId { get; set; }
        public long? PartId { get; set; }
        public long CraftId { get; set; }
        public decimal OrderQuantity { get; set; }
        public string OrderQuantityUnit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? NumberERP { get; set; }
        public bool IsDelete { get; set; } = false;
        public long? DepartmentId { get; set; }
        public string ModelNumber { get; set; } = string.Empty;
        public string ModelDes { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string Pc { get; set; } = string.Empty;
        public long UsePartId { get; set; } = 0;
        public string KnifeMoldNumber { get; set; } = string.Empty;
        public decimal MoldQuantity { get; set; } = 0;
        public decimal FixedCapacity { get; set; } = 0;
        public decimal MQuantity { get; set; } = 0;
        public decimal PcsOrParticlesQuantity { get; set; } = 0;
        public List<POSobody> PoSobodies { get; set;} = new List<POSobody>();


        public Dictionary<string, object?> ToDict ()
        {
            return new Dictionary<string, object?>
            {
                { nameof(Number), Number },
                { nameof(Name), Name },
                { nameof(Description), Description },
                { nameof(Client), Client },
                { nameof(BuildSource), BuildSource },
                { nameof(StartTime), StartTime },
                { nameof(FinishTime), FinishTime },
                { nameof(SourcePOId), SourcePOId },
                { nameof(PartId), PartId },
                { nameof(CraftId), CraftId },
                { nameof(OrderQuantity), OrderQuantity },
                { nameof(OrderQuantityUnit), OrderQuantityUnit },
                { nameof(Status), Status },
                { nameof(NumberERP), NumberERP },
                { nameof(IsDelete), IsDelete },
                { nameof(DepartmentId), DepartmentId },
                { nameof(ModelNumber), ModelNumber },
                { nameof(ModelDes), ModelDes },
                { nameof(Material), Material },
                { nameof(Pc), Pc },
                { nameof(UsePartId), UsePartId },
                { nameof(KnifeMoldNumber), KnifeMoldNumber },
                { nameof(MoldQuantity), MoldQuantity },
                { nameof(FixedCapacity), FixedCapacity },
                { nameof(MQuantity), MQuantity },
                { nameof(PcsOrParticlesQuantity), PcsOrParticlesQuantity }
            };
        }
    }

    public class POSobody
    {
        public long? SobodyId { get; set; }
        public decimal? MoldQuantity { get; set; }
        public DateTime? PreDate { get; set; }
        public long? Box { get; set; }
        public bool? IsNew { get; set; }
        public long? PartId { get; set; }
        public decimal? PreQuantity { get; set; }
        public string? TransactionType { get; set; }
    }
}
