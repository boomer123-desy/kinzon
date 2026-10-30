using Aspose.Cells;
using Kingzon.OLCM.Base.Services;
using Kingzon.OLCM.Base.Services.JsonStructure;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Amoeba.DataBase.Entities;
using KingZon.OLCM.Base.DataBase.Entities;
using KingZon.OLCM.Base.DataBase.Enum;
using KingZon.OLCM.Base.Service;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.WebApi.Dto;
using KingZon.OLCM.Common.Service.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Transactions;
using X.PagedList;
using FilterType = KingZon.Core.DataBase.FilterType;
using Task = System.Threading.Tasks.Task;

namespace KingZon.OLCM.Base.WebApi.Controllers
{
    [Route("api/base/[controller]")]
    [ApiController]
    public class ProductController : KingZonControllerBase
    {
        private readonly CommonService<Base_Category> _productCategoryService;
        private readonly SystemParaService _systemParaService;
        private readonly ProductService _productService;
        private readonly CommonService<Base_UnitConversion> _unitConversionService;
        private readonly CommonService<Base_ProductPackage> _productPackageService;
        private readonly CommonService<Base_ProductRL> _productRLService;
        private readonly CommonService<Base_ProductSpecification> _productSpecificationService;
        private readonly CommonService<Base_Goods> _goodsService;
        private readonly CommonService<Amoeba_ProductProcessPrice> _productProcessPriceService;
        private readonly FileStorageService _fileStorageService;
        private readonly IUnitOfWork _unitOfWork;

