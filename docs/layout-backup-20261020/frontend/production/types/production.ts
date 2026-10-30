import type { FileStorage, SystemParaTreeDto } from './common'
import {
  DispatchOrderStatus,
  DispatchBasisType,
  FormulaDetailType,
  InventoryDataType,
  ProductType,
  ReviewStatus,
  UnitConversionType,
  SaleOrderType,
  BomStatus,
  BomDetailType,
  InventoryDataStatus
} from './enums'
import type { CustomAttribute, SelectOption, UploadFile } from './types'

export class GetDispatchOrderDto {
  basisType: DispatchBasisType = DispatchBasisType.未指定
  // 配方、BOM、工艺单均关联版本Id。
  basisId?: number = undefined
  id: number
  orderNo: string
  productId: number
  specificationId?: number | undefined
  technicalCode?: string | undefined
  productNo: string
  productName: string
  specification?: string | undefined
  productCategory: string
  packageWay?: string | undefined
  productRemark?: string | undefined
  departmentId: number
  departmentName: string
  price: number
  remark?: string | undefined
  dispatchQuantity: number
  completeQuantity: number
  dispatchUnit: string
  mainQuantity: number
  mainCompleteQuantity: number
  baseQuantity: number
  baseCompleteQuantity: number
  incomeQuantity: number
  incomeCompleteQuantity: number
  status: DispatchOrderStatus
  isDeleted: boolean
  dispatchDate: Date
  dispatchUnitInfo?: UnitInfo | undefined
  processPriceId?: number | undefined
  planCompleteDate?: Date | undefined
  completeDate?: Date | undefined
  packageId?: number | undefined
  packageSpecification?: string | undefined
  unitList: SelectOption[]
  pacakgeList: SelectOption[]
  departmentList: SelectOption[]
  formulaVersionId?: number
  formulaName?: string | undefined
  productionAttributeList?: Array<CustomAttribute> = undefined

  productTechnicalCode?: string = undefined
  materialSpecification?: string = undefined
  productSpecification?: string = undefined
  constructor(
    id: number = 0,
    orderNo: string = '',
    productId: number = 0,
    specificationId?: number | undefined,
    technicalCode?: string | undefined,
    productNo: string = '',
    productName: string = '',
    specification?: string | undefined,
    productCategory: string = '',
    packageWay?: string | undefined,
    productRemark: string | undefined = undefined,
    departmentId: number = 0,
    departmentName: string = '',
    price: number = 0,
    remark?: string | undefined,
    dispatchQuantity: number = 0,
    completeQuantity: number = 0,
    mainQuantity: number = 0,
    mainCompleteQuantity: number = 0,
    dispatchUnit: string = '',
    baseQuantity: number = 0,
    baseCompleteQuantity: number = 0,
    incomeQuantity: number = 0,
    incomeCompleteQuantity: number = 0,
    status: DispatchOrderStatus = DispatchOrderStatus.进行中,
    isDeleted: boolean = false,
    dispatchDate: Date = new Date(),
    dispatchUnitInfo?: UnitInfo | undefined,
    processPriceId?: number | undefined,
    planCompleteDate?: Date | undefined,
    completeDate?: Date | undefined,
    packageId?: number | undefined,
    packageSpecification?: string | undefined,
    formulaVersionId?: number | undefined,
    unitList: SelectOption[] = [],
    pacakgeList: SelectOption[] = [],
    departmentList: SelectOption[] = [],
    formulaName?: string | undefined
  ) {
    this.id = id
    this.orderNo = orderNo
    this.productId = productId
    this.specificationId = specificationId
    this.technicalCode = technicalCode
    this.productNo = productNo
    this.productName = productName
    this.specification = specification
    this.productCategory = productCategory
    this.packageWay = packageWay
    this.productRemark = productRemark
    this.departmentId = departmentId
    this.departmentName = departmentName
    this.price = price
    this.remark = remark
    this.dispatchQuantity = dispatchQuantity
    this.completeQuantity = completeQuantity
    this.mainQuantity = mainQuantity
    this.mainCompleteQuantity = mainCompleteQuantity
    this.dispatchUnit = dispatchUnit
    this.baseQuantity = baseQuantity
    this.baseCompleteQuantity = baseCompleteQuantity
    this.incomeQuantity = incomeQuantity
    this.incomeCompleteQuantity = incomeCompleteQuantity
    this.status = status
    this.isDeleted = isDeleted
    this.dispatchDate = dispatchDate
    this.dispatchUnitInfo = dispatchUnitInfo
    this.processPriceId = processPriceId
    this.planCompleteDate = planCompleteDate
    this.completeDate = completeDate
    this.packageId = packageId
    this.packageSpecification = packageSpecification
    this.formulaVersionId = formulaVersionId
    this.unitList = unitList
    this.pacakgeList = pacakgeList
    this.departmentList = departmentList
    this.formulaName = formulaName
  }
}

