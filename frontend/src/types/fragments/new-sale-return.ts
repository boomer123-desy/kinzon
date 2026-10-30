import {
  SaleOrderType,
  SaleOrderStatus,
  ReviewStatus,
  ExamineStatus,
  UnitConversionType,
  SaleReturnType
} from '@/types/enums'
import { CustomAttribute, type Filter, type SelectOption } from '@/types/types'
import {
  FileStorage,
  BaseCustomer,
  User,
  BaseProductPackage,
  CustomerAddressDto
} from '@/types/common'

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
  estimatedDeliveryTime: string = ''
  deliveryOrderDetailInfoList: DeliveryOrderDetailListDto[] = []

  constructor(init?: Partial<GetDeliveryOrderDto>) {
    Object.assign(this, init)
  }
}

export class DeliveryOrderDetailListDto {
  deliveryOrderDetailId: number = 0
  saleOrderDetailId: number = 0
  productId: number = 0
  specificationId?: number = 0
  saleOrderNo: string = ''
  productName: string = ''
  productNo: string = ''
  specification?: string = ''
  technicalCode?: string = ''
  placeOrderQuantity: number = 0
  deliveryQuantity: number = 0
  //实际送货出库数量
  actualDeliveryQuantity: number = 0
  todoQuantity: number = 0
  thisTimeDeliveryQty: number = 0
  planProductionQuantity: number = 0
  planPurchaseQuantity: number = 0
  planDirectDliveryQuantity: number = 0
  planProductionQtyByMainUnit: number = 0
  planPurchaseQtyByMainUnit: number = 0
  planDirectDliveryQtyByMainUnit: number = 0
  salesUnit?: string | undefined
  salesUnitId: number = 0
  salesUnitConversionId?: number = 0
  productUnit?: string | undefined
  planQtyInfo: { content: string; position: 'left' | 'right' | 'top' | 'bottom' } = {
    content: '',
    position: 'right'
  }
  inputUnitConversionId: number = 0
  inputUnitQuantity: number = 0
  unitInfo: InventoryUnitDto[] = []
  inventoryRecordList: InventoryRecordOccupyDetailDto[] = []
  //是否分配(前端使用)
  isAllocate: boolean = false
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
  //该明细已出库量
  deliveryOrderDetailOutboundQuantity: number = 0
  deliveryOrderDetailId: number = 0
  //该明细占用量
  deliveryDetailOccupyQuantity: number = 0
  //是否警告数量超出
  isWarning: boolean = false
  OccupyInputUnitConversionId: number = 0
  OccupyInputQuantity: number = 0
  OccupyUnitInfo: InventoryUnitDto[] = []
  inventoryUnitsInfoList: InventoryUnitDto[] = []
  constructor(init?: Partial<InventoryRecordOccupyDetailDto>) {
    Object.assign(this, init)
  }
}

