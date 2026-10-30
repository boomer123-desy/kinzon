import {
  SaleOrderType,
  SaleOrderStatus,
  ReviewStatus,
  ExamineStatus,
  UnitConversionType,
  DeliveryModeType
} from '@/types/enums'
import { CustomAttribute, CustomAttributeShowDto, type SelectOption } from '@/types/types'
import {
  FileStorage,
  BaseCustomer,
  User,
  BaseProductPackage,
  CustomerAddressDto
} from '@/types/common'
import type { InventoryUnitShowDto } from '@/views/sale_order_manage/inventory/inventory'
import { cloneDeep } from 'lodash'

export class SaleOrderDetailFlatDTO {
  id: number
  orderId: number
  orderNo: string
  orderType: SaleOrderType
  deliveryTime: Date
  remark?: string | undefined
  orderRemark?: string | undefined
  receiveUserId: number
  receiveName: string
  receiveOrderTime: Date

  status: SaleOrderStatus
  orderstatus: SaleOrderStatus
  reviewStatus: ReviewStatus
  examineStatus: ExamineStatus
  saleExtendedInfo: CustomAttribute[]

  customerId: number
  customerCode: string
  customerName: string
  customerOrderNo?: string | undefined
  customerMaterialNo?: string | undefined
  customerMaterialName?: string | undefined
  customerAddress: string

  productId: number
  productSpecificationId?: number | undefined
  productNo: string
  productName: string
  productSpecification?: string | undefined
  productTechnicalCode: string

  placeOrderQuantity: number
  price: number
  totalPrice: number
  orderTotalPrice: number
  processQuantity: number
  planProductionQuantity: number
  planPurchaseQuantity: number
  planDirectDliveryQuantity: number
  pransitQuantity: number
  productionQuantity: number
  productionInventoryQuantity: number
  purchaseQuantity: number
  purchaseInventoryQuantity: number
  stockQuantity: number
  unDeliveryQuantity: number
  deliveryNoticeQuantity: number
  deliveryQuantity: number
  deliveryRate: number
  placeOrderQtyByMainUnit: number
  planProductionQtyByMainUnit: number
  planPurchaseQtyByMainUnit: number
  planDirectDliveryQtyByMainUnit: number
  planQtyInfo?: object | undefined
  productUnit?: string | undefined
  salesUnit?: string | undefined
  inputProductionList?: Array<InputProductionDto> | undefined
  inputDeliveryList?: Array<InputDeliveryDto> | undefined
  inputProductionQtyInfo?: object | undefined
  inputDeliveryQtyInfo?: object | undefined
  selected?: boolean
  //一条销售订单对应的送货通知单总的待处理数量
  todoQuantity: number

  totalInputProductionQty?: number | undefined
  unitConversionList?: Array<SaleUnitConversionDto> | undefined

  constructor(init?: Partial<SaleOrderDetailFlatDTO>) {
    this.id = 0
    this.orderId = 0
    this.orderNo = ''
    this.orderType = 0 as SaleOrderType
    this.deliveryTime = new Date()
    this.remark = undefined
    this.orderRemark = undefined
    this.receiveUserId = 0
    this.receiveName = ''
    this.receiveOrderTime = new Date()

    this.status = 1 as SaleOrderStatus
    this.orderstatus = 1 as SaleOrderStatus
    this.reviewStatus = 1 as ReviewStatus
    this.examineStatus = 1 as ExamineStatus
    this.saleExtendedInfo = []

    this.customerId = 0
    this.customerCode = ''
    this.customerName = ''
    this.customerOrderNo = undefined
    this.customerMaterialNo = undefined
    this.customerMaterialName = undefined
    this.customerAddress = ''

    this.productId = 0
    this.productSpecificationId = undefined
    this.productNo = ''
    this.productName = ''
    this.productSpecification = undefined
    this.productTechnicalCode = ''

    this.placeOrderQuantity = 0
    this.price = 0
    this.totalPrice = 0
    this.orderTotalPrice = 0
    this.processQuantity = 0
    this.planProductionQuantity = 0
    this.planPurchaseQuantity = 0
    this.planDirectDliveryQuantity = 0
    this.pransitQuantity = 0
    this.productionQuantity = 0
    this.productionInventoryQuantity = 0
    this.purchaseQuantity = 0
    this.purchaseInventoryQuantity = 0
    this.stockQuantity = 0
    this.unDeliveryQuantity = 0
    this.deliveryNoticeQuantity = 0
    this.deliveryQuantity = 0
    this.deliveryRate = 0
    this.placeOrderQtyByMainUnit = 0
    this.planProductionQtyByMainUnit = 0
    this.planPurchaseQtyByMainUnit = 0
    this.planDirectDliveryQtyByMainUnit = 0
    this.planQtyInfo = undefined
    this.productUnit = undefined
    this.salesUnit = undefined

    this.inputDeliveryList = undefined
    this.inputProductionList = undefined
    this.inputDeliveryQtyInfo = undefined
    this.inputProductionQtyInfo = undefined
    this.selected = false
    this.todoQuantity = 0

    this.totalInputProductionQty = undefined
    this.unitConversionList = undefined

    // 支持传入部分字段初始化（类似 C# 对象初始化器）
    Object.assign(this, init)
  }
}