export class PostDispatchOrderDto extends GetDispatchOrderDto {
  materialDemands?: PostMaterialDemands
  productionAttributeList?: Array<CustomAttribute> = undefined // 扩展参数

  saleOrders?: Array<SaleOrderDetailDispatchDTO> = undefined // 关联销售单

  constructor(init?: Partial<PostDispatchOrderDto>) {
    super()
    Object.assign(this, init)
  }
}

export class PostMaterialDemands {
  formulaVersionId?: number = undefined
  formulaUnitId?: number = undefined
  formulaConversionId?: number = undefined
  formulaQuantity?: number = undefined
  materials: PostMaterialDemandDetails[] = []
  convRatio?: number = undefined

  constructor(init?: Partial<PostMaterialDemands>) {
    Object.assign(this, init)
  }
}

/** 前端编辑行，领料草稿在保存时转换为 PostMaterialDemands。 */
export class DispatchOrderEditDto extends GetDispatchOrderDto {
  materialDemands?: FormulaMaterialDemandsDto | BomMaterialDemandsDto = undefined
  saleOrders?: SaleOrderDetailDispatchDTO[] = undefined

  constructor(init?: Partial<DispatchOrderEditDto>) {
    super()
    Object.assign(this, init)
  }
}

/** 配方领料编辑结果，保留树形明细和单位选择状态。 */
export class FormulaMaterialDemandsDto {
  formulaVersionId?: number = undefined
  formulaUnitConversionId?: number = undefined
  formulaQuantity?: number = undefined
  formulaUnitInfo?: SelectOption = undefined
  convRatio?: number = undefined
  materials: FormulaDetailDto[] = []
  materialDetails: FormulaDetailDto[] = []
  unitConversitions: any[] = []

  constructor(init?: Partial<FormulaMaterialDemandsDto>) {
    Object.assign(this, init)
  }
}

export class PostMaterialDemandDetails {
  formulaUnitId?: number = undefined
  formulaConversionId?: number = undefined
  formulaQuantity?: number = undefined
  convRatio?: number = undefined
  dataId: number = 0
  dataType: InventoryDataType = InventoryDataType.Product
  demandQuantity: number = 0
  unitId: number = 0
  unitConversionId?: number | undefined
  baseDemandQuantity: number = 0
  dataStatus: InventoryDataStatus = InventoryDataStatus.None
  supplierId?: number = undefined
  detailRemark?: string = undefined
  convUnitInfo: InventoryUnitOutboundDto[] = []

  constructor(init?: Partial<PostMaterialDemandDetails>) {
    Object.assign(this, init)
  }
}