        public ProductController(
            CommonService<Base_Category> productCategoryService,
            SystemParaService systemParaService,
            ProductService productService,
            CommonService<Base_UnitConversion> unitConversionService,
            CommonService<Base_ProductPackage> productPackageService,
            CommonService<Base_ProductRL> productRLService,
            CommonService<Base_ProductSpecification> productSpecificationService,
            CommonService<Base_Goods> goodsService,
            CommonService<Amoeba_ProductProcessPrice> productProcessPriceService,
            FileStorageService fileStorageService,
            IUnitOfWork unitOfWork)
        {
            _productCategoryService = productCategoryService;
            _systemParaService = systemParaService;
            _productService = productService;
            _unitConversionService = unitConversionService;
            _productPackageService = productPackageService;
            _productRLService = productRLService;
            _productSpecificationService = productSpecificationService;
            _goodsService = goodsService;
            _productProcessPriceService = productProcessPriceService;
            _fileStorageService = fileStorageService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 获取所有产品类别（树形结构 只获取叶子节点）
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetAllCategory", Name = "GetAllCategory")]
        public async Task<IActionResult> GetAllCategory()
        {
            var data = await _productCategoryService.GetAll().Where(x => x.CategoryType == CategoryType.产品类别).ToListAsync();
            //只获取叶子节点
            var result = new List<Base_Category>();
            foreach (var item in data)
            {
                var children = data.Where(x => x.ParentId == item.Id);
                if (children.Count() <= 0)
                {
                    result.Add(item);
                }
            }
            return Ok(result);
        }

        [HttpGet("GetAllPackageWay", Name = "GetAllPackageWay")]
        public async Task<IActionResult> GetAllPackageWay()
        {
            var data = new List<SelectionDto>();
            await Task.Run(() =>
             {
                 var types = new List<SystemParaType> { SystemParaType.包装类型 };
                 data = _systemParaService.GetSystermParaByTypesAndCode(types, null)
                 .Select(x => new SelectionDto()
                 {
                     Value = x.SystemPara.Id.ToString(),
                     Text = x.SystemPara.DataValue,
                 }).ToList();

             });
            return Ok(data);
        }

        /// <summary>
        /// 获取产品选项信息
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        [HttpPost("GetProductSelectionList")]
        public async Task<IActionResult> GetProductSelectionList([FromBody] List<Filter> filters)
        {
            var list = await (from product in _productService.GetAll().ApplyFilters(filters)
                              select new GetProductSelectionDto
                              {
                                  Value = product.Id,
                                  Text = $"{product.ProductNo}-{product.ProductName}【{product.TechnicalCode}】",
                                  ProductNo = product.ProductNo,
                                  ProductName = product.ProductName,
                                  TechnicalCode = product.TechnicalCode,
                                  ProductCategoryId = product.ProductCategoryId,
                                  ProductCategoryName = product.ProductCategory.Name,
                                  UnitId = product.BaseUnitId,
                                  UnitName = product.BaseUnit.Name,
                              }).ToListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取产品规格选项信息
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        [HttpPost("GetProductSpecificationSelectionList")]
        public async Task<IActionResult> GetProductSpecificationSelectionList([FromBody] List<Filter> filters)
        {
            var list = await (from specification in _productSpecificationService.GetAll().ApplyFilters(filters)
                              join product in _productService.GetAll(true)
                                   on specification.ProductId equals product.Id
                              select new GetProductSelectionDto
                              {
                                  Value = product.Id,
                                  Text = specification.Specification,
                                  ProductNo = !string.IsNullOrEmpty(specification.Code) ? specification.Code : product.ProductNo,
                                  ProductName = !string.IsNullOrEmpty(specification.Name) ? specification.Name : product.ProductName,
                                  TechnicalCode = product.TechnicalCode,
                                  ProductCategoryId = product.ProductCategoryId,
                                  ProductCategoryName = product.ProductCategory.Name,
                                  UnitId = specification.SpecificationUnitId,
                                  UnitName = specification.SpecificationUnit != null ? specification.SpecificationUnit.Name : string.Empty,
                              }).ToListAsync();
            return Ok(list);
        }

        #region 产品管理（成品）
        public class ProductFilterDto
        {
            public List<Filter> filterList { get; set; } = new List<Filter>();
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
        }
        /// <summary>
        /// 首页 获取成品列表
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        [HttpPost("GetProductList")]
        public async Task<IActionResult> GetProductList([FromBody] ProductFilterDto dto)
        {
            var pageModel = new PageModel() { PageIndex = dto.PageIndex, PageSize = dto.PageSize };
            var productData = await _productService.GetProductList([], []).Where(x => x.ProductType == ProductType.成品)
                .Select(x => new ProductIndexTreeDto
                {
                    ProductId = x.ProductId,
                    ProductNo = x.ProductNo,
                    ProductName = x.ProductName,
                    ProductTechnicalCode = x.ProductTechnicalCode,
                    ProductCategory = x.ProductCategory,
                    ProductUnit = x.ProductUnit,
                    IsDeleted = x.IsDeleted,
                    SpecificationId = x.SpecificationId,
                    ProductSpecification = x.ProductSpecification,
                    ProductPackageWay = x.ProductPackageWay,
                    PackageWayId = x.PackageWayId,
                    Remark = x.Remark,
                    ProductCategoryId = x.ProductCategoryId,
                    ProductUnitId = x.ProductUnitId,
                    ReviewStatus = x.ReviewStatus,
                    ProductType = x.ProductType,
                    InventoryQuantity = x.InventoryQuantity,
                    UnPackageQuantity = x.UnPackageQuantity,
                    ProductMainUnitId = x.ProductMainUnitId,
                    ProductMainUnit = x.ProductMainUnit,
                    CategoryAttribute = x.CategoryAttribute,
                    ProductProperty = x.ProductProperty,
                    MatureTime = x.MatureTime,
                    CustomAttribute = x.CustomAttribute
                }).ApplyFilters(dto.filterList).ToPagedListAsync(pageModel);
            var data = productData.Data.ToList();
            var productIds = data.Select(x => x.ProductId).ToList();
            var specIds = data.Select(x => x.SpecificationId).ToList();
            var unitData = _unitConversionService.GetAll().Include(x => x.FromUnit)
                        .Where(x => (x.ConversionType == UnitConversionType.Product && productIds.Contains(x.SourceId))
                        || ((x.ConversionType == UnitConversionType.ProductSpec || x.ConversionType == UnitConversionType.SpecToProduct)
                                && specIds.Contains(x.SourceId))).ToList();
            var packageData = _productPackageService.GetAll().Where(x => productIds.Contains(x.ProductId)).ToList();
            var RLData = _productRLService.GetAll().Include(x => x.ChildProduct).Include(x => x.ChildSpecification)
                            .Where(x => x.RelationType == DataBase.Enum.ProductRelationType.半成品 && productIds.Contains(x.ParentProductId)).ToList();
            var categoryParaList = _systemParaService.GetSystermParaByTypesAndCode([SystemParaType.类别附加参数], null).ToList();
            var result = new List<ProductIndexTreeDto>();
            foreach (var item in data)
            {
                item.IdField = $"{item.ProductId}_{item.SpecificationId}";
                var option = JsonHelper.DefaultJsonSerializerOptions;
                item.CategoryAttributeList = !String.IsNullOrEmpty(item.CategoryAttribute) ? JsonSerializer.Deserialize<List<CustomAttributeShowDto>>(item.CategoryAttribute, option) : null;
                if (item.CategoryAttributeList != null)
                {
                    foreach (var categoryAttribute in item.CategoryAttributeList)
                    {
                        var para = categoryParaList?.FirstOrDefault(x => x.SystemPara.Id == categoryAttribute.systemParaId
                                                    && x.SystemPara.Code == item.ProductCategoryId.ToString())?.SystemPara;
                        categoryAttribute.Name = para?.DataValue;
                        categoryAttribute.value = para?.DisplayType == CustomizeDisplayType.DateTime && _productService.HasValue(categoryAttribute.value) ? ((JsonElement)categoryAttribute.value!).GetDateTime().ToString("yyyy-MM-dd HH:mm:ss") :
                              para?.DisplayType == CustomizeDisplayType.Switch && _productService.HasValue(categoryAttribute.value) ? (((JsonElement)categoryAttribute.value!).GetBoolean() ? "是" : "否") :
                                 categoryAttribute.value;
                    }
                }
                item.CustomAttributeList = !String.IsNullOrEmpty(item.CustomAttribute) ? JsonSerializer.Deserialize<List<CustomAttributeShowDto>>(item.CustomAttribute, option) : null;
                var unit = unitData.Where(x => (item.SpecificationId == null && x.TargetId == item.ProductId && x.ConversionType == UnitConversionType.Product) ||
                                            (item.SpecificationId == x.TargetId && x.ConversionType == UnitConversionType.ProductSpec));
                if (unit.Count() > 0)
                {
                    item.UnitInfo = String.Join("\n", unit.Select(x =>
                    {
                        return $"1{x.FromUnit.Name}={x.Ratio.ToString("G29")}{item.ProductUnit}";
                    }));
                }
                var mainConversion = unitData.FirstOrDefault(x => (item.SpecificationId == x.SourceId && item.ProductId == x.TargetId &&
                                        x.ConversionType == UnitConversionType.SpecToProduct));
                item.MainUnitRatio = mainConversion?.Ratio;

                var package = packageData.Where(x => x.ProductId == item.ProductId && (x.SpecificationId == null || item.SpecificationId == x.SpecificationId));
                if (package.Count() > 0)
                {
                    item.PackageInfo = String.Join("\n", package.Select(x =>
                    {
                        return $"{x.PackageSpecification}";
                    }));
                }
                var rl = RLData.Where(x => x.ParentProductId == item.ProductId && (x.ParentSpecificationId == null || item.SpecificationId == x.ParentSpecificationId));
                if (rl.Count() > 0)
                {
                    item.SemiInfo = String.Join("\n", rl.Select(x =>
                    {
                        var childProduct = x.ChildProduct;
                        var childSpec = x.ChildSpecification;
                        var childProductNo = childSpec != null ? childSpec.Code : childProduct.ProductNo;
                        var childProductName = childSpec != null ? childSpec.Name : childProduct.ProductName;
                        return $"{childProductNo} {childProductName}";
                    }));
                }

                //树形结构
                var parent = data.FirstOrDefault(x => x.ProductId == item.ProductId && x.SpecificationId == null); //不带规格型号的成品Product
                if (parent == null)
                {
                    item.IsRoot = false;
                    result.Add(item);
                }
                else
                {
                    parent.IsRoot = true;
                    if (parent != item)
                    {
                        item.IsRoot = false;
                        parent.Children.Add(item);
                    }
                    if (!result.Exists(x => x.IdField == parent.IdField))
                    {
                        result.Add(parent);
                    }
                }
            }
            productData.Data = result;
            return Ok(productData);
        }
        /// <summary>
        /// 获取技术型号对应的成品（不含规格）
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        [HttpGet("GetProductByTechnicalCode")]
        public async Task<IActionResult> GetProductByTechnicalCode([FromQuery] string TechnicalCode, bool isDeleted = false, bool allProduct = false)
        {
            var filter = new List<Filter> { new Filter("TechnicalCode", JsonSerializer.SerializeToElement(TechnicalCode), FilterType.Equals, DataType.Text) };
            if (!allProduct)
            {
                filter.Add(new Filter("ProductType", JsonSerializer.SerializeToElement(ProductType.成品), FilterType.Equals, DataType.Integer));
            }
            else
            {
                filter.Add(new Filter("ProductType", JsonSerializer.SerializeToElement(new List<ProductType> { ProductType.成品, ProductType.半成品, ProductType.物料 }), FilterType.IntArrayContains, DataType.MultiSelectNum));
            }

            if (!isDeleted)
            {
                filter.Add(new Filter("IsDeleted", JsonSerializer.SerializeToElement(false), FilterType.Equals, DataType.SelectBoolean));
            }
            var noSpecFilter = new List<Filter> { new Filter("Id", JsonSerializer.SerializeToElement(0), FilterType.LessThan, DataType.Integer) };
            var data = await _productService.GetProductList(filter, noSpecFilter).ToListAsync();
            //产品相关文件
            var productIds = data.Select(x => x.ProductId).ToList();
            var files = _fileStorageService.GetAll().Where(x => x.SourceType == FileStorageSourceType.成品文件 && productIds.Contains(x.SourceId)).ToList();
            foreach (var item in data)
            {
                item.FileList = files.Where(x => x.SourceId == item.ProductId).ToList();
            }
            return Ok(data);
        }
        /// <summary>
        /// 获取半成品下拉选项
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        [HttpGet("GetSemiOptionList")]
        public async Task<IActionResult> GetSemiOptionList()
        {
            var data = await _productService.GetSemiProductList([]).Where(x => !x.IsDeleted).ToListAsync();
            return Ok(data);
        }
        /// <summary>
        /// 获取半成品列表
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        [HttpPost("GetSemiProductList")]
        public async Task<IActionResult> GetSemiProductList([FromBody] List<Filter> filterList)
        {
            var productData = await _productService.GetSemiProductList([])
                .Select(x => new ProductIndexTreeDto
                {
                    ProductId = x.ProductId,
                    ProductNo = x.ProductNo,
                    ProductName = x.ProductName,
                    ProductTechnicalCode = x.ProductTechnicalCode,
                    ProductCategory = x.ProductCategory,
                    ProductUnit = x.ProductUnit,
                    IsDeleted = x.IsDeleted,
                    SpecificationId = x.SpecificationId,
                    ProductSpecification = x.ProductSpecification,
                    ProductPackageWay = x.ProductPackageWay,
                    PackageWayId = x.PackageWayId,
                    Remark = x.Remark,
                    ProductCategoryId = x.ProductCategoryId,
                    ProductUnitId = x.ProductUnitId,
                    ReviewStatus = x.ReviewStatus,
                    ProductType = x.ProductType,
                    InventoryQuantity = x.InventoryQuantity,
                    UnPackageQuantity = x.UnPackageQuantity,
                    ProductMainUnitId = x.ProductMainUnitId,
                    ProductMainUnit = x.ProductMainUnit,
                    CategoryAttribute = x.CategoryAttribute,
                    ProductProperty = x.ProductProperty,
                    MatureTime = x.MatureTime,
                    CustomAttribute = x.CustomAttribute
                }).ApplyFilters(filterList).ToPagedListAsync(this.PageModel);
            var data = productData.Data.ToList();
            var productIds = data.Select(x => x.ProductId).ToList();
            var specIds = data.Select(x => x.SpecificationId).ToList();
            var unitData = _unitConversionService.GetAll().Include(x => x.FromUnit)
                        .Where(x => (x.ConversionType == UnitConversionType.Product && productIds.Contains(x.SourceId))
                        || (x.ConversionType == UnitConversionType.ProductSpec && specIds.Contains(x.SourceId))
                            ).ToList();
            var packageData = _productPackageService.GetAll().Where(x => productIds.Contains(x.ProductId)).ToList();
            var categoryParaList = _systemParaService.GetSystermParaByTypesAndCode([SystemParaType.类别附加参数], null).ToList();
            foreach (var item in data)
            {
                item.IdField = $"{item.ProductId}_{item.SpecificationId}";
                var option = JsonHelper.DefaultJsonSerializerOptions;
                item.CategoryAttributeList = !String.IsNullOrEmpty(item.CategoryAttribute) ? JsonSerializer.Deserialize<List<CustomAttributeShowDto>>(item.CategoryAttribute, option) : null;
                if (item.CategoryAttributeList != null)
                {
                    foreach (var categoryAttribute in item.CategoryAttributeList)
                    {
                        var para = categoryParaList?.FirstOrDefault(x => x.SystemPara.Id == categoryAttribute.systemParaId
                                                     && x.SystemPara.Code == item.ProductCategoryId.ToString())?.SystemPara;
                        categoryAttribute.Name = para?.DataValue;
                        categoryAttribute.value = para?.DisplayType == CustomizeDisplayType.DateTime && _productService.HasValue(categoryAttribute.value) ? ((JsonElement)categoryAttribute.value!).GetDateTime().ToString("yyyy-MM-dd HH:mm:ss") :
                                para?.DisplayType == CustomizeDisplayType.Switch && _productService.HasValue(categoryAttribute.value) ? (((JsonElement)categoryAttribute.value!).GetBoolean() ? "是" : "否") :
                                   categoryAttribute.value;
                    }
                }
                item.CustomAttributeList = !String.IsNullOrEmpty(item.CustomAttribute) ? JsonSerializer.Deserialize<List<CustomAttributeShowDto>>(item.CustomAttribute, option) : null;
                var unit = unitData.Where(x => (item.SpecificationId == null && x.TargetId == item.ProductId && x.ConversionType == UnitConversionType.Product) ||
                                            (item.SpecificationId == x.TargetId && x.ConversionType == UnitConversionType.ProductSpec));
                if (unit.Count() > 0)
                {
                    item.UnitInfo = String.Join("\n", unit.Select(x =>
                    {
                        return $"1{x.FromUnit.Name}={x.Ratio.ToString("G29")}{item.ProductUnit}";
                    }));
                }
                var package = packageData.Where(x => x.ProductId == item.ProductId && (x.SpecificationId == null || item.SpecificationId == x.SpecificationId));
                if (package.Count() > 0)
                {
                    item.PackageInfo = String.Join("\n", package.Select(x =>
                    {
                        return $"{x.PackageSpecification}";
                    }));
                }
            }
            productData.Data = data;
            return Ok(productData);
        }

        public class PostAddProductDto
        {
            public long? ProductId { get; set; }
            public AddProductDto? Product { get; set; }
            public AddProductDto? ProductSpec { get; set; }
            public long? SpecId { get; set; }

        }
        /// <summary>
        /// 新增成品
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("AddProduct")]
        public async Task<IActionResult> AddProduct([FromBody] PostAddProductDto dto)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var product = _productService.GetAll().FirstOrDefault(x => x.Id == dto.ProductId);
                var productId = 0L;
                var productSpecId = 0L;
                if (dto.ProductId != null && product == null)
                {
                    throw new Exception("成品不存在去，请刷新重试");
                }
                if (dto.Product != null)
                {
                    productId = this.AddProductData(dto.Product, ref product);
                }
                if (dto.ProductSpec != null)
                {
                    var category = await _productCategoryService.GetAll(true).FirstOrDefaultAsync(x => x.Id == dto.ProductSpec.CategoryId);
                    if (category == null)
                    {
                        throw new Exception("产品类别不存在，请刷新重试");
                    }
                    if (!CheckCode(dto.ProductSpec.ProductNo, null))
                    {
                        throw new Exception($"{dto.ProductSpec.ProductNo}规格编码已存在，请重新输入");
                    }
                    var newSpec = new Base_ProductSpecification
                    {
                        Product = product!,
                        Specification = dto.ProductSpec.Specification!,
                        Code = dto.ProductSpec.ProductNo,
                        NoSuffix = dto.ProductSpec.NoSuffix,
                        Name = dto.ProductSpec.ProductName,
                        SpecificationUnitId = dto.ProductSpec.ProductUnitId,
                        ProductCategoryId = dto.ProductSpec.CategoryId,
                        Remark = dto.ProductSpec.Remark,
                        CategoryAttribute = dto.ProductSpec.CategoryAttributeList.Count() > 0 ? JsonSerializer.Serialize(dto.ProductSpec.CategoryAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
                        CustomAttribute = dto.ProductSpec.CustomAttributeList.Count() > 0 ? JsonSerializer.Serialize(dto.ProductSpec.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
                        ContainerModelId = dto.ProductSpec.ContainerModelId,
                        DefSaleUnitConversionId = dto.ProductSpec.IsDefaultSaleUnit != null && dto.ProductSpec.IsDefaultSaleUnit == true ? -1 : null
                    };
                    await _productSpecificationService.InsertAsync(newSpec);
                    await _unitOfWork.SaveChangeAsync();
                    productSpecId = newSpec.Id;
                    //更新产品类别最新流水号
                    this.UpdateSequence(ref category, dto.ProductSpec.NoSuffix);

                    //多单位信息
                    foreach (var unit in dto.ProductSpec.UnitList)
                    {
                        var newUnit = new Base_UnitConversion
                        {
                            FromUnitId = unit.UnitId,
                            ToUnitId = (long)newSpec.SpecificationUnitId!,
                            SourceId = newSpec.Id,
                            TargetId = newSpec.Id,
                            Ratio = unit.Ratio,
                            ConversionType = UnitConversionType.ProductSpec
                        };
                        await _unitConversionService.InsertAsync(newUnit);
                        await _unitOfWork.SaveChangeAsync();
                        if (unit.IsDefaultSaleUnit != null && unit.IsDefaultSaleUnit == true)
                        {
                            newSpec.DefSaleUnitConversionId = newUnit.Id;
                        }
                    }
                    //和成品主单位转换
                    var mainUnitConversion = new Base_UnitConversion
                    {
                        FromUnitId = (long)newSpec.SpecificationUnitId!,
                        ToUnitId = product!.BaseUnitId,
                        SourceId = newSpec.Id,
                        TargetId = product!.Id,
                        Ratio = (decimal)dto.ProductSpec.MainUnitRatio!,
                        ConversionType = UnitConversionType.SpecToProduct
                    };
                    await _unitConversionService.InsertAsync(mainUnitConversion);
                }

                await _unitOfWork.SaveChangeAsync();
                scope.Complete();
                var result = new
                {
                    Msg = "新增成功",
                    ProductId = productId,
                    ProductSpecId = productSpecId,
                };
                return Ok(result);
            }
        }
        /// <summary>
        /// 新增Base_Product
        /// </summary>
        /// <param name="Product"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private long AddProductData(AddProductDto Product, ref Base_Product product)
        {
            var category = _productCategoryService.GetAll(true).FirstOrDefault(x => x.Id == Product.CategoryId);
            if (category == null)
            {
                throw new Exception("产品类别不存在，请刷新重试");
            }
            if (!CheckCode(Product.ProductNo, null))
            {
                throw new Exception($"{Product.ProductNo}产品编码已存在，请重新输入");
            }
            var newProduct = new Base_Product
            {
                ProductNo = Product.ProductNo,
                ProductName = Product.ProductName,
                NoSuffix = Product.NoSuffix,
                TechnicalCode = Product.TechnicalCode,
                ProductCategoryId = Product.CategoryId,
                BaseUnitId = Product.ProductUnitId,
                ProductType = ProductType.成品,
                ProductProperty = Product.ProductProperty,
                Remark = Product.Remark,
                ReviewStatus = ReviewStatus.评审通过,//默认评审通过！！！
                CategoryAttribute = Product.CategoryAttributeList.Count() > 0 ? JsonSerializer.Serialize(Product.CategoryAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
                CustomAttribute = Product.CustomAttributeList.Count() > 0 ? JsonSerializer.Serialize(Product.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
                DefSaleUnitConversionId = Product.IsDefaultSaleUnit != null && Product.IsDefaultSaleUnit == true ? -1 : null
            };
            if (newProduct.ProductProperty == ProductProperty.成熟产品)
            {
                newProduct.MatureTime = Product.MatureTime;
            }
            _productService.Insert(newProduct);
            _unitOfWork.SaveChange();
            product = newProduct;
            //更新产品类别最新流水号
            this.UpdateSequence(ref category, Product.NoSuffix);

            //多单位信息
            foreach (var unit in Product.UnitList)
            {
                var newUnit = new Base_UnitConversion
                {
                    FromUnitId = unit.UnitId,
                    ToUnitId = product.BaseUnitId,
                    SourceId = product.Id,
                    TargetId = product.Id,
                    Ratio = unit.Ratio,
                    ConversionType = UnitConversionType.Product
                };
                _unitConversionService.Insert(newUnit);
                _unitOfWork.SaveChange();
                if (unit.IsDefaultSaleUnit != null && unit.IsDefaultSaleUnit == true)
                {
                    newProduct.DefSaleUnitConversionId = newUnit.Id;
                }
            }
            //半成品信息
            foreach (var semi in Product.SemiList)
            {
                var newRL = new Base_ProductRL
                {
                    ParentProductId = product.Id,
                    ChildProductId = semi,
                    ChildSpecificationId = null,
                    RelationType = ProductRelationType.半成品,
                };
                _productRLService.Insert(newRL);
            }
            return newProduct.Id;
        }

        [HttpPost("AddSemiProduct")]
        public async Task<IActionResult> AddSemiProduct([FromBody] AddProductDto semiProduct)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var category = await _productCategoryService.GetAll(true).FirstOrDefaultAsync(x => x.Id == semiProduct.CategoryId);
                if (category == null)
                {
                    throw new Exception("产品类别不存在，请刷新重试");
                }
                if (!CheckCode(semiProduct.ProductNo, null))
                {
                    throw new Exception($"{semiProduct.ProductNo}产品编码已存在，请重新输入");
                }
                var newProduct = new Base_Product
                {
                    ProductNo = semiProduct.ProductNo,
                    ProductName = semiProduct.ProductName,
                    NoSuffix = semiProduct.NoSuffix,
                    TechnicalCode = semiProduct.TechnicalCode,
                    ProductCategoryId = semiProduct.CategoryId,
                    BaseUnitId = semiProduct.ProductUnitId,
                    ProductType = ProductType.半成品,
                    ProductProperty = semiProduct.ProductProperty,
                    Remark = semiProduct.Remark,
                    ReviewStatus = ReviewStatus.评审通过,//默认评审通过！！！
                    CategoryAttribute = semiProduct.CategoryAttributeList.Count() > 0 ? JsonSerializer.Serialize(semiProduct.CategoryAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
                    CustomAttribute = semiProduct.CustomAttributeList.Count() > 0 ? JsonSerializer.Serialize(semiProduct.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
                    DefSaleUnitConversionId = semiProduct.IsDefaultSaleUnit != null && semiProduct.IsDefaultSaleUnit == true ? -1 : null
                };
                if (newProduct.ProductProperty == ProductProperty.成熟产品)
                {
                    newProduct.MatureTime = semiProduct.MatureTime;
                }
                await _productService.InsertAsync(newProduct);
                await _unitOfWork.SaveChangeAsync();
                //更新产品类别最新流水号
                this.UpdateSequence(ref category, semiProduct.NoSuffix);

                //多单位信息
                foreach (var unit in semiProduct.UnitList)
                {
                    var newUnit = new Base_UnitConversion
                    {
                        FromUnitId = unit.UnitId,
                        ToUnitId = newProduct.BaseUnitId,
                        SourceId = newProduct.Id,
                        TargetId = newProduct.Id,
                        Ratio = unit.Ratio,
                        ConversionType = UnitConversionType.Product
                    };
                    await _unitConversionService.InsertAsync(newUnit);
                    await _unitOfWork.SaveChangeAsync();
                    if (unit.IsDefaultSaleUnit != null && unit.IsDefaultSaleUnit == true)
                    {
                        newProduct.DefSaleUnitConversionId = newUnit.Id;
                    }
                }
                await _unitOfWork.SaveChangeAsync();

                scope.Complete();
                var result = new
                {
                    Msg = "新增成功",
                    ProductId = newProduct.Id,
                };
                return Ok(result);
            }
        }

        [HttpPost("ChangeProduct")]
        public async Task<IActionResult> ChangeProduct([FromBody] PostAddProductDto dto)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var productId = dto.ProductId;
                var spec = _productSpecificationService.GetAll(true).FirstOrDefault(x => x.Id == dto.SpecId);
                if (spec == null)
                {
                    throw new Exception("规格不存在，请刷新重试");
                }
                var oldProductId = spec.ProductId;
                if (dto.Product == null)
                {
                    var product = _productService.GetAll(true).FirstOrDefault(x => x.Id == dto.ProductId);
                    if (product == null)
                    {
                        throw new Exception("成品不存在去，请刷新重试");
                    }
                    spec.Product = product;
                }
                else
                {
                    var category = await _productCategoryService.GetAll(true).FirstOrDefaultAsync(x => x.Id == dto.Product.CategoryId);
                    if (category == null)
                    {
                        throw new Exception("产品类别不存在，请刷新重试");
                    }
                    if (!CheckCode(dto.Product.ProductNo, null))
                    {
                        throw new Exception($"{dto.Product.ProductNo}产品编码已存在，请重新输入");
                    }
                    var newProduct = new Base_Product
                    {
                        ProductNo = dto.Product.ProductNo,
                        ProductName = dto.Product.ProductName,
                        NoSuffix = dto.Product.NoSuffix,
                        TechnicalCode = dto.Product.TechnicalCode,
                        ProductCategoryId = dto.Product.CategoryId,
                        BaseUnitId = dto.Product.ProductUnitId,
                        ProductType = ProductType.成品,
                        ProductProperty = dto.Product.ProductProperty,
                        Remark = dto.Product.Remark,
                        ReviewStatus = ReviewStatus.评审通过,//默认评审通过！！！
                        CategoryAttribute = dto.Product.CategoryAttributeList.Count() > 0 ? JsonSerializer.Serialize(dto.Product.CategoryAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
                        CustomAttribute = dto.Product.CustomAttributeList.Count() > 0 ? JsonSerializer.Serialize(dto.Product.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null
                    };
                    if (newProduct.ProductProperty == ProductProperty.成熟产品)
                    {
                        newProduct.MatureTime = dto.Product.MatureTime;
                    }
                    await _productService.InsertAsync(newProduct);
                    //更新产品类别最新流水号
                    this.UpdateSequence(ref category, dto.Product.NoSuffix);

                    await _unitOfWork.SaveChangeAsync();
                    spec.Product = newProduct;
                    productId = newProduct.Id;
                }
                await _unitOfWork.SaveChangeAsync();
                _productService.UpdataSpecProductData(oldProductId, (long)productId!, spec.Id);
                scope.Complete();

                var result = new
                {
                    Msg = "修改成功",
                    ProductId = productId,
                };
                return Ok(result);
            }
        }
        /// <summary>
        /// 检查编码是否重复
        /// </summary>
        /// <returns></returns>
        private bool CheckCode(string code, long? productId)
        {
            var data = _productService.GetAll().Where(x => x.ProductNo == code &&
                        (x.ProductType == ProductType.成品 || x.ProductType == ProductType.半成品)).ToList();
            var specData = _productSpecificationService.GetAll().Where(x => x.Code == code).ToList();
            var exist = data.Where(x => x.Id != productId);
            if (exist.Count() > 0)
            {
                return false;
            }
            var existSpec = specData.Where(x => x.Id != productId);
            if (existSpec.Count() > 0)
            {
                return false;
            }
            return true;
        }

        [HttpGet("GetProductById")]
        public async Task<IActionResult> GetProductById([FromQuery] long productId)
        {
            var product = await _productService.GetAll(true).Include(x => x.BaseUnit).Include(x => x.ProductCategory).FirstOrDefaultAsync(x => x.Id == productId);
            if (product == null)
            {
                throw new Exception("产品不存在，请刷新重试");
            }
            var data = new AddProductDto()
            {
                Id = product.Id,
                ProductNo = product.ProductNo,
                ProductName = product.ProductName,
                CategoryId = product.ProductCategoryId,
                ProductCategory = product.ProductCategory.Name,
                ProductUnitId = product.BaseUnitId,
                ProductUnit = product.BaseUnit.Name,
                NoSuffix = product.NoSuffix,
                ProductProperty = product.ProductProperty,
                MatureTime = product.MatureTime,
                TechnicalCode = product.TechnicalCode,
                Remark = product.Remark,
                CategoryAttributeList = !String.IsNullOrEmpty(product.CategoryAttribute) ? JsonSerializer.Deserialize<List<CustomAttribute>>(product.CategoryAttribute, JsonHelper.DefaultJsonSerializerOptions) ?? new List<CustomAttribute>() : new List<CustomAttribute>(),
                CustomAttributeList = !String.IsNullOrEmpty(product.CustomAttribute) ? JsonSerializer.Deserialize<List<CustomAttribute>>(product.CustomAttribute, JsonHelper.DefaultJsonSerializerOptions) ?? new List<CustomAttribute>() : new List<CustomAttribute>(),
                UnitList = _unitConversionService.GetAll().Where(x => x.ConversionType == UnitConversionType.Product && x.SourceId == product.Id)
                            .Select(x => new AddProductUnitDto
                            {
                                Id = x.Id,
                                UnitId = x.FromUnitId,
                                Ratio = x.Ratio,
                                UnitName = x.FromUnit.Name,
                                IsDefaultSaleUnit = product.DefSaleUnitConversionId != null && product.DefSaleUnitConversionId == x.Id ? true : false
                            }).ToList(),
                SemiList = _productRLService.GetAll().Where(x => x.RelationType == ProductRelationType.半成品 && x.ParentProductId == product.Id)
                        .Select(x => x.ChildProductId).ToList(),
                FileList = _fileStorageService.GetAll().Where(x => x.SourceType == FileStorageSourceType.成品文件 && x.SourceId == product.Id).ToList(),
                IsDefaultSaleUnit = product.DefSaleUnitConversionId != null && product.DefSaleUnitConversionId == -1 ? true : false
            };
            return Ok(data);
        }
        [HttpGet("GetProductSpecById")]
        public async Task<IActionResult> GetProductSpecById([FromQuery] long productSpecId)
        {
            var spec = await _productSpecificationService.GetAll(true).Include(x => x.Product).Include(x => x.SpecificationUnit).FirstOrDefaultAsync(x => x.Id == productSpecId);
            if (spec == null)
            {
                throw new Exception("规格不存在，请刷新重试");
            }
            var mainConversion = _unitConversionService.GetAll().FirstOrDefault(x => x.ConversionType == UnitConversionType.SpecToProduct &&
                                x.SourceId == spec.Id && x.TargetId == spec.ProductId && x.FromUnitId == spec.SpecificationUnitId && x.ToUnitId == spec.Product.BaseUnitId);
            var data = new AddProductDto()
            {
                Id = spec.Id,
                ProductId = spec.ProductId,
                ProductNo = spec.Code!,
                ProductName = spec.Name!,
                CategoryId = (long)spec.ProductCategoryId!,
                ProductUnitId = (long)spec.SpecificationUnitId!,
                NoSuffix = spec.NoSuffix,
                Specification = spec.Specification,
                ContainerModelId = spec.ContainerModelId,
                TechnicalCode = spec.Product.TechnicalCode,
                Remark = spec.Remark,
                MainUnitRatio = mainConversion?.Ratio,
                MainConversionId = mainConversion?.Id,
                ProductUnit = spec.SpecificationUnit?.Name,
                CategoryAttributeList = !String.IsNullOrEmpty(spec.CategoryAttribute) ? JsonSerializer.Deserialize<List<CustomAttribute>>(spec.CategoryAttribute, JsonHelper.DefaultJsonSerializerOptions) ?? new List<CustomAttribute>() : new List<CustomAttribute>(),
                CustomAttributeList = !String.IsNullOrEmpty(spec.CustomAttribute) ? JsonSerializer.Deserialize<List<CustomAttribute>>(spec.CustomAttribute, JsonHelper.DefaultJsonSerializerOptions) ?? new List<CustomAttribute>() : new List<CustomAttribute>(),
                UnitList = _unitConversionService.GetAll().Where(x => x.ConversionType == UnitConversionType.ProductSpec && x.SourceId == spec.Id)
                            .Select(x => new AddProductUnitDto
                            {
                                Id = x.Id,
                                UnitId = x.FromUnitId,
                                Ratio = x.Ratio,
                                UnitName = x.FromUnit.Name,
                                IsDefaultSaleUnit = spec.DefSaleUnitConversionId != null && spec.DefSaleUnitConversionId == x.Id ? true : false
                            }).ToList(),
                FileList = _fileStorageService.GetAll().Where(x => x.SourceType == FileStorageSourceType.成品规格文件 && x.SourceId == spec.Id).ToList(),
                IsDefaultSaleUnit = spec.DefSaleUnitConversionId != null && spec.DefSaleUnitConversionId == -1 ? true : false
            };
            return Ok(data);
        }
        /// <summary>
        /// 编辑产品
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("EditProduct")]
        public async Task<IActionResult> EditProduct([FromBody] PostAddProductDto dto)
        {
            var product = await _productService.GetAll().FirstOrDefaultAsync(x => x.Id == dto.ProductId);
            if (product == null)
            {
                throw new Exception("产品不存在，请刷新重试");
            }
            if (dto.Product != null)
            {
                var category = await _productCategoryService.GetAll(true).FirstOrDefaultAsync(x => x.Id == dto.Product.CategoryId);
                if (category == null)
                {
                    throw new Exception("产品类别不存在，请刷新重试");
                }
                if (!CheckCode(dto.Product.ProductNo, dto.ProductId))
                {
                    throw new Exception($"{dto.Product.ProductNo}产品编码已存在，请重新输入");
                }
                product.ProductNo = dto.Product.ProductNo;
                product.ProductName = dto.Product.ProductName;
                product.NoSuffix = dto.Product.NoSuffix;
                product.TechnicalCode = dto.Product.TechnicalCode;
                product.ProductCategoryId = dto.Product.CategoryId;
                if (dto.Product.ProductUnitId != product.BaseUnitId)
                {
                    product.BaseUnitId = dto.Product.ProductUnitId;

                    var toProductUnit = _unitConversionService.GetAll().Where(x => (x.ConversionType == UnitConversionType.SpecToProduct || x.ConversionType == UnitConversionType.PackageToProduct)
                            && x.TargetId == product.Id)
                            .ToList();
                    //更新 规格主单位 到 成品主单位的转换
                    var specToProductUnit = toProductUnit.Where(x => x.ConversionType == UnitConversionType.SpecToProduct).ToList();
                    foreach (var item in specToProductUnit)
                    {
                        item.ToUnitId = product.BaseUnitId;
                    }
                    //更新 包装单位 到 成品主单位的转换
                    var packToProductUnit = toProductUnit.Where(x => x.ConversionType == UnitConversionType.PackageToProduct).ToList();
                    foreach (var item in packToProductUnit)
                    {
                        item.ToUnitId = product.BaseUnitId;
                    }
                    //更新 加工单价的单位
                    var priceList = _productProcessPriceService.GetAll().Where(x => x.ProductSpecificationId == product.Id && x.PriceUnitConversionId == -1).ToList();
                    foreach (var item in priceList)
                    {
                        item.PriceUnitId = dto.Product.ProductUnitId;
                    }
                }
                product.ProductProperty = dto.Product.ProductProperty;
                product.Remark = dto.Product.Remark;
                product.CategoryAttribute = dto.Product.CategoryAttributeList.Count() > 0 ? JsonSerializer.Serialize(dto.Product.CategoryAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null;
                product.CustomAttribute = dto.Product.CustomAttributeList.Count() > 0 ? JsonSerializer.Serialize(dto.Product.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null;
                product.DefSaleUnitConversionId = dto.Product.IsDefaultSaleUnit != null && dto.Product.IsDefaultSaleUnit == true ? -1 : null;
                if (product.ProductProperty == ProductProperty.新品)
                {
                    product.MatureTime = null;
                }
                if (product.ProductProperty == ProductProperty.成熟产品)
                {
                    product.MatureTime = dto.Product.MatureTime;
                }
                //更新产品类别最新流水号
                this.UpdateSequence(ref category, dto.Product.NoSuffix);

                var oldUnitList = _unitConversionService.GetAll().Where(x => x.ConversionType == UnitConversionType.Product && x.SourceId == product.Id)
                            .ToList();
                //更新多单位
                var delUnit = oldUnitList.Where(x => !dto.Product.UnitList.Any(e => e.Id == x.Id)).ToList();
                var addUnit = dto.Product.UnitList.Where(x => !oldUnitList.Any(e => e.Id == x.Id)).ToList();
                var modifyUnit = oldUnitList.Where(x => dto.Product.UnitList.Any(e => e.Id == x.Id)).ToList();
                _unitConversionService.Delete(delUnit);
                if (delUnit.Any(x => x.Id == product.DefSaleUnitConversionId))
                {
                    product.DefSaleUnitConversionId = null;
                }
                foreach (var addItem in addUnit)
                {
                    var newUnit = new Base_UnitConversion
                    {
                        FromUnitId = addItem.UnitId,
                        ToUnitId = product.BaseUnitId,
                        SourceId = product.Id,
                        TargetId = product.Id,
                        Ratio = addItem.Ratio,
                        ConversionType = UnitConversionType.Product
                    };
                    await _unitConversionService.InsertAsync(newUnit);
                    await _unitOfWork.SaveChangeAsync();
                    if (addItem.IsDefaultSaleUnit != null && addItem.IsDefaultSaleUnit == true)
                    {
                        product.DefSaleUnitConversionId = newUnit.Id;
                    }
                }
                foreach (var item in modifyUnit)
                {
                    var newItem = dto.Product.UnitList.First(x => x.Id == item.Id);
                    item.Ratio = newItem.Ratio;
                    item.FromUnitId = newItem.UnitId;
                    item.ToUnitId = product.BaseUnitId;
                    if (newItem.IsDefaultSaleUnit != null && newItem.IsDefaultSaleUnit == true)
                    {
                        product.DefSaleUnitConversionId = item.Id;
                    }
                }
                var oldSemiList = _productRLService.GetAll().Where(x => x.RelationType == ProductRelationType.半成品 && x.ParentProductId == product.Id)
                        .ToList();
                //更新半成品
                var delSemi = oldSemiList.Where(x => !dto.Product.SemiList.Any(e => e == x.ChildProductId)).ToList();
                var addSemi = dto.Product.SemiList.Where(x => !oldSemiList.Any(e => e.ChildProductId == x)).ToList();
                _productRLService.Delete(delSemi);
                foreach (var addItem in addSemi)
                {
                    var newRL = new Base_ProductRL
                    {
                        ParentProductId = product.Id,
                        ChildProductId = addItem,
                        RelationType = ProductRelationType.半成品,
                    };
                    await _productRLService.InsertAsync(newRL);
                }
            }
            await _unitOfWork.SaveChangeAsync();
            return Ok("编辑成功");
        }
        /// <summary>
        /// 编辑产品规格
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("EditProductSpec")]
        public async Task<IActionResult> EditProductSpec([FromBody] PostAddProductDto dto)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var product = await _productService.GetAll(true).FirstOrDefaultAsync(x => x.Id == dto.ProductId);
                if (dto.ProductId != null && product == null)
                {
                    throw new Exception("产品不存在，请刷新重试");
                }
                var productId = 0L;
                if (dto.Product != null) //可能会新增成品
                {
                    productId = this.AddProductData(dto.Product, ref product);
                }
                var spec = _productSpecificationService.GetAll().FirstOrDefault(x => x.Id == dto.SpecId);
                if (spec == null)
                {
                    throw new Exception("规格不存在，请刷新重试");
                }
                if (dto.ProductSpec != null)
                { //更新规格信息
                    var category = await _productCategoryService.GetAll(true).FirstOrDefaultAsync(x => x.Id == dto.ProductSpec.CategoryId);
                    if (category == null)
                    {
                        throw new Exception("产品类别不存在，请刷新重试");
                    }
                    if (!CheckCode(dto.ProductSpec.ProductNo, dto.SpecId))
                    {
                        throw new Exception($"{dto.ProductSpec.ProductNo}规格编码已存在，请重新输入");
                    }
                    //更新和成品主单位转换
                    var mainConversion = _unitConversionService.GetAll().FirstOrDefault(x => x.FromUnitId == (long)spec.SpecificationUnitId!
                                            && x.ToUnitId == product!.BaseUnitId && x.ConversionType == UnitConversionType.SpecToProduct
                                            && x.SourceId == spec.Id && x.TargetId == product!.Id);
                    if (mainConversion != null)
                    {
                        mainConversion.Ratio = (decimal)dto.ProductSpec.MainUnitRatio!;
                        mainConversion.FromUnitId = dto.ProductSpec.ProductUnitId; //规格单位更新
                    }
                    else
                    {
                        //和成品主单位转换
                        var mainUnitConversion = new Base_UnitConversion
                        {
                            FromUnitId = dto.ProductSpec.ProductUnitId,
                            ToUnitId = product!.BaseUnitId,
                            SourceId = spec.Id,
                            TargetId = product!.Id,
                            Ratio = (decimal)dto.ProductSpec.MainUnitRatio!,
                            ConversionType = UnitConversionType.SpecToProduct
                        };
                        await _unitConversionService.InsertAsync(mainUnitConversion);
                    }
                    await _unitOfWork.SaveChangeAsync();

                    if (spec.ProductId != product!.Id)
                    {
                        _productService.UpdataSpecProductData(spec.ProductId, product!.Id, spec.Id);
                    }
                    spec.ProductId = product!.Id;
                    spec.Code = dto.ProductSpec.ProductNo;
                    spec.Name = dto.ProductSpec.ProductName;
                    spec.NoSuffix = dto.ProductSpec.NoSuffix;
                    spec.Specification = dto.ProductSpec.Specification!;
                    spec.ProductCategoryId = dto.ProductSpec.CategoryId;
                    if (dto.ProductSpec.ProductUnitId != spec.SpecificationUnitId)
                    {
                        spec.SpecificationUnitId = dto.ProductSpec.ProductUnitId;
                        //更新 包装单位 到 规格主单位的转换
                        var packToSpecUnit = _unitConversionService.GetAll().Where(x => x.ConversionType == UnitConversionType.PackageToProductSpec && x.TargetId == spec.Id)
                                .ToList();
                        if (spec.SpecificationUnitId != null)
                        {
                            foreach (var item in packToSpecUnit)
                            {
                                item.ToUnitId = (long)spec.SpecificationUnitId;
                            }
                        }
                        else
                        {
                            _unitConversionService.Delete(packToSpecUnit);
                        }
                        //更新 加工单价的单位
                        var priceList = _productProcessPriceService.GetAll().Where(x => x.ProductSpecificationId == spec.Id && x.PriceUnitConversionId == -1).ToList();
                        foreach (var item in priceList)
                        {
                            item.PriceUnitId = dto.ProductSpec.ProductUnitId;
                        }
                    }
                    spec.Remark = dto.ProductSpec.Remark;
                    spec.ContainerModelId = dto.ProductSpec.ContainerModelId;
                    spec.CategoryAttribute = dto.ProductSpec.CategoryAttributeList.Count() > 0 ? JsonSerializer.Serialize(dto.ProductSpec.CategoryAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null;
                    spec.CustomAttribute = dto.ProductSpec.CustomAttributeList.Count() > 0 ? JsonSerializer.Serialize(dto.ProductSpec.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null;
                    spec.DefSaleUnitConversionId = dto.ProductSpec.IsDefaultSaleUnit != null && dto.ProductSpec.IsDefaultSaleUnit == true ? -1 : null;
                    //更新产品类别最新流水号
                    this.UpdateSequence(ref category, dto.ProductSpec.NoSuffix);

                    var oldUnitList = _unitConversionService.GetAll().Where(x => x.ConversionType == UnitConversionType.ProductSpec && x.SourceId == spec.Id)
                                .ToList();
                    //更新多单位
                    var delUnit = oldUnitList.Where(x => !dto.ProductSpec.UnitList.Any(e => e.Id == x.Id)).ToList();
                    var addUnit = dto.ProductSpec.UnitList.Where(x => !oldUnitList.Any(e => e.Id == x.Id)).ToList();
                    var modifyUnit = oldUnitList.Where(x => dto.ProductSpec.UnitList.Any(e => e.Id == x.Id)).ToList();
                    _unitConversionService.Delete(delUnit);
                    if (delUnit.Any(x => x.Id == spec.DefSaleUnitConversionId))
                    {
                        spec.DefSaleUnitConversionId = null;
                    }
                    foreach (var addItem in addUnit)
                    {
                        var newUnit = new Base_UnitConversion
                        {
                            FromUnitId = addItem.UnitId,
                            ToUnitId = (long)spec.SpecificationUnitId!,
                            SourceId = spec.Id,
                            TargetId = spec.Id,
                            Ratio = addItem.Ratio,
                            ConversionType = UnitConversionType.ProductSpec
                        };
                        await _unitConversionService.InsertAsync(newUnit);
                        await _unitOfWork.SaveChangeAsync();
                        if (addItem.IsDefaultSaleUnit != null && addItem.IsDefaultSaleUnit == true)
                        {
                            spec.DefSaleUnitConversionId = newUnit.Id;
                        }
                    }
                    foreach (var item in modifyUnit)
                    {
                        var newItem = dto.ProductSpec.UnitList.First(x => x.Id == item.Id);
                        item.Ratio = newItem.Ratio;
                        item.FromUnitId = newItem.UnitId;
                        item.ToUnitId = (long)spec.SpecificationUnitId!;
                        if (newItem.IsDefaultSaleUnit != null && newItem.IsDefaultSaleUnit == true)
                        {
                            spec.DefSaleUnitConversionId = item.Id;
                        }
                    }
                }

                await _unitOfWork.SaveChangeAsync();
                scope.Complete();
                var result = new
                {
                    Msg = "编辑成功",
                    ProductId = productId, //新成品Id
                };
                return Ok(result);
            }
        }
        private void UpdateSequence(ref Base_Category category, string noSuffix)
        {
            var numNoSuffix = 0;
            try
            {
                int.TryParse(noSuffix, out numNoSuffix);
            }
            catch
            {
                numNoSuffix = category.Sequence; ;
            }
            if (numNoSuffix > category.Sequence)
            {
                category.Sequence = numNoSuffix;
            }
        }

        public class DeleteProductDto
        {
            public long? ProductId { get; set; }
            public long? SpecId { get; set; }
            public bool UpdateGoods { get; set; }
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("DeletePoduct")]
        public async Task<IActionResult> DeletePoduct([FromBody] DeleteProductDto dto)
        {
            //删除成品或半成品 删除ProductRl 删除unitConversion 删单价 删除文件记录(有规格的不能删除)
            if (dto.ProductId != null)
            {
                var product = await _productService.GetAll(true).FirstOrDefaultAsync(x => x.Id == dto.ProductId);
                if (product != null)
                {
                    var specCount = _productSpecificationService.GetAll().Where(x => x.ProductId == product.Id).Count();
                    if (specCount > 0)
                    {
                        throw new Exception("成品存在包装规格，不能删除");
                    }
                    var productRLs = _productRLService.GetAll().Where(x => (x.ParentProductId == product.Id || x.ChildProductId == product.Id) && x.RelationType == ProductRelationType.半成品).ToList();
                    _productRLService.Delete(productRLs);
                    var unitConversions = _unitConversionService.GetAll().Where(x => (x.ConversionType == UnitConversionType.Product && x.SourceId == product.Id) ||
                                        (x.ConversionType == UnitConversionType.SpecToProduct && x.TargetId == product.Id)).ToList();
                    _unitConversionService.Delete(unitConversions);
                    //删除阿米巴单价配置
                    _productService.DeleteAmoebaPrice(product.Id, null);
                    //删除成品文件
                    var files = _fileStorageService.GetAll().Where(x => x.SourceType == FileStorageSourceType.成品文件 && x.SourceId == product.Id).ToList();
                    _fileStorageService.Delete(files);

                    _productService.Delete(product, false); //硬删
                }
            }

            //删除成品规格 删除unitConversion(包含和成品主单位的转换) 删除文件记录
            if (dto.SpecId != null)
            {
                var spec = await _productSpecificationService.GetAll(true).FirstOrDefaultAsync(x => x.Id == dto.SpecId);
                if (spec != null)
                {
                    var unitConversions = _unitConversionService.GetAll().Where(x => (x.ConversionType == UnitConversionType.ProductSpec && x.SourceId == spec.Id) ||
                                       (x.ConversionType == UnitConversionType.SpecToProduct && x.SourceId == spec.Id)).ToList();
                    _unitConversionService.Delete(unitConversions);
                    //删除阿米巴单价配置
                    _productService.DeleteAmoebaPrice(spec.ProductId, spec.Id);
                    //删除成品规格文件
                    var files = _fileStorageService.GetAll().Where(x => x.SourceType == FileStorageSourceType.成品规格文件 && x.SourceId == spec.Id).ToList();
                    _fileStorageService.Delete(files);

                    _productSpecificationService.Delete(spec, false);
                }
            }
            await _unitOfWork.SaveChangeAsync();
            return Ok("删除成功");
        }
        /// <summary>
        /// 禁用启用产品
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("ModifyEnable")]
        public async Task<IActionResult> ModifyEnable([FromBody] DeleteProductDto dto)
        {
            if (dto.ProductId != null)
            {
                var product = await _productService.GetAll(true).FirstOrDefaultAsync(x => x.Id == dto.ProductId);
                if (product != null)
                {
                    product.IsDeleted = !product.IsDeleted;
                    if (dto.UpdateGoods)
                    {
                        var goods = _goodsService.GetAll(true).Where(x => x.ProductId == product.Id && x.SpecificationId == null).ToList();
                        foreach (var good in goods)
                        {
                            good.IsDeleted = product.IsDeleted;
                        }
                    }
                }
            }

            if (dto.SpecId != null)
            {
                var spec = await _productSpecificationService.GetAll(true).FirstOrDefaultAsync(x => x.Id == dto.SpecId);
                if (spec != null)
                {
                    spec.IsDeleted = !spec.IsDeleted;
                    if (dto.UpdateGoods)
                    {
                        var goods = _goodsService.GetAll(true).Where(x => x.ProductId == spec.ProductId && x.SpecificationId == spec.Id).ToList();
                        foreach (var good in goods)
                        {
                            good.IsDeleted = spec.IsDeleted;
                        }
                    }
                }
            }
            await _unitOfWork.SaveChangeAsync();
            return Ok("操作成功");
        }

        public class ModifyPackageDto
        {
            public long ProductId { get; set; }
            public long? SpecId { get; set; }
            public List<AddPackageDto> PackageList { get; set; } = new List<AddPackageDto>();
        }
        [HttpPost("ModifyPackage")]
        public async Task<IActionResult> ModifyPackage([FromBody] ModifyPackageDto dto)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var product = _productService.GetAll(true).FirstOrDefault(x => x.Id == dto.ProductId);
                if (product == null)
                {
                    throw new Exception("成品不存在，请刷新重试");
                }
                var spec = _productSpecificationService.GetAll().FirstOrDefault(x => x.Id == dto.SpecId);
                if (dto.SpecId != null && spec == null)
                {
                    throw new Exception("成品规格不存在，请刷新重试");
                }
                var oldData = await _productPackageService.GetAll().Where(x => x.ProductId == dto.ProductId && x.SpecificationId == dto.SpecId).ToListAsync();
                var addData = dto.PackageList.Where(x => x.Id == null);
                var delData = oldData.Where(x => !dto.PackageList.Any(e => e.Id == x.Id));
                var modifyData = oldData.Where(x => dto.PackageList.Any(e => e.Id == x.Id));
                //新增包装
                foreach (var item in addData)
                {
                    var newPack = new Base_ProductPackage
                    {
                        ProductId = dto.ProductId,
                        SpecificationId = item.SpecificationId ?? dto.SpecId,
                        PackageSpecification = item.PackageSpecification,
                        PackageUnitId = item.PackageUnitId,
                        Remark = item.Remark,
                        PalletWegiht = item.PalletWegiht,
                        PackageWegiht = item.PackageWegiht,
                        PalletPackageRatio = item.PalletPackageRatio,
                        CustomAttribute = item.CustomAttributeList.Count > 0 ? JsonSerializer.Serialize(item.CustomAttributeList) : null,
                        PackTempLateId = item.PackTempLateId,
                    };
                    _productPackageService.Insert(newPack);
                    await _unitOfWork.SaveChangeAsync();

                    if (item.CustomerIds != null)
                    {
                        await ModifyPackgeCustomer(newPack, item.CustomerIds);
                    }

                    item.Id = newPack.Id;
                    //新增单位转换
                    if (item.PackageUnitId != null && item.mainUnitRatio != null)
                    {
                        var unitConversion = new Base_UnitConversion
                        {
                            FromUnitId = (long)newPack.PackageUnitId!,
                            SourceId = newPack.Id,
                            Ratio = (decimal)item.mainUnitRatio!,
                        };
                        if (item.SpecificationId != null)
                        {
                            unitConversion.ConversionType = UnitConversionType.PackageToProductSpec;
                            var specTmp = _productSpecificationService.GetAll(true).FirstOrDefault(x => x.Id == item.SpecificationId);
                            if (specTmp != null && specTmp.SpecificationUnitId != null)
                            {
                                unitConversion.ToUnitId = (long)specTmp.SpecificationUnitId;
                                unitConversion.TargetId = specTmp.Id;
                                _unitConversionService.Insert(unitConversion);
                            }
                        }
                        else
                        {
                            unitConversion.ConversionType = UnitConversionType.PackageToProduct;
                            unitConversion.ToUnitId = product.BaseUnitId;
                            unitConversion.TargetId = product.Id;
                            _unitConversionService.Insert(unitConversion);
                        }

                        //var unitConversion = new Base_UnitConversion
                        //{
                        //    FromUnitId = (long)newPack.PackageUnitId!,
                        //    ToUnitId = dto.SpecId != null ? (long)spec!.SpecificationUnitId! : product.BaseUnitId,
                        //    SourceId = newPack.Id,
                        //    TargetId = dto.SpecId != null ? (long)spec!.Id! : product.Id,
                        //    ConversionType = dto.SpecId != null ? UnitConversionType.PackageToProductSpec : UnitConversionType.PackageToProduct,
                        //    Ratio = (decimal)item.mainUnitRatio!,
                        //};
                        //_unitConversionService.Insert(unitConversion);
                    }
                }
                var packIds = oldData.Select(x => x.Id).ToList();
                var oldUnitConversion = _unitConversionService.GetAll().Where(x => packIds.Contains(x.SourceId) &&
                                        (x.ConversionType == UnitConversionType.PackageToProduct || x.ConversionType == UnitConversionType.PackageToProductSpec)).ToList();
                var oldFiles = _fileStorageService.GetAll().Where(x => x.SourceType == FileStorageSourceType.成品包装文件 && packIds.Contains(x.SourceId)).ToList();
                //删除包装
                foreach (var item in delData)
                {
                    //删除单位转换
                    var delUnit = oldUnitConversion.Where(x => x.SourceId == item.Id);
                    foreach (var unit in delUnit)
                    {
                        _unitConversionService.Delete(unit);
                    }

                    await ModifyPackgeCustomer(item, []);

                    //删除对应的文件
                    var delFiles = oldFiles.Where(x => x.SourceId == item.Id).ToList();
                    _fileStorageService.DeleteWihtFiles(delFiles);

                    _productPackageService.Delete(item);
                }
                //修改包装
                foreach (var item in modifyData)
                {
                    var dtoItem = dto.PackageList.First(x => x.Id == item.Id);

                    item.PackageSpecification = dtoItem.PackageSpecification;
                    item.PackageUnitId = dtoItem.PackageUnitId;
                    item.Remark = dtoItem.Remark;
                    item.PalletWegiht = dtoItem.PalletWegiht;
                    item.PackageWegiht = dtoItem.PackageWegiht;
                    item.PalletPackageRatio = dtoItem.PalletPackageRatio;
                    item.SpecificationId = dtoItem.SpecificationId;
                    item.CustomAttribute = dtoItem.CustomAttributeList.Count > 0 ? JsonSerializer.Serialize(dtoItem.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null;
                    item.PackTempLateId = dtoItem.PackTempLateId;

                    await ModifyPackgeCustomer(item, dtoItem.CustomerIds ?? []);

                    var oldUnit = oldUnitConversion.FirstOrDefault(x => x.SourceId == item.Id);
                    if (dtoItem.PackageUnitId != null && dtoItem.mainUnitRatio != null)
                    {
                        var specTmp = _productSpecificationService.GetAll(true).FirstOrDefault(x => x.Id == item.SpecificationId);

                        //新增单位转换
                        if (oldUnit == null)
                        {
                            var unitConversion = new Base_UnitConversion
                            {
                                FromUnitId = (long)dtoItem.PackageUnitId!,
                                SourceId = item.Id,
                                Ratio = (decimal)dtoItem.mainUnitRatio!,
                            };
                            if (item.SpecificationId != null)
                            {
                                unitConversion.ConversionType = UnitConversionType.PackageToProductSpec;
                                if (specTmp != null && specTmp.SpecificationUnitId != null)
                                {
                                    unitConversion.ToUnitId = (long)specTmp.SpecificationUnitId;
                                    unitConversion.TargetId = specTmp.Id;
                                    _unitConversionService.Insert(unitConversion);
                                }
                            }
                            else
                            {
                                unitConversion.ConversionType = UnitConversionType.PackageToProduct;
                                unitConversion.ToUnitId = product.BaseUnitId;
                                unitConversion.TargetId = product.Id;
                                _unitConversionService.Insert(unitConversion);
                            }
                            //var unitConversion = new Base_UnitConversion
                            //{
                            //    FromUnitId = (long)dtoItem.PackageUnitId!,
                            //    ToUnitId = dto.SpecId != null ? (long)spec!.SpecificationUnitId! : product.BaseUnitId,
                            //    SourceId = item.Id,
                            //    TargetId = dto.SpecId != null ? (long)spec!.Id! : product.Id,
                            //    ConversionType = dto.SpecId != null ? UnitConversionType.PackageToProductSpec : UnitConversionType.PackageToProduct,
                            //    Ratio = (decimal)dtoItem.mainUnitRatio!,
                            //};
                            //_unitConversionService.Insert(unitConversion);
                            continue;
                        }
                        //修改单位转换
                        oldUnit.FromUnitId = (long)dtoItem.PackageUnitId!;
                        oldUnit.Ratio = (decimal)dtoItem.mainUnitRatio!;
                        if (dtoItem.SpecificationId == null)
                        {
                            oldUnit.TargetId = product.Id;
                            oldUnit.ToUnitId = product.BaseUnitId;
                            continue;
                        }
                        if (specTmp != null && specTmp.SpecificationUnitId != null)
                        {
                            oldUnit.TargetId = specTmp.Id;
                            oldUnit.ToUnitId = (long)specTmp.SpecificationUnitId!;
                        }
                        else
                        {
                            _unitConversionService.Delete(oldUnit);
                        }
                    }
                    else
                    {//删除单位转换 
                        if (oldUnit != null)
                        {
                            _unitConversionService.Delete(oldUnit);
                        }
                    }

                }
                _unitOfWork.SaveChange();
                scope.Complete();
                var result = dto.PackageList;
                return Ok(result);
            }
        }

        private async Task ModifyPackgeCustomer(Base_ProductPackage package, List<long> customerIds)
        {
            //产品 对应的goods
            var goods = await _goodsService.GetAll().Where(x => x.ProductId == package.ProductId && package.SpecificationId == x.SpecificationId).ToListAsync();
            //包装方案和产品 对应的goods
            var existCustomerGoods = goods.Where(x => x.PackageId == package.Id).ToList();
            //包装方案 已有的客户
            var existIds = existCustomerGoods.Select(x => x.CustomerId).ToList();

            //删除客户
            var delData = existCustomerGoods.Where(x => !customerIds.Contains(x.CustomerId));
            foreach (var del in delData)
            {
                del.PackageId = null;
            }
            //新增客户
            var addIds = customerIds.Where(x => !existIds.Contains(x)).ToList();
            foreach (var addCustomerIdId in addIds)
            {
                var oldData = goods.Where(x => x.CustomerId == addCustomerIdId);
                if (oldData.Count() > 0) //已有goods则修改
                {
                    foreach (var mod in oldData)
                    {
                        mod.PackageId = package.Id;
                    }
                }
                else //没有则新增
                {
                    var good = new Base_Goods
                    {
                        PackageId = package.Id,
                        CustomerId = addCustomerIdId,
                        ProductId = package.ProductId,
                        SpecificationId = package.SpecificationId,
                        SalesPrice = 0
                    };
                    _goodsService.Insert(good);
                }
            }
            await _unitOfWork.SaveChangeAsync();
        }

        /// <summary>
        /// 获取产品外包装
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpGet("GetProductPackageList")]
        public async Task<IActionResult> GetProductPackageList([FromQuery] long ProductId, long? SpecId)
        {
            var query = _productPackageService.GetAll().Include(x => x.PackageUnit)
                        .Where(x => x.ProductId == ProductId && x.SpecificationId == SpecId);
            var data = await query.ToListAsync();
            var ids = data.Select(x => x.Id).ToList();
            var fileList = await _fileStorageService.GetAll().Where(x => x.SourceType == FileStorageSourceType.成品包装文件 && ids.Contains(x.SourceId)).ToListAsync();
            var tagFileList = await _fileStorageService.GetAll().Where(x => x.SourceType == FileStorageSourceType.成品标签文件 && ids.Contains(x.SourceId)).ToListAsync();
            var unitConversionList = await _unitConversionService.GetAll().Where(x => (x.ConversionType == UnitConversionType.PackageToProduct || x.ConversionType == UnitConversionType.PackageToProductSpec) &&
                                    ids.Contains(x.SourceId)).ToListAsync();

            //产品对应的goods记录
            var goods = await _goodsService.GetAll().Where(x => x.ProductId == ProductId && x.SpecificationId == SpecId).ToListAsync();

            var res = data.Select(x => new
            {
                x.Id,
                x.ProductId,
                x.SpecificationId,
                x.PackageSpecification,
                x.Remark,
                x.PackageUnitId,
                x.PackTempLateId,
                x.PalletPackageRatio,
                x.PackageWegiht,
                x.PalletWegiht,
                MainUnitRatio = unitConversionList.FirstOrDefault(e => e.SourceId == x.Id)?.Ratio,
                PackageUnit = x.PackageUnit != null ? x.PackageUnit.Name : null,
                FileList = fileList.Where(e => e.SourceId == x.Id),
                TagFileList = tagFileList.Where(e => e.SourceId == x.Id),
                CustomerIds = goods.Where(e => e.PackageId == x.Id).Select(x=>x.CustomerId),
                CustomAttributeList = !String.IsNullOrEmpty(x.CustomAttribute) ? JsonSerializer.Deserialize<List<CustomAttribute>>(x.CustomAttribute, JsonHelper.DefaultJsonSerializerOptions) : new List<CustomAttribute>()
            }).ToList();
            return Ok(res);
        }