export class SaleOrderDetail {
  id: number
  placeOrderQuantity: number
  inputPlaceOrderQuantity: number
  planProductionQuantity: number
  planPurchaseQuantity: number
  planDirectDliveryQuantity: number
  deliveryQuantity: number
  deliveryNoticeQuantity: number
  price: number | undefined
  totalPrice: number
  isUrgent: boolean
  reviewStatus: ReviewStatus
  status: SaleOrderStatus
  deliveryTime: Date
  remark?: string
  goodsId?: number | undefined
  saleOrderId: number
  customerMaterialNo?: string | undefined
  customerMaterialName?: string | undefined
  packageId?: number | undefined
  salesUnitId?: number | undefined
  product: SaleGoodsDto
  salesUnit?: string | undefined
  packageSpecification?: string | undefined
  unitConversionList: Array<SaleUnitConversionDto>
  productionQuantity: number
  purchaseQuantity: number
  allOrganizeQuantity?: number | undefined

  salesUnitConversionId?: number | undefined
  selected?: boolean

  constructor(init?: Partial<SaleOrderDetail>) {
    this.id = 0
    this.placeOrderQuantity = 0
    this.inputPlaceOrderQuantity = 0
    this.planProductionQuantity = 0
    this.planPurchaseQuantity = 0
    this.planDirectDliveryQuantity = 0
    this.deliveryQuantity = 0
    this.deliveryNoticeQuantity = 0
    this.price = 0
    this.totalPrice = 0
    this.isUrgent = false
    this.reviewStatus = ReviewStatus.待提交
    this.status = SaleOrderStatus.待下单
    this.deliveryTime = new Date()
    this.remark = undefined
    this.goodsId = undefined
    this.saleOrderId = 0
    this.customerMaterialNo = undefined
    this.customerMaterialName = undefined
    this.packageId = undefined
    this.product = new SaleGoodsDto()
    this.salesUnitId = undefined
    this.salesUnit = undefined
    this.packageSpecification = undefined
    this.unitConversionList = []
    this.productionQuantity = 0
    this.purchaseQuantity = 0
    this.allOrganizeQuantity = undefined

    this.salesUnitConversionId = undefined
    this.selected = false
    // 支持传入部分字段初始化（类似 C# 对象初始化器）
    Object.assign(this, init)
  }
}

export class SaleOrderDetailDlivery extends SaleOrderDetail {
  totalWeight?: number = undefined // 该条记录的理论重量
  packageUnitName?: string = ''
  packageNum?: number = 0 // 该条记录所需包装数量
  palletNum?: number = 0 // 该条记录所需托盘数量
}

export class SaleOrder {
  id: number
  orderNo: string
  customerAddressIndex?: number | undefined
  orderTotalPrice: number
  remark?: string
  status: SaleOrderStatus
  receiveOrderTime: Date
  receiveUserId: number | undefined
  customerId: number | undefined
  receiveUser: User
  customer: BaseCustomer
  saleOrderDetails: SaleOrderDetail[]
  customerOrderNo?: string | undefined
  orderFileList?: Array<FileStorage> | undefined
  orderType: SaleOrderType
  customerAddress?: CustomerAddressDto | undefined
  reviewStatus: ReviewStatus