export class UnitInfo {
  mainUnitId: number // 主计量单位
  mainUnitName: string
  mainRatio: number
  baseUnitId: number // 基础计量单位
  baseUnitName: string
  productionUnitId: number // 生产单位
  productionUnitName: string
  baseRatio: number // 单位转换率: Ratio * base单位 = 1 production单位 (decimal → number)
  incomeUnitId: number // 收入单位
  incomeUnitName: string
  incomeRatio: number // 单位转换率: Ratio * income单位 = 1 production单位 (decimal → number)
  unitConversionId?: number | undefined
  incomeUnitConversionId?: number | undefined
  baseUnitConversionId?: number | undefined
  mainUnitConversionId?: number | undefined
  constructor(
    mainUnitId: number = 0,
    mainUnitName: string = '',
    mainRatio: number = 0,
    baseUnitId: number = 0,
    baseUnitName: string = '',
    productionUnitId: number = 0,
    productionUnitName: string = '',
    baseRatio: number = 0,
    incomeUnitId: number = 0,
    incomeUnitName: string = '',
    incomeRatio: number = 0,
    unitConversionId?: number | undefined,
    incomeUnitConversionId?: number | undefined,
    baseUnitConversionId?: number | undefined,
    mainUnitConversionId?: number | undefined
  ) {
    this.mainUnitId = mainUnitId
    this.mainUnitName = mainUnitName
    this.mainRatio = mainRatio
    this.baseUnitId = baseUnitId
    this.baseUnitName = baseUnitName
    this.productionUnitId = productionUnitId
    this.productionUnitName = productionUnitName
    this.baseRatio = baseRatio
    this.incomeUnitId = incomeUnitId
    this.incomeUnitName = incomeUnitName
    this.incomeRatio = incomeRatio
    this.unitConversionId = unitConversionId
    this.incomeUnitConversionId = incomeUnitConversionId
    this.baseUnitConversionId = baseUnitConversionId
    this.mainUnitConversionId = mainUnitConversionId
  }
}

export class GetProductUnitDto {
  value?: string | undefined // 选项选中值
  code?: string | undefined // 选项编码
  text?: string | undefined // 选择显示值
  extension?: string | undefined
  packageId?: number | undefined
  unitId: number
  isMainUnit: boolean
  constructor(
    value?: string | undefined,
    code?: string | undefined,
    text?: string | undefined,
    extension?: string | undefined,
    packageId?: number | undefined,
    unitId: number = 0,
    isMainUnit: boolean = false
  ) {
    this.value = value
    this.code = code
    this.text = text
    this.extension = extension
    this.packageId = packageId
    this.unitId = unitId
    this.isMainUnit = isMainUnit
  }
}

export class GetWaitDispatchOrderDto extends GetDispatchOrderDto {
  receiveQuantity: number
  mainReceiveQuantity: number
  mainCompleteQuantity: number
  baseReceiveQuantity: number
  baseCompleteQuantity: number
  incomReceiveQuantity: number
  incomeCompleteQuantity: number
  mainQuantity: number
  waitQuantity: number
  doingQuantity: number
  mainWaitQuantity: number
  mainDoingQuantity: number
  baseWaitQuantity: number
  baseDoingQuantity: number
  incomeWaitQuantity: number
  incomeDoingQuantity: number
  constructor(
    receiveQuantity: number = 0,
    mainReceiveQuantity: number = 0,
    mainCompleteQuantity: number = 0,
    baseReceiveQuantity: number = 0,
    baseCompleteQuantity: number = 0,
    incomReceiveQuantity: number = 0,
    incomeCompleteQuantity: number = 0,
    mainQuantity: number = 0,
    waitQuantity: number = 0,
    doingQuantity: number = 0,
    mainWaitQuantity: number = 0,
    mainDoingQuantity: number = 0,
    baseWaitQuantity: number = 0,
    baseDoingQuantity: number = 0,
    incomeWaitQuantity: number = 0,
    incomeDoingQuantity: number = 0
  ) {
    super()
    this.receiveQuantity = receiveQuantity
    this.mainQuantity = mainQuantity
    this.mainReceiveQuantity = mainReceiveQuantity
    this.mainCompleteQuantity = mainCompleteQuantity
    this.baseReceiveQuantity = baseReceiveQuantity
    this.baseCompleteQuantity = baseCompleteQuantity
    this.incomReceiveQuantity = incomReceiveQuantity
    this.incomeCompleteQuantity = incomeCompleteQuantity
    this.waitQuantity = waitQuantity
    this.doingQuantity = doingQuantity
    this.mainWaitQuantity = mainWaitQuantity
    this.mainDoingQuantity = mainDoingQuantity
    this.baseWaitQuantity = baseWaitQuantity
    this.baseDoingQuantity = baseDoingQuantity
    this.incomeWaitQuantity = incomeWaitQuantity
    this.incomeDoingQuantity = incomeDoingQuantity
  }
}