        [HttpPost("ModifyPackCustomAttributeById")]
        public async Task<IActionResult> ModifyPackCustomAttributeById([FromBody] AddPackageDto dtoItem)
        {
            var item = await _productPackageService.GetAll().FirstOrDefaultAsync(x => x.Id == dtoItem.Id);
            if (item == null)
            {
                throw new Exception("包装方案不存在 请刷新重试");
            }
            item.CustomAttribute = dtoItem.CustomAttributeList.Count > 0 ? JsonSerializer.Serialize(dtoItem.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null;
            _unitOfWork.SaveChange();
            return Ok();
        }


        #endregion

        #region

        public class PrintDto
        {
            public long packegeId { get; set; }
        }

        [HttpPost("PrintPackegeById")]
        public async Task<IActionResult> PrintPackegeById([FromBody] PrintDto data)
        {
            using var workbook = await BuildPackageWorkbookAsync(data.packegeId);
            var htmlParts = _fileStorageService.GetMultiSheetHtmlStr(workbook);

            return Content(htmlParts, "text/html; charset=utf-8");
        }

        [HttpPost("PreviewPackagePdf")]
        public async Task<IActionResult> PreviewPackagePdf([FromBody] PrintDto data)
        {
            using var workbook = await BuildPackageWorkbookAsync(data.packegeId);

            SetPdfPageSetup(workbook);

            using var stream = new MemoryStream();

            var options = new PdfSaveOptions
            {
                OnePagePerSheet = true
            };

            workbook.Save(stream, options);

            return File(stream.ToArray(), "application/pdf", WebUtility.UrlEncode("PreViewPDF.pdf"));
        }


        [HttpPost("DownloadPackageExcel")]
        public async Task<IActionResult> DownloadPackageExcel([FromBody] PrintDto data)
        {
            using var workbook = await BuildPackageWorkbookAsync(data.packegeId);
            using var stream = new MemoryStream();

            workbook.Save(stream, SaveFormat.Xlsx);

            var fileName = $"产品包装标准_{data.packegeId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private static void SetPdfPageSetup(Workbook workbook)
        {
            foreach (Worksheet worksheet in workbook.Worksheets)
            {
                var pageSetup = worksheet.PageSetup;

                pageSetup.PaperSize = PaperSizeType.PaperA4;
                pageSetup.Orientation = PageOrientationType.Landscape;
                pageSetup.IsPercentScale = false;
                pageSetup.SetFitToPages(1, 1);

                pageSetup.LeftMarginInch = 0.2;
                pageSetup.RightMarginInch = 0.2;
                pageSetup.TopMarginInch = 0.25;
                pageSetup.BottomMarginInch = 0.25;

                worksheet.HorizontalPageBreaks.Clear();
                worksheet.VerticalPageBreaks.Clear();
            }
        }

        private async Task<Workbook> BuildPackageWorkbookAsync(long packageId)
        {
            var packageInfo = _productPackageService.GetAll().Include(x => x.Product).Include(x => x.ProductSpecification).Where(x => x.Id == packageId).FirstOrDefault();

            if (packageInfo == null)
                throw new InvalidOperationException("包装方案不存在，请刷新后重试");

            var goodsInfo = await _goodsService.GetAll().Include(x => x.Customer).Where(x => x.PackageId == packageId).ToListAsync();
            var customerNames = goodsInfo.Select(x => x.Customer.CustomerName).Distinct();
            var customerMaterialNo = goodsInfo.Where(x => !String.IsNullOrEmpty(x.CustomerMaterialNo)).Select(x => x.CustomerMaterialNo).Distinct();
            var customerMaterialName = goodsInfo.Where(x => !String.IsNullOrEmpty(x.CustomerMaterialNo)).Select(x => x.CustomerMaterialName).Distinct();

            var unitConver = _unitConversionService.GetAll().Include(x => x.ToUnit).Include(x => x.FromUnit).Where(x => x.SourceId == packageId).FirstOrDefault();

            var templatePara = _systemParaService.GetById((int)(packageInfo.PackTempLateId ?? 0));

            var halfProductPara = _systemParaService.GetAll().Where(x => x.DataType == SystemParaType.产品附加参数 && x.Code == "Half").FirstOrDefault();

            if (templatePara == null)
                throw new InvalidOperationException("包装模板不存在，请刷新后重试");

            if (string.IsNullOrWhiteSpace(templatePara.Extention))
                throw new InvalidOperationException("包装模板文件路径未配置");

            var templateFilePath = Path.Combine(
                AppContext.BaseDirectory,
                templatePara.Extention);

            if (!System.IO.File.Exists(templateFilePath))
                throw new FileNotFoundException($"没有找到包装模板文件：{templateFilePath}");

            var customAttributes = JsonSerializer.Deserialize<List<CustomAttribute>>(
                packageInfo.CustomAttribute ?? "[]",
                JsonHelper.DefaultJsonSerializerOptions);

            if (customAttributes == null || customAttributes.Count == 0)
                throw new InvalidOperationException("模板数据不存在，请刷新后重试");

            var productAttrs = JsonSerializer.Deserialize<List<CustomAttribute>>(
                (packageInfo.SpecificationId.HasValue ? packageInfo.ProductSpecification!.CustomAttribute :
                packageInfo.Product.CustomAttribute) ?? "[]",
                JsonHelper.DefaultJsonSerializerOptions);

            var halfName = "";

            if (productAttrs != null)
            {
                var tmp = productAttrs.FirstOrDefault(x => x.systemParaId == halfProductPara?.Id);
                if (tmp != null && tmp.value != null)
                {
                    halfName = ((JsonElement)tmp.value).GetString();
                }
            }

            var attributeMap = customAttributes
                .GroupBy(x => x.systemParaId)
                .ToDictionary(x => x.Key, x => x.Last());

            var parameterIds = attributeMap.Keys.ToList();

            var templateFields = await _systemParaService.GetAll()
                .AsNoTracking()
                .Where(x => parameterIds.Contains(x.Id))
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var files = await _fileStorageService.GetAll()
                .AsNoTracking()
                .Where(x =>
                    x.SourceType == FileStorageSourceType.成品包装文件 &&
                    x.SourceId == packageId)
                .ToListAsync();

            var fileMap = files
                .GroupBy(x => Convert.ToInt64(x.Id))
                .ToDictionary(x => x.Key, x => x.First());

            var textData = new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase);

            textData.Add("ProductModel", packageInfo.Product.TechnicalCode);
            textData.Add("ProductName", packageInfo.Product.ProductName);
            textData.Add("MaterialName", halfName);
            textData.Add("CustomerName", string.Join("，", customerNames));
            textData.Add("CustomerMaterialNo", string.Join("，", customerMaterialNo));
            textData.Add("CustomerMaterialName", string.Join("，", customerMaterialName));
            if (unitConver != null)
            {
                textData.Add("PackQty", ((float)unitConver.Ratio) + unitConver.ToUnit.Name + "/"+unitConver.FromUnit.Name);
            }


            var imageData = new Dictionary<string, List<byte[]>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var field in templateFields)
            {
                if (!attributeMap.TryGetValue(field.Id, out var attribute)) continue;

                var fieldCode = field.Extention?.Trim();

                if (string.IsNullOrWhiteSpace(fieldCode) || attribute.value == null) continue;

                if (field.DisplayType != CustomizeDisplayType.SelectFile)
                {
                    var text = ((JsonElement)attribute.value).GetString();
                    textData[fieldCode] = NormalizeText(text);
                    continue;
                }


                var fileIds = ((JsonElement)attribute.value).EnumerateArray().Select(it =>
                {
                    if(it.ValueKind == JsonValueKind.Number)
                    {
                        it.TryGetInt32(out var number);
                        return number;
                    }
                    return -1;
                }).ToArray();
                var imageList = new List<byte[]>();

                foreach (var fileId in fileIds)
                {
                    if (fileId == -1)
                    {
                        continue;
                    }

                    if (!fileMap.TryGetValue(fileId, out var file))
                        throw new InvalidOperationException($"没有找到文件记录，文件ID：{fileId}");

                    var filePath = _fileStorageService.GetfilePath(
                        file.Base.ToString(),
                        file.Path,
                        file.Uuid);

                    if (!System.IO.File.Exists(filePath))
                        throw new FileNotFoundException($"服务器文件丢失：{filePath}");

                    imageList.Add(await System.IO.File.ReadAllBytesAsync(filePath));
                }

                imageData[fieldCode] = imageList;
            }

            var dataTable = CreateSingleRowDataTable("Data", textData);
            var workbook = new Workbook(templateFilePath);
            var worksheet = workbook.Worksheets[0];

            var designer = new WorkbookDesigner
            {
                Workbook = workbook
            };

            designer.SetDataSource(dataTable);
            designer.Process(0, false);
            designer.ClearDataSource();

            var imageRenderer = new ExcelImagePlaceholderRenderer();
            imageRenderer.Render(worksheet, imageData);

            worksheet.Name = "产品包装标准";

            return workbook;

        }
        private string NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 统一换行
            text = text.Replace("\r\n", "\n")
                       .Replace("\r", "\n");

