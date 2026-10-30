using Kingzon.OLCM.Base.Services;
using Kingzon.OLCM.Base.Services.JsonStructure;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Infrastructure;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Common.DataBase.Entities;
using KingZon.OLCM.Inventory.Service.Dto;
using KingZon.OLCM.Inventory.Shared.DataBase.Entities;
using KingZon.OLCM.Inventory.Shared.DataBase.Enum;
using KingZon.OLCM.Inventory.Shared.DataBase.TmpEntities;
using KingZon.OLCM.Production.Shared.DataBase.Entities;
using KingZon.OLCM.Purchase.DataBase.Entities;
using KingZon.OLCM.Sale.Shared.DataBase.Entities;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;

namespace KingZon.OLCM.Inventory.Service
{
    [Service]
    public class InventoryService : CommonService<Inventory_Record>
    {
        private readonly IRepository<PurchaseArrivalRecord> _arrivalRecordRepository;
        private readonly IRepository<Purchase_Order_Detail> _purchaseOrderDetailRepository;
        private readonly IRepository<Inventory_Record> _inventoryRecordRepository;
        private readonly IRepository<Inventory_Position> _positionRepository;
        private readonly IRepository<Temp_Inventory_Keys> _tempInventoryKeysRepository;
        private readonly IRepository<Base_Product> _productRepository;
        private readonly IRepository<Base_ProductSpecification> _specificationRepository;
        private readonly IRepository<Inventory_Record_RL> _inventoryRecordRLRepository;
        private readonly IRepository<Inventory_Material_CheckInfo> _materialCheckInfoService;
        private readonly IRepository<Inventory_CheckDetail> _inventoryCheckDetailRepository;
        private readonly IRepository<Inventory_CheckRecord> _inventoryCheckRepository;
        private readonly IRepository<Production_Dispatch_Order> _dispatchOrderRepository;
        private readonly IRepository<Inventory_DistributeMaterial_OrderDetail> _distributeMaterialRepository;
        private readonly IRepository<Inventory_Record_Production_Ext> _inventoryProductionExtRepository;
        private readonly IRepository<Inventory_Record_Purchase_Ext> _inventoryPurchaseExtRepository;
        private readonly Utils _utils;
        private readonly IRepository<Sale_OrderDetail> _saleOrderDetailRepository;
        private readonly IRepository<Inventory_OccupyDetail> _occupyDetailRepository;
        private readonly IRepository<Inventory_DeliveryOrderDetail> _deliveryOrderDetailRepository;
        private readonly IRepository<Base_Goods> _goodsRepository;
        private readonly IRepository<Base_SystemPara> _systemParaRepository;
        private readonly IUnitOfWork _unitOfWork;

        public InventoryService(
            IRepository<PurchaseArrivalRecord> arrivalRecordRepository,
            IRepository<Purchase_Order_Detail> purchaseOrderDetailRepository,
            IRepository<Inventory_Record> repository,
            IRepository<Inventory_Position> positionRepository,
            IRepository<Temp_Inventory_Keys> tempInventoryKeysRepository,
            IRepository<Base_Product> productRepository,
            IRepository<Base_ProductSpecification> specificationRepository,
            IRepository<Inventory_Record_RL> inventoryRecordRLRepository,
            IRepository<Inventory_Material_CheckInfo> materialCheckInfoService,
            IRepository<Inventory_CheckDetail> inventoryCheckDetailRepository,
            IRepository<Inventory_CheckRecord> inventoryCheckRepository,
            IRepository<Production_Dispatch_Order> dispatchOrderRepository,
            IRepository<Inventory_DistributeMaterial_OrderDetail> distributeMaterialRepository,
            IRepository<Inventory_Record_Production_Ext> inventoryProductionExtRepository,
            IRepository<Inventory_Record_Purchase_Ext> inventoryPurchaseExtRepository,
            Utils utils,
            IRepository<Sale_OrderDetail> saleOrderDetailRepository,
            IRepository<Inventory_OccupyDetail> occupyDetailRepository,
            IRepository<Inventory_DeliveryOrderDetail> deliveryOrderDetailRepository,
            IRepository<Base_Goods> goodsRepository,
            IRepository<Base_SystemPara> systemParaRepository,
            IUnitOfWork unitOfWork) : base(repository)
        {
            _arrivalRecordRepository = arrivalRecordRepository;
            _purchaseOrderDetailRepository = purchaseOrderDetailRepository;
            _inventoryRecordRepository = repository;
            _productRepository = productRepository;
            _specificationRepository = specificationRepository;
            _positionRepository = positionRepository;
            _tempInventoryKeysRepository = tempInventoryKeysRepository;
            _productRepository = productRepository;
            _specificationRepository = specificationRepository;
            _inventoryRecordRLRepository = inventoryRecordRLRepository;
            _materialCheckInfoService = materialCheckInfoService;
            _inventoryCheckDetailRepository = inventoryCheckDetailRepository;
            _inventoryCheckRepository = inventoryCheckRepository;
            _dispatchOrderRepository = dispatchOrderRepository;
            _distributeMaterialRepository = distributeMaterialRepository;
            _inventoryProductionExtRepository = inventoryProductionExtRepository;
            _inventoryPurchaseExtRepository = inventoryPurchaseExtRepository;
            _utils = utils;
            _saleOrderDetailRepository = saleOrderDetailRepository;
            _occupyDetailRepository = occupyDetailRepository;
            _deliveryOrderDetailRepository = deliveryOrderDetailRepository;
            _goodsRepository = goodsRepository;
            _systemParaRepository = systemParaRepository;
            _unitOfWork = unitOfWork;
        }