export class GetProductionSumInfoDto {
  productId?: number | undefined
  productSpecificationId?: number | undefined
  unitId?: number | undefined
  unitName?: string | undefined
  quantity?: number | undefined

  constructor(
    productId?: number | undefined,
    productSpecificationId?: number | undefined,
    unitId?: number | undefined,
    unitName?: string | undefined,
    quantity?: number | undefined
  ) {
    this.productId = productId
    this.productSpecificationId = productSpecificationId
    this.unitId = unitId
    this.unitName = unitName
    this.quantity = quantity
  }
}

export class ProductDto {
  productId: number
  productNo?: string | undefined
  productName?: string | undefined
  productTechnicalCode?: string | undefined
  specificationId?: number | undefined
  productSpecification?: string | undefined
  productCategory?: string | undefined
  productUnit?: string | undefined
  productPackageWay?: string | undefined // 包装方式
  packageWayId?: number | undefined
  isDeleted: boolean
  selectable: boolean
  productCategoryId?: number | undefined
  productUnitId?: number | undefined
  remark?: string | undefined
  reviewStatus: ReviewStatus // 产品评审状态
  productType: ProductType
  inventoryQuantity: number // 总库存 (decimal → number)
  abnormalQuantity: number // 异常库存
  unPackageQuantity: number // 未包装库存
  productMainUnitId?: number | undefined
  productMainUnit?: string | undefined
  categoryAttribute?: string | undefined
  matureTime?: Date | undefined
  customAttribute?: string | undefined // JSON 字符串
  safetyStock: number
  maxStock: number
  occupyInventory: number // 占用库存
  materialSpecification?: string | undefined
  materialGrade?: string | undefined // 材质牌号

  unitData: GetProductUnitDto[] = [] // 产品单位列表

  constructor(
    productId: number = 0,
    productNo?: string | undefined,
    productName?: string | undefined,
    productTechnicalCode?: string | undefined,
    specificationId?: number | undefined,
    productSpecification?: string | undefined,
    productCategory?: string | undefined,
    productUnit?: string | undefined,
    productPackageWay?: string | undefined,
    packageWayId?: number | undefined,
    isDeleted: boolean = false,
    selectable: boolean = true,
    productCategoryId?: number | undefined,
    productUnitId?: number | undefined,
    remark?: string | undefined,
    reviewStatus: ReviewStatus = ReviewStatus.评审通过, // 请根据实际枚举调整
    productType: ProductType = ProductType.成品, // 请根据实际枚举调整
    inventoryQuantity: number = 0,
    abnormalQuantity: number = 0,
    unPackageQuantity: number = 0,
    productMainUnitId?: number | undefined,
    productMainUnit?: string | undefined,
    categoryAttribute?: string | undefined,
    matureTime?: Date | undefined,
    customAttribute?: string | undefined,
    safetyStock: number = 0,
    maxStock: number = 0,
    occupyInventory: number = 0,
    materialSpecification?: string | undefined,
    materialGrade?: string | undefined
  ) {
    this.productId = productId
    this.productNo = productNo
    this.productName = productName
    this.productTechnicalCode = productTechnicalCode
    this.specificationId = specificationId
    this.productSpecification = productSpecification
    this.productCategory = productCategory
    this.productUnit = productUnit
    this.productPackageWay = productPackageWay
    this.packageWayId = packageWayId
    this.isDeleted = isDeleted
    this.selectable = selectable
    this.productCategoryId = productCategoryId
    this.productUnitId = productUnitId
    this.remark = remark
    this.reviewStatus = reviewStatus
    this.productType = productType
    this.inventoryQuantity = inventoryQuantity
    this.abnormalQuantity = abnormalQuantity
    this.unPackageQuantity = unPackageQuantity
    this.productMainUnitId = productMainUnitId
    this.productMainUnit = productMainUnit
    this.categoryAttribute = categoryAttribute
    this.matureTime = matureTime
    this.customAttribute = customAttribute
    this.safetyStock = safetyStock
    this.maxStock = maxStock
    this.occupyInventory = occupyInventory
    this.materialSpecification = materialSpecification
    this.materialGrade = materialGrade
  }
}

