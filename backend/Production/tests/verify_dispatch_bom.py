"""Execute the actual dispatch action/DTOs/entities with host service doubles, without a database."""
from pathlib import Path
import re
import subprocess
import tempfile


def block(source, pattern):
    match = re.search(pattern, source)
    if not match:
        raise ValueError(pattern)
    pos = source.index("{", match.start()) + 1
    depth = 1
    while depth:
        depth += (source[pos] == "{") - (source[pos] == "}")
        pos += 1
    return source[match.start():pos]


SUPPORT = r'''
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using Kingzon.OLCM.Base.Services.JsonStructure;
public enum DispatchOrderStatus { 进行中=1 }
public enum InventoryDataType { Product=1, Specification=2 }
public enum InventoryDataStatus { None=0, 正常=1 }
public enum MaterialDemandType { 领料需求=1, 补料需求=2 }
public enum TableName { DispatchOrder=1 }
public class BaseEntity { public long Id { get; set; } }
public interface ISoftDeletedEntity { }
public class Base_Product { }
public class Base_ProductSpecification { }
public class Base_ProductPackage { }
public class Base_Department { }
public class Base_Unit { }
public class Base_Partner { }
public class Production_NodeSetting { }
public class Base_UnitConversion { public long Id { get; set; } public int? ConversionType { get; set; } }
namespace Kingzon.OLCM.Base.Services.JsonStructure {
    public class UnitInfo { }
    public class CustomAttribute { }
    public class FormulaUnitInfo {
        public decimal? FormulaQuantity { get; set; }
        public decimal? FormulaUnitId { get; set; }
        public decimal? FormulaConversionId { get; set; }
        public decimal? ConvRatio { get; set; }
    }
    public class InventoryUnitOutboundDto {
        public long Id { get; set; } public long UnitId { get; set; }
        public decimal Ratio { get; set; } public decimal OutboundQuantity { get; set; }
    }
}
public static class JsonHelper { public static JsonSerializerOptions DefaultJsonSerializerOptions = new(JsonSerializerDefaults.Web); }
public class DispatchUnit { public long Id { get; set; } public decimal DispatchQuantity { get; set; } public int? ConversionType { get; set; } }
public class SaleOrderDetailDispatchDTO {
    public long Id { get; set; } public long CustomerId { get; set; }
    public decimal DispatchQuantity { get; set; } public long? DispatchUnitConversionId { get; set; }
    public List<DispatchUnit> DispatchUnitList { get; set; } = new();
}
public class Production_Dispatch_SaleRL {
    public Production_Dispatch_Order DispatchOrder { get; set; } = null!;
    public long CustomerId { get; set; } public long SaleOrderDetailId { get; set; }
    public decimal DispatchQuantity { get; set; } public long? DispatchUnitConversionId { get; set; }
    public decimal Quantity { get; set; } public decimal InboundQuantity { get; set; } public string? UnitConversionInfo { get; set; }
}
public class CommonService<T> {
    public List<T> Rows { get; } = new();
    public IQueryable<T> GetAll(bool tracking = false) => Rows.AsQueryable();
    public Task InsertAsync(IEnumerable<T> rows) { Rows.AddRange(rows); return Task.CompletedTask; }
    public void Insert(T row) => Rows.Add(row);
    public Task InsertAsync(T row) { Rows.Add(row); return Task.CompletedTask; }
}
public static class QueryExtensions {
    public static Task<int?> MaxAsync<T>(this IQueryable<T> rows, System.Linq.Expressions.Expression<Func<T,int?>> selector)
        => Task.FromResult(rows.Select(selector).DefaultIfEmpty().Max());
}
public class SequenceService { private int index; public string GenerateCode(TableName table) => "PG" + ++index; }
public class UnitOfWork { public int Saves; public Task SaveChangeAsync() { Saves++; return Task.CompletedTask; } }
public class DispatchOrderManageController : ControllerBase {
    public CommonService<Production_Dispatch_Order> _dispatchOrderService = new();
    public CommonService<Production_Material_Receive_Demand> _materialReceiveDemandService = new();
    public CommonService<Production_Dispatch_SaleRL> _dispatchSaleRLService = new();
    public CommonService<Base_UnitConversion> _unitConversionService = new();
    public SequenceService _sequenceService = new();
    public UnitOfWork _unitOfWork = new();
    // ACTUAL_ACTION
}
// ACTUAL_TYPES
'''