export class CreateOrUpdateDeliveryOrderPostDto {
  id: number = 0
  customerId: number | null = 0
  customerAddress?: CustomerAddressDto = new CustomerAddressDto()
  estimatedDeliveryTime: string | null = ''
  isDirectOutbound: boolean = false
  isInventoryTransfer: boolean = false
  remark: string | null = ''
  customAttributeList: CustomAttribute[] = []
  deliveryOrderDetailList: DeliveryOrderDetailListDto[] = []
  constructor(init?: Partial<InventoryRecordOccupyDetailDto>) {
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

/** 销售退货页面的公共查询参数。 */
export class SaleReturnOrderFilterListDTO {
  customerId: number = 0
  deliveryOrderDetailFilterList: Filter[] = []
  deliveryOrderFilterList: Filter[] = []
  saleOrderDetailFilterList: Filter[] = []
  saleReturnOrderFilterList: Filter[] = []
  saleReturnOrderDetailFilterList: Filter[] = []
  productFilterList: Filter[] = []

  constructor(init?: Partial<SaleReturnOrderFilterListDTO>) {
    Object.assign(this, init)
  }
}

/** 可退货送货明细的查询响应。 */
export class DeliveryOrderDetailDTO {
  id: number = 0
  deliveryOrderId: number = 0
  deliveryOrderNo: string = ''
  deliveryQuantity: number = 0
  actualDeliveryQuantity: number = 0
  returnQuantity: number = 0
  deliveryTime?: string | Date
  saleOrderDetailId: number = 0
  saleOrderDetailPrice: number = 0
  productId: number = 0
  productNo: string = ''
  productName: string = ''
  productCustomerMaterialNo: string = ''
  productSpecification: string = ''
  productMaterialGrade?: string
  productMaterialSpecification?: string
  productUnit: string = ''
  saleOrderNo: string = ''
  unitInfos: InventoryUnitDto[] = []
  selected?: boolean

  constructor(init?: Partial<DeliveryOrderDetailDTO>) {
    Object.assign(this, init)
  }
}

/** 销售退货明细列表的查询响应。 */
export class SaleReturnOrderDetailViewDTO {
  id: number = 0
  returnQuantity: number = 0
  inventoryQuantity: number = 0
  price: number = 0
  totalPrice: number = 0
  isComplete: boolean = false
  isAccount: boolean = false
  inputReturnQuantity?: number
  inputUnitConversionId?: number
  unitInfos: InventoryUnitDto[] = []
  saleReturnType: SaleReturnType = SaleReturnType.成品
  deliveryOrderDetailId?: number
  deliveryOrderNo: string = ''
  deliveryQuantity: number = 0
  actualDeliveryQuantity: number = 0
  deliveryOrderReturnQuantity: number = 0
  customerId: number = 0
  customerCode: string = ''
  customerName: string = ''
  saleReturnOrderId: number = 0
  saleReturnOrderRemark?: string
  saleReturnOrderReturnTime?: string | Date
  saleReturnOrderNo: string = ''
  saleReturnOrderTotalPrice: number = 0
  productId: number = 0
  productNo: string = ''
  productName: string = ''
  productMaterialSpecification?: string
  productMaterialGrade?: string
  productSpecification: string = ''
  productUnit: string = ''
  productCustomerMaterialNo: string = ''
  saleOrderNo: string = ''
  reason?: string
  remark?: string

  constructor(init?: Partial<SaleReturnOrderDetailViewDTO>) {
    Object.assign(this, init)
  }
}

/** 新增销售退货单请求。 */
export class PostSaleReturnOrderDetailDTO {
  deliveryOrderDetailId: number = 0
  goodsId?: number
  returnQuantity: number = 0
  returnUnitId: number = -1
  unitInfos: InventoryUnitDto[] = []
  saleReturnType: SaleReturnType = SaleReturnType.成品
  reason?: string
  remark?: string

  constructor(init?: Partial<PostSaleReturnOrderDetailDTO>) {
    Object.assign(this, init)
  }
}

export class PostSaleReturnOrderDTO {
  customerId: number = 0
  returnTime: Date = new Date()
  remark?: string
  details: PostSaleReturnOrderDetailDTO[] = []

  constructor(init?: Partial<PostSaleReturnOrderDTO>) {
    Object.assign(this, init)
  }
}

/** 客户产品直接退货的可选产品。数量均为产品主单位。 */
export class CustomerDeliveryProductDTO {
  goodsId: number = 0
  productId: number = 0
  specificationId?: number
  productNo: string = ''
  productName: string = ''
  customerMaterialNo?: string
  specification?: string
  unit: string = ''
  deliveryCount: number = 0
  returnCount: number = 0
  price: number = 0
  unitInfos: InventoryUnitDto[] = []
  selected?: boolean

  constructor(init?: Partial<CustomerDeliveryProductDTO>) {
    Object.assign(this, init)
  }
}

/** 客户产品直接退货页使用的明细草稿。 */
export class ProductReturnDetailDraft extends CustomerDeliveryProductDTO {
  returnQuantity: number = 0
  returnUnitId: number = -1
  saleReturnType: SaleReturnType = SaleReturnType.成品
  reason: string = ''
  remark: string = ''

  constructor(init?: Partial<ProductReturnDetailDraft>) {
    super()
    Object.assign(this, init)
  }
}

/** 送货单退货编辑页使用的明细草稿。 */
export class SaleReturnOrderDetailDraft {
  deliveryOrderDetail: DeliveryOrderDetailDTO = new DeliveryOrderDetailDTO()
  returnQuantity: number = 0
  returnUnitId: number = -1
  saleReturnType: SaleReturnType = SaleReturnType.成品
  reason: string = ''
  remark: string = ''

  constructor(init?: Partial<SaleReturnOrderDetailDraft>) {
    Object.assign(this, init)
  }
}