  customSaleAttributeList?: Array<CustomAttribute> | undefined //自定义销售属性列表（前端使用）

  constructor(
    id: number = 0,
    orderNo: string = '',
    customerAddressIndex: number | undefined = undefined,
    orderTotalPrice: number = 0,
    remark: string | undefined = undefined,
    status: SaleOrderStatus = SaleOrderStatus.进行中,
    receiveOrderTime: Date = new Date(),
    receiveUserId: number = 0,
    customerId: number | undefined = undefined,
    receiveUser: User = new User(),
    customer: BaseCustomer = new BaseCustomer(),
    saleOrderDetails: SaleOrderDetail[] = new Array<SaleOrderDetail>(),
    customerOrderNo: string | undefined = undefined,
    orderFileList: Array<FileStorage> | undefined = undefined,
    orderType: SaleOrderType = SaleOrderType.客户订单,
    customerAddress: CustomerAddressDto | undefined = undefined,
    customSaleAttributeList: Array<CustomAttribute> | undefined = undefined
  ) {
    this.id = id
    this.orderNo = orderNo
    this.customerAddressIndex = customerAddressIndex
    this.orderTotalPrice = orderTotalPrice
    this.remark = remark
    this.status = status
    this.receiveOrderTime = receiveOrderTime
    this.receiveUserId = receiveUserId
    this.customerId = customerId
    this.receiveUser = receiveUser
    this.customer = customer
    this.saleOrderDetails = saleOrderDetails
    this.customerOrderNo = customerOrderNo
    this.orderFileList = orderFileList
    this.orderType = orderType
    this.customerAddress = customerAddress
    this.reviewStatus = ReviewStatus.待提交
    this.customSaleAttributeList = customSaleAttributeList
  }
}

export class SaleOrderDeliveryDto extends SaleOrder {
  totalWeight?: number = undefined // 理论重量
  transitPrice?: number = undefined // 运费单价
  transitCost?: number = undefined // 运费金额预估
  deliveryMode?: DeliveryModeType | undefined = undefined // 运输方式（物流公司）1-物流，2-自提
  logisticsId?: number = undefined // 物流公司ID,systemParaId
  costCategoryId?: number | null = null // 费用类别Id
  saleOrderDetails: SaleOrderDetailDlivery[] = []
  constructor(init?: Partial<InputDeliveryDto>) {
    super()
    Object.assign(this, init)
  }
}

export class SaleGoodsDto {
  idField: string
  productId: number
  specificationId: number
  productNo: string
  productName: string
  productSpecification: string
  productTechnicalCode: string
  productUnitId: number
  price: number
  customerMaterialNo?: string | undefined
  customerMaterialName?: string | undefined
  packageId?: number | undefined
  goodsId?: number | undefined
  salesUnitId?: number | undefined
  salesUnitConversionId?: number | undefined

  packageList?: Array<BaseProductPackage> | undefined
  unitList?: Array<SaleGoodsUnitDto> | undefined
  checkBoxDisabled: boolean
  selected: boolean

  constructor(init?: Partial<SaleGoodsDto>) {
    this.idField = ''
    this.productId = 0
    this.specificationId = 0
    this.productNo = ''
    this.productName = ''
    this.productSpecification = ''
    this.productTechnicalCode = ''
    this.productUnitId = 0
    this.customerMaterialNo = undefined
    this.customerMaterialName = undefined
    this.packageId = undefined
    this.price = 0
    this.goodsId = undefined
    this.salesUnitId = undefined
    this.salesUnitConversionId = undefined
    this.packageList = undefined
    this.unitList = undefined

    this.checkBoxDisabled = false
    this.selected = false

    Object.assign(this, init)
  }
}
export class SaleGoodsUnitDto {
  code: number //id
  unitId: number //是不是成品主单位
  value: number //ratio
  text: string
  extension?: string | undefined
  packageId?: number | undefined
  constructor(init?: Partial<SaleGoodsUnitDto>) {
    this.code = 0
    this.unitId = 0
    this.value = 0
    this.text = ''
    this.extension = undefined
    this.packageId = undefined

    Object.assign(this, init)
  }
}
export class SaleUnitConversionDto {
  id?: number | undefined
  unitId?: number | undefined
  unitName?: string | undefined
  placeOrderQuantity: number
  ratio: number