PROGRAM = r'''
using System.Text.Json;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using Kingzon.OLCM.Base.Services.JsonStructure;
static void Check(bool value, string message) { if (!value) throw new Exception(message); }
PostDispatchOrderDto Order() => new() { DispatchUnit = "{}", ProductId = 1, DispatchQuantity = 6 };
PostMaterialDemandDetails Material(long id, InventoryDataType type) => new() {
    DataId = id, DataType = type, UnitId = 100, UnitConversionId = -1, DemandQuantity = 2,
    BaseDemandQuantity = 24, SupplierId = 8, DataStatus = InventoryDataStatus.正常, DetailRemark = "备注",
    ConvUnitInfo = new() { new() { Id = -1, UnitId = 100, Ratio = 12, OutboundQuantity = 2 } }
};
var target = new DispatchOrderManageController();
var bom = Order(); bom.BasisType = DispatchBasisType.物料清单; bom.BasisId = 501;
bom.MaterialDemands = new() { Materials = new() { Material(41, InventoryDataType.Product), Material(421, InventoryDataType.Specification) } };
// Stray formula metadata must not create formula references on a BOM order.
bom.MaterialDemands.FormulaVersionId = 888; bom.MaterialDemands.FormulaUnitId = 999;
bom.MaterialDemands.Materials[0].FormulaUnitId = 999;
bom.SaleOrders = new() { new() { Id = 7, CustomerId = 8, DispatchQuantity = 6, DispatchUnitList = new() { new() { Id = -1, DispatchQuantity = 6 } } } };
// An old client omitting the new basis fields still saves the original formula units.
var formula = JsonSerializer.Deserialize<PostDispatchOrderDto>("""
{"productId":2,"dispatchUnit":"{}","dispatchQuantity":4,"materialDemands":{"formulaVersionId":701,"formulaUnitId":100,"formulaQuantity":4,"convRatio":1,"materials":[{"dataId":77,"dataType":1,"unitId":100,"demandQuantity":8,"baseDemandQuantity":8,"formulaUnitId":100,"formulaQuantity":8,"convRatio":2}]}}
""", JsonHelper.DefaultJsonSerializerOptions)!;
await target.AddDispatchOrderList(new() { bom, formula, Order() });
var orders = target._dispatchOrderService.Rows;
var details = target._materialReceiveDemandService.Rows;
Check(orders.Count == 3 && details.Count == 3 && target._unitOfWork.Saves == 1, "batch saves parents and selected materials once");
Check(orders[0].BasisType == DispatchBasisType.物料清单 && orders[0].BasisId == 501, "BOM basis/version");
Check(orders[0].FormulaVersionId == null && orders[0].FormulaUnitInfo == null && details[0].FormulaUnitInfo == null, "BOM does not reuse formula metadata");
Check(details[0].DataId == 41 && details[1].DataId == 421 && details[1].DataType == InventoryDataType.Specification, "product/specification identities");
Check(details[0].DispatchOrder == orders[0] && details[1].DispatchOrder == orders[0] && details[2].DispatchOrder == orders[1], "correct parent relations");
Check(details[0].DemandQuantity == 2 && details[0].BaseDemandQuantity == 24 && details[0].UnitQuantity == 2m / 6, "BOM quantities are not converted twice");
Check(details[0].UnitConversionId == null && details[0].SupplierId == 8 && details[0].Remark == "备注", "existing material metadata");
Check(details[0].Order == 1 && details[1].Order == 2 && details[2].Order == 1, "per-order sequence");
var units = JsonSerializer.Deserialize<List<InventoryUnitOutboundDto>>(details[0].ConvUnitInfo, JsonHelper.DefaultJsonSerializerOptions)!;
Check(units[0].Ratio == 12 && units[0].OutboundQuantity == 2, "unit snapshot");
Check(orders[1].BasisType == DispatchBasisType.配方 && orders[1].BasisId == 701 && orders[1].FormulaVersionId == 701, "legacy formula basis fallback");
var header = JsonSerializer.Deserialize<FormulaUnitInfo>(orders[1].FormulaUnitInfo!, JsonHelper.DefaultJsonSerializerOptions)!;
var detail = JsonSerializer.Deserialize<FormulaUnitInfo>(details[2].FormulaUnitInfo!, JsonHelper.DefaultJsonSerializerOptions)!;
Check(header.FormulaQuantity == 4 && header.ConvRatio == 1 && detail.FormulaQuantity == 8 && detail.ConvRatio == 2, "formula unit behavior unchanged");
Check(orders[2].BasisType == DispatchBasisType.未指定 && orders[2].BasisId == null, "unspecified basis");
Check(target._dispatchSaleRLService.Rows.Single().DispatchOrder == orders[0], "sale associations preserved");
async Task Reject(PostDispatchOrderDto data, string message) {
    var controller = new DispatchOrderManageController();
    try { await controller.AddDispatchOrderList(new() { data }); }
    catch (Exception ex) { Check(ex.Message.Contains(message), ex.Message); Check(controller._unitOfWork.Saves == 0, "invalid request not committed"); return; }
    throw new Exception("accepted invalid request");
}
var invalid = Order(); invalid.BasisType = DispatchBasisType.物料清单;
await Reject(invalid, "物料清单版本");
invalid = Order(); invalid.DispatchQuantity = 0; invalid.MaterialDemands = new() { Materials = new() { Material(41, InventoryDataType.Product) } };
await Reject(invalid, "派工数量");
var supplement = new DispatchOrderManageController();
PostAddMaterialDemandDto Extra(long dispatch) => new() { DispatchOrderId=dispatch, DataId=41, DataType=InventoryDataType.Product,
    DemandQuantity=2, BaseDemandQuantity=2, UnitId=100, MaterialDemandType=MaterialDemandType.补料需求 };
await supplement.AddMaterialDemand(new() { Extra(50), Extra(50) });
Check(supplement._materialReceiveDemandService.Rows.Select(x=>x.Order).SequenceEqual(new[]{1,2}), "empty dispatch supplements start at one");
supplement._materialReceiveDemandService.Rows.Add(new() { DispatchOrderId=60, Order=100, ConvUnitInfo="[]" });
await supplement.AddMaterialDemand(new() { Extra(50), Extra(50) });
Check(supplement._materialReceiveDemandService.Rows.Where(x=>x.DispatchOrderId==50).Select(x=>x.Order).SequenceEqual(new[]{1,2,3,4}), "supplement batch begins after max for this dispatch only");
await supplement.AddMaterialDemand(new() { Extra(50) });
Check(supplement._materialReceiveDemandService.Rows.Last().Order==5, "repeated saves never duplicate previous maximum");
Console.WriteLine("PASS: BOM/formula dispatch save and supplementary demand sequence for empty, existing and repeated batches");
'''


