import type { Filter } from '@/types/types'

/** 销售退货明细保存的可用单位换算信息。 */
export class ReturnArrivalUnitDTO {
    id = 0
    unitId = 0
    unitName = ''
    ratio = 1

    constructor(init?: Partial<ReturnArrivalUnitDTO>) {
        Object.assign(this, init)
    }
}

/** 退货到货查询条件。 */
export class ReturnArrivalFilterDTO {
    isComplete = false
    saleReturnOrderFilterList: Filter[] = []
    saleReturnOrderDetailFilterList: Filter[] = []
    productFilterList: Filter[] = []
    customerFilterList: Filter[] = []
}

/** 退货到货明细，按销售退货明细维度展示。 */
export class ReturnArrivalDetailDTO {
    id = 0
    saleReturnOrderId = 0
    saleReturnOrderNo = ''
    saleReturnTime?: string | Date
    customerCode = ''
    customerName = ''
    productId = 0
    productNo = ''
    productName = ''
    // 产品/客户产品扩展信息
    customerMaterialNo?: string
    technicalCode?: string
    productCategory?: string
    materialGrade?: string
    materialSpecification?: string
    productSpecification = ''
    salePrice?: number
    saleUnit?: string
    saleUnitId?: number
    productRemark?: string
    // 退货/到货信息
    productUnit = ''
    productUnitId = 0
    returnUnitConversionId = -1
    returnQuantity = 0
    arrivalQuantity = 0
    remainingQuantity = 0
    isComplete = false
    reason?: string
    remark?: string
    unitInfos: ReturnArrivalUnitDTO[] = []

    constructor(init?: Partial<ReturnArrivalDetailDTO>) {
        Object.assign(this, init)
    }
}

/** 销售退货到货记录，按每一条到货记录维度展示。 */
export class ReturnArrivalRecordDTO {
    id = 0
    arrivalRecordNo = ''
    saleReturnOrderDetailId = 0
    saleReturnOrderId = 0
    saleReturnOrderNo = ''
    customerCode = ''
    customerName = ''
    productId = 0
    productNo = ''
    productName = ''
    // 产品/客户产品扩展信息
    customerMaterialNo?: string
    technicalCode?: string
    productCategory?: string
    materialGrade?: string
    materialSpecification?: string
    productSpecification = ''
    salePrice?: number
    saleUnit?: string
    saleUnitId?: number
    productRemark?: string
    productUnit = ''
    productUnitId = 0
    arrivalTime?: string | Date
    arrivalQuantity = 0
    baseArrivalQuantity = 0
    inventoryQuantity = 0
    arrivalUnit = ''
    arrivalUnitConversionId = -1
    isCompleted = false
    remark?: string
    unitInfos: ReturnArrivalUnitDTO[] = []

    constructor(init?: Partial<ReturnArrivalRecordDTO>) {
        Object.assign(this, init)
    }
}

/** 新增销售退货到货记录请求。 */
export class PostReturnArrivalRecordDTO {
    saleReturnOrderDetailId = 0
    arrivalQuantity = 0
    /** -1 表示产品主单位。 */
    arrivalUnitConversionId = -1
    dataStatus = 0
    remark?: string
    arrivalTime: Date = new Date()

    constructor(init?: Partial<PostReturnArrivalRecordDTO>) {
        Object.assign(this, init)
    }
}