            // 去除每行首尾空格
            var lines = text.Split('\n')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            // 先把原来的换行统一成一个空格
            text = string.Join(" ", lines);

            // 多个空白压缩成一个空格
            text = Regex.Replace(text, @"\s+", " ");

            // 在 1.、2.、3. 前换行
            text = Regex.Replace(
                text,
                @"\s+(?=\d+\.)",
                Environment.NewLine
            );

            return text.Trim();
        }

        private static DataTable CreateSingleRowDataTable(string tableName,IReadOnlyDictionary<string, object?> data)
        {
            var table = new DataTable(tableName);

            foreach (var item in data)
            {

                var columnType = item.Value?.GetType() ?? typeof(object);

                table.Columns.Add(item.Key,columnType);
            }

            var row = table.NewRow();

            foreach (var item in data)
            {
                row[item.Key] = item.Value ?? DBNull.Value;
            }

            table.Rows.Add(row);

            return table;
        }

        

        #endregion

        [HttpGet("GetProductByProductNo")]
        public async Task<IActionResult> GetProductByProductNo([FromQuery] string productNo, bool isDeleted = false)
        {
            var filter = new List<Filter> { new Filter("ProductNo", JsonSerializer.SerializeToElement(productNo), FilterType.Equals, DataType.Text) };
            var specFilter = new List<Filter> { new Filter("Code", JsonSerializer.SerializeToElement(productNo), FilterType.Equals, DataType.Text) };

            if (!isDeleted)
            {
                filter.Add(new Filter("IsDeleted", JsonSerializer.SerializeToElement(false), FilterType.Equals, DataType.SelectBoolean));
                specFilter.Add(new Filter("IsDeleted", JsonSerializer.SerializeToElement(false), FilterType.Equals, DataType.SelectBoolean));
            }

            var data = await _productService.GetProductList(filter, specFilter).ToListAsync();
            return Ok(data);
        }


        public class GetProductSelectionDto
        {
            public long Value { get; set; }
            public required string Text { get; set; }
            public string? ProductNo { get; set; }
            public string? ProductName { get; set; }
            public string? TechnicalCode { get; set; }
            public long ProductCategoryId { get; set; }
            public required string ProductCategoryName { get; set; }
            public long? UnitId { get; set; }
            public string? UnitName { get; set; }
        }
    }
}
