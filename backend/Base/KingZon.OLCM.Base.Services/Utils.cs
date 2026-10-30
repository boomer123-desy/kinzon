using KingZon.Core.DataBase;
using KingZon.Core.Infrastructure;
using KingZon.OLCM.Base.Service.Dto;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Common.DataBase.Entities;
using KingZon.OLCM.Purchase.DataBase.Entities;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Kingzon.OLCM.Base.Services
{
    [Service]
    public class Utils
    {
        private readonly IRepository<Base_Goods> _goodsRepository;
        private readonly IRepository<Purchase_Order_Detail> _purchasedDetailRepository;
        private readonly IRepository<Base_UnitConversion> _unitConversionRepository;
        private readonly IRepository<Base_Product> _productRepository;
        private readonly IRepository<Base_ProductSpecification> _specificationRepository;

        public Utils(
           IRepository<Base_Product> productRepository,
           IRepository<Base_ProductSpecification> specificationRepository,
           IRepository<Base_Goods> goodsRepository,
           IRepository<Purchase_Order_Detail> purchasedDetailRepository,
            IRepository<Base_UnitConversion> unitConversionRepository)
        {
            _productRepository = productRepository;
            _specificationRepository = specificationRepository;
            _goodsRepository = goodsRepository;
            _purchasedDetailRepository = purchasedDetailRepository;
            _unitConversionRepository = unitConversionRepository;
        }

        /// <summary>
        /// 获取记录 对应的 Data物料信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="abnormals"></param>
        /// <param name="keySelectorType"></param>
        /// <param name="keySelectorId"></param>
        /// <param name="resultSelector"></param>
        /// <param name="isNullable"></param>
        /// <returns></returns>
        public IQueryable<TResult> JoinWithData<T, TResult>(IQueryable<T> entities, string keySelectorType, string keySelectorId, Expression<Func<T, DataInfo, TResult>> resultSelector, bool isNullable = false, bool isFilter = false,
                                                                List<Filter>? materialFilter = null)
        {
            if (isFilter)
            {//过滤 dataId和dataType对应的 物料、成品、半成品
             //                             成品规格
             //                             ----<工序产物>
                var productFilterList = new List<Filter>();
                var specificationFilterList = new List<Filter>();
                //var semiProductFilterList = new List<Filter>();
                if (materialFilter != null && materialFilter.Count() > 0)
                {
                    foreach (var item in materialFilter)
                    {
                        switch (item.Key)
                        {
                            case "materialNo":
                                productFilterList.Add(GenageFilter("productNo", item));
                                specificationFilterList.Add(GenageFilter("Code", item));
                                //semiProductFilterList.Add(GenageFilter("Code", item));
                                break;
                            case "materialName":
                                productFilterList.Add(GenageFilter("ProductName", item));
                                specificationFilterList.Add(GenageFilter("name", item));
                                //semiProductFilterList.Add(GenageFilter("Name", item));
                                break;
                            case "specification":
                                productFilterList.Add(GenageFilter("MaterialSpecification", item));
                                specificationFilterList.Add(GenageFilter("Specification", item));
                                //semiProductFilterList.Add(GenageFilter("Product.Specification", item));
                                break;
                            case "materialGrade":
                                productFilterList.Add(GenageFilter("MaterialGrade", item));
                                break;
                            case "categoryId":
                                productFilterList.Add(GenageFilter("ProductCategoryId", item));
                                specificationFilterList.Add(GenageFilter("ProductCategoryId", item));
                                break;
                            case "technicalCode":
                                productFilterList.Add(GenageFilter("TechnicalCode", item));
                                specificationFilterList.Add(GenageFilter("Product.TechnicalCode", item));
                                break;
                            case "customerId":
                                productFilterList.Add(GenageFilter("CustomerId", item));
                                specificationFilterList.Add(GenageFilter("CustomerId", item));
                                break;
                            case "customerMaterialNo":
                                productFilterList.Add(GenageFilter("CustomerMaterialNo", item));
                                specificationFilterList.Add(GenageFilter("CustomerMaterialNo", item));
                                break;
                            case "customerName":
                                productFilterList.Add(GenageFilter("Customer.CustomerName", item));
                                specificationFilterList.Add(GenageFilter("Customer.CustomerName", item));
                                break;
                            case "isOutboundCost":
                                productFilterList.Add(GenageFilter("ProductCategory.IsOutboundCost", item));
                                specificationFilterList.Add(GenageFilter("ProductCategory.IsOutboundCost", item));
                                break;
                            case "remark":
                                productFilterList.Add(GenageFilter("Remark", item));
                                specificationFilterList.Add(GenageFilter("Remark", item));
                                break;
                        }
                    }
                }
                var productIds = _productRepository.GetAll(true).ApplyFilters(productFilterList).Select(p => p.Id);
                var specIds = _specificationRepository.GetAll(true).ApplyFilters(specificationFilterList).Select(p => p.Id);
                //var semiProductIds = _semiProductRepository.GetAll().ApplyFilters(semiProductFilterList).Select(p => p.Id);

                if (productFilterList.Any(x => x.Key == "MaterialGrade"))
                {
                    specIds = specIds.Where(x => x < 0); //成品规格不包含材质牌号，过滤掉成品规格
                }

                entities = entities.Where(o => (EF.Property<InventoryDataType>(o, keySelectorType) == InventoryDataType.Product && productIds.Contains(isNullable ? EF.Property<long?>(o, keySelectorId).GetValueOrDefault() : EF.Property<long>(o, keySelectorId)))
                                                || (EF.Property<InventoryDataType>(o, keySelectorType) == InventoryDataType.Specification && specIds.Contains(isNullable ? EF.Property<long?>(o, keySelectorId).GetValueOrDefault() : EF.Property<long>(o, keySelectorId))));

                if (productFilterList.Any(x => x.Key == "CustomerMaterialNo") || productFilterList.Any(x => x.Key == "CustomerId") || productFilterList.Any(x => x.Key == "Customer.CustomerName"))
                {
                    var filter = productFilterList.Where(x => x.Key == "CustomerMaterialNo" || x.Key == "customerId" || x.Key == "Customer.CustomerName").ToList();
                    var materialNoProductIds = _goodsRepository.GetAll().ApplyFilters(filter).Where(x => x.SpecificationId == null).Select(x => x.ProductId);
                    var materialNoSpecIds = _goodsRepository.GetAll().ApplyFilters(filter).Where(x => x.SpecificationId != null).Select(x => x.SpecificationId);
                    entities = entities.Where(o => EF.Property<InventoryDataType>(o, keySelectorType) == InventoryDataType.Product && materialNoProductIds.Contains(isNullable ? EF.Property<long?>(o, keySelectorId).GetValueOrDefault() : EF.Property<long>(o, keySelectorId)) ||
                                                EF.Property<InventoryDataType>(o, keySelectorType) == InventoryDataType.Specification && materialNoSpecIds.Contains(isNullable ? EF.Property<long?>(o, keySelectorId).GetValueOrDefault() : EF.Property<long>(o, keySelectorId)));
                }
            }

            // 先写好 SQL
            string sql = @"
                            SELECT c.ProductId,
                                   IFNULL(c.SpecificationId, 0) AS SpecificationId,
                                   GROUP_CONCAT( 
                                                CASE
                                                    WHEN c.CustomerMaterialNo IS NOT NULL
                                                     AND c.CustomerMaterialNo <> ''
                                                    THEN c.CustomerMaterialNo
                                                END 
                                            SEPARATOR '\n') AS CustomerMaterialNo,
                                    GROUP_CONCAT(
                                                CONCAT(
                                                    IFNULL(c.CustomerMaterialNo, ''),
                                                    '-',
                                                    cu.CustomerName) 
                                            SEPARATOR '\n') AS MaterialNoAndCustomerName
                            FROM Base_Goods c
                            INNER JOIN Base_Partner cu ON cu.Id = c.CustomerId
                            WHERE cu.IsDeleted = 0
                            GROUP BY c.ProductId,c.SpecificationId
                        ";
            // 从 DbContext 获取 IQueryable<ProductCustomerMaterialNoDto>
            var productCustomerMaterialNoQuery = _productRepository.DbContext.Set<Tmp_Product_CustomerMaterialNo>()
                .FromSqlRaw(sql);

            var query = from entity in entities.AsExpandable()
                        join product in _productRepository.GetAll(true)
                            on new { DataType = EF.Property<InventoryDataType>(entity, keySelectorType), DataId = (isNullable ? EF.Property<long?>(entity, keySelectorId).GetValueOrDefault() : EF.Property<long>(entity, keySelectorId)) } equals new { DataType = InventoryDataType.Product, DataId = product.Id }
                                into tmp1
                        from product in tmp1.DefaultIfEmpty()
                        join specification in _specificationRepository.GetAll(true)
                            on new { DataType = EF.Property<InventoryDataType>(entity, keySelectorType), DataId = (isNullable ? EF.Property<long?>(entity, keySelectorId).GetValueOrDefault() : EF.Property<long>(entity, keySelectorId)) } equals new { DataType = InventoryDataType.Specification, DataId = specification.Id }
                                into tmp3
                        from specification in tmp3.DefaultIfEmpty()
                            //join semiProduct in _semiProductRepository.GetAll(true).Include(x => x.NodeTemplateSetting)
                            //    on new { DataType = EF.Property<InventoryDataType>(entity, keySelectorType), DataId = (isNullable ? EF.Property<int?>(entity, keySelectorId).GetValueOrDefault() : EF.Property<int>(entity, keySelectorId)) } equals new { DataType = InventoryDataType.半成品, DataId = semiProduct.Id }
                            //        into tmp2
                            //from semiProduct in tmp2.DefaultIfEmpty()
                        let productId = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.Id :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.ProductId :
                                           0
                        let specId = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? 0 :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.Id :
                                            0
                        let purchaseId = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.PurchaseOrderDetailId :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.PurchaseOrderDetailId :
                                           0
                        join pCus in productCustomerMaterialNoQuery
                            on new { productId = productId, specId = specId } equals new { productId = pCus.ProductId, specId = pCus.SpecificationId } into tmp4
                        from pCus in tmp4.DefaultIfEmpty()
                        join purchase in _purchasedDetailRepository.GetAll(true)
                            on purchaseId equals purchase.Id into tmp5
                        from purchase in tmp5.DefaultIfEmpty()
                        let dataInfo = new DataInfo
                        {
                            DataNo = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.ProductNo :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.Code :
                                            string.Empty,
                            DataName = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.ProductName :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.Name :
                                            null,
                            DataSpecification = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.MaterialSpecification :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.Specification :
                                            null,
                            DataMaterialGrade = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.MaterialGrade :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? "" :
                                            null,
                            DataCategory = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.ProductCategory.Name :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null && specification.ProductCategory != null) ? specification.ProductCategory!.Name :
                                            null,
                            DataCategoryId = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.ProductCategoryId :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.ProductCategoryId :
                                            null,
                            DataProductType = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.ProductType.ToString() :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.Product.ProductType.ToString() :
                                            //(EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.semi && semiProduct != null) ? "工序产物" :
                                            null,
                            DataTechnicalCode = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.TechnicalCode :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.Product.TechnicalCode :
                                            null,
                            DataUnit = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.BaseUnit.Name :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null && specification.SpecificationUnit != null) ? specification.SpecificationUnit!.Name :
                                            null,
                            DataUnitId = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.BaseUnitId :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.SpecificationUnitId :
                                            null,
                            DataCustomerMaterialNo = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && pCus != null) ? pCus.CustomerMaterialNo :
                                                          (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? pCus.CustomerMaterialNo :
                                                      string.Empty,
                            DataMaterialNoCustomerName = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && pCus != null) ? pCus.MaterialNoAndCustomerName :
                                                          (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? pCus.MaterialNoAndCustomerName :
                                                      string.Empty,
                            DataMinStock = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.SafetyStock :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.SafetyStock :
                                            null,
                            DataMaxStock = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.MaxStock :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.MaxStock :
                                            null,
                            DataTotalStock = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.InventoryQuantity :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.SpecificationInventoryQuantity :
                                            0,
                            DataBaseUnitId = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.BaseUnitId :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.Product.BaseUnitId :
                                            null,
                            DataProductId = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.Id :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.ProductId :
                                            null,
                            DataRemark = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && product != null) ? product.Remark :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && specification != null) ? specification.Remark :
                                            null,
                            CurrentPurchasePrice = (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Product && purchase != null) ? purchase.Price :
                                        (EF.Property<InventoryDataType>(entity, keySelectorType) == InventoryDataType.Specification && purchase != null) ? purchase.Price :
                                            null,
                        }
                        select resultSelector.Invoke(entity, dataInfo);
            return query;
        }

        public Filter GenageFilter(string key, Filter filter)
        {
            var res = new Filter(key, filter.Value ?? "", filter.FilterType, filter.DataType);
            return res;
        }

        public DataInfo? GetDataInfo(InventoryDataType dataType, long dataId)
        {
            var result = new DataInfo();
            switch (dataType)
            {
                case InventoryDataType.Product:
                    var dataProduct = _productRepository.GetAll(true).Include(x => x.ProductCategory).Include(x => x.BaseUnit)
                        .FirstOrDefault(x => dataId == x.Id);
                    if (dataProduct == null)
                    {
                        return null;
                    }
                    result.DataNo = dataProduct.ProductNo;
                    result.DataName = dataProduct.ProductName;
                    result.DataSpecification = dataProduct.MaterialSpecification;
                    result.DataMaterialGrade = dataProduct.MaterialGrade;
                    result.DataTechnicalCode = dataProduct.TechnicalCode;
                    result.DataCategory = dataProduct.ProductCategory.Name;
                    result.DataCategoryId = dataProduct.ProductCategoryId;
                    result.DataUnit = dataProduct.BaseUnit.Name;
                    result.DataUnitId = dataProduct.BaseUnitId;
                    result.DataBaseUnitId = dataProduct.BaseUnitId;
                    result.DataProductId = dataProduct.Id;
                    break;
                case InventoryDataType.Specification:
                    var dataSpecification = _specificationRepository.GetAll(true).Include(x => x.ProductCategory)
                        .Include(x => x.Product).ThenInclude(x => x.BaseUnit)
                        .Include(x => x.SpecificationUnit)
                        .FirstOrDefault(x => dataId == x.Id);
                    if (dataSpecification == null)
                    {
                        return null;
                    }
                    result.DataNo = dataSpecification.Code;
                    result.DataName = dataSpecification.Name;
                    result.DataSpecification = dataSpecification.Specification;
                    result.DataMaterialGrade = null;
                    result.DataTechnicalCode = dataSpecification.Product.TechnicalCode;
                    result.DataCategory = dataSpecification.ProductCategory?.Name;
                    result.DataCategoryId = dataSpecification.ProductCategoryId;
                    result.DataUnit = dataSpecification.SpecificationUnit?.Name;
                    result.DataUnitId = dataSpecification.SpecificationUnitId;
                    result.DataBaseUnitId = dataSpecification.Product.BaseUnitId;
                    result.DataBaseUnitName = dataSpecification.Product.BaseUnit.Name;
                    result.DataProductId = dataSpecification.ProductId;
                    var ratio = _unitConversionRepository.GetAll().FirstOrDefault(x => x.SourceId == dataSpecification.Id && x.TargetId == dataSpecification.ProductId
                                && x.FromUnitId == dataSpecification.SpecificationUnitId && x.ToUnitId == dataSpecification.Product.BaseUnitId
                                && x.ConversionType == UnitConversionType.SpecToProduct);
                    result.ToBaseUnitRatio = ratio?.Ratio;
                    result.ToBaseUnitConversionId = ratio?.Id;
                    break;

            }
            return result;
        }

        public System.Data.DataTable ToDataTable<T>(IEnumerable<T> data)
        {
            var dt = new System.Data.DataTable();
            var ps = System.ComponentModel.TypeDescriptor.GetProperties(typeof(T));
            // 添加列，处理 Nullable 类型
            foreach (System.ComponentModel.PropertyDescriptor dp in ps)
            {
                Type colType = dp.PropertyType;

                // 如果是 Nullable<T>，取 T
                if (colType.IsGenericType && colType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    colType = Nullable.GetUnderlyingType(colType);
                }

                dt.Columns.Add(dp.Name, colType);
            }

            // 添加行
            foreach (T t in data)
            {
                var dr = dt.NewRow();
                foreach (System.ComponentModel.PropertyDescriptor dp in ps)
                {
                    var value = dp.GetValue(t);
                    dr[dp.Name] = value ?? DBNull.Value; // null 值转为 DBNull
                }
                dt.Rows.Add(dr);
            }

            return dt;
        }


        //最多保留4位小数
        public decimal formatDigital4(decimal data, int digit = 4)
        {
            return Math.Round(data, digit, MidpointRounding.AwayFromZero);
        }
    }
}