export class GetProductListByProductionDto extends ProductDto {
  id: number
  waitProductionList: GetProductionSumInfoDto[]
  customAttributeList: CustomAttribute[]
  selected: boolean
  unitList: SelectOption[] // 产品单位列表
  constructor(
    // 本类新增字段
    id: number = 0,
    waitProductionList: GetProductionSumInfoDto[] = [],
    customAttributeList: CustomAttribute[] = [],
    selected: boolean = false,
    unitList: SelectOption[] = []
  ) {
    super()
    this.id = id
    this.waitProductionList = waitProductionList
    this.customAttributeList = customAttributeList
    this.selected = selected
    this.unitList = unitList
  }
}

export class FormulaDetailProductDto {
  productId: number
  productNo: string
  productName: string
  productTechCode: string
  productSpecification: string
  productUnit: string
  productMaterialGrade: string

  constructor(
    productId: number = 0,
    productNo: string = '',
    productName: string = '',
    productTechCode: string = '',
    productSpecification: string = '',
    productUnit: string = '',
    productMaterialGrade: string = ''
  ) {
    this.productId = productId
    this.productNo = productNo
    this.productName = productName
    this.productTechCode = productTechCode
    this.productSpecification = productSpecification
    this.productUnit = productUnit
    this.productMaterialGrade = productMaterialGrade
  }
}

export class FormulaDetailDto {
  formulaQuantity?: number = undefined
  formulaUnitId?: number = undefined
  formulaConversionId?: number = undefined
  formulaRatio: number = 0
  // 前端领料编辑字段。
  baseDemandQuantity: number = 0
  convRatio: number = 0
  dataStatus?: InventoryDataStatus = undefined
  supplierId?: number = undefined
  detailRemark?: string = undefined
  convUnitInfo: InventoryUnitOutboundDto[] = []
  id?: number | undefined
  versionId?: number | undefined
  detailType?: FormulaDetailType | undefined
  detailId: number
  order: number
  quantity: number // decimal → number
  ratio: number // decimal → number
  upperTolerance: number // decimal → number
  lowerTolerance: number // decimal → number
  detailDataNo: string
  detailDataName: string
  detailDataTechCode: string
  detailDataSpecification: string
  detailDataUnit: string
  detailDataVersion: string
  detailProductType?: ProductType | undefined
  detailFormulaId?: number | undefined
  children: FormulaDetailDto[] // 递归子级
  replaceable: boolean
  baseQuantity: number // 底数（前端使用）
  curQuantity: number // 当前用量（前端使用）
  curRatio: number // decimal → number
  curBaseQuantity: number // decimal → number
  editable: boolean
  formulaProductInfo?: FormulaDetailProductDto | undefined
  // 领料需求相关
  detailDataMaterialGrade: string // 材质牌号
  detailDataCategoryName: string // 物料类别
  occupyInventory: number // 占用库存
  inventoryQuantity: number // 正常库存
  abnormalQuantity: number // 异常库存
  detailDataUnitId?: number | undefined // 单位
  isDemand: boolean // 是否领用
  totalRatio: number // 明细对于总配方的比例