  remark?: string | undefined
  isBaseUnit: boolean
  packageId?: number | undefined

  constructor(init?: Partial<SaleUnitConversionDto>) {
    this.id = undefined
    this.unitId = undefined
    this.unitName = undefined
    this.placeOrderQuantity = 0
    this.ratio = 0

    this.remark = undefined
    this.isBaseUnit = false
    this.packageId = undefined

    // 支持传入部分字段初始化（类似 C# 对象初始化器）
    Object.assign(this, init)
  }
}

export class InputProductionDto {
  index: number
  productionId?: number | undefined
  orderNo: string
  quantity: number
  reportUnit: string
  reportUnitConversionId: number
  ratio: number
  quantityBySalesUnit: number
  isOrderProduct: boolean
  remark?: string | undefined
  productNo?: string | undefined
  productName?: string | undefined
  isEdit: boolean //前端使用

  constructor(init?: Partial<InputProductionDto>) {
    this.index = 0
    this.productionId = undefined
    this.orderNo = ''
    this.quantity = 0
    this.reportUnit = ''
    this.reportUnitConversionId = 0
    this.ratio = 0
    this.quantityBySalesUnit = 0
    this.isOrderProduct = false
    this.remark = undefined
    this.productNo = undefined
    this.productName = undefined
    this.isEdit = false

    Object.assign(this, init)
  }
}

export class InputDeliveryDto {
  index: number
  orderNo: string
  quantity: number
  remark?: string | undefined

  constructor(init?: Partial<InputDeliveryDto>) {
    this.index = 0
    this.orderNo = ''
    this.quantity = 0
    this.remark = undefined

    Object.assign(this, init)
  }
}

export class ProductIdDto {
  idField: string // ProductId_SpecificationId_GoodsId
  productId: number
  specificationId?: number | undefined
  packageList?: BaseProductPackage[] | undefined
  unitList?: GetProductUnitDto[] | undefined
  isFilterGoods?: boolean | undefined // 是否过滤已存在客户产品的包装规格
  constructor(
    idField: string = '',
    productId: number = 0,
    specificationId?: number | undefined,
    packageList?: BaseProductPackage[] | undefined,
    unitList?: GetProductUnitDto[] | undefined,
    isFilterGoods?: boolean | undefined
  ) {
    this.idField = idField
    this.productId = productId
    this.specificationId = specificationId
    this.packageList = packageList
    this.unitList = unitList
    this.isFilterGoods = isFilterGoods
  }
}

export class GetProductUnitDto {
  value?: string | undefined // 选项选中值
  code?: string | undefined // 选项编码
  text?: string | undefined // 选择显示值
  extension?: string | undefined
  packageId?: number | undefined
  unitId: number
  constructor(
    value?: string | undefined,
    code?: string | undefined,
    text?: string | undefined,
    extension?: string | undefined,
    packageId?: number | undefined,
    unitId: number = 0
  ) {
    this.value = value
    this.code = code
    this.text = text
    this.extension = extension
    this.packageId = packageId
    this.unitId = unitId
  }
}

//获取所有的送货通知订单
export class GetAllDeliveryOrderDetailDto {
  orderId: number = 0
  deliveryOrderDetailId: number = 0
  deliveryOrderNo: string = ''
  customerId: number = 0
  customerName: string = ''
  customerAddress: string = ''
  productName: string = ''
  productNo: string = ''
  productSpecification: string = ''
  customAttribute: string = ''
  customAttributeList: { name: string; value: string }[] = []
  estimatedDeliveryTime: Date | undefined = undefined
  deliveryQuantity: number = 0
  isDirectOutbound: boolean = false
  isInventoryTransfer: boolean = false
  isOutboundInspection: boolean = false
  outboundTime: Date | undefined = undefined
  outboundOperator: string = ''

