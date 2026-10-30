using Kingzon.OLCM.Base.Services;
using Kingzon.OLCM.Base.Services.JsonStructure;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Base.Service;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Common.DataBase.Entities;
using KingZon.OLCM.Common.Service.Dto;
using KingZon.OLCM.Quality.Shared.DataBase.Entities;
using KingZon.OLCM.Technical.Shared.DataBase.Entities;
using KingZon.OLCM.Technical.Shared.DataBase.Enum;
using KingZon.OLCM.Technical.WebApi.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Index.HPRtree;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Transactions;

namespace KingZon.OLCM.Technical.WebApi.Controllers
{
    [Route("api/technical/[controller]")]
    [ApiController]
    public class FormulaManageController : KingZonControllerBase
    {
        private readonly ProductService _productService;
        private readonly CommonService<Technical_FormulaVersion> _formulaVersionService;
        private readonly CommonService<Technical_FormulaDetail> _formulaDetailService;
        private readonly CommonService<Technical_Formula> _formulaService;
        private readonly CommonService<Quality_Standard> _qualityStandardService;
        private readonly CommonService<Technical_Inspection> _inspectionService;
        private readonly CommonService<Technical_InspectionItem_Value> _inspectionItemValueService;
        private readonly CommonService<Technical_InspectionItem> _inspectionItemService;
        private readonly CommonService<Quality_InspCharacteristic> _qualityCharacteristicService;
        private readonly CommonService<Base_Partner> _customerService;
        private readonly FileStorageService _fileStorageService;
        private readonly IUnitOfWork _unitOfWork;