  specificationId?: number | undefined
  unitList: SelectOption[] // 单位列表
  unitConversionId?: number | undefined // 单位转换ID
  unitId?: number | undefined // 单位ID
  demandQuantity?: number | undefined // 领料需求量
  constructor(
    id?: number | undefined,
    versionId?: number | undefined,
    detailType?: FormulaDetailType | undefined, // 请根据实际枚举调整默认值
    detailId: number = 0,
    order: number = 1,
    quantity: number = 0,
    ratio: number = 0,
    upperTolerance: number = 0,
    lowerTolerance: number = 0,
    detailDataNo: string = '',
    detailDataName: string = '',
    detailDataTechCode: string = '',
    detailDataSpecification: string = '',
    detailDataUnit: string = '',
    detailDataVersion: string = '',
    detailProductType?: ProductType | undefined,
    detailFormulaId?: number | undefined,
    children: FormulaDetailDto[] = [],
    replaceable: boolean = false,
    baseQuantity: number = 0,
    curQuantity: number = 0,
    curRatio: number = 0,
    curBaseQuantity: number = 0,
    editable: boolean = true,
    detailDataMaterialGrade: string = '',
    detailDataCategoryName: string = '',
    occupyInventory: number = 0,
    inventoryQuantity: number = 0,
    abnormalQuantity: number = 0,
    detailDataUnitId?: number | undefined,
    isDemand: boolean = false,
    totalRatio: number = 0,
    specificationId?: number | undefined,
    unitList: SelectOption[] = [],
    unitConversionId?: number | undefined,
    unitId?: number | undefined,
    formulaProductInfo?: FormulaDetailProductDto | undefined
  ) {
    this.id = id
    this.versionId = versionId
    this.detailType = detailType
    this.detailId = detailId
    this.order = order
    this.quantity = quantity
    this.ratio = ratio
    this.upperTolerance = upperTolerance
    this.lowerTolerance = lowerTolerance
    this.detailDataNo = detailDataNo
    this.detailDataName = detailDataName
    this.detailDataTechCode = detailDataTechCode
    this.detailDataSpecification = detailDataSpecification
    this.detailDataUnit = detailDataUnit
    this.detailDataVersion = detailDataVersion
    this.detailProductType = detailProductType
    this.detailFormulaId = detailFormulaId
    this.children = children
    this.replaceable = replaceable
    this.baseQuantity = baseQuantity
    this.curQuantity = curQuantity
    this.curRatio = curRatio
    this.curBaseQuantity = curBaseQuantity
    this.editable = editable
    this.detailDataMaterialGrade = detailDataMaterialGrade
    this.detailDataCategoryName = detailDataCategoryName
    this.occupyInventory = occupyInventory
    this.inventoryQuantity = inventoryQuantity
    this.abnormalQuantity = abnormalQuantity
    this.detailDataUnitId = detailDataUnitId
    this.isDemand = isDemand
    this.totalRatio = totalRatio
    this.specificationId = specificationId
    this.unitList = unitList
    this.unitConversionId = unitConversionId
    this.unitId = unitId
    this.formulaProductInfo = formulaProductInfo
  }
}

export class InventoryUnitDto {
  id: number = 0
  unitId: number = 0
  unitName: string = ''
  ratio: number = 0
  remark?: string = undefined
  conversionType?: UnitConversionType

  constructor(init?: Partial<InventoryUnitDto>) {
    if (!init) return

    this.id = init.id ?? 0
    this.unitId = init.unitId ?? 0
    this.unitName = init.unitName ?? ''
    this.ratio = init.ratio ?? 0
    this.remark = init.remark
    this.conversionType = init.conversionType
  }
}

export class SaleOrderDetailDispatchDTO {
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
  salesUnitConversionId?: number = undefined
  unitConversionList: SaleUnitConversionDto[] = [] // 单位转换列表，前端使用

  selected: boolean = false // 是否选中，前端使用
  dispatchUnitList: DispatchUnitDto[] = [] // 派工单位列表，前端使用
  dispatchUnitListExMain: DispatchUnitDto[] = [] // 派工单位列表（不包含主单位），前端使用
  dispatchUnitConversionId?: number = undefined // 派工单位转换Id
  dispatchUnitId?: number = undefined // 派工单位Id，前端使用
  dispatchUnit?: string = undefined
  dispatchQuantity?: number = undefined //派工数量

  productUnitId?: number = undefined
  specificationId?: number = undefined

  saleToBaseRatio: number = 0
  salesUnitId: number = 0