  constructor(init?: Partial<GetAllDeliveryOrderDetailDto>) {
    Object.assign(this, init)
  }
}

export class GetSaleOrderDetailForDeliveryDto {
  customerName: string = ''
  saleOrderDetailList: DeliveryOrderDetailListDto[] = []
  constructor(init?: Partial<GetSaleOrderDetailForDeliveryDto>) {
    Object.assign(this, init)
  }
}

//获取编辑页面的Dto
export class GetDeliveryOrderDto {
  id: number = 0
  customerId: number = 0
  customerName: string = ''
  customAttributesList: CustomAttribute[] = []
  customerAddress: CustomerAddressDto | undefined = undefined
  remark: string = ''
  isDirectOutbound: boolean = false
  isInventoryTransfer: boolean = false
  estimatedDeliveryTime?: Date = undefined
  positionId?: number = undefined
  positionName?: string = undefined
  deliveryOrderDetailList: DeliveryOrderDetailListDto[] = []
  transferPositionId?: number = undefined
  transferAreaId?: number = undefined
  transferPositionName?: string = undefined
  totalWeight?: number = undefined // 理论重量
  transitPrice?: number = undefined // 运费单价
  transitCost?: number = undefined // 运费金额预估
  deliveryMode?: DeliveryModeType | undefined = undefined // 运输方式（物流公司）1-物流，2-自提
  logisticsId?: number = undefined // 物流公司ID,systemParaId
  costCategoryId?: number | null = null // 费用类别Id
  costCategoryName?: string = ''

  constructor(init?: Partial<GetDeliveryOrderDto>) {
    Object.assign(this, init)
  }
}

export class DeliveryOrderDetailListDto {
  deliveryOrderDetailId: number = 0
  saleOrderDetailId: number = 0
  goodsId?: number = undefined
  productId: number = 0
  specificationId?: number = 0
  saleOrderNo: string = ''
  productName: string = ''
  productNo: string = ''
  specification?: string = ''
  technicalCode?: string = ''
  productUnitId?: number = undefined
  placeOrderQuantity: number = 0
  //通知送货量
  deliveryQuantity: number = 0
  deliveryQtyByMainUnit: number = 0
  //实际送货出库数量
  actualDeliveryQuantity: number = 0
  actualDeliveryQtyByMainUnit: number = 0
  //待送货数量（已通知未送货）
  todoQuantity: number = 0
  todoQtyByMainUnit: number = 0
  planDirectDliveryQuantity: number = 0
  totalInputProductionQty: number = 0
  productionInventoryQuantity: number = 0
  purchaseInventoryQuantity: number = 0
  orderInventory: number = 0
  maxDeliveryQuantity: number = 0
  salesUnit?: string | undefined
  salesUnitId: number = 0
  salesUnitConversionId?: number = 0
  saleToBaseUnitRatio: number = 0
  saleCustomerAddress?: CustomerAddressDto | undefined
  productUnit?: string | undefined
  planQtyInfo: { content: string; position: 'left' | 'right' | 'top' | 'bottom' } = {
    content: '',
    position: 'right'
  }
  inputUnitConversionId: number = 0
  inputUnitQuantity: number = 0
  unitInfo: InventoryUnitShowDto[] = []
  unitConversionList: SaleUnitConversionDto[] = []
  inventoryRecordList: InventoryRecordOccupyDetailDto[] = []