        public IQueryable<TResult> JoinWithSource<T, TResult>(IQueryable<T> inventorys, string keySelectorType, string keySelectorId, Expression<Func<T, SourceInfoDto, TResult>> resultSelector, bool isNullable = false,
                                                                bool isFilter = false, List<Filter>? filters = null)
        {
            //查询 目前只做了 采购入库、派工入库、生产发料、移库出库、移库入库、送货出库、客户库房调拨
            //------<生产入库、外协入库、退料入库、包装入库、移库入库、退货入库、成品报废入库的查询>
            if (isFilter)
            {
                var productionCardFilterList = new List<Filter>();
                var arrivalFilterList = new List<Filter>();
                var returnMaterialFilterList = new List<Filter>();
                var saleReturnFilterList = new List<Filter>();
                var packageFilterList = new List<Filter>();
                var moveFilterList = new List<Filter>();
                var deliveryDetailFilterList = new List<Filter>();
                var outSourceFilterList = new List<Filter>();
                var outSourceReturnFilterList = new List<Filter>();
                var dispatchFilterList = new List<Filter>();
                var distributeMaterialFilterList = new List<Filter>();
                if (filters != null && filters.Count() > 0)
                {
                    foreach (var item in filters)
                    {
                        switch (item.Key)
                        {
                            case "sourceNo":
                                productionCardFilterList.Add(GenageFilter("cardNo", item));
                                arrivalFilterList.Add(GenageFilter("arrivalRecordNo", item));
                                packageFilterList.Add(GenageFilter("recordNo", item));
                                moveFilterList.Add(GenageFilter("recordNo", item));
                                returnMaterialFilterList.Add(GenageFilter("returnMaterialOrder.orderNo", item));
                                saleReturnFilterList.Add(GenageFilter("saleReturnOrder.orderNo", item));
                                inventorys = inventorys.Where(o => EF.Property<InventorySourceType>(o, keySelectorType) != InventorySourceType.盘库盈亏
                                                        && EF.Property<InventorySourceType>(o, keySelectorType) != InventorySourceType.直接入库
                                                            && EF.Property<InventorySourceType>(o, keySelectorType) != InventorySourceType.直接出库);
                                dispatchFilterList.Add(GenageFilter("orderNo", item));
                                distributeMaterialFilterList.Add(GenageFilter("distributeMaterialOrder.orderNo", item));
                                deliveryDetailFilterList.Add(GenageFilter("DeliveryOrder.DeliveryOrderNo", item));
                                break;
                            case "sourceType":
                                if (item.FilterType == FilterType.Equals)
                                {
                                    inventorys = inventorys.Where(x => EF.Property<InventorySourceType>(x, keySelectorType) == (InventorySourceType)item.Value);
                                }
                                if (item.FilterType == FilterType.IntArrayContains)
                                {
                                    inventorys = inventorys.Where(x => item.Value != null && ((List<InventorySourceType>)item.Value).Contains(EF.Property<InventorySourceType>(x, keySelectorType)));
                                }
                                break;
                            case "sourceOrderId":
                                saleReturnFilterList.Add(GenageFilter("id", item));
                                deliveryDetailFilterList.Add(GenageFilter("id", item));
                                outSourceFilterList.Add(GenageFilter("outSourceOrderId", item));
                                outSourceReturnFilterList.Add(GenageFilter("outSourceReturnOrderId", item));
                                break;
                        }
                    }
                }
                var arrivalIds = _arrivalRecordRepository.GetAll().ApplyFilters(arrivalFilterList).Select(p => p.Id);
                var dispatchIds = _dispatchOrderRepository.GetAll().ApplyFilters(dispatchFilterList).Select(p => p.Id);
                var distributeIds = _distributeMaterialRepository.GetAll().ApplyFilters(distributeMaterialFilterList).Select(p => p.Id);
                var moveInventoryIds = _inventoryRecordRepository.GetAll().ApplyFilters(moveFilterList).Select(p => p.Id);
                var deliveryDetailIds = _deliveryOrderDetailRepository.GetAll().ApplyFilters(deliveryDetailFilterList).Select(p => p.Id);

                //var productionCardIds = _productionCardRepository.GetAll().ApplyFilters(productionCardFilterList).Select(p => p.Id);
                //var returnMaterialIds = _returnMaterialOrderDetailRepository.GetAll().ApplyFilters(returnMaterialFilterList).Select(p => p.Id);
                //var packageIds = _packageRecordRepository.GetAll().ApplyFilters(packageFilterList).Select(x => x.Id);
                //var saleReturnIds = _saleReturnOrderDetailRepository.GetAll().ApplyFilters(saleReturnFilterList).Select(p => p.Id);
                //var deliveryDetailIds = _deliveryOrderDetailRepository.GetAll().ApplyFilters(deliveryDetailFilterList).Select(p => p.Id);
                //var outSourceMaterialIds = _outSourceOrderMaterialDetailRepository.GetAll().ApplyFilters(outSourceFilterList).Select(p => p.Id);
                //var outSourceReturnDetialIds = _outSourceReturnOrderDetailRepository.GetAll().ApplyFilters(outSourceReturnFilterList).Select(p => p.Id);

                inventorys = inventorys.Where(o => (EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.采购入库 || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.外协入库) && arrivalIds.Contains(isNullable ? EF.Property<long?>(o, keySelectorId).GetValueOrDefault() : EF.Property<long>(o, keySelectorId))
                                                           || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.报工入库 && dispatchIds.Contains(isNullable ? EF.Property<long?>(o, keySelectorId).GetValueOrDefault() : EF.Property<long>(o, keySelectorId))
                                                           || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.生产发料 && distributeIds.Contains(isNullable ? EF.Property<long?>(o, keySelectorId).GetValueOrDefault() : EF.Property<long>(o, keySelectorId))
                                                           || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.移库出库 && moveInventoryIds.Contains(isNullable ? EF.Property<long?>(o, keySelectorId).GetValueOrDefault() : EF.Property<long>(o, keySelectorId))
                                                           || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.移库入库 && moveInventoryIds.Contains(isNullable ? EF.Property<long?>(o, keySelectorId).GetValueOrDefault() : EF.Property<long>(o, keySelectorId))
                                                           || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.送货出库 && deliveryDetailIds.Contains(isNullable ? EF.Property<long?>(o, keySelectorId).GetValueOrDefault() : EF.Property<long>(o, keySelectorId))
                                                           || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.发货调拨 && deliveryDetailIds.Contains(isNullable ? EF.Property<long?>(o, keySelectorId).GetValueOrDefault() : EF.Property<long>(o, keySelectorId))
                                                           //|| EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.生产入库 && productionCardIds.Contains(isNullable ? EF.Property<int?>(o, keySelectorId).GetValueOrDefault() : EF.Property<int>(o, keySelectorId))
                                                           //    || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.退料入库 && returnMaterialIds.Contains(isNullable ? EF.Property<int?>(o, keySelectorId).GetValueOrDefault() : EF.Property<int>(o, keySelectorId))
                                                           //        || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.包装入库 && packageIds.Contains(isNullable ? EF.Property<int?>(o, keySelectorId).GetValueOrDefault() : EF.Property<int>(o, keySelectorId))
                                                           //                || (EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.退货入库 || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.成品报废入库) && saleReturnIds.Contains(isNullable ? EF.Property<int?>(o, keySelectorId).GetValueOrDefault() : EF.Property<int>(o, keySelectorId))
                                                           //                    || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.送货出库 && deliveryDetailIds.Contains(isNullable ? EF.Property<int?>(o, keySelectorId).GetValueOrDefault() : EF.Property<int>(o, keySelectorId))
                                                           //                        || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.外协出库 && outSourceMaterialIds.Contains(isNullable ? EF.Property<int?>(o, keySelectorId).GetValueOrDefault() : EF.Property<int>(o, keySelectorId))
                                                           //                            || EF.Property<InventorySourceType>(o, keySelectorType) == InventorySourceType.外协退货 && outSourceReturnDetialIds.Contains(isNullable ? EF.Property<int?>(o, keySelectorId).GetValueOrDefault() : EF.Property<int>(o, keySelectorId))
                                                           );
            }
            var query = from inventory in inventorys.AsExpandable()
                        join arrival in _arrivalRecordRepository.GetAll()
                          on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<long?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<long>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.采购入库, SourceId = arrival.Id } into tmp2
                        from arrival in tmp2.DefaultIfEmpty()
                        join purchaseOrderDetail in _purchaseOrderDetailRepository.GetAll()
                           on arrival.PurchaseOrderDetailId equals purchaseOrderDetail.Id into tmp2_1
                        from purchaseOrderDetail in tmp2_1.DefaultIfEmpty()
                        join dispatchOrder in _dispatchOrderRepository.GetAll()
                           on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<long?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<long>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.报工入库, SourceId = dispatchOrder.Id } into tmp14
                        from dispatchOrder in tmp14.DefaultIfEmpty()
                        join distributeDetail in _distributeMaterialRepository.GetAll()
                            on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<long?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<long>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.生产发料, SourceId = distributeDetail.Id } into tmp15
                        from distributeDetail in tmp15.DefaultIfEmpty()
                        join moveInventory in _inventoryRecordRepository.GetAll().Where(s => s.SourceType == InventorySourceType.移库出库)
                            on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<long?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<long>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.移库入库, SourceId = moveInventory.Id } into tmp12
                        from moveInventory in tmp12.DefaultIfEmpty()
                        join moveOriginInventory in _inventoryRecordRepository.GetAll().Where(x => x.InventoryType == InventoryType.入库 || x.InventoryType == InventoryType.报废入库 || x.InventoryType == InventoryType.改制采购入库 || x.InventoryType == InventoryType.线边入库)
                            on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<long?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<long>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.移库出库, SourceId = moveOriginInventory.Id } into tmp13
                        from moveOriginInventory in tmp13.DefaultIfEmpty()
                        join delivery in _deliveryOrderDetailRepository.GetAll()
                            on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<long?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<long>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.送货出库, SourceId = delivery.Id } into tmp8
                        from delivery in tmp8.DefaultIfEmpty()
                        join delivery2 in _deliveryOrderDetailRepository.GetAll()
                          on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<long?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<long>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.发货调拨, SourceId = delivery2.Id } into tmp16
                        from delivery2 in tmp16.DefaultIfEmpty()

                            //join productionCard in _productionCardRepository.GetAll()
                            //    on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.生产入库, SourceId = productionCard.Id }
                            //        into tmp1
                            //from productionCard in tmp1.DefaultIfEmpty()
                            //join purchaseReturnOrderDetail in _purchaseReturnOrderDetailRepository.GetAll()
                            //    on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.采购退货, SourceId = purchaseReturnOrderDetail.Id } into tmp1_1
                            //from purchaseReturnOrderDetail in tmp1_1.DefaultIfEmpty()
                            //join outSourceArrival in _arrivalRecordRepository.GetAll()
                            //     on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.外协入库, SourceId = outSourceArrival.Id } into tmp3
                            //from outSourceArrival in tmp3.DefaultIfEmpty()
                            //join outSourceInspection in _arrivalInspectionRecordRepository.GetAll()
                            //   on outSourceArrival.Id equals outSourceInspection.ArrivalRecordId into tmp3_1
                            //from outSourceInspection in tmp3_1.DefaultIfEmpty()
                            //join outSourceOrderDetail in _outSourceOrderSemiMaterialDetailRepository.GetAll()
                            //    on outSourceArrival.SourceId equals outSourceOrderDetail.Id into tmp3_2
                            //from outSourceOrderDetail in tmp3_2.DefaultIfEmpty()
                            //join returnMaterial in _returnMaterialOrderDetailRepository.GetAll()
                            //   on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.退料入库, SourceId = returnMaterial.Id } into tmp4
                            //from returnMaterial in tmp4.DefaultIfEmpty()
                            //join pick in _pickRecordRepository.GetAll()
                            //    on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.产品拣选, SourceId = pick.Id } into tmp5
                            //from pick in tmp5.DefaultIfEmpty()
                            //join receive in _receiveMaterialOrderDetailRepository.GetAll()
                            //    on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.生产发料, SourceId = receive.Id } into tmp6
                            //from receive in tmp6.DefaultIfEmpty()
                            //join outSourceMaterial in _outSourceOrderMaterialDetailRepository.GetAll()
                            //    on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.外协出库, SourceId = outSourceMaterial.Id } into tmp7
                            //from outSourceMaterial in tmp7.DefaultIfEmpty()
                            //join delivery in _deliveryOrderDetailRepository.GetAll()
                            //    on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.送货出库, SourceId = delivery.Id } into tmp8
                            //from delivery in tmp8.DefaultIfEmpty()
                            //join returnSale in _saleReturnOrderDetailRepository.GetAll()
                            //    on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.退货入库, SourceId = returnSale.Id } into tmp9
                            //from returnSale in tmp9.DefaultIfEmpty()
                            //join returnSaleScrap in _saleReturnOrderDetailRepository.GetAll()
                            //    on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.成品报废入库, SourceId = returnSaleScrap.Id } into tmp10
                            //from returnSaleScrap in tmp10.DefaultIfEmpty()
                            //join packageDetail in _packageRecordRepository.GetAll()
                            //    on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.包装入库, SourceId = packageDetail.Id } into tmp11
                            //from packageDetail in tmp11.DefaultIfEmpty()

                            //join outSourceReturnOrderDetail in _outSourceReturnOrderDetailRepository.GetAll()
                            //    on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.外协退货, SourceId = outSourceReturnOrderDetail.Id } into tmp6_1
                            //from outSourceReturnOrderDetail in tmp6_1.DefaultIfEmpty()
                            //join workFixtuerReceive in _workFixtureReceiveOrderRepository.GetAll()
                            //    on new { InventorySourceType = EF.Property<InventorySourceType>(inventory, keySelectorType), SourceId = (isNullable ? EF.Property<int?>(inventory, keySelectorId).GetValueOrDefault() : EF.Property<int>(inventory, keySelectorId)) } equals new { InventorySourceType = InventorySourceType.工模治具领用出库, SourceId = workFixtuerReceive.Id } into tmp6_2
                            //from workFixtuerReceive in tmp6_2.DefaultIfEmpty()
                        let sourceInfo = new SourceInfoDto()
                        {
                            sourceNo = (EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.采购入库 && arrival != null) ? arrival.ArrivalRecordNo :
                                            (EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.报工入库 && dispatchOrder != null) ? dispatchOrder.OrderNo :
                                            (EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.生产发料 && distributeDetail != null) ? distributeDetail.DistributeMaterialOrder.OrderNo :
                                            (EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.移库入库 && moveInventory != null) ? moveInventory.RecordNo :
                                            (EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.移库出库 && moveOriginInventory != null) ? moveOriginInventory.RecordNo :
                                            (EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.送货出库 && delivery != null) ? delivery.DeliveryOrder.DeliveryOrderNo :
                                            (EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.发货调拨 && delivery2 != null) ? delivery2.DeliveryOrder.DeliveryOrderNo :

                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.生产入库 && productionCard != null) ? productionCard.CardNo :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.外协入库 && outSourceArrival != null) ? outSourceArrival.RecordNo :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.退料入库 && returnMaterial != null) ? returnMaterial.ReturnMaterialOrder.OrderNo :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.产品拣选 && pick != null) ? pick.RecordNo :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.生产发料 && receive != null) ? receive.ReceiveMaterialOrder.OrderNo :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.外协出库 && outSourceMaterial != null) ? outSourceMaterial.OutSourceOrder.OrderNo :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.送货出库 && delivery != null && delivery.DeliveryOrder != null) ? delivery.DeliveryOrder!.OrderNo :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.退货入库 && returnSale != null) ? returnSale.SaleReturnOrder.OrderNo :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.成品报废入库 && returnSaleScrap != null) ? returnSaleScrap.SaleReturnOrder.OrderNo :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.包装入库 && packageDetail != null) ? packageDetail.RecordNo :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.外协退货 && outSourceReturnOrderDetail != null) ? outSourceReturnOrderDetail.OutSourceReturnOrder.OrderNo :
                                            null,
                            sourceOrderId = (EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.采购入库 && purchaseOrderDetail != null) ? purchaseOrderDetail.Id : //采购入库对应的采购订单详情
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.采购退货 && purchaseReturnOrderDetail != null) ? purchaseReturnOrderDetail.Id :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.外协入库 && outSourceOrderDetail != null) ? outSourceOrderDetail.Id : //外协入库对应的外协订单完工详情
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.退料入库 && returnMaterial != null) ? returnMaterial.ReturnMaterialOrder.Id :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.产品拣选 && pick != null) ? pick.Id :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.生产发料 && receive != null) ? receive.ReceiveMaterialOrder.Id :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.外协出库 && outSourceMaterial != null) ? outSourceMaterial.OutSourceOrder.Id :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.送货出库 && delivery != null) ? delivery.Id :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.退货入库 && returnSale != null) ? returnSale.SaleReturnOrder.Id :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.成品报废入库 && returnSaleScrap != null) ? returnSaleScrap.SaleReturnOrder.Id :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.包装入库 && packageDetail != null) ? packageDetail.Id :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.移库入库 && moveInventory != null) ? moveInventory.SourceInventoryRecords.First().SourceInventory.Id :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.移库出库 && moveOriginInventory != null) ? moveOriginInventory.Id :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.外协退货 && outSourceReturnOrderDetail != null) ? outSourceReturnOrderDetail.OutSourceReturnOrder.Id :
                                            0,
                            limitQuantity = (EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.采购入库 && arrival != null) ? arrival.ArrivalQuantity :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.生产入库 && productionCard != null) ? productionCard.Quantity :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.外协入库 && outSourceInspection != null) ? outSourceInspection.QualifiedQuantity + outSourceInspection.ConcessionQuantity :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.退货入库 && returnSale != null) ? returnSale.ReturnQuantity :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.成品报废入库 && returnSaleScrap != null) ? returnSaleScrap.ReturnQuantity :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.包装入库 && packageDetail != null) ? packageDetail.PackageQuantity :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.移库入库 && moveInventory != null) ? moveInventory.Quantity :
                                            //(EF.Property<InventorySourceType>(inventory, keySelectorType) == InventorySourceType.外协退货 && outSourceReturnOrderDetail != null) ? outSourceReturnOrderDetail.ReturnQuantity :
                                            0,
                        }
                        select resultSelector.Invoke(inventory, sourceInfo);
            return query;
        }

        private Filter GenageFilter(string key, Filter filter)
        {
            var res = new Filter(key, filter.Value ?? "", filter.FilterType, filter.DataType);
            return res;
        }

        public void InsertDataToTempInventoryKeys(List<Temp_Inventory_Keys> keys)
        {
            DropTempInventoryKeyTable();
            _repository.DbContext.Database.ExecuteSqlRaw(@"
                CREATE TEMPORARY TABLE Temp_Inventory_Keys
                (
                    Id BIGINT NOT NULL AUTO_INCREMENT,

                    DataType INT NOT NULL,
                    DataId BIGINT NOT NULL,
                    DataStatus INT NOT NULL,
                    SupplierId BIGINT NULL,

                    PRIMARY KEY (Id),

                    UNIQUE KEY UK_InventoryKey
                    (
                        DataType,
                        DataId,
                        DataStatus,
                        SupplierId
                    )
                )");
            _repository.DbContext.Set<Temp_Inventory_Keys>().AddRange(keys);
            _unitOfWork.SaveChange();
        }

        public void DropTempInventoryKeyTable()
        {
            _repository.DbContext.Database.ExecuteSqlRaw("DROP TEMPORARY TABLE IF EXISTS Temp_Inventory_Keys");
        }

        public void ClearTempInventoryKeyTable()
        {
            _repository.DbContext.Database.ExecuteSqlRaw("DELETE FROM Temp_Inventory_Keys");
        }

        public List<InventoryUnitShowDto> GetInventoryUnitShowDtoList(Inventory_Record? record,string? InventoryUnitInfo, List<Inventory_OccupyDetail>? occupyList)
        {
            if (record == null)
            {
                return new List<InventoryUnitShowDto>();
            }

            var inventoryUnitList = InventoryUnitInfo != null ? JsonSerializer.Deserialize<List<InventoryUnitShowDto>>(InventoryUnitInfo ?? "[]", JsonHelper.DefaultJsonSerializerOptions)! : new List<InventoryUnitShowDto>();
            inventoryUnitList.ForEach(e =>
            {
                //出/入库数量 直接读取unitInfo
                e.Quantity = GetQtyFromUnitInfo(record.SourceType, InventoryUnitInfo, e.Id);//formatDigital4(record.Quantity / e.Ratio);
                //使用数量 通过RL的unitInfo统计
                e.UsedQuantity = record.InventoryType == InventoryType.入库 || record.InventoryType == InventoryType.报废入库 || record.InventoryType == InventoryType.改制采购入库 || record.InventoryType == InventoryType.线边入库 ?
                                record.TargetInventoryRecords.Sum(x => GetQtyFromUnitInfo(x.Type, x.InventoryUnitInfo, e.Id)) : //formatDigital4(record.UsedQuantity / e.Ratio) :
                                 e.Quantity;
                e.RemainQuantity = record.InventoryType == InventoryType.入库 || record.InventoryType == InventoryType.报废入库 || record.InventoryType == InventoryType.改制采购入库 || record.InventoryType == InventoryType.线边入库 ?
                                e.Quantity - e.UsedQuantity :
                                0;
                //可用数量 通过occupy的unitInfo统计
                e.AvailableQuantity = e.RemainQuantity - occupyList?.Sum(x => GetQtyFromUnitInfo(InventorySourceType.送货出库, x.UnitInfo, e.Id)) ?? 0;//formatDigital4((record.RemainQuantity - occupyQuantity) / e.Ratio);
            });
            inventoryUnitList = inventoryUnitList.OrderBy(e => e.Id).ToList();
            return inventoryUnitList;
        }
        private decimal GetQtyFromUnitInfo(InventorySourceType sourceType, string? unitInfo, long conversionId)
        {
            if (sourceType == InventorySourceType.盘库盈亏)
            {
                var unitList = unitInfo != null ? JsonSerializer.Deserialize<List<InventoryUnitCheckDto>>(unitInfo ?? "[]", JsonHelper.DefaultJsonSerializerOptions)! : new List<InventoryUnitCheckDto>();
                var qty = Math.Abs(unitList.FirstOrDefault(x => x.Id == conversionId)?.PAndLQuantity ?? 0);
                return formatDigital4(qty);
            }
            if (sourceType == InventorySourceType.直接入库 || sourceType == InventorySourceType.采购入库 || sourceType == InventorySourceType.移库入库
                || sourceType == InventorySourceType.报工入库 || sourceType == InventorySourceType.线边入库 || sourceType == InventorySourceType.外协入库
                || sourceType == InventorySourceType.生产入库 || sourceType == InventorySourceType.退料入库 || sourceType == InventorySourceType.退货入库
                || sourceType == InventorySourceType.包装入库 || sourceType == InventorySourceType.工模治具入库 || sourceType == InventorySourceType.遗失报废
                || sourceType == InventorySourceType.损耗报废 || sourceType == InventorySourceType.工模治具置换入库 || sourceType == InventorySourceType.成品报废入库
                || sourceType == InventorySourceType.异常入库 || sourceType == InventorySourceType.维修入库 || sourceType == InventorySourceType.单本异常入库
                || sourceType == InventorySourceType.改制采购入库 || sourceType == InventorySourceType.改制外协退货入库)
            {
                var unitList = unitInfo != null ? JsonSerializer.Deserialize<List<InventoryUnitInboundDto>>(unitInfo ?? "[]", JsonHelper.DefaultJsonSerializerOptions)! : new List<InventoryUnitInboundDto>();
                var qty = unitList.FirstOrDefault(x => x.Id == conversionId)?.Quantity;
                return formatDigital4(qty);
            }
            if (sourceType == InventorySourceType.直接出库 || sourceType == InventorySourceType.生产发料 || sourceType == InventorySourceType.送货出库
                || sourceType == InventorySourceType.移库出库 || sourceType == InventorySourceType.发货调拨 || sourceType == InventorySourceType.外协出库
                || sourceType == InventorySourceType.采购退货 || sourceType == InventorySourceType.外协退货 || sourceType == InventorySourceType.产品拣选
                || sourceType == InventorySourceType.工模治具置换出库 || sourceType == InventorySourceType.工模治具领用出库 || sourceType == InventorySourceType.单本异常出库
                || sourceType == InventorySourceType.改制采购出库 || sourceType == InventorySourceType.改制采购退货 || sourceType == InventorySourceType.异常出库 || sourceType == InventorySourceType.客户库存出库)
            {
                var unitList = unitInfo != null ? JsonSerializer.Deserialize<List<InventoryUnitOutboundDto>>(unitInfo ?? "[]", JsonHelper.DefaultJsonSerializerOptions)! : new List<InventoryUnitOutboundDto>();
                var qty = unitList.FirstOrDefault(x => x.Id == conversionId)?.OutboundQuantity;
                return formatDigital4(qty);
            }
            return 0;
        }
        //最多保留4位小数
        public decimal formatDigital4(decimal? data)
        {
            if (data == null)
            {
                return 0;
            }
            return Math.Round((decimal)data!, 4, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 获取指定库房区域的 物料库存列表
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="sortInfoList"></param>
        /// <returns></returns>
        public List<MaterialInventoryDto> GetAreaMaterialInventoryList(GetPositionInventoryDto dto, List<SortInfo>? sortInfoList)
        {
            var query = _repository.GetAll().Include(x => x.Supplier).Include(x => x.OccupyList)
                       .ApplyFilters(dto.InventoryFilterList)
                       .Where(x => x.InventoryType == InventoryType.入库 && x.RemainQuantity > 0);
            var postionQuery = _positionRepository.GetAll();
            if (dto.AreaIds != null)
            {
                postionQuery = postionQuery.Where(x => dto.AreaIds.Contains(x.Location.StorageLocation_AreaId));
            }
            if (dto.PositionId.HasValue)
            {
                postionQuery = postionQuery.Where(x => x.Id == dto.PositionId.Value);
            }
            if(dto.PositionIds!=null && dto.PositionIds.Count>0)
            {
                postionQuery = postionQuery.Where(x => dto.PositionIds.Contains(x.Id));
            }
            query = from inventory in query
                    join position in postionQuery
                    on inventory.PositionId equals position.Id
                    select inventory;
            if (dto.ExtFilterList.Count > 0)
            {
                var q1 = from record in query
                         where record is Inventory_Record_Purchase_Ext
                         join purchaseRecord in _inventoryPurchaseExtRepository.GetAll().ApplyFilters(dto.ExtFilterList)
                            on record.Id equals purchaseRecord.Id
                         select record;
                var q2 = from record in query
                         where record is Inventory_Record_Production_Ext
                         join productionRecord in _inventoryProductionExtRepository.GetAll().ApplyFilters(dto.ExtFilterList)
                            on record.Id equals productionRecord.Id
                         select record;
                query = q1.Union(q2);
            }
            var queryWithSource = this.JoinWithSource(query, "SourceType", "SourceId",
                (inventory, source) => new InventorySourceTmpDto
                {
                    Inventory = inventory,
                    SourceNo = source.sourceNo,
                    DataType = inventory.DataType,
                    DataId = inventory.DataId,
                }, false, dto.SourceFilterList.Count > 0 ? true : false, dto.SourceFilterList);
            var result = _utils.JoinWithData(queryWithSource, "DataType", "DataId",
                (inventory, materialInfo) => new MaterialInventoryDto
                {
                    Id = inventory.Inventory.Id,
                    RecordNo = inventory.Inventory.RecordNo,
                    CreateTime = inventory.Inventory.CreateTime,
                    Quantity = inventory.Inventory.Quantity,
                    UsedQuantity = inventory.Inventory.UsedQuantity,
                    RemainQuantity = inventory.Inventory.RemainQuantity,
                    AvailableQuantity = inventory.Inventory.RemainQuantity - inventory.Inventory.OccupyList.Where(x => !x.IsCompleted).Sum(x => x.OccupyQuantity - x.ReleaseQuantity),
                    Price = inventory.Inventory.Price,
                    DataType = inventory.DataType,
                    DataId = inventory.DataId,
                    CreateUserName = inventory.Inventory.CreateUserName,
                    Remark = inventory.Inventory.Remark,
                    SourceType = inventory.Inventory.SourceType,
                    MaterialNo = materialInfo.DataNo,
                    MaterialName = materialInfo.DataName,
                    MaterialCategory = materialInfo.DataCategory,
                    MaterialTechnicalCode = materialInfo.DataTechnicalCode,
                    MaterialSpecification = materialInfo.DataSpecification,
                    MaterialGrade = materialInfo.DataMaterialGrade,
                    MaterialUnit = materialInfo.DataUnit,
                    DataStatus = inventory.Inventory.DataStatus,
                    SupplierId = inventory.Inventory.SupplierId,
                    SupplierName = inventory.Inventory.Supplier != null ? inventory.Inventory.Supplier.CustomerName : "",
                    CustomAttribute = inventory.Inventory is Inventory_Record_Production_Ext ? (inventory.Inventory as Inventory_Record_Production_Ext)!.CustomAttribute :
                            inventory.Inventory is Inventory_Record_Purchase_Ext ? (inventory.Inventory as Inventory_Record_Purchase_Ext)!.CustomAttribute : null,
                    OccupyQuantity = inventory.Inventory.OccupyList.Where(x => !x.IsCompleted).Sum(x => x.OccupyQuantity - x.ReleaseQuantity),
                    SaleOrderDetailId = inventory.Inventory is Inventory_Record_Production_Ext ? (inventory.Inventory as Inventory_Record_Production_Ext)!.SaleOrderDetailId : null,
                    CustomerId = inventory.Inventory is Inventory_Record_Production_Ext ? (inventory.Inventory as Inventory_Record_Production_Ext)!.CustomerId : null,
                    SourceNo = inventory.SourceNo,
                    InventoryUnitInfo = inventory.Inventory.InventoryUnitInfo,
                    PositionId = inventory.Inventory.PositionId,
                    ProductId = materialInfo.DataProductId ?? 0,
                    InventoryType = inventory.Inventory.InventoryType
                }, false, true, dto.MaterialFilterList);
            if (sortInfoList != null)
            {
                result = result.ApplySorts(sortInfoList);
            }
            var data = result.ToList();

            var inventoryIds = result.Select(x => x.Id).Distinct().ToList();
            var inventoryData = _repository.GetAll().Include(x => x.OccupyList)
                                .Include(x => x.Position).ThenInclude(x => x.Location).ThenInclude(x => x.Area)
                                .Include(x => x.TargetInventoryRecords)
                                    .Where(x => inventoryIds.Contains(x.Id)).ToList();
            foreach (var item in data)
            {
                var inventory = inventoryData.FirstOrDefault(e => e.Id == item.Id);
                var occupyList = inventory?.OccupyList.Where(x => !x.IsCompleted).ToList();
                var inventoryUnitList = this.GetInventoryUnitShowDtoList(inventory, item.InventoryUnitInfo, occupyList);
                item.InventoryUnitList = inventoryUnitList;
                item.AreaId = inventory?.Position != null ? inventory.Position.Location.StorageLocation_AreaId : null;
                item.PositionInfo = inventory?.Position != null ? $"{inventory.Position.Location.Area.AreaNo ?? ""}{inventory.Position.Location.Area.Name}-{inventory.Position.Location.Name}-{inventory.Position.Name}" : null;
            }
            return data;
        }

        #region 更新总库存
        public void UpdateDataInventory(List<Temp_Inventory_Keys> keys, List<long> positionIds)
        {
            keys = keys.DistinctBy(x => new { x.DataType, x.DataId, x.DataStatus, x.SupplierId }).ToList();
            if (keys.Count > 0)
            {
                this.InsertDataToTempInventoryKeys(keys);
                //更新Data总库存
                var inventoryData = (from inventory in _repository.GetAll().Where(x => x.RemainQuantity > 0 && x.InventoryType == InventoryType.入库)
                                     join data in _tempInventoryKeysRepository.GetAll()
                                          on new { inventory.DataType, inventory.DataId } equals new { data.DataType, data.DataId }
                                     group inventory by new { inventory.DataType, inventory.DataId, inventory.DataStatus } into g
                                     select new
                                     {
                                         DataType = g.Key.DataType,
                                         DataId = g.Key.DataId,
                                         DataStatus = g.Key.DataStatus,
                                         NormalQuantity = g.Where(x => x.SourceType != InventorySourceType.异常入库).Sum(x => x.RemainQuantity),
                                         AbnormalQuantity = g.Where(x => x.SourceType == InventorySourceType.异常入库).Sum(x => x.RemainQuantity)
                                     }).ToList();
                this.DropTempInventoryKeyTable();
                var productIds = keys.Where(x => x.DataType == InventoryDataType.Product).Select(x => x.DataId).ToList();
                var specIds = keys.Where(x => x.DataType == InventoryDataType.Specification).Select(x => x.DataId).ToList();
                var productData = _productRepository.GetAll(true).Where(x => productIds.Contains(x.Id)).ToList();
                var specificatioData = _specificationRepository.GetAll(true).Where(x => specIds.Contains(x.Id)).ToList();
                foreach (var key in keys)
                {
                    switch (key.DataType)
                    {
                        case InventoryDataType.Product:
                            var data = productData.FirstOrDefault(x => x.Id == key.DataId);
                            if (data != null)
                            {
                                data.InventoryQuantity = inventoryData.Where(x => x.DataType == InventoryDataType.Product && x.DataId == data.Id).Sum(x => x.NormalQuantity);
                                data.PackageQuantity = inventoryData.Where(x => x.DataType == InventoryDataType.Product && x.DataId == data.Id && x.DataStatus == InventoryDataStatus.已包装).Sum(x => x.NormalQuantity);
                                data.UnPackageQuantity = inventoryData.Where(x => x.DataType == InventoryDataType.Product && x.DataId == data.Id && x.DataStatus == InventoryDataStatus.待包装).Sum(x => x.NormalQuantity);
                                data.AbnormalQuantity = inventoryData.Where(x => x.DataType == InventoryDataType.Product && x.DataId == data.Id).Sum(x => x.AbnormalQuantity);
                            }
                            break;
                        case InventoryDataType.Specification:
                            var specData = specificatioData.FirstOrDefault(x => x.Id == key.DataId);
                            if (specData != null)
                            {
                                specData.SpecificationInventoryQuantity = inventoryData.Where(x => x.DataType == InventoryDataType.Specification && x.DataId == specData.Id).Sum(x => x.NormalQuantity);
                                specData.AbnormalQuantity = inventoryData.Where(x => x.DataType == InventoryDataType.Specification && x.DataId == specData.Id).Sum(x => x.AbnormalQuantity);
                            }
                            break;
                        case InventoryDataType.WorkFixture:
                            //????????????
                            break;
                    }
                }
            }
            //更新库位容量
            var positionSummaryData = (from inventory in _repository.GetAll().Where(x => x.RemainQuantity > 0 &&
                                            (x.InventoryType == InventoryType.入库 || x.InventoryType == InventoryType.报废入库 || x.InventoryType == InventoryType.改制采购入库) &&
                                            positionIds.Contains(x.PositionId ?? 0))
                                       group inventory by inventory.PositionId into g
                                       select new
                                       {
                                           positionId = g.Key,
                                           quantity = g.Sum(x => x.RemainQuantity)
                                       }).ToList();
            var positionData = _positionRepository.GetAll().Where(x => positionIds.Contains(x.Id)).ToList();
            foreach (var item in positionData)
            {
                item.UsedCapacity = positionSummaryData.FirstOrDefault(x => x.positionId == item.Id)?.quantity ?? 0;
            }
        }
        public List<long> UpdateInventoryUsed(List<long> inventoryIds)
        {
            //入库记录的使用量
            var inventoryData = _repository.GetAll().Where(x => inventoryIds.Contains(x.Id)).ToList();
            var usedData = _inventoryRecordRLRepository.GetAll().Where(x => inventoryIds.Contains(x.SourceInventoryId)).ToList();
            foreach (var item in inventoryData)
            {
                var usedQuantity = usedData.Where(x => x.SourceInventoryId == item.Id).Sum(x => x.Quantity);
                if (usedQuantity > item.Quantity)
                {
                    throw new Exception($"数据错误，{item.RecordNo}出库数量不能大于实际库存，请刷新重试");
                }
                item.UsedQuantity = usedQuantity;
            }
            return inventoryData.Select(s => s.PositionId ?? 0).Distinct().ToList();
        }
        #endregion

        #region 更新最近盘点时间
        public void UpdateDataRecentCheck(List<Temp_Inventory_Keys> keys, long areaId)
        {
            keys = keys.DistinctBy(x => new { x.DataType, x.DataId, x.DataStatus, x.SupplierId }).ToList();
            if (keys.Count <= 0)
            {
                return;
            }
            this.InsertDataToTempInventoryKeys(keys);
            var allMaterialCheckData = (from material in _materialCheckInfoService.GetAll().Where(x => x.AreaId == areaId)
                                        join key in _tempInventoryKeysRepository.GetAll()
                                            on new { material.DataType, material.DataId, material.DataStatus, material.SupplierId }
                                            equals new { key.DataType, key.DataId, key.DataStatus, key.SupplierId }
                                        select material).ToList();
            var currentCheckRecord = _inventoryCheckRepository.GetAll().OrderByDescending(x => x.Id).FirstOrDefault(x => x.AreaId == areaId);
            var currentCheckDetailData = (from checkDetail in _inventoryCheckDetailRepository.GetAll().Where(x => x.CheckRecord.AreaId == areaId)
                                          join key in _tempInventoryKeysRepository.GetAll()
                                                                      on new { checkDetail.DataType, checkDetail.DataId, checkDetail.DataStatus, checkDetail.SupplierId }
                                                                      equals new { key.DataType, key.DataId, key.DataStatus, key.SupplierId }
                                          group checkDetail by new { checkDetail.DataType, checkDetail.DataId, checkDetail.DataStatus, checkDetail.SupplierId } into g
                                          select new
                                          {
                                              DataType = g.Key.DataType,
                                              DataId = g.Key.DataId,
                                              DataStatus = g.Key.DataStatus,
                                              SupplierId = g.Key.SupplierId,
                                              LastCheckTime = g.Max(x => x.CheckRecord.CheckTime)
                                          }).ToList();
            this.DropTempInventoryKeyTable();

            foreach (var item in allMaterialCheckData)
            {
                var recentCheck = currentCheckDetailData.FirstOrDefault(x => x.DataType == item.DataType && x.DataId == item.DataId && x.DataStatus == item.DataStatus && x.SupplierId == item.SupplierId);
                item.LastCheckTime = recentCheck?.LastCheckTime;
            }
        }
        #endregion
        class InventoryRecordWithOrderDetailId
        {
            public long GoodsId { get; set; }
            public long SaleOrderDetailId { get; set; }
            public long DeliveryOrderDetailId { get; set; }
            public Inventory_Record InventoryRecord { get; set; } = null!;
            public Inventory_Position? Position { get; set; }
            public Inventory_Location? Location { get; set; }
            public Inventory_Area? Area { get; set; }
            public required long? AssignSaleOrderDetailId { get; set; }
            public required string? CustomAttribute { get; set; }
            public required List<Inventory_OccupyDetail>? OccupyList { get; set; }

            public InventorySourceType SourceType { get; set; }
            public long SourceId { get; set; }

            public string? SourceNo { get; set; }
        }

        /// <summary>
        /// 根据销售订单详情Id获取对应的库存记录占用信息
        /// </summary>
        /// <param name="saleOrderDetailIds"></param>
        /// <returns></returns>
        public List<InventoryRecordOccupyDetailDto> GetInventoryRecordOccupyInfo(long[] saleOrderDetailIds)
        {
            //通过销售订单明细的 SpecificationId/ProductId 确定对应的物料(DataId/DataType)，再关联库存记录
            var inventoryRecordQuery = from saleOrderDetail in _saleOrderDetailRepository.GetAll()
                                            .Where(o => saleOrderDetailIds.Contains(o.Id))
                                       let DataId = saleOrderDetail.SpecificationId.HasValue ? saleOrderDetail.SpecificationId.Value : saleOrderDetail.ProductId
                                       let DataType = saleOrderDetail.SpecificationId.HasValue ? InventoryDataType.Specification : InventoryDataType.Product
                                       join inventoryRecord in _repository.GetAll()
                                          .Where(o => o.InventoryType == InventoryType.入库
                                              && o.RemainQuantity > 0
                                              && o.Position!.Location.Area.Purpose == WarehousePurpose.普通仓
                                              && o.Position!.Location.Area.Features.HasFlag(WarehouseFeature.送货业务))
                                       on new { DataId, DataType } equals new { inventoryRecord.DataId, inventoryRecord.DataType }
                                       select new InventoryRecordWithOrderDetailId
                                       {
                                           SaleOrderDetailId = saleOrderDetail.Id,
                                           InventoryRecord = inventoryRecord,
                                           Position = inventoryRecord.Position,
                                           Location = inventoryRecord.Position != null ? inventoryRecord.Position.Location : null,
                                           Area = inventoryRecord.Position != null ? inventoryRecord.Position.Location.Area : null,
                                           AssignSaleOrderDetailId = inventoryRecord is Inventory_Record_Production_Ext ? (inventoryRecord as Inventory_Record_Production_Ext)!.SaleOrderDetailId : null,
                                           CustomAttribute = inventoryRecord is Inventory_Record_Production_Ext ? (inventoryRecord as Inventory_Record_Production_Ext)!.CustomAttribute : null,
                                           OccupyList = inventoryRecord.OccupyList.Where(o => !o.IsCompleted).ToList(),
                                           SourceType = inventoryRecord.SourceType,
                                           SourceId = inventoryRecord.SourceId,
                                       };

            return this.BuildInventoryRecordOccupyDetailList(inventoryRecordQuery);
        }

        /// <summary>
        /// 根据客户产品Id(GoodsId)列表批量获取对应的库存记录占用信息（按 goodsId 分组返回）
        /// </summary>
        /// <param name="goodsIds"></param>
        /// <returns></returns>
        public Dictionary<long, List<InventoryRecordOccupyDetailDto>> GetInventoryRecordOccupyInfoByGoodsIds(long[] goodsIds)
        {
            //没有对应的 goods 直接返回失败
            var goodsQuery = _goodsRepository.GetAll().Where(o => goodsIds.Contains(o.Id));
            if (!goodsQuery.Any())
            {
                throw new Exception("未找到对应的客户产品(Goods)，无法查询可用库存");
            }

            //通过客户产品 goods 的 SpecificationId/ProductId 确定对应的物料(DataId/DataType)，再关联库存记录
            var inventoryRecordQuery = from goods in goodsQuery
                                       let DataId = goods.SpecificationId.HasValue ? goods.SpecificationId.Value : goods.ProductId
                                       let DataType = goods.SpecificationId.HasValue ? InventoryDataType.Specification : InventoryDataType.Product
                                       join inventoryRecord in _repository.GetAll()
                                          .Where(o => o.InventoryType == InventoryType.入库
                                              && o.RemainQuantity > 0
                                              && o.Position!.Location.Area.Purpose == WarehousePurpose.普通仓
                                              && o.Position!.Location.Area.Features.HasFlag(WarehouseFeature.送货业务))
                                       on new { DataId, DataType } equals new { inventoryRecord.DataId, inventoryRecord.DataType }
                                       select new InventoryRecordWithOrderDetailId
                                       {
                                           GoodsId = goods.Id,
                                           InventoryRecord = inventoryRecord,
                                           Position = inventoryRecord.Position,
                                           Location = inventoryRecord.Position != null ? inventoryRecord.Position.Location : null,
                                           Area = inventoryRecord.Position != null ? inventoryRecord.Position.Location.Area : null,
                                           AssignSaleOrderDetailId = inventoryRecord is Inventory_Record_Production_Ext ? (inventoryRecord as Inventory_Record_Production_Ext)!.SaleOrderDetailId : null,
                                           CustomAttribute = inventoryRecord is Inventory_Record_Production_Ext ? (inventoryRecord as Inventory_Record_Production_Ext)!.CustomAttribute : null,
                                           OccupyList = inventoryRecord.OccupyList.Where(o => !o.IsCompleted).ToList(),
                                           SourceType = inventoryRecord.SourceType,
                                           SourceId = inventoryRecord.SourceId,
                                       };

            var inventoryRecordList = this.BuildInventoryRecordOccupyDetailList(inventoryRecordQuery);
            return inventoryRecordList
                .GroupBy(o => o.GoodsId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        //共用方法：根据已关联好物料的库存记录，统计占用数量并生成库存记录占用明细（销售订单/客户产品两个入口共用）
        private List<InventoryRecordOccupyDetailDto> BuildInventoryRecordOccupyDetailList(IQueryable<InventoryRecordWithOrderDetailId> inventoryRecordQuery)
        {
            //所有可使用的库存记录
            var canUseInventoryRecordList = this.JoinWithSource(inventoryRecordQuery, "SourceType", "SourceId", (inventory, source) =>
                   new InventoryRecordWithOrderDetailId
                   {
                       GoodsId = inventory.GoodsId,
                       SaleOrderDetailId = inventory.SaleOrderDetailId,
                       InventoryRecord = inventory.InventoryRecord,
                       Position = inventory.Position,
                       Location = inventory.Location,
                       Area = inventory.Area,
                       AssignSaleOrderDetailId = inventory.AssignSaleOrderDetailId,
                       CustomAttribute = inventory.CustomAttribute,
                       SourceNo = source.sourceNo,
                       OccupyList = inventory.OccupyList
                   }).ToList();

            //获取所有库存记录对应的，占用数量
            var inventoryRecordIds = canUseInventoryRecordList.Select(o => o.InventoryRecord.Id).ToList();

            var occupyQuantityList = (from occupyDetail in _occupyDetailRepository.GetAll()
                                .Where(o => o.Occupy.SourceType == Shared.DataBase.Enum.OccupySourceType.销售送货
                                    && inventoryRecordIds.Contains(o.InventoryRecordId))
                                      group occupyDetail by occupyDetail.InventoryRecordId into g
                                      select new
                                      {
                                          InventoryRecordId = g.Key,
                                          OccupyQuantity = g.Sum(o => o.OccupyQuantity - o.ReleaseQuantity)
                                      }).ToList();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            //拿到所有的库存记录对应的占用明细
            var inventoryOccupyDetailList = (from inventoryRecord in canUseInventoryRecordList
                                             join occupyDetail in occupyQuantityList
                                                 on inventoryRecord.InventoryRecord.Id equals occupyDetail.InventoryRecordId into occupyDetailTmp
                                             from occupyDetail in occupyDetailTmp.DefaultIfEmpty()
                                             select new InventoryRecordOccupyDetailDto
                                             {
                                                 GoodsId = inventoryRecord.GoodsId,
                                                 InventoryRecordId = inventoryRecord.InventoryRecord.Id,
                                                 InventoryRecordNo = inventoryRecord.InventoryRecord.RecordNo,
                                                 PositionId = inventoryRecord.InventoryRecord.PositionId,
                                                 InventoryCustomAttribute = inventoryRecord.CustomAttribute,
                                                 InventoryCustomAttributeList = JsonSerializer.Deserialize<List<CustomAttributeShowDto>>(inventoryRecord.CustomAttribute ?? "[]", options) ?? new List<CustomAttributeShowDto>(),
                                                 PositionName = inventoryRecord.Position?.Name,
                                                 LocationName = inventoryRecord.Position?.Location.Name,
                                                 AreaName = inventoryRecord.Position?.Location.Area.Name,
                                                 InboundQuantity = inventoryRecord.InventoryRecord.Quantity,
                                                 UsedQuantity = inventoryRecord.InventoryRecord.UsedQuantity,
                                                 RemainingQuantity = inventoryRecord.InventoryRecord.RemainQuantity,
                                                 OccupyQuantity = occupyDetail != null ? occupyDetail.OccupyQuantity : 0,
                                                 SaleOrderDetailId = inventoryRecord.SaleOrderDetailId,
                                                 AssignSaleOrderDetailId = inventoryRecord.AssignSaleOrderDetailId,
                                                 CreateTime = inventoryRecord.InventoryRecord.CreateTime,
                                                 //获取库存对应的单位信息
                                                 InventoryUnitsInfoList = this.GetInventoryUnitShowDtoList(inventoryRecord.InventoryRecord, inventoryRecord.InventoryRecord.InventoryUnitInfo, inventoryRecord.OccupyList),
                                                 InventoryInputUnitConversionId = inventoryRecord.InventoryRecord.InputUnitConversionId,
                                                 InventoryInputQuantity = inventoryRecord.InventoryRecord.InputQuantity,
                                                 DeliveryDetailOccupyQuantity = 0,
                                                 OriginOccupyQuantity = occupyDetail != null ? occupyDetail.OccupyQuantity : 0,
                                                 OriginRemainingQuantity = inventoryRecord.InventoryRecord.RemainQuantity,
                                                 InventorySourceType = inventoryRecord.InventoryRecord.SourceType,
                                                 InventorySourceNo = inventoryRecord.SourceNo,
                                             })
                                             .OrderByDescending(o => o.SaleOrderDetailId == o.AssignSaleOrderDetailId)
                                             .ThenBy(o => o.CreateTime)
                                             .ToList();

            this._SetInventoryCustomAttributeName(inventoryOccupyDetailList);
            
            return inventoryOccupyDetailList;
        }
        public List<InventoryRecordOccupyDetailDto> GetInventoryRecordOccupyInfo(long deliveryOrderId)
        {
            //获取送货通知单明细中所有物料对应的所有在库库存记录（入库记录&& RemainQuantity >0），包括产品和规格
            var inventoryRecordQuery = from _deliveryOrderDetail in _deliveryOrderDetailRepository.GetAll()
                                                 .Where(o => o.DeliveryOrderId == deliveryOrderId)
                                              let DataId = _deliveryOrderDetail.SpecificationId.HasValue? _deliveryOrderDetail.SpecificationId.Value: _deliveryOrderDetail.ProductId
                                              let DataType = _deliveryOrderDetail.SpecificationId.HasValue ? InventoryDataType.Specification : InventoryDataType.Product
                                              join inventoryRecord in _repository.GetAll()
                                                 .Include(o=>o.OccupyList)
                                                 .Where(o =>o.InventoryType == InventoryType.入库
                                                     && o.RemainQuantity > 0
                                                     && o.Position!.Location.Area.Purpose == WarehousePurpose.普通仓
                                                     && o.Position!.Location.Area.Features.HasFlag(WarehouseFeature.送货业务))
                                                 on new { DataId, DataType } equals new { inventoryRecord.DataId, inventoryRecord.DataType }
                                              select new InventoryRecordWithOrderDetailId
                                              {
                                                  DeliveryOrderDetailId = _deliveryOrderDetail.Id,
                                                  SaleOrderDetailId = _deliveryOrderDetail.SaleOrderDetailId,
                                                  InventoryRecord = inventoryRecord,
                                                  Position = inventoryRecord.Position,
                                                  Location = inventoryRecord.Position != null ? inventoryRecord.Position.Location : null,
                                                  Area = inventoryRecord.Position != null ? inventoryRecord.Position.Location.Area : null,
                                                  AssignSaleOrderDetailId = inventoryRecord is Inventory_Record_Production_Ext ? (inventoryRecord as Inventory_Record_Production_Ext)!.SaleOrderDetailId : null,
                                                  CustomAttribute = inventoryRecord is Inventory_Record_Production_Ext ? (inventoryRecord as Inventory_Record_Production_Ext)!.CustomAttribute : null,
                                                  SourceType = inventoryRecord.SourceType,
                                                  SourceId = inventoryRecord.SourceId,
                                                  OccupyList = inventoryRecord.OccupyList.Where(o => !o.IsCompleted).ToList()
                                              };
            var canUseInventoryRecordList = this.JoinWithSource(inventoryRecordQuery, "SourceType", "SourceId", (inventory, source) =>
                    new InventoryRecordWithOrderDetailId
                    {
                        DeliveryOrderDetailId = inventory.DeliveryOrderDetailId,
                        SaleOrderDetailId = inventory.SaleOrderDetailId,
                        InventoryRecord = inventory.InventoryRecord,
                        Position = inventory.Position,
                        Location = inventory.Location,
                        Area = inventory.Area,
                        AssignSaleOrderDetailId = inventory.AssignSaleOrderDetailId,
                        CustomAttribute = inventory.CustomAttribute,
                        SourceNo = source.sourceNo,
                        OccupyList = inventory.OccupyList
                    }).ToList();

            //送货通知明细
            var deliveryOrderDetailQuery = _deliveryOrderDetailRepository.GetAll()
                                                .Where(o => o.DeliveryOrderId == deliveryOrderId);


            //送货通知明细对应的已出库的出库记录
            var alreadyOutboundInventoryRecordQuery = _repository.GetAll()
                                                        .Where(o => o.InventoryType == InventoryType.出库
                                                            && o.SourceType == InventorySourceType.送货出库
                                                            && deliveryOrderDetailQuery.Any(p => p.Id == o.SourceId));


            var alreadyOutboundInventoryRecordList = (from outboundRecord in alreadyOutboundInventoryRecordQuery
                                                      join deliveryOrderDetail in deliveryOrderDetailQuery
                                                          on outboundRecord.SourceId equals deliveryOrderDetail.Id
                                                      group outboundRecord by deliveryOrderDetail.Id into g
                                                      select new
                                                      {
                                                          DeliveryOrderDetailId = g.Key,
                                                          DeliveryOutboundQuantity = g.Sum(o => o.Quantity)
                                                      }).ToList();
            //出库记录 使用的 入库记录
            var usedInboundInventoryRecordQuery = from outboundRecord in alreadyOutboundInventoryRecordQuery
                                                  join inventoryRL in _inventoryRecordRLRepository.GetAll()
                                                      on outboundRecord.Id equals inventoryRL.TargetInventoryId
                                                  join inboundRecord in _repository.GetAll()
                                                      on inventoryRL.SourceInventoryId equals inboundRecord.Id
                                                  select new InventoryRecordWithOrderDetailId
                                                  {
                                                      SaleOrderDetailId = outboundRecord.SourceId,
                                                      InventoryRecord = inboundRecord,
                                                      Position = inboundRecord.Position,
                                                      Location = inboundRecord.Position != null ? inboundRecord.Position.Location : null,
                                                      Area = inboundRecord.Position != null ? inboundRecord.Position.Location.Area : null,
                                                      AssignSaleOrderDetailId = inboundRecord is Inventory_Record_Production_Ext ? (inboundRecord as Inventory_Record_Production_Ext)!.SaleOrderDetailId : null,
                                                      CustomAttribute = inboundRecord is Inventory_Record_Production_Ext ? (inboundRecord as Inventory_Record_Production_Ext)!.CustomAttribute : null,
                                                      SourceType = inboundRecord.SourceType,
                                                      SourceId = inboundRecord.SourceId,
                                                      OccupyList = inboundRecord.OccupyList.Where(o => !o.IsCompleted).ToList()
                                                  };

            var usedInboundInventoryRecordList = this.JoinWithSource(usedInboundInventoryRecordQuery, "SourceType", "SourceId", (inventory, source) =>
                    new InventoryRecordWithOrderDetailId
                    {
                        SaleOrderDetailId = inventory.SaleOrderDetailId,
                        InventoryRecord = inventory.InventoryRecord,
                        Position = inventory.Position,
                        Location = inventory.Location,
                        Area = inventory.Area,
                        AssignSaleOrderDetailId = inventory.AssignSaleOrderDetailId,
                        CustomAttribute = inventory.CustomAttribute,
                        SourceNo = source.sourceNo,
                        OccupyList = inventory.InventoryRecord.OccupyList.Where(o => !o.IsCompleted).ToList()
                    }).ToList();

            //送货已占用 但是没有余量的库存
            var alreadyOccupyInventoryWithNoRemain = this.JoinWithSource(from inventory in _repository.GetAll().Where(x => x.InventoryType == InventoryType.入库)
                                                                         join occupy in _occupyDetailRepository.GetAll().Where(x => (x.Occupy.SourceType == OccupySourceType.销售送货 || x.Occupy.SourceType == OccupySourceType.库存发货暂存))
                                                                            on inventory.Id equals occupy.InventoryRecordId
                                                                         join delivery in deliveryOrderDetailQuery
                                                                             on occupy.Occupy.SourceId equals delivery.Id
                                                                         where inventory.RemainQuantity <= 0
                                                                         select new InventoryRecordWithOrderDetailId
                                                                         {
                                                                             SaleOrderDetailId = delivery.SaleOrderDetailId,
                                                                             DeliveryOrderDetailId = delivery.Id,
                                                                             InventoryRecord = inventory,
                                                                             Position = inventory.Position,
                                                                             Location = inventory.Position != null ? inventory.Position.Location : null,
                                                                             Area = inventory.Position != null ? inventory.Position.Location.Area : null,
                                                                             AssignSaleOrderDetailId = inventory is Inventory_Record_Production_Ext ? (inventory as Inventory_Record_Production_Ext)!.SaleOrderDetailId : null,
                                                                             CustomAttribute = inventory is Inventory_Record_Production_Ext ? (inventory as Inventory_Record_Production_Ext)!.CustomAttribute : null,
                                                                             SourceType = inventory.SourceType,
                                                                             SourceId = inventory.SourceId,
                                                                             OccupyList = inventory.OccupyList.Where(o => !o.IsCompleted).ToList()
                                                                         },
                                                                         "SourceType",
                                                                         "SourceId",
                                                                         (inventory, source) => new InventoryRecordWithOrderDetailId
                                                                         {
                                                                             SaleOrderDetailId = inventory.SaleOrderDetailId,
                                                                             DeliveryOrderDetailId = inventory.DeliveryOrderDetailId,
                                                                             InventoryRecord = inventory.InventoryRecord,
                                                                             Position = inventory.Position,
                                                                             Location = inventory.Location,
                                                                             Area = inventory.Area,
                                                                             AssignSaleOrderDetailId = inventory.AssignSaleOrderDetailId,
                                                                             CustomAttribute = inventory.CustomAttribute,
                                                                             SourceNo = source.sourceNo,
                                                                             OccupyList = inventory.OccupyList
                                                                         }).ToList();


            //将可使用的库存记录和已使用的库存记录合并（去重），得到最终的库存记录列表
            //这里是为了能显示已经出完了的库存记录
            canUseInventoryRecordList = canUseInventoryRecordList.Union(usedInboundInventoryRecordList).Union(alreadyOccupyInventoryWithNoRemain).ToList();

            //获取所有库存记录对应的，占用数量（总的占用量）
            var inventoryRecordIds = canUseInventoryRecordList.Select(o => o.InventoryRecord.Id).ToList();
            var occupyQuantityList = (from occupyDetail in _occupyDetailRepository.GetAll()
                                        .Where(o=> !o.IsCompleted
                                            && inventoryRecordIds.Contains(o.InventoryRecordId))
                                      group occupyDetail by occupyDetail.InventoryRecordId into g
                                      select new
                                      {
                                          InventoryRecordId = g.Key,
                                          OccupyQuantity = g.Sum(o => o.OccupyQuantity - o.ReleaseQuantity)
                                      }).ToList();

            //查询送货通知单对应的库存记录占用情况，这里不做group by；每条送货明细针对一条InventoryRecord 只会有一条占用记录
            var devlieryOccupyQuantityList = (from occupyDetail in _occupyDetailRepository.GetAll()
                                       .Where(o => o.Occupy.SourceType == OccupySourceType.销售送货 
                                            && !o.IsCompleted)
                                              join deliveryOrderDetail in deliveryOrderDetailQuery
                                                  on occupyDetail.Occupy.SourceId equals deliveryOrderDetail.Id
                                              select new
                                              {
                                                  InventoryRecordId = occupyDetail.InventoryRecordId,
                                                  DeliveryOrderDetailId = deliveryOrderDetail.Id,
                                                  OccupyQuantity = occupyDetail.OccupyQuantity,
                                                  ReleaseQuantity = occupyDetail.ReleaseQuantity,
                                                  OccupyInputUnitConversionId = occupyDetail.InputUnitConversionId,
                                                  OccupyInputQuantity = occupyDetail.InputQuantity,
                                                  OccupyUnitInfo = occupyDetail.UnitInfo
                                              }).ToList();

            //查询库存发货暂存的占用情况，这里不做group by；每条送货明细针对一条InventoryRecord 只会有一条占用记录
            var outboundOccupyQuantityList = (from occupyDetail in _occupyDetailRepository.GetAll()
                                       .Where(o => o.Occupy.SourceType == OccupySourceType.库存发货暂存
                                            && !o.IsCompleted)
                                              join deliveryOrderDetail in deliveryOrderDetailQuery
                                                  on occupyDetail.Occupy.SourceId equals deliveryOrderDetail.Id
                                              select new
                                              {
                                                  InventoryRecordId = occupyDetail.InventoryRecordId,
                                                  DeliveryOrderDetailId = deliveryOrderDetail.Id,
                                                  OccupyQuantity = occupyDetail.OccupyQuantity,
                                                  ReleaseQuantity = occupyDetail.ReleaseQuantity,
                                                  OccupyInputUnitConversionId = occupyDetail.InputUnitConversionId,
                                                  OccupyInputQuantity = occupyDetail.InputQuantity,
                                                  OccupyUnitInfo = occupyDetail.UnitInfo
                                              }).ToList();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var inventoryOccupyDetailList = (from inventoryRecord in canUseInventoryRecordList
                                             join occupyQuantity in occupyQuantityList
                                                 on inventoryRecord.InventoryRecord.Id equals occupyQuantity.InventoryRecordId into occupyDetailTmp
                                             from occupyDetail in occupyDetailTmp.DefaultIfEmpty()
                                             join deliveryOutbound in alreadyOutboundInventoryRecordList
                                                 on inventoryRecord.DeliveryOrderDetailId equals deliveryOutbound.DeliveryOrderDetailId into deliveryOutboundTmp
                                             from deliveryOutbound in deliveryOutboundTmp.DefaultIfEmpty()
                                             join deliveryOccupyQuantity in devlieryOccupyQuantityList
                                                 on new { InventoryId = inventoryRecord.InventoryRecord.Id, DeliveryOrderDetailId = inventoryRecord.DeliveryOrderDetailId }
                                                    equals new { InventoryId = deliveryOccupyQuantity.InventoryRecordId, DeliveryOrderDetailId = deliveryOccupyQuantity.DeliveryOrderDetailId } into deliveryOccupyTmp
                                             from deliveryOccupyQuantity in deliveryOccupyTmp.DefaultIfEmpty()
                                             join outboundOccupyQuantity in outboundOccupyQuantityList
                                                on new { InventoryId = inventoryRecord.InventoryRecord.Id, DeliveryOrderDetailId = inventoryRecord.DeliveryOrderDetailId }
                                                   equals new { InventoryId = outboundOccupyQuantity.InventoryRecordId, DeliveryOrderDetailId = outboundOccupyQuantity.DeliveryOrderDetailId } into outboundOccupyTmp
                                             from outboundOccupyQuantity in outboundOccupyTmp.DefaultIfEmpty()
                                             select new InventoryRecordOccupyDetailDto
                                             {
                                                 InventoryRecordId = inventoryRecord.InventoryRecord.Id,
                                                 InventoryRecordNo = inventoryRecord.InventoryRecord.RecordNo,
                                                 PositionId = inventoryRecord.InventoryRecord.PositionId,
                                                 PositionName = inventoryRecord.Position?.Name,
                                                 LocationName = inventoryRecord.Position?.Location.Name,
                                                 AreaName = inventoryRecord.Position?.Location.Area.Name,
                                                 InboundQuantity = inventoryRecord.InventoryRecord.Quantity,
                                                 UsedQuantity = inventoryRecord.InventoryRecord.UsedQuantity,
                                                 RemainingQuantity = inventoryRecord.InventoryRecord.RemainQuantity,
                                                 InventoryCustomAttribute = inventoryRecord.CustomAttribute,
                                                 InventoryCustomAttributeList = JsonSerializer.Deserialize<List<CustomAttributeShowDto>>(inventoryRecord.CustomAttribute ?? "[]", options) ?? new List<CustomAttributeShowDto>(),
                                                 InventorySourceType = inventoryRecord.InventoryRecord.SourceType,
                                                 InventorySourceNo = inventoryRecord.SourceNo,
                                                 //总的占用量
                                                 OccupyQuantity = occupyDetail != null ? occupyDetail.OccupyQuantity : 0,
                                                 OriginOccupyQuantity = occupyDetail != null ? occupyDetail.OccupyQuantity : 0,
                                                 SaleOrderDetailId = inventoryRecord.SaleOrderDetailId,
                                                 AssignSaleOrderDetailId = inventoryRecord.AssignSaleOrderDetailId,
                                                 CreateTime = inventoryRecord.InventoryRecord.CreateTime,

                                                 DeliveryOrderDetailId = inventoryRecord.DeliveryOrderDetailId,
                                                 DeliveryOrderDetailOutboundQuantity = deliveryOutbound != null && deliveryOccupyQuantity!=null ? deliveryOutbound.DeliveryOutboundQuantity != deliveryOccupyQuantity.ReleaseQuantity ? throw new Exception("已出库数量与占用释放数量不符") : deliveryOutbound.DeliveryOutboundQuantity : 0,
                                                 DeliveryDetailOccupyQuantity = deliveryOccupyQuantity != null ? deliveryOccupyQuantity.OccupyQuantity : 0,
                                                 DeliveryDetailOutboundOccupyQuantity = outboundOccupyQuantity != null ? outboundOccupyQuantity.OccupyQuantity : 0,
                                                 //获取库存对应的单位信息
                                                 InventoryUnitsInfoList = this.GetInventoryUnitShowDtoList(inventoryRecord.InventoryRecord,inventoryRecord.InventoryRecord.InventoryUnitInfo,inventoryRecord.OccupyList),
                                                 InventoryInputUnitConversionId = inventoryRecord.InventoryRecord.InputUnitConversionId,
                                                 InventoryInputQuantity = inventoryRecord.InventoryRecord.InputQuantity,
                                                 //以下是占用相关信息,在有 【销售送货】占用的情况下，不会有【库存发货暂存】占用的情况
                                                 OccupyInputUnitConversionId = outboundOccupyQuantity != null ? outboundOccupyQuantity.OccupyInputUnitConversionId : deliveryOccupyQuantity != null ? deliveryOccupyQuantity.OccupyInputUnitConversionId : null,
                                                 OccupyInputQuantity = outboundOccupyQuantity != null ? outboundOccupyQuantity.OccupyInputQuantity : deliveryOccupyQuantity != null ? deliveryOccupyQuantity.OccupyInputQuantity : null,
                                                 OccupyUnitInfo = outboundOccupyQuantity != null ? outboundOccupyQuantity.OccupyUnitInfo : deliveryOccupyQuantity != null ? deliveryOccupyQuantity.OccupyUnitInfo : "[]",
                                             })
                                             .OrderByDescending(o => o.SaleOrderDetailId == o.AssignSaleOrderDetailId)
                                             .ThenBy(o => o.CreateTime)
                                             .ToList();
            this._SetInventoryCustomAttributeName(inventoryOccupyDetailList);
            return inventoryOccupyDetailList;
        }
        private void _SetInventoryCustomAttributeName(List<InventoryRecordOccupyDetailDto> records)
        {
            var ids = records.SelectMany(o => o.InventoryCustomAttributeList.Select(o => o.systemParaId)).ToArray();
            var systemParas = _systemParaRepository.GetAll()
                .Where(o => ids.Contains(o.Id))
                .ToList()
                .ToDictionary(o => o.Id, o => o);
            records.ForEach(o =>
            {
                o.InventoryCustomAttributeList.ForEach(p => p.Name = systemParas[p.systemParaId].DataValue);
            });
        }
    }
}