        public FormulaManageController(
            ProductService productService,
            CommonService<Technical_FormulaVersion> formulaVersionService,
            CommonService<Technical_FormulaDetail> formulaDetailService,
            CommonService<Technical_Formula> formulaService,
            CommonService<Quality_Standard> qualityStandardService,
            CommonService<Technical_Inspection> inspectionService,
            CommonService<Technical_InspectionItem_Value> inspectionItemValueService,
            CommonService<Technical_InspectionItem> inspectionItemService,
            CommonService<Quality_InspCharacteristic> qualityCharacteristicService,
            CommonService<Base_Partner> customerService,
            FileStorageService fileStorageService,
            IUnitOfWork unitOfWork)
        {
            _productService = productService;
            _formulaVersionService = formulaVersionService;
            _formulaDetailService = formulaDetailService;
            _formulaService = formulaService;
            _qualityStandardService = qualityStandardService;
            _inspectionService = inspectionService;
            _inspectionItemValueService = inspectionItemValueService;
            _inspectionItemService = inspectionItemService;
            _qualityCharacteristicService = qualityCharacteristicService;
            _customerService = customerService;
            _fileStorageService = fileStorageService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 获取配方列表
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetFormulaList")]
        public async Task<IActionResult> GetFormulaList([FromBody] List<Filter> filters)
        {
            var data = await _formulaVersionService.GetAll(true).ApplyFilters(filters)
                .Select(x => new FormulaIndexDto
                {
                    VersionId = x.Id,
                    FormulaId = x.FormulaId,
                    FormulaCode = x.Formula.Code,
                    FormulaName = x.Formula.Name,
                    FormulaRemark = x.Formula.Remark,
                    FormulaCustomAttribute = x.Formula.CustomAttribute,
                    FormulaStatus = x.Formula.Status,
                    FormulaIsDeleted = x.Formula.IsDeleted,
                    ProductId = x.Formula.ProductId,
                    ProductName = x.Formula.Product != null ? x.Formula.Product.ProductName : null,
                    ProductRemark = x.Formula.Product != null ? x.Formula.Product.Remark : null,
                    ProductTechnicalCode = x.Formula.Product != null ? x.Formula.Product.TechnicalCode : null,
                    Status = x.Status,
                    IsMain = x.IsMain,
                    Version = x.Version,
                    Remark = x.Remark,
                    VersionCustomAttribute = x.CustomAttribute,
                    IsDeleted = x.IsDeleted,
                    CreateTime = x.CreateTime,
                    CreateUserName = x.CreateUserName,
                }).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            return Ok(data);
        }

        [HttpGet("GetFormulaOptions")]
        public async Task<IActionResult> GetFormulaOptions()
        {
            var data = await _formulaService.GetAll()
                .Select(x => new SelectionDto
                {
                    Text = x.Code + "-" + x.Name,
                    Value = x.Id + "",
                    Code = x.Code,
                }).ToListAsync();
            return Ok(data);
        }

        [HttpPost("GetRawMaterialResourceList")]
        public async Task<IActionResult> GetRawMaterialResourceList([FromBody] List<Filter> filters)
        {
            var data = await _productService.GetMaterialList([]).Where(x => !x.IsDeleted && x.ReviewStatus == ReviewStatus.评审通过)
                .Select(x => new FormulaResourceDto
                {
                    Code = x.ProductNo,
                    Name = x.ProductName,
                    Unit = x.ProductUnit,
                    TechCode = x.ProductTechnicalCode,
                    Specification = x.MaterialSpecification,
                    Remark = x.Remark,
                    DetailType = FormulaDetailType.Product,
                    DetailId = x.ProductId,
                    ProductType = x.ProductType,
                }).ApplyFilters(filters).ToPagedListAsync(this.PageModel);
            return Ok(data);
        }

        [HttpPost("GetProductResourceList")]
        public async Task<IActionResult> GetProductResourceList([FromBody] List<Filter> filters, [FromQuery] ProductType productType = ProductType.成品)
        {
            var formulaData = _formulaVersionService.GetAll().Include(x => x.Formula).Include(x => x.DetailList);
            var productQuery = _productService.GetProductList([], []).Where(x => x.ProductType == productType && !x.IsDeleted && x.ReviewStatus == ReviewStatus.评审通过
                                && x.SpecificationId == null); //只取成品表数据
            var productData = from product in productQuery
                              join formula in formulaData
                               on product.ProductId equals formula.Formula.ProductId into tmp
                              from formula in tmp.DefaultIfEmpty()
                              select new FormulaResourceDto
                              {
                                  Code = product.ProductNo,
                                  Name = product.ProductName,
                                  Unit = product.ProductUnit,
                                  TechCode = product.ProductTechnicalCode,
                                  Remark = product.Remark,
                                  DetailType = formula != null ? FormulaDetailType.Formula : FormulaDetailType.Product,
                                  DetailId = formula != null ? formula.Id : product.ProductId,
                                  FormulaCode = formula != null ? formula.Formula.Code : null,
                                  FormulaName = formula != null ? formula.Formula.Name : null,
                                  FormulaVersion = formula != null ? formula.Version : null,
                                  FormulaRemark = formula != null ? formula.Remark : null,
                                  ProductType = product.ProductType,
                                  FormulaId = formula != null ? formula.FormulaId : null,
                              };
            var data = await productData.ApplyFilters(filters).ToPagedListAsync(this.PageModel);
            return Ok(data);
        }

        [HttpPost("GetFormulaResourceList")]
        public async Task<IActionResult> GetFormulaResourceList([FromBody] List<Filter> filters)
        {
            var data = await _formulaVersionService.GetAll().Include(x => x.Formula).ThenInclude(x => x.Product)
                .Select(formula => new FormulaResourceDto
                {
                    Code = formula.Formula.Code,
                    Name = formula.Formula.Name,
                    Remark = formula.Remark,
                    Version = formula.Version,
                    DetailType = FormulaDetailType.Formula,
                    DetailId = formula.Id,
                    ProductCode = formula.Formula.Product != null ? formula.Formula.Product.TechnicalCode : null, //产品技术型号
                    ProductName = formula.Formula.Product != null ? formula.Formula.Product.ProductName : null,
                    ProductRemark = formula.Formula.Product != null ? formula.Formula.Product.Remark : null,
                    FormulaId = formula.FormulaId,
                }).ApplyFilters(filters).ToPagedListAsync(this.PageModel);
            return Ok(data);
        }

        /// <summary>
        /// 读取配方的所有明细
        /// </summary>
        /// <param name="formulaId"></param>
        [HttpGet("GetFormulaDataById")]
        public async Task<IActionResult> GetFormulaDataById([FromQuery] long formulaVersionId, [FromQuery] bool editable, [FromQuery] decimal quantity)
        {
            var formula = await _formulaVersionService.GetAll().Include(x => x.Formula).ThenInclude(x => x.Product).Include(x => x.DetailList)
                .Where(x => x.Id == formulaVersionId)
                .FirstOrDefaultAsync();
            if (formula == null)
            {
                throw new Exception("配方不存在,请刷新重试");
            }
            var data = new FormulaVersionDataDto
            {
                Version = new FormulaVersionDto
                {
                    Id = formula.Id,
                    Version = formula.Version,
                    IsMain = formula.IsMain,
                    Remark = formula.Remark,
                    Status = formula.Status,
                    TotalQuantity = formula.TotalQuantity,
                    IsDeleted = formula.IsDeleted
                },
                DetailList = formula.DetailList != null ? formula.DetailList.Select(detail => new FormulaDetailDto
                {
                    Id = detail.Id,
                    DetailType = detail.DetailType,
                    DetailId = detail.DetailId,
                    Quantity = detail.Quantity,
                    Ratio = detail.Ratio,
                    CurRatio = detail.Ratio,
                    BaseQuantity = formula.TotalQuantity,
                    LowerTolerance = detail.LowerTolerance,
                    UpperTolerance = detail.UpperTolerance,
                    Replaceable = detail.Replaceable,
                    Editable = editable
                }).ToList() : new List<FormulaDetailDto>(),
            };

            //寻找子配方 以及 配方明细对应的物料信息
            if (data.DetailList != null && data.DetailList.Count > 0)
            {
                getFormulaChildrenData(data.DetailList, quantity);
            }
            return Ok(data);
        }
        private void getFormulaChildrenData(List<FormulaDetailDto> DetailList, Decimal? CurBaseQuantity)
        {
            var materialIds = DetailList.Where(x => x.DetailType == FormulaDetailType.Product).Select(x => x.DetailId).ToList();
            var formulaIds = DetailList.Where(x => x.DetailType == FormulaDetailType.Formula).Select(x => x.DetailId).ToList();
            var materialData = _productService.GetProductList([], []).Where(x => materialIds.Contains(x.ProductId)).ToList();
            var formulaData = _formulaVersionService.GetAll().Include(x => x.Formula).ThenInclude(x => x.Product).ThenInclude(x => x.BaseUnit)
                                .Include(x => x.DetailList).Where(x => formulaIds.Contains(x.Id)).ToList();
            foreach (var detail in DetailList)
            {
                detail.CurBaseQuantity = CurBaseQuantity ?? 0;
                detail.CurQuantity = detail.CurBaseQuantity == detail.BaseQuantity ? detail.Quantity : Math.Round((detail.CurRatio / 100) * detail.CurBaseQuantity, 4); //保留4位小数
                if (detail.DetailType == FormulaDetailType.Product)
                {
                    var material = materialData.FirstOrDefault(x => x.ProductId == detail.DetailId);
                    if (material != null)
                    {
                        detail.DetailDataNo = material.ProductNo ?? "";
                        detail.DetailDataName = material.ProductName ?? "";
                        detail.DetailDataTechCode = material.ProductTechnicalCode ?? "";
                        detail.DetailDataSpecification = material.ProductType == ProductType.物料 ? material.MaterialSpecification ?? "" : material.ProductSpecification ?? "";
                        detail.DetailDataUnit = material.ProductUnit ?? "";
                        detail.DetailDataUnitId = material.ProductUnitId;
                        detail.DetailProductType = material.ProductType;
                        detail.DetailDataMaterialGrade = material.MaterialGrade??"";
                        detail.DetailDataCategoryName = material.ProductCategory ?? "";
                        detail.InventoryQuantity = material.InventoryQuantity;
                        detail.OccupyInventory = material.OccupyInventory;
                        detail.AbnormalQuantity = material.AbnormalQuantity;
                        detail.MainUnitId = material.ProductMainUnitId;
                        detail.MainUnitName = material.ProductMainUnit;
                    }
                }
                else if (detail.DetailType == FormulaDetailType.Formula)
                {
                    var formula = formulaData.FirstOrDefault(x => x.Id == detail.DetailId);
                    if (formula != null)
                    {
                        detail.DetailDataNo = formula.Formula.Code;
                        detail.DetailDataName = formula.Formula.Name;
                        detail.DetailDataVersion = formula.Version;
                        detail.DetailFormulaId = formula.FormulaId;
                        detail.DetailDataTechCode = formula.Formula.Product != null && formula.Formula.Product.TechnicalCode != null ? formula.Formula.Product.TechnicalCode : "";
                        //获取配方对应的物料信息
                        detail.DetailDataUnit = formula.Formula.Product != null ? formula.Formula.Product.BaseUnit.Name : "";
                        detail.DetailDataUnitId = formula.Formula.Product != null ? formula.Formula.Product.BaseUnitId :null;
                        detail.InventoryQuantity = formula.Formula.Product != null ? formula.Formula.Product.InventoryQuantity : 0;
                        detail.OccupyInventory = formula.Formula.Product != null ? formula.Formula.Product.OccupyInventory : 0;
                        detail.AbnormalQuantity = formula.Formula.Product != null ? formula.Formula.Product.AbnormalQuantity : 0;
                        detail.MainUnitId = formula.Formula.Product != null ? formula.Formula.Product.BaseUnitId : null;
                        detail.MainUnitName = formula.Formula.Product != null ? formula.Formula.Product.BaseUnit.Name : null;
                        detail.FormulaProductInfo = formula.Formula.Product != null ? new FormulaDetailProductDto
                        {
                            ProductId = (long)formula.Formula.ProductId!,
                            ProductNo = formula.Formula.Product.ProductNo,
                            ProductName = formula.Formula.Product.ProductName,
                            ProductUnit = formula.Formula.Product.BaseUnit.Name,
                            ProductSpecification = formula.Formula.Product.ProductType == ProductType.物料 ? formula.Formula.Product.MaterialSpecification ?? "" : "",
                            ProductTechCode = formula.Formula.Product.TechnicalCode ?? "",
                            ProductMaterialGrade =  formula.Formula.Product.MaterialGrade??"",
                        } : null;
                        //获取子配方明细
                        detail.Children = formula.DetailList != null ? formula.DetailList.Select(fd => new FormulaDetailDto
                        {
                            Id = fd.Id,
                            DetailType = fd.DetailType,
                            DetailId = fd.DetailId,
                            Quantity = fd.Quantity,
                            Ratio = fd.Ratio,
                            CurRatio = fd.Ratio,
                            BaseQuantity = formula.TotalQuantity,
                            LowerTolerance = fd.LowerTolerance,
                            UpperTolerance = fd.UpperTolerance,
                            Replaceable = fd.Replaceable,
                            Editable = false,
                            TotalRatio = fd.Ratio / 100 * detail.TotalRatio,
                        }).ToList() : new List<FormulaDetailDto>();
                        if (detail.Children != null && detail.Children.Count > 0)
                        {
                            getFormulaChildrenData(detail.Children, detail.CurQuantity);
                        }
                    }
                }
            }

        }

        [HttpGet("GetallQualityStandard")]
        public async Task<IActionResult> GetallQualityStandard()
        {
            var data = await _qualityStandardService.GetAll().Where(x => !x.IsDeleted).Select(x => new SelectionDto
            {
                Value = x.Id.ToString(),
                Code = x.Code,
                Text = x.Code + x.Name,
            }).ToListAsync();
            return Ok(data);
        }
        [HttpGet("GetAllCharacteristic")]
        public async Task<IActionResult> GetAllCharacteristic()
        {
            var data = await _qualityCharacteristicService.GetAll().Where(x => !x.IsDeleted).Select(x => new SelectionDto
            {
                Value = x.Id.ToString(),
                Code = x.Code,
                Text = x.Name,
                Extension = x.Unit
            }).ToListAsync();
            return Ok(data);
        }

        public class CreateFormulaDto
        {
            public FormulaDto Info { get; set; } = new FormulaDto();
            public List<FormulaVersionDataDto> VersionList { get; set; } = new List<FormulaVersionDataDto>();
            public List<FormulaInspectionDataDto> InspectionList { get; set; } = new List<FormulaInspectionDataDto>();
            public bool returnVersionId { get; set; } = false;
        }
        /// <summary>
        /// 新增配方
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("CreateFormula")]
        public async Task<IActionResult> CreateFormula([FromBody] CreateFormulaDto dto)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var existFormula = _formulaService.GetAll().Where(x => x.Code == dto.Info.FormulaCode).ToList();
                if (existFormula.Count > 0)
                {
                    throw new Exception("配方编码已存在，请修改编码");
                }
                var formulaEntity = new Technical_Formula
                {
                    ProductId = dto.Info.ProductId,
                    Code = dto.Info.FormulaCode,
                    Name = dto.Info.FormulaName,
                    Remark = dto.Info.Remark,
                    CustomAttribute = dto.Info.CustomAttributeList != null && dto.Info.CustomAttributeList.Count > 0 ? JsonSerializer.Serialize(dto.Info.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
                };
                await _formulaService.InsertAsync(formulaEntity);

                var mainVersion = dto.VersionList.FirstOrDefault(x => x.Version.IsMain);
                foreach (var item in dto.VersionList)
                {
                    this.addVersion(mainVersion, item, formulaEntity);
                }
                foreach (var item in dto.InspectionList)
                {
                    this.addInspection(item, formulaEntity);
                }
                await _unitOfWork.SaveChangeAsync();

                if (dto.returnVersionId) //新增子配方时 只会有一个版本 返回版本id给前端
                {
                    var versionId = _formulaVersionService.GetAll().FirstOrDefault(x => x.FormulaId == formulaEntity.Id)?.Id;

                    scope.Complete();
                    return Ok(new
                    {
                        formulaId = formulaEntity.Id,
                        versionId = versionId
                    });
                }
                else
                {
                    scope.Complete();
                    return Ok(formulaEntity.Id);
                }
            }
        }
        private void addInspection(FormulaInspectionDataDto item, Technical_Formula formulaEntity)
        {
            var insp = item.Inspection;
            var inspEntity = new Technical_Inspection
            {
                Formula = formulaEntity,
                Name = insp.Name,
                IsDefault = insp.IsDefault,
                QualityStandardId = insp.QualityStandardId,
                CustomerId = insp.CustomerId,
                Remark = insp.Remark,
            };
            _inspectionService.Insert(inspEntity);
            if (item.DetailList.Count > 0)
            {
                var groupData = item.DetailList.GroupBy(x => x.GroupId);
                foreach (var group in groupData)
                {
                    var inspectionItem = group.FirstOrDefault(x => !x.IsGroupMember) ?? group.First();
                    var firstItemId = addInspectionItem(inspectionItem, inspEntity);
                    foreach (var detail in group.Skip(1))
                    {
                        addInspectionItem(detail, inspEntity, firstItemId);
                    }
                }
            }
        }
        private long addInspectionItem(InspectionItemDto inspectionItem, Technical_Inspection inspEntity, long? groupId = null)
        {
            var inspItemEntity = new Technical_InspectionItem
            {
                Inspection = inspEntity,
                InspectionName = inspectionItem.InspectionName,
                CharacteristicId = inspectionItem.CharacteristicId,
                InspectionMethod = inspectionItem.InspectionMethod,
                FrequencyType = inspectionItem.FrequencyType,
                Frequency = inspectionItem.Frequency,
                Equipment = inspectionItem.EquipmentList != null && inspectionItem.EquipmentList.Count > 0 ? JsonSerializer.Serialize(inspectionItem.EquipmentList, JsonHelper.DefaultJsonSerializerOptions) : null,
                SafetyTips = inspectionItem.SafetyTips,
                Remark = inspectionItem.Remark,
                GroupId = groupId,
                Order = inspectionItem.Order,
                CustomerIds = inspectionItem.CustomerList != null && inspectionItem.CustomerList.Count > 0 ? JsonSerializer.Serialize(inspectionItem.CustomerList, JsonHelper.DefaultJsonSerializerOptions) : null,
            };
            _inspectionItemService.Insert(inspItemEntity);
            if (inspectionItem.ValueList.Count > 0)
            {
                foreach (var value in inspectionItem.ValueList)
                {
                    this.addInspectionItemValue(value, inspItemEntity);
                    //var inspItemValueEntity = new Technical_InspectionItem_Value
                    //{
                    //    InspectionItem = inspItemEntity,
                    //    TextValue = value.TextValue,
                    //    StandardValue = value.StandardValue,
                    //    UnitValue = value.UnitValue,
                    //    MinValue = value.MinValue,
                    //    MaxValue = value.MaxValue,
                    //    LowerTolerance = value.LowerTolerance,
                    //    UpperTolerance = value.UpperTolerance,
                    //    ValueMode = value.ValueMode,
                    //};
                    //_inspectionItemValueService.Insert(inspItemValueEntity);
                }
            }
            _unitOfWork.SaveChange();
            return inspItemEntity.Id;
        }
        private void addInspectionItemValue(InspectionItemValueDto value, Technical_InspectionItem inspItemEntity)
        {
            var inspItemValueEntity = new Technical_InspectionItem_Value
            {
                InspectionItem = inspItemEntity,
                TextValue = value.TextValue,
                StandardValue = value.StandardValue,
                UnitValue = value.UnitValue,
                MinValue = value.MinValue,
                MaxValue = value.MaxValue,
                LowerTolerance = value.LowerTolerance,
                UpperTolerance = value.UpperTolerance,
                ValueMode = value.ValueMode,
            };
            _inspectionItemValueService.Insert(inspItemValueEntity);
        }
        private void addVersionDetail(FormulaVersionDataDto? mainVersion, FormulaDetailDto detail, Technical_Formula formulaEntity, Technical_FormulaVersion versionEntity)
        {
            var mainVersionDetail = mainVersion != null ? mainVersion.DetailList.FirstOrDefault(x => x.DetailType == detail.DetailType && x.DetailId == detail.DetailId) : null;
            var RatioDiff2MainVersion = mainVersionDetail != null ? detail.Ratio - mainVersionDetail.Ratio : 0;
            var quantityDiff2MainVersion = mainVersionDetail != null ? detail.Quantity - mainVersionDetail.Quantity : 0;
            var detailEntity = new Technical_FormulaDetail
            {
                Formula = formulaEntity,
                FormulaVersion = versionEntity,
                DetailType = detail.DetailType,
                DetailId = detail.DetailId,
                Order = detail.Order,
                Quantity = detail.Quantity,
                Ratio = detail.Ratio,
                LowerTolerance = detail.LowerTolerance,
                UpperTolerance = detail.UpperTolerance,
                Replaceable = detail.Replaceable,
                QuantityDiff2MainVersion = quantityDiff2MainVersion,
                RatioDiff2MainVersion = RatioDiff2MainVersion,
            };
            _formulaDetailService.Insert(detailEntity);
        }
        private void addVersion(FormulaVersionDataDto? mainVersion, FormulaVersionDataDto item, Technical_Formula formulaEntity)
        {
            var version = item.Version;
            var versionEntity = new Technical_FormulaVersion
            {
                Formula = formulaEntity,
                Version = version.Version,
                IsMain = version.IsMain,
                Remark = version.Remark,
                TotalQuantity = version.TotalQuantity ?? 0,
                Status = version.Status,
                CustomAttribute = version.CustomAttributeList != null && version.CustomAttributeList.Count > 0 ?
                            JsonSerializer.Serialize(version.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
            };
            _formulaVersionService.Insert(versionEntity);

            if (item.DetailList.Count > 0)
            {
                foreach (var detail in item.DetailList)
                {
                    this.addVersionDetail(mainVersion, detail, formulaEntity, versionEntity);
                }
            }
        }

        public class ModifyFormulaDto : CreateFormulaDto
        {
            public long FormulaId { get; set; }
        }
        /// <summary>
        /// 获取配方信息、版本列表及明细、检验方案列表及明细
        /// </summary>
        /// <param name="formulaVersionId"></param>
        /// <returns></returns>
        [HttpGet("GetFormulaById")]
        public async Task<IActionResult> GetFormulaById([FromQuery] long formulaId, [FromQuery] long? formulaVersionId = null)
        {

            var formula = await _formulaService.GetAll(true).Include(x => x.Product).ThenInclude(x => x.ProductCategory)
                                .Include(x => x.VersionList)
                                .Include(x => x.InspectionList)
                                .FirstOrDefaultAsync(x => x.Id == formulaId);
            if (formula == null)
            {
                throw new Exception("配方不存在，请刷新重试");
            }
            var info = new FormulaDto
            {
                Id = formula.Id,
                FormulaCode = formula.Code,
                FormulaName = formula.Name,
                ProductId = formula.ProductId,
                Remark = formula.Remark,
                ProductCategory = formula.Product != null ? formula.Product.ProductCategory.Name : null,
                ProductName = formula.Product != null ? formula.Product.ProductName : null,
                ProductRemark = formula.Product != null ? formula.Product.Remark : null,
                ProductTechnicalCode = formula.Product != null ? formula.Product.TechnicalCode : null,
                CustomAttributeList = formula.CustomAttribute != null ? JsonSerializer.Deserialize<List<CustomAttribute>>(formula.CustomAttribute, JsonHelper.DefaultJsonSerializerOptions) : new List<CustomAttribute>(),
            };
            var detailData = await _formulaDetailService.GetAll().Where(x => x.FormulaId == formulaId).ToListAsync();
            var versionList = new List<FormulaVersionDataDto>();
            var index = 0;
            if (formula.VersionList != null && formula.VersionList.Count > 0)
            {
                var formulaVersionList = formula.VersionList.OrderBy(x => x.Status).ThenByDescending(x => x.IsMain).ThenByDescending(x => x.Id).ToList();
                if (formulaVersionId != null)
                {
                    formulaVersionList = formulaVersionList.Where(x => x.Id == formulaVersionId).ToList();
                }
                foreach (var item in formulaVersionList)
                {
                    var entity = new FormulaVersionDataDto
                    {
                        Version = new FormulaVersionDto
                        {
                            Id = item.Id,
                            IsMain = item.IsMain,
                            Remark = item.Remark,
                            Status = item.Status,
                            IsDeleted = item.IsDeleted,
                            TotalQuantity = item.TotalQuantity,
                            Version = item.Version,
                            CustomAttributeList = item.CustomAttribute != null ? JsonSerializer.Deserialize<List<CustomAttribute>>(item.CustomAttribute, JsonHelper.DefaultJsonSerializerOptions) : new List<CustomAttribute>(),
                        },
                        DetailList = detailData.Where(x => x.VersionId == item.Id).Select(x => new FormulaDetailDto
                        {
                            Id = x.Id,
                            VersionId = x.VersionId,
                            DetailType = x.DetailType,
                            DetailId = x.DetailId,
                            Order = x.Order,
                            Quantity = x.Quantity,
                            Ratio = x.Ratio,
                            CurRatio = x.Ratio,
                            TotalRatio = x.Ratio/100,
                            UpperTolerance = x.UpperTolerance,
                            LowerTolerance = x.LowerTolerance,
                            Replaceable = x.Replaceable,
                            BaseQuantity = item.TotalQuantity,
                            Editable = true,
                        }).OrderBy(x=>x.Order).ToList(),
                        Index = index++,
                    };
                    //寻找子配方
                    if (entity.DetailList != null && entity.DetailList.Count > 0)
                    {
                        getFormulaChildrenData(entity.DetailList, entity.Version.TotalQuantity);
                    }
                    versionList.Add(entity);
                }
            }
            var inspectionIds = formula.InspectionList?.Select(x => x.Id) ?? [];
            var standardIds = formula.InspectionList?.Select(x => x.QualityStandardId) ?? [];
            var ItemData = await _inspectionItemService.GetAll().Include(x => x.ValueList)
                            .Where(x => inspectionIds.Contains(x.InspectionId)).ToListAsync();
            var customerIds = formula.InspectionList?.Select(x => x.CustomerId ?? 0) ?? [];
            customerIds = ItemData.SelectMany(x => JsonSerializer.Deserialize<List<long>>(x.CustomerIds ?? "[]", JsonHelper.DefaultJsonSerializerOptions) ?? []).Concat(customerIds);
            var customerData = _customerService.GetAll(true).Where(x => customerIds.Contains(x.Id)).ToList();
            var standardData = await _qualityStandardService.GetAll(true).Where(x => standardIds.Contains(x.Id)).ToListAsync();
            var characteristicIds = ItemData.Select(x => x.CharacteristicId).Distinct().ToList();
            var characteristicData = await _qualityCharacteristicService.GetAll().Where(x => characteristicIds.Contains(x.Id)).ToListAsync();
            var inspectionList = new List<FormulaInspectionDataDto>();
            var index2 = 0;
            if (formula.InspectionList != null && formula.InspectionList.Count > 0)
            {
                foreach (var item in formula.InspectionList)
                {
                    var customerEntity = customerData.FirstOrDefault(x => x.Id == item.CustomerId);
                    var standardEntity = standardData.FirstOrDefault(x => x.Id == item.QualityStandardId);
                    var entity = new FormulaInspectionDataDto
                    {
                        Inspection = new FormulaInspectionDto
                        {
                            Id = item.Id,
                            CustomerId = item.CustomerId,
                            IsDefault = item.IsDefault,
                            Name = item.Name,
                            Remark = item.Remark,
                            QualityStandardId = item.QualityStandardId,
                            CustomerNo = customerEntity?.CustomerNo,
                            CustomerName = customerEntity?.CustomerName,
                            StandardCode = standardEntity?.Code,
                            StandardName = standardEntity?.Name
                        },
                        DetailList = ItemData.Where(x => x.InspectionId == item.Id).Select(x => new InspectionItemDto
                        {
                            Id = x.Id,
                            Equipment = x.Equipment,
                            EquipmentList = x.Equipment != null ? JsonSerializer.Deserialize<List<string>>(x.Equipment, JsonHelper.DefaultJsonSerializerOptions) : null,
                            CharacteristicId = x.CharacteristicId,
                            CharacteristicName = characteristicData.FirstOrDefault(e => e.Id == x.CharacteristicId)?.Name,
                            CustomerList = x.CustomerIds != null ? JsonSerializer.Deserialize<List<long>>(x.CustomerIds, JsonHelper.DefaultJsonSerializerOptions) : null,
                            CustomerText = string.Join(",", customerData.Where(e => (JsonSerializer.Deserialize<List<long>>(x.CustomerIds ?? "[]", JsonHelper.DefaultJsonSerializerOptions) ?? new List<long>()).Contains(e.Id))
                                            .Select(e => e.CustomerName)),
                            Frequency = x.Frequency,
                            FrequencyType = x.FrequencyType,
                            InspectionMethod = x.InspectionMethod,
                            InspectionName = x.InspectionName,
                            Remark = x.Remark,
                            SafetyTips = x.SafetyTips,
                            Order = x.Order,
                            IndexField = x.Id,
                            GroupId = x.GroupId ?? x.Id,
                            IsGroupMember = x.GroupId != null,
                            ValueList = x.ValueList?.Select(x => new InspectionItemValueDto
                            {
                                Id = x.Id,
                                Index = x.Id,
                                MinValue = x.MinValue,
                                MaxValue = x.MaxValue,
                                StandardValue = x.StandardValue,
                                UnitValue = x.UnitValue,
                                UpperTolerance = x.UpperTolerance,
                                LowerTolerance = x.LowerTolerance,
                                TextValue = x.TextValue,
                                ValueMode = x.ValueMode
                            }).ToList() ?? new List<InspectionItemValueDto>(),
                        }).OrderBy(x => x.Order).ToList(),
                        Index = index2++,
                    };
                    inspectionList.Add(entity);
                }
            }
            var result = new ModifyFormulaDto
            {
                FormulaId = formula.Id,
                Info = info,
                VersionList = versionList,
                InspectionList = inspectionList
            };
            return Ok(result);
        }

        /// <summary>
        /// 修改配方
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("ModifyFormula")]
        public async Task<IActionResult> ModifyFormula([FromBody] ModifyFormulaDto dto)
        {
            var formula = await _formulaService.GetAll(true).Include(x => x.VersionList).Include(x => x.InspectionList)
                             .FirstOrDefaultAsync(x => x.Id == dto.FormulaId);
            if (formula == null)
            {
                throw new Exception("配方不存在，请刷新重试");
            }
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var existFormula = _formulaService.GetAll().Where(x => x.Code == dto.Info.FormulaCode && x.Id != dto.FormulaId).ToList();
                if (existFormula.Count > 0)
                {
                    throw new Exception("配方编码已存在，请修改编码");
                }
                formula.Code = dto.Info.FormulaCode;
                formula.Name = dto.Info.FormulaName;
                formula.Remark = dto.Info.Remark;
                formula.ProductId = dto.Info.ProductId;
                formula.CustomAttribute = dto.Info.CustomAttributeList != null && dto.Info.CustomAttributeList.Count > 0 ? JsonSerializer.Serialize(dto.Info.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null;

                var versionIds = dto.VersionList.Select(x => x.Version.Id).ToList();
                var delVersionData = formula.VersionList?.Where(x => !versionIds.Contains(x.Id)).ToList();
                var existVersionData = formula.VersionList?.Where(x => versionIds.Contains(x.Id)).ToList() ?? new List<Technical_FormulaVersion>();
                var addVersionData = dto.VersionList.Where(x => x.Version.Id == null).ToList();
                //删除版本数据
                if (delVersionData != null && delVersionData.Count > 0)
                {
                    var delVersionIds = delVersionData.Select(x => x.Id).ToList();
                    var delDetailData = _formulaDetailService.GetAll().Where(x => delVersionIds.Contains(x.VersionId)).ToList();
                    _formulaDetailService.Delete(delDetailData);
                    _formulaVersionService.Delete(delVersionData, false);
                }
                //修改版本数据
                var mainVersion = dto.VersionList.FirstOrDefault(x => x.Version.IsMain);
                var versionDetailData = _formulaDetailService.GetAll().Where(x => versionIds.Contains(x.VersionId)).ToList();
                foreach (var item in existVersionData)
                {
                    var dtoVersion = dto.VersionList.First(x => x.Version.Id == item.Id);
                    item.Version = dtoVersion.Version.Version;
                    item.IsMain = dtoVersion.Version.IsMain;
                    item.Status = dtoVersion.Version.Status;
                    item.Remark = dtoVersion.Version.Remark;
                    item.TotalQuantity = dtoVersion.Version.TotalQuantity ?? 0;
                    item.CustomAttribute = dtoVersion.Version.CustomAttributeList != null && dtoVersion.Version.CustomAttributeList.Count > 0 ?
                                    JsonSerializer.Serialize(dtoVersion.Version.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null;

                    var detailIds = dtoVersion.DetailList.Select(x => x.Id).ToList();
                    var itemDetailData = versionDetailData.Where(x => x.VersionId == item.Id).ToList();
                    var delDetailData = itemDetailData.Where(x => !detailIds.Any(d => d == x.Id)).ToList();
                    if (delDetailData != null && delDetailData.Count > 0)
                    {
                        _formulaDetailService.Delete(delDetailData);
                    }
                    //修改明细数据
                    var existDetailData = itemDetailData.Where(x => detailIds.Contains(x.Id)).ToList() ?? new List<Technical_FormulaDetail>();
                    foreach (var detail in existDetailData)
                    {
                        var dtoDetail = dtoVersion.DetailList.First(x => x.Id == detail.Id);
                        detail.DetailType = dtoDetail.DetailType;
                        detail.DetailId = dtoDetail.DetailId;
                        detail.Order = dtoDetail.Order;
                        detail.Quantity = dtoDetail.Quantity;
                        detail.Ratio = dtoDetail.Ratio;
                        detail.LowerTolerance = dtoDetail.LowerTolerance;
                        detail.UpperTolerance = dtoDetail.UpperTolerance;
                        detail.Replaceable = dtoDetail.Replaceable;
                        if (item.IsMain)
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
                    }
                    //增加明细数据
                    var addDetailData = dtoVersion.DetailList.Where(x => x.Id == null).ToList();
                    foreach (var detail in addDetailData)
                    {
                        this.addVersionDetail(mainVersion, detail, formula, item);
                    }
                }
                //新增版本数据
                foreach (var item in addVersionData)
                {
                    this.addVersion(mainVersion, item, formula);
                }

                //删除、修改、新增检验方案数据
                var inspIds = dto.InspectionList.Select(x => x.Inspection.Id).ToList();
                var delInspData = formula.InspectionList?.Where(x => !inspIds.Contains(x.Id)).ToList();
                var existInspData = formula.InspectionList?.Where(x => inspIds.Contains(x.Id)).ToList() ?? new List<Technical_Inspection>();
                var addInspData = dto.InspectionList.Where(x => x.Inspection.Id == null).ToList();
                if (delInspData != null && delInspData.Count > 0)
                {
                    var delInspIds = delInspData.Select(x => x.Id).ToList();
                    var delInspItemData = _inspectionItemService.GetAll().Include(x => x.ValueList)
                                        .Where(x => delInspIds.Contains(x.InspectionId)).ToList();
                    var delValueData = delInspItemData.Where(x => x.ValueList != null).SelectMany(x => x.ValueList!).ToList();
                    _inspectionItemValueService.Delete(delValueData);
                    _inspectionItemService.Delete(delInspItemData);
                    _inspectionService.Delete(delInspData);
                }
                var inspectionItemData = _inspectionItemService.GetAll().Include(x => x.ValueList).Where(x => inspIds.Contains(x.InspectionId)).ToList();
                foreach (var item in existInspData)
                {
                    var dtoInsp = dto.InspectionList.First(x => x.Inspection.Id == item.Id);
                    item.Name = dtoInsp.Inspection.Name;
                    item.Remark = dtoInsp.Inspection.Remark;
                    item.QualityStandardId = dtoInsp.Inspection.QualityStandardId;
                    item.CustomerId = dtoInsp.Inspection.CustomerId;
                    item.IsDefault = dtoInsp.Inspection.IsDefault;

                    var inspItemIds = dtoInsp.DetailList.Select(x => x.Id).ToList();
                    var itemItemData = inspectionItemData.Where(x => x.InspectionId == item.Id).ToList();
                    var delInspItemData = itemItemData.Where(x => !inspItemIds.Contains(x.Id)).ToList();
                    if (delInspItemData != null && delInspItemData.Count > 0)
                    {
                        var delInspItemIds = delInspItemData.Select(x => x.Id).ToList();
                        var delValueData = _inspectionItemValueService.GetAll().Where(x => delInspItemIds.Contains(x.InspectionItemId)).ToList();
                        _inspectionItemValueService.Delete(delValueData);
                        _inspectionItemService.Delete(delInspItemData);
                    }
                    if (dtoInsp.DetailList != null && dtoInsp.DetailList.Count > 0)
                    {
                        var groupData = dtoInsp.DetailList.GroupBy(x => x.GroupId);
                        foreach (var group in groupData)
                        {
                            var dtoFirstInspItem = group.FirstOrDefault(x => !x.IsGroupMember) ?? group.First();
                            var firstInspItem = itemItemData.FirstOrDefault(x => x.Id == dtoFirstInspItem.Id);
                            var firstItemId = firstInspItem?.Id;
                            if (firstInspItem == null)
                            {
                                firstItemId = addInspectionItem(dtoFirstInspItem, item);
                            }
                            else
                            {  //修改
                                this.modifyInspectionItem(dtoFirstInspItem, firstInspItem);
                            }

                            foreach (var detail in group.Skip(1))
                            {
                                var inspItem = itemItemData.FirstOrDefault(x => x.Id == detail.Id);
                                if (inspItem != null)
                                { //修改
                                    this.modifyInspectionItem(detail, inspItem, firstItemId);
                                }
                                else
                                {
                                    addInspectionItem(detail, item, firstItemId);
                                }
                            }
                        }
                    }
                }
                foreach (var item in addInspData)
                {
                    this.addInspection(item, formula);
                }
                await _unitOfWork.SaveChangeAsync();
                scope.Complete();
                return Ok(dto.FormulaId);
            }
        }

        private void modifyInspectionItem(InspectionItemDto dtoInspItem, Technical_InspectionItem inspItem, long? groupId = null)
        {
            inspItem.CharacteristicId = dtoInspItem.CharacteristicId;
            inspItem.Frequency = dtoInspItem.Frequency;
            inspItem.FrequencyType = dtoInspItem.FrequencyType;
            inspItem.InspectionMethod = dtoInspItem.InspectionMethod;
            inspItem.InspectionName = dtoInspItem.InspectionName;
            inspItem.Remark = dtoInspItem.Remark;
            inspItem.SafetyTips = dtoInspItem.SafetyTips;
            inspItem.Equipment = dtoInspItem.EquipmentList != null && dtoInspItem.EquipmentList.Count > 0 ? JsonSerializer.Serialize(dtoInspItem.EquipmentList, JsonHelper.DefaultJsonSerializerOptions) : null;
            inspItem.GroupId = groupId;
            inspItem.CustomerIds = dtoInspItem.CustomerList != null && dtoInspItem.CustomerList.Count > 0 ? JsonSerializer.Serialize(dtoInspItem.CustomerList, JsonHelper.DefaultJsonSerializerOptions) : null;
            inspItem.Order = dtoInspItem.Order;

            var valueIds = dtoInspItem.ValueList.Select(x => x.Id).ToList();
            var delValueData = inspItem.ValueList?.Where(x => !valueIds.Contains(x.Id)).ToList() ?? new List<Technical_InspectionItem_Value>();
            _inspectionItemValueService.Delete(delValueData);
            var modifyValueData = inspItem.ValueList?.Where(x => valueIds.Contains(x.Id)).ToList() ?? new List<Technical_InspectionItem_Value>();
            foreach (var item in modifyValueData)
            {
                var dtoValue = dtoInspItem.ValueList.First(x => x.Id == item.Id);
                item.TextValue = dtoValue.TextValue;
                item.MinValue = dtoValue.MinValue;
                item.MaxValue = dtoValue.MaxValue;
                item.StandardValue = dtoValue.StandardValue;
                item.UnitValue = dtoValue.UnitValue;
                item.LowerTolerance = dtoValue.LowerTolerance;
                item.UpperTolerance = dtoValue.UpperTolerance;
                item.ValueMode = dtoValue.ValueMode;
            }
            var newValueData = dtoInspItem.ValueList.Where(x => x.Id == null).ToList();
            foreach (var item in newValueData)
            {
                this.addInspectionItemValue(item, inspItem);
            }
        }

        /// <summary>
        /// 删除配方
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpPost("DeleteFormula")]
        public async Task<IActionResult> DeleteFormula([FromBody] IdLongObject Id)
        {
            var formula = await _formulaService.GetAll(true).Include(x => x.VersionList).Include(x => x.InspectionList)
                          .FirstOrDefaultAsync(x => x.Id == Id.Id);
            if (formula == null)
            {
                throw new Exception("配方不存在，请刷新重试");
            }
            //删除版本
            var detailList = _formulaDetailService.GetAll().Where(x => x.FormulaId == Id.Id).ToList();
            _formulaDetailService.Delete(detailList);
            if (formula.VersionList != null && formula.VersionList.Count > 0)
            {
                _formulaVersionService.Delete(formula.VersionList, false);
            }
            //删除检验方案
            var inspectionIds = formula.InspectionList?.Select(x => x.Id).ToList();
            if (inspectionIds != null && inspectionIds.Count > 0)
            {
                var itemList = _inspectionItemService.GetAll().Include(x => x.ValueList).Where(x => inspectionIds.Contains(x.InspectionId)).ToList();
                var valueList = itemList.Where(x => x.ValueList != null).SelectMany(x => x.ValueList!).ToList();
                _inspectionItemValueService.Delete(valueList);
                _inspectionItemService.Delete(itemList);
                _inspectionService.Delete(formula.InspectionList!);
            }
            //删除文件
            var files = _fileStorageService.GetAll().Where(x => x.SourceType == FileStorageSourceType.配方文件 && x.SourceId == Id.Id).ToList();
            _fileStorageService.DeleteWihtFiles(files);

            //删除 配方
            _formulaService.Delete(formula, false);

            await _unitOfWork.SaveChangeAsync();
            return Ok("删除成功");
        }