  //以下属性为前端使用
  //是否分配
  isAllocate: boolean = false
  isAlreadyOutbound: boolean = false //是否确认
  backupInfo: {
    inputUnitConversionId: number
    inputUnitQuantity: number
    isAllocate: boolean
    isAlreadyOutbound: boolean
    inventoryRecordList: InventoryRecordOccupyDetailDto[]
  } = {
    inputUnitConversionId: 0,
    inputUnitQuantity: 0,
    inventoryRecordList: [],
    isAllocate: false,
    isAlreadyOutbound: false
  }
  occupyInputQtyBySalesUnit?: number = undefined
  occupyInputQtyByMainUnit?: number = undefined
  occupyQuantityByMain?: number = undefined //主单位对应的占用数量（库存占用数量）
  packageId?: number = 0 // Sale_orderDetail对应的外包装方案Id
  totalWeight?: number = undefined // 该条记录的理论重量
  packageUnitName?: string = ''
  packageNum?: number = 0 // 该条记录所需包装数量
  palletNum?: number = 0 // 该条记录所需托盘数量
  constructor(init?: Partial<DeliveryOrderDetailListDto>) {
    Object.assign(this, init)
  }
}

export class InventoryRecordOccupyDetailDto {
  saleOrderDetailId: number = 0
  inventoryRecordId: number = 0
  inventoryRecordNo: string | null = ''
  positionId: number | null = 0
  positionName: string | null = ''
  locationName: string | null = ''
  areaName: string | null = ''
  inboundQuantity: number = 0
  usedQuantity: number = 0
  remainingQuantity: number = 0
  //该库存总的占用量
  occupyQuantity: number = 0
  originOccupyQuantity: number = 0
  //该明细已出库量
  deliveryOrderDetailOutboundQuantity: number = 0
  deliveryOrderDetailId: number = 0
  //该明细占用量
  deliveryDetailOccupyQuantity: number = 0
  deliveryDetailOutboundOccupyQuantity: number = 0
  //是否警告数量超出
  isWarning: boolean = false
  occupyInputUnitConversionId: number | undefined = 0
  occupyInputQuantity: number | undefined = 0
  occupyUnitInfo: InventoryUnitOutboundDto[] = []
  inventoryUnitsInfoList: InventoryUnitShowDto[] = []
  originRemainingQuantity: number = 0

  //以下字段为前端使用
  constructor(init?: Partial<InventoryRecordOccupyDetailDto>) {
    Object.assign(this, init)
  }
}

export class CreateOrUpdateDeliveryOrderPostDto {
  id: number = 0
  customerId?: number = undefined
  customerAddress?: CustomerAddressDto = new CustomerAddressDto()
  estimatedDeliveryTime?: Date | null = undefined
  isDirectOutbound: boolean = false
  isInventoryTransfer: boolean = false
  transferPositionId?: number = undefined
  remark: string | null = ''
  customAttributeList: CustomAttribute[] = []
  deliveryOrderDetailList: CreateOrUpdateDeliveryDetailPostDto[] = []
  totalWeight?: number = undefined // 理论重量
  transitPrice?: number = undefined // 运费单价
  transitCost?: number = undefined // 运费金额预估
  deliveryMode?: DeliveryModeType | undefined = undefined // 运输方式（物流公司）1-物流，2-自提
  logisticsId?: number = undefined // 物流公司ID,systemParaId
  costCategoryId?: number | null = null // 费用类别Id

  constructor(init?: Partial<CreateOrUpdateDeliveryOrderPostDto>) {
    Object.assign(this, init)
  }
}
export class CreateOrUpdateDeliveryDetailPostDto {
  deliveryOrderDetailId?: number | undefined = undefined
  isAllocate: boolean = false
  saleOrderDetailId: number = 0
  saleOrderNo: string = ''
  salesUnit: string = ''

  goodsId: number = 0
  productId: number = 0
  specificationId?: number | undefined = undefined
  productNo: string = ''
  productUnitId: number = 0
  // 送货通知量（主单位）
  deliveryQuantity: number = 0
  inputQuantityBySale: number = 0
  inputQuantity: number = 0
  inputUnitConversionId?: number | undefined = undefined
  unitInfo: InventoryUnitOutboundDto[] = []

  inventoryRecordList: CreateOrUpdateDeliveryInventoryPostDto[] = []
  packageId?: number = undefined // Sale_orderDetail对应的外包装方案Id
  palletNum?: number = 0 // 该条记录所需托盘数量