def main():
    module = Path(__file__).resolve().parents[1]
    source = (module / "KingZon.OLCM.Production.WebApi/Controllers/DispatchOrderManageController.cs").read_text(encoding="utf-8")
    action = block(source, r"public async Task<IActionResult> AddDispatchOrderList\(")
    action += "\n" + block(source, r"public async Task<IActionResult> AddMaterialDemand\(")
    types = "\n".join(block(source, r"public class " + name + r"\s*\{") for name in [
        "PostDispatchOrderDto", "PostMaterialDemands", "PostMaterialDemandDetails", "PostAddMaterialDemandDto"])
    for name in ["Production_Dispatch_Order", "Production_Material_Receive_Demand"]:
        entity = (module / f"KingZon.OLCM.Production.DataBase/Entities/{name}.cs").read_text(encoding="utf-8")
        types += "\n" + block(entity, r"public class " + name + r"\s*:")
    with tempfile.TemporaryDirectory(prefix="dispatch-bom-checks-") as directory:
        folder = Path(directory)
        (folder / "Support.cs").write_text(SUPPORT.replace("// ACTUAL_ACTION", action).replace("// ACTUAL_TYPES", types), encoding="utf-8")
        (folder / "Program.cs").write_text(PROGRAM, encoding="utf-8")
        enum = module / "KingZon.OLCM.Production.DataBase/Enum/DispatchBasisType.cs"
        project = folder / "Checks.csproj"
        project.write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App"/><Compile Include="{enum}" Link="DispatchBasisType.cs"/></ItemGroup></Project>''', encoding="utf-8")
        subprocess.run(["dotnet", "run", "--project", str(project), "--verbosity", "quiet"], check=True)


if __name__ == "__main__":
    main()