        /// <summary>
        /// 删除配方的版本
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpPost("DeleteFormulaVersion")]
        public async Task<IActionResult> DeleteFormulaVersion([FromBody] IdLongObject Id)
        {
            var formulaVersion = await _formulaVersionService.GetAll(true).Include(x => x.DetailList)
                          .FirstOrDefaultAsync(x => x.Id == Id.Id);
            if (formulaVersion == null)
            {
                throw new Exception("配方版本不存在，请刷新重试");
            }
            //删除版本
            var detailList = formulaVersion.DetailList;
            if (detailList != null && detailList.Count > 0)
            {
                _formulaDetailService.Delete(detailList);
            }
            _formulaVersionService.Delete(formulaVersion, false);

            await _unitOfWork.SaveChangeAsync();
            return Ok("删除成功");
        }

        /// <summary>
        /// 淘汰配方
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpPost("ObsoleteFormula")]
        public async Task<IActionResult> ObsoleteFormula([FromBody] IdLongObject Id)
        {
            var formula = await _formulaService.GetAll(true).Include(x => x.VersionList)
                              .FirstOrDefaultAsync(x => x.Id == Id.Id);
            if (formula == null)
            {
                throw new Exception("配方不存在，请刷新重试");
            }
            formula.Status = FormulaStatus.淘汰;
            if (formula.VersionList != null)
            {
                foreach (var item in formula.VersionList)
                {
                    item.Status = FormulaStatus.淘汰;
                }
            }
            await _unitOfWork.SaveChangeAsync();
            return Ok("操作成功");
        }


        /// <summary>
        /// 恢复淘汰的配方
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpPost("RecoverFormula")]
        public async Task<IActionResult> RecoverFormula([FromBody] IdLongObject Id)
        {
            var formula = await _formulaService.GetAll(true).Include(x => x.VersionList)
                              .FirstOrDefaultAsync(x => x.Id == Id.Id);
            if (formula == null)
            {
                throw new Exception("配方不存在，请刷新重试");
            }
            formula.Status = FormulaStatus.已生效;
            if (formula.VersionList != null)
            {
                foreach (var item in formula.VersionList)
                {
                    item.Status = FormulaStatus.已生效;
                }
            }
            await _unitOfWork.SaveChangeAsync();
            return Ok("操作成功");
        }

        /// <summary>
        /// 禁用/启用 配方版本
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("DisabledFormulaVersion")]
        public async Task<IActionResult> DisabledFormulaVersion([FromBody] IdLongObject Id)
        {
            var formulaVersion = await _formulaVersionService.GetAll(true)
                          .FirstOrDefaultAsync(x => x.Id == Id.Id);
            if (formulaVersion == null)
            {
                throw new Exception("配方版本不存在，请刷新重试");
            }
            formulaVersion.IsDeleted = !formulaVersion.IsDeleted;
            var formula = await _formulaService.GetAll(true).Include(x => x.VersionList)
                        .FirstOrDefaultAsync(x => x.Id == formulaVersion.FormulaId);
            if (formula == null)
            {
                throw new Exception("配方不存在，请刷新重试");
            }
            var versionList = formula.VersionList;
            if (versionList != null)
            {
                if (!formulaVersion.IsDeleted && !versionList.Any(x => x.Id != formulaVersion.Id && x.IsDeleted != formulaVersion.IsDeleted))
                {
                    formula.IsDeleted = formulaVersion.IsDeleted;
                }
                if (formulaVersion.IsDeleted)
                {
                    formula.IsDeleted = formulaVersion.IsDeleted;
                }
            }

            await _unitOfWork.SaveChangeAsync();
            return Ok("删除成功");
        }

        /// <summary>
        /// 获取所有配方选项
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        [HttpPost("GetFormulaOptions")]
        public async Task<IActionResult> GetFormulaOptions([FromBody] List<Filter> filters)
        {
            var options = await _formulaVersionService.GetAll().ApplyFilters(filters).OrderByDescending(s => s.IsMain).Select(s => new SelectionDto
            {
                Value = s.Id.ToString(),
                Text = $"{s.Version}-{s.Formula.Code}【{s.Formula.Name}】{(s.IsMain ? "(主版本)" : "")}",
                Code = s.FormulaId.ToString(),
                Extension = s.IsMain.ToString()
            }).ToListAsync();
            return Ok(options);
        }
    }
}