  constructor(init?: Partial<CreateOrUpdateDeliveryDetailPostDto>) {
    Object.assign(this, init)
  }
}
export class CreateOrUpdateDeliveryInventoryPostDto {
  inventoryRecordId: number = 0
  inventoryRecordNo: string = ''
  positionId: number = 0
  occupyDetailId?: number | undefined = undefined
  // 占用了（主单位）
  occupyQuantity: number = 0
  inputQuantity: number = 0
  inputUnitConversionId: number = 0
  unitInfo: InventoryUnitOutboundDto[] = []

  constructor(init?: Partial<CreateOrUpdateDeliveryInventoryPostDto>) {
    Object.assign(this, init)
  }
}

export class InventoryUnitDto {
  id: number = 0
  unitId: number = 0
  unitName: string = ''
  ratio: number = 0
  remark?: string = ''
  conversionType?: UnitConversionType = undefined
  constructor(init?: Partial<InventoryUnitDto>) {
    Object.assign(this, init)
  }
}

export class InventoryUnitOutboundDto extends InventoryUnitDto {
  outboundQuantity: number = 0

  constructor(init?: Partial<InventoryUnitOutboundDto>) {
    super(init)
    this.outboundQuantity = init?.outboundQuantity ?? 0
  }
}
export class DeliveryOrderListDto {
  deliveryOrderId: number = 0 //送货订单Id
  deliveryOrderDetailId: number = 0 //送货订单详情Id
  saleOrderDetailId: number = 0
  deliveryOrderNo: string = '' // 送货订单编号
  customerId: number = 0 // 客户ID
  customerName: string = '' // 客户Name
  customerAddress?: CustomerAddressDto | undefined = undefined //客户地址
  logisticsCycle?: number | undefined = undefined
  estimatedDeliveryTime?: Date | undefined // 预计送达时间
  isDirectOutbound: boolean = false // 是否直接出库(管理送货通知页面显示为出库检验)
  isInventoryTransfer: boolean = false // 是否库存调拨
  productId: number = 0 // 产品Id 1
  productNo: string = '' // 产品编码
  productName: string = '' // 产品 1
  specificationId?: number = undefined // 产品规格Id
  specification: string = '' // 产品规格
  technologyCode: string = '' // 技术型号
  inspection: string = '' // 产品质量
  transferPositionName?: string | undefined = undefined // 调拨库房
  saleOrderNo: string = '' // 销售单号
  saleCustomerMaterialNo: string = '' // 客户产品图号
  saleCustomerOrderNo: string = '' // 客户订单号

  price: number = 0 //单价
  totalPrice: number = 0 //总价
  isUrgent: boolean = false //是否加急
  deliveryTime: Date = new Date() //交货日期
  saleRemark?: string = undefined //售货订单的备注

  deliveryQuantity: number = 0 //(通知)送货数量
  actualDeliveryQuantity: number = 0 //(实际)送货数量
  outboundTime?: Date | undefined //出库时间
  outboundOperator?: string = undefined // 出库操作人，兼容旧字段
  remark?: string = undefined // 送货订单详情备注

  unitName: string = '' //送货单位
  inputUnitConversionId?: number = undefined //送货单位Id
  inventoryUnitList: InventoryUnitOutboundDto[] = []
  inventoryUnitListExMain: InventoryUnitOutboundDto[] = []
  customAttributeList?: Array<CustomAttributeShowDto> = undefined //动态列
  isCompleteDelivery: boolean = false //完成状态

  constructor(init?: Partial<DeliveryOrderListDto>) {
    Object.assign(this, init)
  }
}

export class GetCostCategoryByCostCategoryFeatureDto {
  costCategoryId: number = 0
  name?: string = undefined
  constructor(init?: Partial<GetCostCategoryByCostCategoryFeatureDto>) {
    Object.assign(this, init)
  }
}

export class GetPackageUnitToTargetUnitRatioDto {
  packageId: number = 0
  targetId: number = 0
  unitConversionType?: UnitConversionType = undefined
  packageUnitId: number = 0
  targetUnitId: number = 0
  ratio: number = 0
  packageUnitName?: string = ''
  constructor(init?: Partial<GetPackageUnitToTargetUnitRatioDto>) {
    Object.assign(this, init)
  }
}
