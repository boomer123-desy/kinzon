using Kingzon.OLCM.Base.Services;
using Kingzon.OLCM.Base.Services.JsonStructure;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Base.Service;
using KingZon.OLCM.Base.Service.Dto;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Common.DataBase.Entities;
using KingZon.OLCM.Common.Service.Dto;
using KingZon.OLCM.Production.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using KingZon.OLCM.Production.WebApi.Dto;
using KingZon.OLCM.Technical.Shared.DataBase.Entities;
using KingZon.OLCM.Technical.Shared.DataBase.Enum;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NetTopologySuite.Index.HPRtree;
using System.Linq;
using System.Security;
using System.Text.Json;
using System.Transactions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KingZon.OLCM.Production.WebApi.Controllers
{
    [Route("api/production/[controller]")]
    [ApiController]
    public class BomManageController : KingZonControllerBase
    {
        private readonly ProductService _productService;
        private readonly CommonService<Technical_FormulaVersion> _formulaVersionService;
        private readonly CommonService<Production_MBom> _bomnService;
        private readonly CommonService<Production_MBom_Version> _bomVersionService;
        private readonly CommonService<Production_MBom_Detail> _bomDetailService;
        private readonly CommonService<Base_SystemPara> _systemParaService;
        private readonly FileStorageService _fileService;
        private readonly Utils _utils;
        private readonly IUnitOfWork _unitOfWork;

        public BomManageController(ProductService productService,
            CommonService<Technical_FormulaVersion> formulaVersionService,
            CommonService<Production_MBom> bomnService,
            CommonService<Production_MBom_Version> bomVersionService,
            CommonService<Production_MBom_Detail> bomDetailService,
            CommonService<Base_SystemPara> systemParaService,
            FileStorageService fileService,
            Utils utils,
            IUnitOfWork unitOfWork)
        {
            _productService = productService;
            _formulaVersionService = formulaVersionService;
            _bomnService = bomnService;
            _bomVersionService = bomVersionService;
            _bomDetailService = bomDetailService;
            _systemParaService = systemParaService;
            _fileService = fileService;
            _utils = utils;
            _unitOfWork = unitOfWork;
        }

        [HttpPost("GetRawMaterialResourceList")]
        public async Task<IActionResult> GetRawMaterialResourceList([FromBody] List<Filter> filters)
        {
            var materialQuery = _productService.GetMaterialList([]).Where(x => !x.IsDeleted && x.ReviewStatus == ReviewStatus.评审通过);
            var materialData = materialQuery.Select(x => new BomResourceDto
            {
                Code = x.ProductNo,
                Name = x.ProductName,
                Unit = x.ProductUnit,
                TechCode = x.ProductTechnicalCode,
                Specification = x.MaterialSpecification,
                Remark = x.Remark,
                DetailType = BomDetailType.Product,
                DetailId = x.ProductId,
                ProductId = x.ProductId,
                ProductType = x.ProductType,
                BomId = null,
                BomVersion = null,
                BomRemark = null,
            });
            var data = materialData;
            var dataPermission = _systemParaService.GetAll().FirstOrDefault(x => x.DataType == SystemParaType.资料库查看权限);

            if (dataPermission != null && Enum.TryParse<LibraryPermission>(dataPermission.DataValue, ignoreCase: true, out var permission))
            {
                if (permission.HasFlag(LibraryPermission.Bom))
                {
                    var bomData = _bomVersionService.GetAll().Include(x => x.MBom).Where(x => x.Status != BomStatus.淘汰 //需要改为 只取评审通过的物料清单
                           && !x.MBom.ProductSpecificationId.HasValue);
                    var materialWithBom = from material in materialQuery
                                          join bom in bomData
                                           on material.ProductId equals bom.MBom.ProductId
                                          select new BomResourceDto
                                          {
                                              Code = material.ProductNo,
                                              Name = material.ProductName,
                                              Unit = material.ProductUnit,
                                              TechCode = material.ProductTechnicalCode,
                                              Specification = material.MaterialSpecification,
                                              Remark = material.Remark,
                                              DetailType = BomDetailType.Bom,
                                              DetailId = bom.Id,
                                              ProductId = material.ProductId,
                                              ProductType = material.ProductType,
                                              BomId = bom.Id,
                                              BomVersion = bom.Version,
                                              BomRemark = bom.Remark,
                                          };
                    data = data.Concat(materialWithBom);
                }
            }

            var res = await data.OrderBy(x => x.Code).ApplyFilters(filters).ToPagedListAsync(this.PageModel);

            return Ok(res);
        }

        [HttpPost("GetProductResourceList")]
        public async Task<IActionResult> GetProductResourceList([FromBody] List<Filter> filters, [FromQuery] ProductType productType = ProductType.成品)
        {
            var productQuery = _productService.GetProductList([], []).Where(x => x.ProductType == productType && !x.IsDeleted && x.ReviewStatus == ReviewStatus.评审通过);
            var productData = productQuery.Select(x => new BomResourceDto
            {
                Code = x.ProductNo,
                Name = x.ProductName,
                Unit = x.ProductUnit,
                TechCode = x.ProductTechnicalCode,
                Specification = x.ProductSpecification,
                Remark = x.Remark,
                DetailType = x.SpecificationId.HasValue ? BomDetailType.Specication : BomDetailType.Product,
                DetailId = x.SpecificationId.HasValue ? x.SpecificationId.Value : x.ProductId,
                ProductId = x.ProductId,
                SpecificationId = x.SpecificationId,
                ProductType = x.ProductType,
                FormulaCode = null,
                FormulaName = null,
                FormulaVersion = null,
                FormulaRemark = null,
                FormulaId = null,
                BomId = null,
                BomVersion = null,
                BomRemark = null,
            });
            var data = productData;

            var dataPermission = _systemParaService.GetAll().FirstOrDefault(x => x.DataType == SystemParaType.资料库查看权限);
            if (dataPermission != null && Enum.TryParse<LibraryPermission>(dataPermission.DataValue, ignoreCase: true, out var permission))
            {
                if (permission.HasFlag(LibraryPermission.Formula))
                {
                    var formulaData = _formulaVersionService.GetAll().Include(x => x.Formula).Where(x => x.Status != FormulaStatus.淘汰); //需要改为 只取评审通过的配方
                    var productDataWithFormula = from product in productQuery.Where(x => !x.SpecificationId.HasValue) //只取非规格产品
                                                 join formula in formulaData
                                                  on product.ProductId equals formula.Formula.ProductId
                                                 select new BomResourceDto
                                                 {
                                                     Code = product.ProductNo,
                                                     Name = product.ProductName,
                                                     Unit = product.ProductUnit,
                                                     TechCode = product.ProductTechnicalCode,
                                                     Specification = product.ProductSpecification,
                                                     Remark = product.Remark,
                                                     DetailType = BomDetailType.Formula,
                                                     DetailId = formula.Id,
                                                     ProductId = product.ProductId,
                                                     SpecificationId = null,
                                                     ProductType = product.ProductType,
                                                     FormulaCode = formula.Formula.Code,
                                                     FormulaName = formula.Formula.Name,
                                                     FormulaVersion = formula.Version,
                                                     FormulaRemark = formula.Remark,
                                                     FormulaId = formula.FormulaId,
                                                     BomId = null,
                                                     BomVersion = null,
                                                     BomRemark = null,
                                                 };
                    data = data.Concat(productDataWithFormula);
                }
                if (permission.HasFlag(LibraryPermission.Bom))
                {
                    var bomData = _bomVersionService.GetAll().Include(x => x.MBom).Where(x => x.Status != BomStatus.淘汰); //需要改为 只取评审通过的物料清单
                    var productDataWithBom = from product in productQuery
                                             join bom in bomData
                                              on new { ProductId = product.ProductId, SpecificationId = product.SpecificationId }
                                              equals new { ProductId = bom.MBom.ProductId ?? 0, SpecificationId = bom.MBom.ProductSpecificationId }
                                             select new BomResourceDto
                                             {
                                                 Code = product.ProductNo,
                                                 Name = product.ProductName,
                                                 Unit = product.ProductUnit,
                                                 TechCode = product.ProductTechnicalCode,
                                                 Specification = product.ProductSpecification,
                                                 Remark = product.Remark,
                                                 DetailType = BomDetailType.Bom,
                                                 DetailId = bom.Id,
                                                 ProductId = product.ProductId,
                                                 SpecificationId = product.SpecificationId,
                                                 ProductType = product.ProductType,
                                                 FormulaCode = null,
                                                 FormulaName = null,
                                                 FormulaVersion = null,
                                                 FormulaRemark = null,
                                                 FormulaId = null,
                                                 BomId = bom.MBomId,
                                                 BomVersion = bom.Version,
                                                 BomRemark = bom.Remark,
                                             };
                    data = data.Concat(productDataWithBom);
                }
            }
            var res = await data.OrderBy(x => x.Code).ApplyFilters(filters).ToPagedListAsync(this.PageModel);

            return Ok(res);
        }

        [HttpPost("GetFormulaResourceList")]
        public async Task<IActionResult> GetFormulaResourceList([FromBody] List<Filter> filters)
        {
            var data = await _formulaVersionService.GetAll().Include(x => x.Formula).ThenInclude(x => x.Product)
                .Where(x => x.Status != FormulaStatus.淘汰)
                .Select(formula => new BomResourceDto
                {
                    Code = formula.Formula.Code,
                    Name = formula.Formula.Name,
                    Remark = formula.Remark,
                    Version = formula.Version,
                    Unit = "kg",
                    DetailType = BomDetailType.Formula,
                    DetailId = formula.Id,
                    ProductCode = formula.Formula.Product != null ? formula.Formula.Product.TechnicalCode : null, //产品技术型号
                    ProductName = formula.Formula.Product != null ? formula.Formula.Product.ProductName : null,
                    ProductRemark = formula.Formula.Product != null ? formula.Formula.Product.Remark : null,
                    FormulaId = formula.FormulaId,
                }).ApplyFilters(filters).ToPagedListAsync(this.PageModel);
            return Ok(data);
        }


        [HttpPost("GetBomResourceList")]
        public async Task<IActionResult> GetBomResourceList([FromBody] List<Filter> filters)
        {
            var data = await _bomVersionService.GetAll().Include(x => x.MBom.Product).Include(x => x.MBom.ProductSpecification)
                .Where(x => x.Status != BomStatus.淘汰)
                .Select(bom => new BomResourceDto
                {
                    Code = bom.MBom.ProductSpecification != null ? bom.MBom.ProductSpecification.Code :
                        bom.MBom.Product != null ? bom.MBom.Product.ProductNo : null,
                    Name = bom.MBom.ProductSpecification != null ? bom.MBom.ProductSpecification.Name :
                        bom.MBom.Product != null ? bom.MBom.Product.ProductName : null,
                    Remark = bom.Remark,
                    Version = bom.Version,
                    DetailType = BomDetailType.Bom,
                    DetailId = bom.Id,
                    ProductRemark = bom.MBom.ProductSpecification != null ? bom.MBom.ProductSpecification.Remark :
                        bom.MBom.Product != null ? bom.MBom.Product.Remark : null,
                    ProductCode = bom.MBom.Product != null ? bom.MBom.Product.TechnicalCode : null,
                    Specification = bom.MBom.ProductSpecification != null ? bom.MBom.ProductSpecification.Specification : null,
                    BomId = bom.MBomId,
                }).ApplyFilters(filters).ToPagedListAsync(this.PageModel);
            return Ok(data);
        }


        public class CreateBomDto
        {
            public long? ProductId { get; set; }
            public long? SpecificationId { get; set; }
            public List<BomVersionDataDto> VersionList { get; set; } = new List<BomVersionDataDto>();
        }

        /// <summary>
        /// 新增物料清单
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("CreateBom")]
        public async Task<IActionResult> CreateBom([FromBody] CreateBomDto dto)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var existingBom = await _bomnService.GetAll().Include(x => x.VersionList)
                    .Where(x => x.ProductId == dto.ProductId && x.ProductSpecificationId == dto.SpecificationId)
                    .FirstOrDefaultAsync();
                var mainVersion = existingBom?.VersionList.Where(x => x.IsMain)?.Select(x => new BomVersionDataDto
                {
                    VersionId = x.Id,
                    DetailList = x.DetailList.Select(e => new BomDetailDto
                    {
                        DetailType = e.DetailType,
                        DetailId = e.DetailId,
                        CurRatio = e.Ratio,
                        CurQuantity = e.Quantity,
                    }).ToList()
                }).FirstOrDefault();
                if (existingBom == null) //一个产品只能有一个物料清单 可以有多个版本
                {
                    existingBom = new Production_MBom
                    {
                        ProductId = dto.ProductId,
                        ProductSpecificationId = dto.SpecificationId,
                        Status = BomStatus.待审核,
                    };
                    _bomnService.Insert(existingBom);
                }
                if (mainVersion == null)
                {
                    mainVersion = dto.VersionList.FirstOrDefault(x => x.Version.IsMain);
                }
                if (mainVersion == null)
                {
                    throw new Exception("至少需要一个主版本");
                }
                foreach (var item in dto.VersionList)
                {
                    this.addVersion(mainVersion, item, existingBom, mainVersion.VersionId <= 0 && item == mainVersion ? true : false);
                }
                await _unitOfWork.SaveChangeAsync();


                var res = dto.VersionList.SelectMany(x =>
                {
                    if (x.DetailList.Count > 0)
                    {
                        return x.DetailList.Select(y => new CreateBomResponseDto
                        {
                            Index = x.Index,
                            VersionId = x.VersionId,
                            Version = x.Version.Version,
                            Order = y.Order,
                            DetailId = y.Id
                        });
                    }
                    else
                    {
                        return new List<CreateBomResponseDto> {
                        new CreateBomResponseDto
                        {
                            Index = x.Index,
                            VersionId = x.VersionId,
                            Version = x.Version.Version,
                            Order = null,
                            DetailId = null
                        }};
                    }
                }).ToList();
                scope.Complete();
                return Ok(new
                {
                    details = res,
                    bomId = existingBom.Id,
                });
            }
        }
        private class CreateBomResponseDto
        {
            public int Index { get; set; }
            public long? VersionId { get; set; }
            public string Version { get; set; } = "";
            public int? Order { get; set; }
            public long? DetailId { get; set; }
        }

        private void addVersion(BomVersionDataDto? mainVersion, BomVersionDataDto item, Production_MBom bom, bool isMain = false)
        {
            var version = item.Version;
            var versionEntity = new Production_MBom_Version
            {
                MBom = bom,
                Version = version.Version,
                IsMain = isMain,
                Remark = version.Remark,
                TotalQuantity = version.TotalQuantity,
                UnitConversionId = version.UnitConversionId,
                UnitInfo = version.UnitInfoList.Count > 0 ? JsonSerializer.Serialize(version.UnitInfoList, JsonHelper.DefaultJsonSerializerOptions) : "[]",
                Status = version.Status,
                CustomAttribute = version.CustomAttributeList != null && version.CustomAttributeList.Count > 0 ?
                            JsonSerializer.Serialize(version.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
            };
            _bomVersionService.Insert(versionEntity);
            _unitOfWork.SaveChange();
            item.VersionId = versionEntity.Id;

            if (item.DetailList.Count > 0)
            {
                foreach (var detail in item.DetailList)
                {
                    this.addVersionDetail(mainVersion, detail, bom, versionEntity);
                    if (detail.ReplaceData.Count > 0)
                    {
                        _unitOfWork.SaveChange();

                        foreach (var subDetail in detail.ReplaceData)
                        {
                            this.addVersionDetail(mainVersion, subDetail, bom, versionEntity, true, detail.Id);
                        }
                    }
                }
            }
        }
        public class GetFormulaReplaceDto
        {
            public List<BomDetailDto> FormulaReplaceData { get; set; } = new List<BomDetailDto>();
            public bool DelAllReplaceData { get; set; } = false;
            public long FormulaDetailId { get; set; }

        }
        private List<GetFormulaReplaceDto> getFormulaReplaceData(List<BomDetailDto> detailList)
        {
            //寻找配方明细的替换料
            var formulaDetails = detailList.Where(x => x.FormulaDetailId.HasValue && x.FormulaDetailId.Value > 0);
            var replaceData = formulaDetails.Select(x => new GetFormulaReplaceDto
            {
                FormulaReplaceData = x.ReplaceData,
                DelAllReplaceData = x.ReplaceData.Count() <= 0,
                FormulaDetailId = (long)x.FormulaDetailId!
            });

            var childrenReplaceData = new List<GetFormulaReplaceDto>();
            foreach (var item in detailList)
            {
                var rd = getFormulaReplaceData(item.Children);
                childrenReplaceData = childrenReplaceData.Concat(rd).ToList();
            }
            var data = replaceData.Concat(childrenReplaceData).ToList();

            return data;
        }
        private void addVersionDetail(BomVersionDataDto? mainVersion, BomDetailDto detail, Production_MBom bomEntity, Production_MBom_Version versionEntity, bool IsSubstitute = false, long? MainDetailId = null)
        {
            var mainVersionDetail = mainVersion != null ? mainVersion.DetailList.FirstOrDefault(x => x.DetailType == detail.DetailType && x.DetailId == detail.DetailId) : null;
            var RatioDiff2MainVersion = mainVersionDetail != null ? detail.CurRatio - mainVersionDetail.CurRatio : 0;
            var quantityDiff2MainVersion = mainVersionDetail != null ? detail.CurQuantity - mainVersionDetail.CurQuantity : 0;
            var detailEntity = new Production_MBom_Detail
            {
                MBom = bomEntity,
                BomVersion = versionEntity,
                DetailType = detail.DetailType,
                DetailId = detail.DetailId,
                Order = detail.Order,
                Quantity = detail.CurQuantity,
                UnitConversionId = detail.UnitConversionId,
                UnitInfo = detail.UnitInfoList.Count > 0 ? JsonSerializer.Serialize(detail.UnitInfoList, JsonHelper.DefaultJsonSerializerOptions) : "[]",
                Ratio = detail.CurRatio,
                LowerTolerance = detail.LowerTolerance,
                UpperTolerance = detail.UpperTolerance,
                IsSubstitute = IsSubstitute,
                MainDetailId = MainDetailId,
                MainFormulaDetailId = detail.MainFormulaDetailId, //配方明细的替换料
                QuantityDiff2MainVersion = quantityDiff2MainVersion,
                RatioDiff2MainVersion = RatioDiff2MainVersion,
                FormulaDetailInfo = detail.Children.Count > 0 && detail.DetailType == BomDetailType.Formula ? JsonSerializer.Serialize(detail.Children, JsonHelper.DefaultJsonSerializerOptions) : null,
            };
            _bomDetailService.Insert(detailEntity);
            _unitOfWork.SaveChange();

            detail.Id = detailEntity.Id;
        }


        /// <summary>
        /// 读取物料清单的所有明细
        /// </summary>
        /// <param name="bomVersionId"></param>
        [HttpGet("GetBomDataById")]
        public async Task<IActionResult> GetBomDataById([FromQuery] long bomVersionId, [FromQuery] bool editable, [FromQuery] decimal quantity)
        {
            var bomVersion = await _bomVersionService.GetAll().Include(x => x.MBom.Product)
                                .Include(x => x.MBom.ProductSpecification)
                                .Include(x => x.DetailList)
                                .Where(x => x.Id == bomVersionId)
                                .FirstOrDefaultAsync();
            if (bomVersion == null)
            {
                throw new Exception("物料清单不存在,请刷新重试");
            }
            var detailIds = bomVersion.DetailList.Select(x => x.Id).ToList();
            var allFileData = _fileService.GetAll().Where(x => (x.SourceType == FileStorageSourceType.物料清单明细文件 && detailIds.Contains(x.SourceId)) ||
                            (x.SourceType == FileStorageSourceType.物料清单文件 && x.SourceId == bomVersion.Id)).ToList();
            var data = new BomVersionDataDto
            {
                VersionId = bomVersion.Id,
                Version = new BomVersionDto
                {
                    Id = bomVersion.Id,
                    Version = bomVersion.Version,
                    IsMain = bomVersion.IsMain,
                    Remark = bomVersion.Remark,
                    Status = bomVersion.Status,
                    TotalQuantity = bomVersion.TotalQuantity,
                    UnitConversionId = bomVersion.UnitConversionId,
                    UnitInfoList = JsonSerializer.Deserialize<List<InventoryUnitOutboundDto>>(bomVersion.UnitInfo, JsonHelper.DefaultJsonSerializerOptions) ?? new List<InventoryUnitOutboundDto>(),
                    FileData = allFileData.Where(x => x.SourceType == FileStorageSourceType.物料清单文件 && x.SourceId == bomVersion.Id).ToList(),
                },
                DetailList = bomVersion.DetailList != null ? bomVersion.DetailList.Where(x => !x.IsSubstitute)
                .Select(detail =>
                {
                    return getBomDetailDto(detail, bomVersion, allFileData, editable);
                }).OrderBy(x => x.Order).ToList() : new List<BomDetailDto>(),
            };

            //寻找子配方 以及 配方明细对应的物料信息
            if (data.DetailList != null && data.DetailList.Count > 0)
            {
                getBomChildrenData(data.DetailList, quantity, null, bomVersion.DetailList);
            }
            return Ok(data);
        }
        private BomDetailDto getBomDetailDto(Production_MBom_Detail detail, Production_MBom_Version bomVersion, List<Base_FileStorage> allFileData, bool editable)
        {
            return new BomDetailDto
            {
                Id = detail.Id,
                VersionId = detail.VersionId,
                DetailType = detail.DetailType,
                DetailId = detail.DetailId,
                Quantity = detail.Quantity,
                UnitConversionId = detail.UnitConversionId,
                UnitInfoList = JsonSerializer.Deserialize<List<InventoryUnitOutboundDto>>(detail.UnitInfo, JsonHelper.DefaultJsonSerializerOptions) ?? new List<InventoryUnitOutboundDto>(),
                Ratio = detail.Ratio,
                CurRatio = detail.Ratio,
                BaseQuantity = bomVersion.TotalQuantity,
                LowerTolerance = detail.LowerTolerance,
                UpperTolerance = detail.UpperTolerance,
                Editable = editable,
                ReplaceData = bomVersion.DetailList.Where(x => x.IsSubstitute && x.MainDetailId == detail.Id && x.MainFormulaDetailId == null).Select(x =>
                {
                    return getBomDetailDto(x, bomVersion, allFileData, editable);
                }).ToList(),
                FileData = allFileData.Where(x => x.SourceType == FileStorageSourceType.物料清单明细文件 && x.SourceId == detail.Id).ToList(),
                MainFormulaDetailId = detail.MainFormulaDetailId,
            };
        }
        private void getBomChildrenData(List<BomDetailDto> DetailList, Decimal? CurBaseQuantity, string? unitName = null, List<Production_MBom_Detail>? AllDetailData = null)
        {
            var productIds = DetailList.Where(x => x.DetailType == BomDetailType.Product).Select(x => x.DetailId).ToList();
            var specIds = DetailList.Where(x => x.DetailType == BomDetailType.Specication).Select(x => x.DetailId).ToList();
            var formulaIds = DetailList.Where(x => x.DetailType == BomDetailType.Formula).Select(x => x.DetailId).ToList();
            var bomIds = DetailList.Where(x => x.DetailType == BomDetailType.Bom).Select(x => x.DetailId).ToList();

            //产品和产品规格
            var materialData = _productService.GetProductList([], []).Where(x => productIds.Contains(x.ProductId) || (x.SpecificationId != null && specIds.Contains((long)x.SpecificationId!))).ToList();
            var formulaData = _formulaVersionService.GetAll().Include(x => x.Formula).ThenInclude(x => x.Product).ThenInclude(x => x.BaseUnit)
                                .Include(x => x.Formula.Product.ProductCategory)
                                .Include(x => x.DetailList).Where(x => formulaIds.Contains(x.Id)).ToList();
            var bomData = _bomVersionService.GetAll().Include(x => x.MBom.Product).ThenInclude(x => x.BaseUnit)
                                .Include(x => x.MBom.ProductSpecification).ThenInclude(x => x.Product)
                                .Include(x => x.MBom.ProductSpecification).ThenInclude(x => x.SpecificationUnit)
                                .Include(x => x.DetailList).Where(x => bomIds.Contains(x.Id)).ToList();
            var bomDetailIds = bomData.SelectMany(x => x.DetailList).Select(x => x.Id).ToList();
            var bomFileData = _fileService.GetAll().Where(x => (x.SourceType == FileStorageSourceType.物料清单明细文件 && bomDetailIds.Contains(x.SourceId)) ||
                       (x.SourceType == FileStorageSourceType.物料清单文件 && bomIds.Contains(x.SourceId))).ToList();
            var productIdFields = materialData.Select(x => new ProductIdDto
            {
                ProductId = x.ProductId,
                SpecificationId = x.SpecificationId,
                IdField = x.ProductId + "_" + x.SpecificationId
            }).ToList();
            var allUnitData = _productService.GetProductUnitList(productIdFields);
            foreach (var detail in DetailList)
            {
                detail.Unit = detail.UnitInfoList?.FirstOrDefault(x => x.Id == detail.UnitConversionId)?.UnitName ?? ""; //明细对应的单位
                detail.CurBaseQuantity = CurBaseQuantity ?? 0;
                var scale = detail.BaseQuantity != 0 ? detail.CurBaseQuantity / detail.BaseQuantity : 1;
                detail.CurQuantity = _utils.formatDigital4(detail.Quantity * scale); //保留4位小数
                if (detail.DetailType == BomDetailType.Product || detail.DetailType == BomDetailType.Specication)
                {
                    var material = materialData.FirstOrDefault(x => (detail.DetailType == BomDetailType.Product && x.ProductId == detail.DetailId && x.SpecificationId == null) ||
                                    (detail.DetailType == BomDetailType.Specication && x.SpecificationId != null && x.SpecificationId == detail.DetailId));
                    if (material != null)
                    {
                        detail.DetailDataNo = material.ProductNo ?? "";
                        detail.DetailDataName = material.ProductName ?? "";
                        detail.DetailDataTechCode = material.ProductTechnicalCode ?? "";
                        detail.DetailDataSpecification = material.ProductType == ProductType.物料 ? material.MaterialSpecification ?? "" : material.ProductSpecification ?? "";
                        detail.DetailDataUnit = material.ProductUnit ?? "";
                        detail.DetailDataUnitId = material.ProductUnitId;
                        detail.DetailProductType = material.ProductType;
                        detail.DetailDataMaterialGrade = material.MaterialGrade ?? "";
                        detail.DetailDataCategoryName = material.ProductCategory ?? "";
                        detail.InventoryQuantity = material.InventoryQuantity;
                        detail.OccupyInventory = material.OccupyInventory;
                        detail.AbnormalQuantity = material.AbnormalQuantity;
                        detail.MainUnitId = material.ProductMainUnitId;
                        detail.MainUnitName = material.ProductMainUnit;
                        detail.DetailWithProductDto = new DetailWithProductDto
                        {
                            ProductId = material.ProductId,
                            SpecificationId = material.SpecificationId,
                            ProductNo = detail.DetailDataNo,
                            ProductName = detail.DetailDataName,
                            ProductUnit = detail.DetailDataUnit,
                            ProductSpecification = detail.DetailDataSpecification,
                            ProductTechCode = detail.DetailDataTechCode,
                            ProductMaterialGrade = detail.DetailDataMaterialGrade,
                            ProductCategoryName = detail.DetailDataCategoryName,
                            ProductType = material.ProductType
                        };
                        detail.Unit = unitName ?? (string.IsNullOrWhiteSpace(detail.Unit) ? detail.DetailDataUnit : detail.Unit);
                        //产品可选单位
                        detail.UnitData = allUnitData.FirstOrDefault(x => x.ProductId == material.ProductId && x.SpecificationId == material.SpecificationId)?.UnitList.Select(x => new SelectionDto
                        {
                            Value = x.Value,
                            Text = x.Text,
                            Code = x.Code
                        }).ToList();
                    }
                }
                else if (detail.DetailType == BomDetailType.Formula)
                {
                    var formula = formulaData.FirstOrDefault(x => x.Id == detail.DetailId);
                    if (formula != null)
                    {
                        detail.DetailDataNo = formula.Formula.Code;
                        detail.DetailDataName = formula.Formula.Name;
                        detail.DetailDataVersion = formula.Version;
                        detail.DetailFormulaId = formula.FormulaId;
                        detail.DetailDataTechCode = formula.Formula.Product != null && formula.Formula.Product.TechnicalCode != null ? formula.Formula.Product.TechnicalCode : "";
                        detail.DetailDataUnit = formula.Formula.Product != null ? formula.Formula.Product.BaseUnit.Name : "";
                        detail.DetailDataUnitId = formula.Formula.Product != null ? formula.Formula.Product.BaseUnitId : null;
                        detail.InventoryQuantity = formula.Formula.Product != null ? formula.Formula.Product.InventoryQuantity : 0;
                        detail.OccupyInventory = formula.Formula.Product != null ? formula.Formula.Product.OccupyInventory : 0;
                        detail.AbnormalQuantity = formula.Formula.Product != null ? formula.Formula.Product.AbnormalQuantity : 0;
                        detail.MainUnitId = formula.Formula.Product != null ? formula.Formula.Product.BaseUnitId : null;
                        detail.MainUnitName = formula.Formula.Product != null ? formula.Formula.Product.BaseUnit.Name : null;
                        //获取配方对应的物料信息
                        detail.DetailWithProductDto = formula.Formula.Product != null ? new DetailWithProductDto
                        {
                            ProductId = (long)formula.Formula.ProductId!,
                            ProductNo = formula.Formula.Product.ProductNo,
                            ProductName = formula.Formula.Product.ProductName,
                            ProductUnit = formula.Formula.Product.BaseUnit.Name,
                            ProductSpecification = formula.Formula.Product.ProductType == ProductType.物料 ? formula.Formula.Product.MaterialSpecification ?? "" : "",
                            ProductTechCode = formula.Formula.Product.TechnicalCode ?? "",
                            ProductMaterialGrade = formula.Formula.Product.MaterialGrade ?? "",
                            ProductCategoryName = formula.Formula.Product.ProductCategory.Name,
                            ProductType = formula.Formula.Product.ProductType,
                        } : null;
                        //获取子配方明细
                        detail.Children = formula.DetailList != null ? formula.DetailList.Select(fd => new BomDetailDto
                        {
                            Id = fd.Id,
                            DetailType = transformFormulaDetailType(fd.DetailType),
                            DetailId = fd.DetailId,
                            Quantity = fd.Quantity,
                            Ratio = fd.Ratio,
                            CurRatio = fd.Ratio,
                            BaseQuantity = formula.TotalQuantity,
                            LowerTolerance = fd.LowerTolerance,
                            UpperTolerance = fd.UpperTolerance,
                            Editable = false,
                            FormulaDetailId = fd.Id, //配方明细Id
                            ReplaceData = AllDetailData?.Where(x => x.IsSubstitute && x.MainDetailId == null && x.MainFormulaDetailId == fd.Id)
                            .Select(x =>
                            {
                                var entity = getBomDetailDto(x, x.BomVersion, [], false);
                                entity.BaseQuantity = detail.CurQuantity;
                                return entity;
                            }).ToList() ?? new List<BomDetailDto>(),//配方明细的替换料
                        }).ToList() : new List<BomDetailDto>();

                        //配方可选单位
                        detail.Unit = "kg";
                        detail.UnitConversionId = -1;
                        detail.UnitData = new List<SelectionDto> {
                        new SelectionDto
                        {
                            Value = "1",
                            Text = "kg",
                            Code ="-1"
                        }};
                    }
                }
                else if (detail.DetailType == BomDetailType.Bom)
                {
                    var bom = bomData.FirstOrDefault(x => x.Id == detail.DetailId);
                    if (bom != null)
                    {
                        var product = bom.MBom.Product;
                        var spec = bom.MBom.ProductSpecification;
                        //获取对应的物料信息
                        detail.DetailDataNo = spec != null ? spec.Code ?? "" : product != null ? product.ProductNo : "";
                        detail.DetailDataName = spec != null ? spec.Name ?? "" : product != null ? product.ProductName : ""; ;
                        detail.DetailDataVersion = bom.Version;
                        detail.DetailBomId = bom.MBomId;
                        detail.DetailDataTechCode = product?.TechnicalCode ?? "";
                        detail.DetailDataSpecification = spec != null ? spec.Specification : product != null ? product.MaterialSpecification ?? "" : "";
                        detail.DetailDataMaterialGrade = product?.MaterialGrade ?? "";
                        detail.DetailDataUnit = spec != null ? spec.SpecificationUnit?.Name ?? "" : product != null ? product.BaseUnit?.Name ?? "" : "";
                        detail.DetailDataUnitId = spec != null ? spec.SpecificationUnitId : product != null ? product.BaseUnitId : null;
                        detail.DetailDataCategoryName = spec != null && spec.ProductCategory != null ? spec.ProductCategory.Name : product != null ? product.ProductCategory.Name ?? "" : "";
                        detail.InventoryQuantity = spec != null ? spec.SpecificationInventoryQuantity : product != null ? product.InventoryQuantity : 0;
                        detail.OccupyInventory = spec != null ? spec.OccupyInventory : product != null ? product.OccupyInventory : 0; ;
                        detail.AbnormalQuantity = spec != null ? spec.AbnormalQuantity : product != null ? product.AbnormalQuantity : 0; ;
                        detail.MainUnitId = product?.BaseUnitId;
                        detail.MainUnitName = product?.BaseUnit?.Name;
                        detail.DetailWithProductDto = product != null ? new DetailWithProductDto
                        {
                            ProductId = product.Id,
                            SpecificationId = spec?.Id,
                            ProductNo = detail.DetailDataNo,
                            ProductName = detail.DetailDataName,
                            ProductUnit = detail.DetailDataUnit,
                            ProductSpecification = detail.DetailDataSpecification,
                            ProductTechCode = detail.DetailDataTechCode,
                            ProductMaterialGrade = detail.DetailDataMaterialGrade,
                            ProductCategoryName = detail.DetailDataCategoryName,
                            ProductType = spec != null ? ProductType.成品 : product.ProductType
                        } : null;
                        detail.DetailFileData = bomFileData.Where(x => x.SourceType == FileStorageSourceType.物料清单文件 && x.SourceId == bom.Id).ToList(); //物料清单自身对应的文件
                        //获取子清单明细
                        detail.Children = bom.DetailList != null ? bom.DetailList.Select(fb => new BomDetailDto
                        {
                            Id = fb.Id,
                            DetailType = fb.DetailType,
                            DetailId = fb.DetailId,
                            Quantity = fb.Quantity,
                            Ratio = fb.Ratio,
                            CurRatio = fb.Ratio,
                            BaseQuantity = bom.TotalQuantity,
                            LowerTolerance = fb.LowerTolerance,
                            UpperTolerance = fb.UpperTolerance,
                            Editable = false,
                            FileData = bomFileData.Where(x => x.SourceType == FileStorageSourceType.物料清单明细文件 && x.SourceId == fb.Id).ToList(),
                        }).ToList() : new List<BomDetailDto>();

                        //物料清单可选单位
                        detail.UnitData = detail.UnitInfoList?.Select(x => new SelectionDto
                        {
                            Value = x.Ratio.ToString(),
                            Text = x.UnitName,
                            Code = x.Id.ToString()
                        }).ToList();
                    }
                }
                if (detail.ReplaceData.Count > 0)
                {
                    getBomChildrenData(detail.ReplaceData, CurBaseQuantity, unitName, AllDetailData);
                }
                if (detail.Children != null && detail.Children.Count > 0)
                {
                    if (detail.DetailType == BomDetailType.Formula)
                    {
                        getBomChildrenData(detail.Children, detail.CurQuantity, "kg", AllDetailData); //配方及明细的单位都是kg
                    }
                    else
                    {
                        getBomChildrenData(detail.Children, detail.CurQuantity, unitName, AllDetailData);
                    }
                }
            }
        }
        private BomDetailType transformFormulaDetailType(FormulaDetailType detailType)
        {
            switch (detailType)
            {
                case FormulaDetailType.Product:
                    return BomDetailType.Product;
                case FormulaDetailType.Formula:
                    return BomDetailType.Formula;
            }
            return BomDetailType.Product;
        }

        /// <summary>
        /// 派工物料清单选项，仅返回当前产品/规格的有效版本及其计量信息。
        /// </summary>
        [HttpGet("GetProductionBomOptions")]
        public async Task<IActionResult> GetProductionBomOptions([FromQuery] long productId, [FromQuery] long? specificationId = null)
        {
            if (productId <= 0) throw new Exception("请选择派工产品");
            var versions = await _bomVersionService.GetAll().AsNoTracking()
                .Where(x => !x.IsDeleted && !x.MBom.IsDeleted
                    && x.MBom.ProductId == productId && x.MBom.ProductSpecificationId == specificationId
                    && x.Status != BomStatus.淘汰 && x.MBom.Status != BomStatus.淘汰) //审核功能上线后 只取审核通过的物料清单！！！
                .OrderByDescending(x => x.IsMain).ThenByDescending(x => x.Id)
                .Select(x => new
                {
                    VersionId = x.Id,
                    BomId = x.MBomId,
                    x.Version,
                    x.IsMain,
                    x.Status,
                    x.Remark,
                    x.TotalQuantity,
                    x.UnitConversionId,
                    x.UnitInfo,
                }).ToListAsync();
            return Ok(versions.Select(x => new
            {
                x.VersionId,
                x.BomId,
                x.Version,
                x.IsMain,
                x.Status,
                x.Remark,
                x.TotalQuantity,
                x.UnitConversionId,
                UnitInfoList = JsonSerializer.Deserialize<List<InventoryUnitOutboundDto>>(x.UnitInfo, JsonHelper.DefaultJsonSerializerOptions)
                    ?? new List<InventoryUnitOutboundDto>(),
            }));
        }

        [HttpPost("GetBomList")]
        public async Task<IActionResult> GetBomList([FromBody] List<Filter> filters)
        {
            var productData = _productService.GetProductList([], []);
            var data = await (from x in _bomVersionService.GetAll(true)
                              join product in productData
                                on new { ProductId = x.MBom.ProductId ?? 0, SpecificationId = x.MBom.ProductSpecificationId }
                                equals new { ProductId = product.ProductId, SpecificationId = product.SpecificationId } into tmp
                              from product in tmp.DefaultIfEmpty()
                              select new BomIndexDto
                              {
                                  VersionId = x.Id,
                                  BomId = x.MBomId,
                                  BomIsDeleted = x.MBom.IsDeleted,
                                  BomStatus = x.MBom.Status,
                                  ProductId = product.ProductId,
                                  SpecificationId = product.SpecificationId,
                                  ProductNo = product.ProductNo,
                                  ProductName = product.ProductName,
                                  ProductRemark = product.Remark,
                                  ProductTechnicalCode = product.ProductTechnicalCode,
                                  ProductSpecification = product.ProductType == ProductType.物料 ? product.MaterialSpecification : product.ProductSpecification,
                                  Status = x.Status,
                                  IsMain = x.IsMain,
                                  Version = x.Version,
                                  Remark = x.Remark,
                                  VersionCustomAttribute = x.CustomAttribute,
                                  IsDeleted = x.IsDeleted,
                                  CreateTime = x.CreateTime,
                                  CreateUserName = x.CreateUserName,
                              }).ApplyFilters(filters).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            return Ok(data);
        }

        /// <summary>
        /// 删除物料清单
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpPost("DeleteBom")]
        public async Task<IActionResult> DeleteBom([FromBody] IdLongObject Id)
        {
            var bom = await _bomnService.GetAll(true).Include(x => x.VersionList).FirstOrDefaultAsync(x => x.Id == Id.Id);
            if (bom == null)
            {
                throw new Exception("物料清单不存在，请刷新重试");
            }
            //删除版本
            var detailList = _bomDetailService.GetAll().Where(x => x.MBomId == Id.Id).ToList();
            var detailIds = detailList.Select(x => x.Id).ToList();
            _bomDetailService.Delete(detailList);
            var versionIds = bom.VersionList.Select(x => x.Id).ToList();
            if (bom.VersionList != null && bom.VersionList.Count > 0)
            {
                _bomVersionService.Delete(bom.VersionList, false);
            }
            //删除文件
            var files = _fileService.GetAll().Where(x => x.SourceType == FileStorageSourceType.物料清单文件 && versionIds.Contains(x.SourceId)
                         || x.SourceType == FileStorageSourceType.物料清单明细文件 && detailIds.Contains(x.SourceId)).ToList();
            _fileService.DeleteWihtFiles(files);

            //删除 配方
            _bomnService.Delete(bom, false);

            await _unitOfWork.SaveChangeAsync();
            return Ok("删除成功");
        }

        /// <summary>
        /// 删除物料清单的版本
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpPost("DeleteBomVersion")]
        public async Task<IActionResult> DeleteBomVersion([FromBody] IdLongObject Id)
        {
            var bomVersion = await _bomVersionService.GetAll(true).Include(x => x.DetailList)
                          .FirstOrDefaultAsync(x => x.Id == Id.Id);
            if (bomVersion == null)
            {
                throw new Exception("该版本物料清单不存在，请刷新重试");
            }
            //删除版本
            var detailList = bomVersion.DetailList;
            var detailIds = detailList.Select(x => x.Id).ToList();
            if (detailList != null && detailList.Count > 0)
            {
                _bomDetailService.Delete(detailList);
            }
            _bomVersionService.Delete(bomVersion, false);

            //删除文件
            var files = _fileService.GetAll().Where(x => x.SourceType == FileStorageSourceType.物料清单文件 && x.SourceId == bomVersion.Id
                         || x.SourceType == FileStorageSourceType.物料清单明细文件 && detailIds.Contains(x.SourceId)).ToList();
            _fileService.DeleteWihtFiles(files);

            await _unitOfWork.SaveChangeAsync();
            return Ok("删除成功");
        }


        /// <summary>
        /// 获取物料清单信息、版本列表及明细、文件
        /// </summary>
        /// <param name="formulaVersionId"></param>
        /// <returns></returns>
        [HttpGet("GetBomById")]
        public async Task<IActionResult> GetBomById([FromQuery] long bomId, [FromQuery] long? bomVersionId = null)
        {
            var bom = await _bomnService.GetAll(true).Include(x => x.Product).Include(x => x.ProductSpecification)
                               .Include(x => x.VersionList)
                               .FirstOrDefaultAsync(x => x.Id == bomId);
            if (bom == null)
            {
                throw new Exception("物料清单不存在，请刷新重试");
            }
            var info = new BomDto
            {
                Id = bom.Id,
                ProductId = bom.ProductId,
                SpecificationId = bom.ProductSpecificationId,
                ProductNo = bom.ProductSpecification != null ? bom.ProductSpecification.Code :
                                bom.Product != null ? bom.Product.ProductNo : null,
                ProductName = bom.ProductSpecification != null ? bom.ProductSpecification.Name :
                                bom.Product != null ? bom.Product.ProductName : null,
                ProductTechnicalCode = bom.Product != null ? bom.Product.TechnicalCode : null,
                ProductSpecification = bom.ProductSpecification != null ? bom.ProductSpecification.Specification : null,
                ProductRemark = bom.ProductSpecification != null ? bom.ProductSpecification.Remark :
                                    bom.Product != null ? bom.Product.Remark : null,
            };
            var detailData = await _bomDetailService.GetAll().Where(x => x.MBomId == bomId).ToListAsync();
            var versionIds = bom.VersionList.Select(x => x.Id).ToList();
            var detailIds = detailData.Select(x => x.Id).ToList();
            var allFileData = _fileService.GetAll().Where(x => x.SourceType == FileStorageSourceType.物料清单文件 && versionIds.Contains(x.SourceId)
                         || x.SourceType == FileStorageSourceType.物料清单明细文件 && detailIds.Contains(x.SourceId)).ToList();
            var versionList = new List<BomVersionDataDto>();
            var index = 0;
            if (bom.VersionList != null && bom.VersionList.Count > 0)
            {
                var bomVersionList = bom.VersionList.OrderBy(x => x.Status).ThenByDescending(x => x.IsMain).ThenByDescending(x => x.Id).ToList();
                if (bomVersionId != null)
                {
                    bomVersionList = bomVersionList.Where(x => x.Id == bomVersionId).ToList();
                }
                foreach (var item in bomVersionList)
                {
                    var entity = new BomVersionDataDto
                    {
                        VersionId = item.Id,
                        Version = new BomVersionDto
                        {
                            Id = item.Id,
                            IsMain = item.IsMain,
                            Remark = item.Remark,
                            Status = item.Status,
                            IsDeleted = item.IsDeleted,
                            TotalQuantity = item.TotalQuantity,
                            UnitConversionId = item.UnitConversionId,
                            Version = item.Version,
                            UnitInfoList = item.UnitInfo != null ? JsonSerializer.Deserialize<List<InventoryUnitOutboundDto>>(item.UnitInfo, JsonHelper.DefaultJsonSerializerOptions) ?? new List<InventoryUnitOutboundDto>() : new List<InventoryUnitOutboundDto>(),
                            CustomAttributeList = item.CustomAttribute != null ? JsonSerializer.Deserialize<List<CustomAttribute>>(item.CustomAttribute, JsonHelper.DefaultJsonSerializerOptions) : new List<CustomAttribute>(),
                            FileData = allFileData.Where(x => x.SourceType == FileStorageSourceType.物料清单文件 && x.SourceId == item.Id).ToList(),
                        },
                        DetailList = item.DetailList != null ? item.DetailList.Where(x => !x.IsSubstitute)
                            .Select(detail =>
                            {
                                return getBomDetailDto(detail, item, allFileData, true);
                            }).OrderBy(x => x.Order).ToList() : new List<BomDetailDto>(),
                        Index = index++,
                    };
                    //寻找子配方 以及 配方明细对应的物料信息
                    if (entity.DetailList != null && entity.DetailList.Count > 0)
                    {
                        getBomChildrenData(entity.DetailList, entity.Version.TotalQuantity, null, item.DetailList);
                    }
                    versionList.Add(entity);
                }
            }
            var result = new ModifyBomDto
            {
                BomId = bom.Id,
                Info = info,
                VersionList = versionList,
            };
            return Ok(result);
        }
        public class ModifyBomDto : CreateBomDto
        {
            public long BomId { get; set; }
            public BomDto Info { get; set; } = new BomDto();
        }

        /// <summary>
        /// 编辑物料清单
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("ModifyBom")]
        public async Task<IActionResult> ModifyBom([FromBody] ModifyBomDto dto)
        {
            var bom = await _bomnService.GetAll(true).Include(x => x.VersionList)
                                .FirstOrDefaultAsync(x => x.Id == dto.BomId);
            if (bom == null)
            {
                throw new Exception("物料清单不存在，请刷新重试");
            }
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                bom.ProductId = dto.ProductId;
                bom.ProductSpecificationId = dto.SpecificationId;

                var versionIds = dto.VersionList.Select(x => x.Version.Id).ToList();
                var delVersionData = bom.VersionList?.Where(x => !versionIds.Contains(x.Id)).ToList();
                var existVersionData = bom.VersionList?.Where(x => versionIds.Contains(x.Id)).ToList() ?? new List<Production_MBom_Version>();
                var addVersionData = dto.VersionList.Where(x => x.Version.Id == null).ToList();
                //删除版本数据
                if (delVersionData != null && delVersionData.Count > 0)
                {
                    var delVersionIds = delVersionData.Select(x => x.Id).ToList();
                    var delDetailData = _bomDetailService.GetAll().Where(x => delVersionIds.Contains(x.VersionId)).ToList();
                    var detailIds = delDetailData.Select(x => x.Id).ToList();
                    _bomDetailService.Delete(delDetailData);
                    _bomVersionService.Delete(delVersionData, false);
                    //删除文件
                    var files = _fileService.GetAll().Where(x => x.SourceType == FileStorageSourceType.物料清单文件 && delVersionIds.Contains(x.SourceId)
                         || x.SourceType == FileStorageSourceType.物料清单明细文件 && detailIds.Contains(x.SourceId)).ToList();
                    _fileService.DeleteWihtFiles(files);
                }
                //修改版本数据
                var mainVersion = dto.VersionList.FirstOrDefault(x => x.Version.IsMain);
                var versionDetailData = _bomDetailService.GetAll().Include(x => x.BomVersion).Include(x => x.MBom)
                    .Where(x => versionIds.Contains(x.VersionId)).ToList();
                foreach (var item in existVersionData)
                {
                    var dtoVersion = dto.VersionList.First(x => x.Version.Id == item.Id);
                    item.Version = dtoVersion.Version.Version;
                    item.IsMain = dtoVersion.Version.IsMain;
                    item.Status = dtoVersion.Version.Status;
                    item.Remark = dtoVersion.Version.Remark;
                    item.TotalQuantity = dtoVersion.Version.TotalQuantity;
                    item.UnitConversionId = dtoVersion.Version.UnitConversionId;
                    item.UnitInfo = dtoVersion.Version.UnitInfoList.Count > 0 ? JsonSerializer.Serialize(dtoVersion.Version.UnitInfoList, JsonHelper.DefaultJsonSerializerOptions) : "[]";
                    item.CustomAttribute = dtoVersion.Version.CustomAttributeList != null && dtoVersion.Version.CustomAttributeList.Count > 0 ?
                                    JsonSerializer.Serialize(dtoVersion.Version.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null;

                    this.modifyBomVersionDetails(dtoVersion.DetailList, item.Id, item.IsMain, mainVersion, versionDetailData, null, null);

                    //增加明细数据 
                    var addDetailData = dtoVersion.DetailList.Where(x => x.Id == null).ToList();
                    foreach (var detail in addDetailData)
                    {
                        this.addVersionDetail(mainVersion, detail, bom, item);
                        if (detail.ReplaceData.Count > 0)
                        {
                            _unitOfWork.SaveChange();

                            foreach (var subDetail in detail.ReplaceData)
                            {
                                this.addVersionDetail(mainVersion, subDetail, bom, item, true, detail.Id);
                            }
                        }
                    }
                    //特殊处理 增加明细中 配方明细的替换料
                    var allFormulaReplaceData = getFormulaReplaceData(addDetailData);
                    if (allFormulaReplaceData.Count > 0)
                    {
                        var entities = allFormulaReplaceData.SelectMany(x => x.FormulaReplaceData).ToList();
                        foreach (var subDetail in entities)
                        {
                            this.addVersionDetail(mainVersion, subDetail, bom, item, true);
                        }
                    }
                }
                //新增版本数据
                foreach (var item in addVersionData)
                {
                    this.addVersion(mainVersion, item, bom);
                }
                await _unitOfWork.SaveChangeAsync();
                scope.Complete();

                //返回id 用于文件上传
                var res = dto.VersionList.SelectMany(x =>
                {
                    if (x.DetailList.Count > 0)
                    {
                        return x.DetailList.Select(y => new CreateBomResponseDto
                        {
                            Index = x.Index,
                            VersionId = x.VersionId,
                            Version = x.Version.Version,
                            Order = y.Order,
                            DetailId = y.Id
                        });
                    }
                    else
                    {
                        return new List<CreateBomResponseDto> {
                        new CreateBomResponseDto
                        {
                            Index = x.Index,
                            VersionId = x.VersionId,
                            Version = x.Version.Version,
                            Order = null,
                            DetailId = null
                        }};
                    }
                }).ToList();

                return Ok(new
                {
                    details = res,
                    bomId = dto.BomId,
                });
            }
        }
        private void modifyBomVersionDetails(List<BomDetailDto> dtoDetailList, long versionId, bool isMainVersion, BomVersionDataDto? mainVersion, List<Production_MBom_Detail> versionDetailData, long? mainDetailId, long? mainFormulaDetailId)
        {
            var isSubstitute = mainDetailId.HasValue || mainFormulaDetailId.HasValue ? true : false; //处理的是否为替换料
            var detailIds = dtoDetailList.Select(x => x.Id).ToList();
            var itemDetailData = versionDetailData.Where(x => x.VersionId == versionId && x.IsSubstitute == isSubstitute
                                && x.MainDetailId == mainDetailId && x.MainFormulaDetailId == mainFormulaDetailId).ToList();
            var delDetailData = itemDetailData.Where(x => !detailIds.Any(d => d == x.Id)).ToList();
            if (delDetailData != null && delDetailData.Count > 0)
            {
                //需要删除的替换料
                var delIds = delDetailData.Select(x => x.Id).ToList();
                var delReplaceData = versionDetailData.Where(x => x.IsSubstitute
                                    && !x.MainFormulaDetailId.HasValue
                                    && x.MainDetailId.HasValue && delIds.Contains(x.MainDetailId.Value)).ToList();
                _bomDetailService.Delete(delReplaceData);

                _bomDetailService.Delete(delDetailData);
            }
            //修改明细数据
            var existDetailData = itemDetailData.Where(x => detailIds.Contains(x.Id)).ToList() ?? new List<Production_MBom_Detail>();
            foreach (var detail in existDetailData)
            {
                var dtoDetail = dtoDetailList.First(x => x.Id == detail.Id);
                detail.DetailType = dtoDetail.DetailType;
                detail.DetailId = dtoDetail.DetailId;
                detail.Order = dtoDetail.Order;
                detail.Quantity = dtoDetail.Quantity;
                detail.Ratio = dtoDetail.Ratio;
                detail.LowerTolerance = dtoDetail.LowerTolerance;
                detail.UpperTolerance = dtoDetail.UpperTolerance;
                detail.IsSubstitute = isSubstitute;
                detail.MainDetailId = mainDetailId;
                detail.MainFormulaDetailId = mainFormulaDetailId;
                detail.UnitConversionId = dtoDetail.UnitConversionId;
                detail.UnitInfo = dtoDetail.UnitInfoList.Count > 0 ? JsonSerializer.Serialize(dtoDetail.UnitInfoList, JsonHelper.DefaultJsonSerializerOptions) : "[]";

                if (dtoDetail.DetailType == BomDetailType.Formula && dtoDetail.Children.Count > 0)
                {
                    detail.FormulaDetailInfo = JsonSerializer.Serialize(dtoDetail.Children, JsonHelper.DefaultJsonSerializerOptions);
                }
                else
                {
                    detail.FormulaDetailInfo = null;
                }

                if (isMainVersion)
                {
                    detail.RatioDiff2MainVersion = 0;
                    detail.QuantityDiff2MainVersion = 0;
                }
                else
                {
                    var mainVersionDetail = mainVersion != null ? mainVersion.DetailList.FirstOrDefault(x => x.DetailType == detail.DetailType && x.DetailId == detail.DetailId) : null;
                    detail.RatioDiff2MainVersion = mainVersionDetail != null ? detail.Ratio - mainVersionDetail.Ratio : 0;
                    detail.QuantityDiff2MainVersion = mainVersionDetail != null ? detail.Quantity - mainVersionDetail.Quantity : 0;
                }

                //处理替换料
                modifyBomVersionDetails(dtoDetail.ReplaceData, versionId, isMainVersion, mainVersion, versionDetailData, dtoDetail.Id, null);
                //新增替换料
                var addReplaceData = dtoDetail.ReplaceData.Where(x => x.Id == null).ToList();
                foreach (var replace in addReplaceData)
                {
                    this.addVersionDetail(mainVersion, replace, detail.MBom, detail.BomVersion, true, detail.Id);
                }

                //特殊处理 配方明细的替换料
                if (!isSubstitute)
                {
                    var frData = getFormulaReplaceData(dtoDetail.Children);
                    var allFormulaReplaceData = frData.SelectMany(x => x.FormulaReplaceData).ToList();
                    //编辑
                    var oldFormulaReplaceData = allFormulaReplaceData.Where(x => x.Id.HasValue).ToList();
                    var gorupFormulaReplaceData = oldFormulaReplaceData.GroupBy(x => x.MainFormulaDetailId);
                    var groupKeys = gorupFormulaReplaceData.Select(x => x.Key).ToList();
                    foreach (var item in gorupFormulaReplaceData)
                    {
                        modifyBomVersionDetails(item.ToList(), versionId, isMainVersion, mainVersion, versionDetailData, null, item.Key);
                    }
                    //新增
                    var addFormulaReplaceData = allFormulaReplaceData.Where(x => x.Id == null).ToList();
                    foreach (var subDetail in addFormulaReplaceData)
                    {
                        this.addVersionDetail(mainVersion, subDetail, detail.MBom, detail.BomVersion, true);
                    }
                    //删除
                    foreach (var item in frData)
                    {
                        if (item.DelAllReplaceData)
                        {
                            var delFormulaReplaceData = versionDetailData.Where(x => x.VersionId == versionId
                                    && x.IsSubstitute && x.MainDetailId == null
                                    && x.MainFormulaDetailId == item.FormulaDetailId).ToList();
                            _bomDetailService.Delete(delFormulaReplaceData);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 派工 读取可领用物料清单
        /// 只领用第一个层级的物料清单明细
        /// </summary>
        /// <param name="bomVersionId"></param>
        [HttpGet("GetProductionBomDataById")]
        public async Task<IActionResult> GetProductionBomDataById([FromQuery] long bomVersionId, [FromQuery] decimal quantity)
        {
            var bomVersion = await _bomVersionService.GetAll().Include(x => x.MBom.Product)
                                .Include(x => x.MBom.ProductSpecification)
                                .Include(x => x.DetailList)
                                .Where(x => x.Id == bomVersionId)
                                .FirstOrDefaultAsync();
            if (bomVersion == null)
            {
                throw new Exception("物料清单不存在,请刷新重试");
            }
            var detailIds = bomVersion.DetailList.Select(x => x.Id).ToList();
            var allFileData = _fileService.GetAll().Where(x => (x.SourceType == FileStorageSourceType.物料清单明细文件 && detailIds.Contains(x.SourceId)) ||
                            (x.SourceType == FileStorageSourceType.物料清单文件 && x.SourceId == bomVersion.Id)).ToList();
            var data = new BomVersionDataDto
            {
                VersionId = bomVersion.Id,
                Version = new BomVersionDto
                {
                    Id = bomVersion.Id,
                    Version = bomVersion.Version,
                    IsMain = bomVersion.IsMain,
                    Remark = bomVersion.Remark,
                    Status = bomVersion.Status,
                    TotalQuantity = bomVersion.TotalQuantity,
                    UnitConversionId = bomVersion.UnitConversionId,
                    UnitInfoList = JsonSerializer.Deserialize<List<InventoryUnitOutboundDto>>(bomVersion.UnitInfo, JsonHelper.DefaultJsonSerializerOptions) ?? new List<InventoryUnitOutboundDto>(),
                    FileData = allFileData.Where(x => x.SourceType == FileStorageSourceType.物料清单文件 && x.SourceId == bomVersion.Id).ToList(),
                },
                DetailList = bomVersion.DetailList != null ? bomVersion.DetailList.Where(x => !x.IsSubstitute)
                .Select(detail =>
                {
                    return getBomDetailDto(detail, bomVersion, allFileData, true);
                }).OrderBy(x => x.Order).ToList() : new List<BomDetailDto>(),
            };

            //寻找子配方 以及 配方明细对应的物料信息
            if (data.DetailList != null && data.DetailList.Count > 0)
            {
                getBomChildrenData(data.DetailList, quantity, null, bomVersion.DetailList);
            }

            if (data.DetailList != null && data.DetailList.Count > 0)
            {
                // 同产品且不带规格的配方省略自身节点，将配方明细提升一级。
                var baseUnit = data.Version.UnitInfoList.FirstOrDefault(x => x.Id == data.Version.UnitConversionId);
                var details = new List<BomDetailDto>();
                foreach (var detail in data.DetailList)
                {
                    if (detail.DetailType == BomDetailType.Formula && detail.DetailWithProductDto != null
                        && detail.DetailWithProductDto.ProductId == bomVersion.MBom.ProductId //配方只会对应Product 不对应Specification
                        && bomVersion.MBom.ProductSpecificationId == null)
                    {
                        foreach (var child in detail.Children)
                        {
                            if (baseUnit != null)
                                child.BaseUnitName = baseUnit.UnitName;
                        }
                        details.AddRange(detail.Children);
                    }
                    else
                    {
                        details.Add(detail);
                    }
                }
                data.DetailList = details;
                //只返回第一个层级的数据
                foreach (var item in data.DetailList)
                {
                    item.Children = new List<BomDetailDto>();
                }
            }

            return Ok(data);
        }
    }
}