  constructor(init?: Partial<SaleOrderDetailDispatchDTO>) {
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

    // 支持传入部分字段初始化（类似 C# 对象初始化器）
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

export class DispatchUnitDto extends InventoryUnitDto {
  dispatchQuantity: number
  unitShowText: string = '' // 前端使用，单位显示文本

  constructor(init?: Partial<DispatchUnitDto>) {
    super()
    this.dispatchQuantity = 0
  }
}

export class InventoryUnitOutboundDto extends InventoryUnitDto {
  outboundQuantity: number = 0

  constructor(init?: Partial<InventoryUnitOutboundDto>) {
    super(init)
    this.outboundQuantity = init?.outboundQuantity ?? 0
  }
}

export class BomDto {
  id?: number = undefined
  productId?: number = undefined
  specificationId?: number = undefined
  customAttributeList?: Array<CustomAttribute> = undefined
  productNo?: string = undefined
  productName?: string = undefined
  productTechnicalCode?: string = undefined
  productSpecification?: string = undefined
  productRemark?: string = undefined

  constructor(init?: Partial<BomDto>) {
    Object.assign(this, init)
  }
}

export class BomVersionDataDto {
  versionId?: number = undefined
  version: BomVersionDto = new BomVersionDto()
  detailList: Array<BomDetailDto> = [new BomDetailDto({})]
  index: number = 0
  newFileData?: Map<number, UploadFile[]> = undefined //前端使用 需要新增的文件 number为detail的order/id

  constructor(init?: Partial<BomVersionDataDto>) {
    Object.assign(this, init)
  }
}

export class BomVersionDto {
  id?: number = undefined
  version: string = 'V1'
  isMain: boolean = false
  status: BomStatus = BomStatus.待审核
  totalQuantity?: number = undefined
  unitConversionId?: number = undefined
  unitInfoList?: Array<InventoryUnitOutboundDto> = undefined
  remark?: string = undefined
  isDeleted: boolean = false
  customAttributeList?: Array<CustomAttribute> = undefined
  fileData: Array<FileStorage> = [] //版本文件
  newFileData: UploadFile[] = [] //新增的版本文件

  //前端使用
  customParaList?: Array<SystemParaTreeDto> = undefined

  constructor(init?: Partial<BomVersionDto>) {
    Object.assign(this, init)
  }
}

export class BomDetailDto {
  id?: number = undefined
  versionId?: number = undefined
  detailType: BomDetailType = BomDetailType.Product
  detailId: number = 0
  order: number = 1
  quantity: number = 0
  ratio: number = 0
  upperTolerance: number = 0
  lowerTolerance: number = 0
  detailDataNo: string = ''
  detailDataName: string = ''
  detailDataTechCode: string = ''
  detailDataSpecification: string = ''
  detailDataUnit: string = ''
  detailDataVersion: string = ''
  detailDataMaterialGrade: string = ''
  detailDataCategoryName: string = ''
  inventoryQuantity: number = 0
  occupyInventory: number = 0
  abnormalQuantity: number = 0
  totalStockQuantity: number = 0
  mainUnitId?: number = undefined
  mainUnitName?: string = undefined
  detailWithProductDto?: DetailWithProductDto = undefined
  detailProductType?: ProductType = undefined
  detailBomId?: number = undefined
  isSubstitute: boolean = false
  mainDetailId?: number = undefined
  mainFormulaDetailId?: number = undefined
  detailDataUnitId?: number = undefined
  unitInfoList?: Array<InventoryUnitOutboundDto> = undefined
  unitConversionId?: number = undefined
  unit: string = ''
  formulaDetailId?: number = undefined

  children: Array<BomDetailDto> = []

  indexField: string = '' //前端使用
  baseQuantity: number = 0 //底数 前端使用
  //当前用量 前端使用
  curQuantity: number = 0
  curRatio: number = 0
  curBaseQuantity: number = 0
  editable: boolean = true //是否可编辑 前端使用

  isEdit: boolean = false
  highlight: boolean = false //前端使用，行高亮

  replaceData: Array<BomDetailDto> = [] //替换料
  fileData: Array<FileStorage> = [] //阶次文件
  unitData: Array<any> = [] //产品可选单位数据
  newFileData: UploadFile[] = [] //新增的阶次文件

  constructor(init?: Partial<BomDetailDto>) {
    Object.assign(this, init)
  }
}

export class BomResourceDto {
  code?: string = undefined
  name?: string = undefined
  unit?: string = undefined
  techCode?: string = undefined
  version?: string = undefined
  specification?: string = undefined
  remark?: string = undefined
  bomId?: number = undefined
  detailType: BomDetailType = BomDetailType.Product
  detailId: number = 0
  bomVersion?: string = undefined
  bomRemark?: string = undefined
  formulaCode?: string = undefined
  formulaName?: string = undefined
  formulaVersion?: string = undefined
  formulaRemark?: string = undefined
  productCode?: string = undefined
  productName?: string = undefined
  productRemark?: string = undefined
  productType?: ProductType = undefined
  formulaId?: number = undefined
  productId?: number = undefined
  specificationId?: number = undefined

  constructor(init?: Partial<BomResourceDto>) {
    Object.assign(this, init)
  }
}

export class BomIndexDto {
  versionId: number = 0
  bomId: number = 0
  bomStatus: BomStatus = BomStatus.待审核
  version: string = ''
  isMain: boolean = false
  remark?: string = undefined
  status: BomStatus = BomStatus.待审核
  productId?: number = undefined
  specificationId?: number = undefined
  productTechnicalCode?: string = undefined
  productNo?: string = undefined
  productName?: string = undefined
  productRemark?: string = undefined
  productSpecification?: string = undefined

  createUserName?: string = undefined

  createTime?: Date = undefined

  isDeleted: boolean = false

  versionCustomAttribute?: string = undefined

  bomIsDeleted: boolean = false
  totalQuantity: number = 0
  unitConversionId?: number = undefined
  unitInfoList: InventoryUnitOutboundDto[] = []

  constructor(init?: Partial<BomIndexDto>) {
    Object.assign(this, init)
  }
}

export class DetailWithProductDto {
  productId: number = 0
  specificationId?: number = undefined
  productNo: string = ''
  productName: string = ''
  productTechCode: string = ''
  productSpecification: string = ''
  productUnit: string = ''
  productMaterialGrade: string = ''

  constructor(init?: Partial<DetailWithProductDto>) {
    Object.assign(this, init)
  }
}

/** 领料页单位选项；同一来源内 ratio 均相对于该来源的基础单位。 */
export class BomDemandUnitDto extends InventoryUnitDto {
  value: number = 0
  text: string = ''
  isMainUnit: boolean = false

  constructor(init?: Partial<BomDemandUnitDto>) {
    super()
    Object.assign(this, init)
  }
}

/** 前端 BOM 领料草稿；indexField 是树节点键，id 保留 BOM 明细原 ID。 */
export class BomDemandDetailDto extends BomDetailDto {
  children: BomDemandDetailDto[] = []
  replaceData: BomDemandDetailDto[] = []
  groupKey: string = ''
  isOther: boolean = false
  isGroup: boolean = false
  isDemand: boolean = false
  productId: number = 0
  specificationId?: number = undefined
  unitList: BomDemandUnitDto[] = []
  unitId?: number = undefined
  demandQuantity?: number = undefined
  baseDemandQuantity: number = 0
  demandRatio: number = 0
  standardQuantity: number = 0
  standardRatio: number = 0
  detailRemark: string = ''
  supplierId?: number = undefined
  dataStatus?: InventoryDataStatus = undefined
  convUnitInfo: InventoryUnitOutboundDto[] = []
  unitError: string = ''
  dataId: number = 0
  dataType: InventoryDataType = InventoryDataType.Product

  constructor(init?: Partial<BomDemandDetailDto>) {
    super()
    Object.assign(this, init)
  }
}

/** 返回父页面的 BOM 领料结果，不在子页直接保存。 */
export class BomMaterialDemandsDto {
  basisType: DispatchBasisType = DispatchBasisType.未指定
  basisId?: number = undefined
  bomVersionId?: number = undefined
  bomUnitConversionId?: number = undefined
  bomUnitInfo?: BomDemandUnitDto = undefined
  // 记录确认领料时的派工输入，父页据此判断是否需要重新确认。
  dispatchQuantity?: number = undefined
  dispatchUnitId?: number = undefined
  dispatchUnitConversionId?: number = undefined
  bomQuantity: number = 0
  convRatio: number = 0
  materialDetails: BomDemandDetailDto[] = []
  materials: BomDemandDetailDto[] = []

  constructor(init?: Partial<BomMaterialDemandsDto>) {
    Object.assign(this, init)
  }
}

export class BomDemandCurrentRowDto extends GetDispatchOrderDto {
  productPackageWay?: string = undefined
  materialDemands?: BomMaterialDemandsDto = undefined

  constructor(init?: Partial<BomDemandCurrentRowDto>) {
    super()
    Object.assign(this, init)
  }
}
