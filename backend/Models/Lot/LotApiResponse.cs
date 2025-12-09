namespace backend.Models.Lot;

public class LotApiResponse
{
    public Dictionary<string, object>? Errors { get; set; }
    public LotResultObject? ResultObject { get; set; }
    public bool Avoid404ForNullResponse { get; set; }
}

public class LotResultObject
{
    public LocationInfo? Location { get; set; }
    public LotAddressInfo? LotAddress { get; set; }
    public AmenitiesInfo? Amenities { get; set; }
    public BuildTypeInfo? BuildType { get; set; }
    public JobCompleteInfo? JobComplete { get; set; }
}

public class LocationInfo
{
    public RegionInfo? Region { get; set; }
    public DistrictInfo? District { get; set; }
    public AreaInfo? Area { get; set; }
}

public class RegionInfo
{
    public string Label { get; set; } = string.Empty;
}

public class DistrictInfo
{
    public string Label { get; set; } = string.Empty;
}

public class AreaInfo
{
    public string Label { get; set; } = string.Empty;
}

public class LotAddressInfo
{
    public string Street { get; set; } = string.Empty;
    public string Suburb { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
}

public class AmenitiesInfo
{
    public decimal FloorArea { get; set; }
    public decimal LandArea { get; set; }
    public int NoBedrooms { get; set; }
    public int NoBathrooms { get; set; }
}

public class BuildTypeInfo
{
    public string Label { get; set; } = string.Empty;
}

public class JobCompleteInfo
{
    public string? Estimate { get; set; }
}
