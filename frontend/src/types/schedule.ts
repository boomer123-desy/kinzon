// 排产管理领域 DTO；属性默认值和浅赋值构造方式沿用 technical.ts。
import { MaterialRequirementStatus, ProductionScheduleStatus, ScheduleReceiptStatus } from './enums'

export class MaterialInfoDto {
    dataType: number = 0
    dataId: number = 0
    code: string = ''
    name: string = ''
    specification: string = ''
    unitId: number = 0
    unit: string = ''

    constructor(init?: Partial<MaterialInfoDto>) {
        Object.assign(this, init)
    }
}

export class MaterialQuantityDto extends MaterialInfoDto {
    gross: number = 0
    stock: number = 0
    occupied: number = 0
    safetyStock: number = 0
    available: number = 0
    net: number = 0
    transit: number | null = null
    inProduction: number | null = null
    scheduledTotal: number | null = null

    constructor(init?: Partial<MaterialQuantityDto>) {
        super()
        Object.assign(this, init)
    }
}

export class MaterialSubstituteDto extends MaterialInfoDto {
    materialRlIds: Array<number> = []
    nodeSettingTemplateIds: Array<number> = []
    craftVersionId: number | null = null
    quantityPerParentUnit: number = 0

    constructor(init?: Partial<MaterialSubstituteDto>) {
        super()
        Object.assign(this, init)
    }
}

export class MaterialNodeDto extends MaterialQuantityDto {
    id: string = ''
    parentId: string = ''
    level: number = 0
    kind: 'semi' | 'material' = 'material'
    status: MaterialRequirementStatus = MaterialRequirementStatus.无需需求
    sourceCraftVersionId: number = 0
    materialRlIds: Array<number> = []
    nodeSettingTemplateIds: Array<number> = []
    craftVersionId: number | null = null
    children: Array<MaterialNodeDto> = []
    substitutes: Array<MaterialSubstituteDto> = []

    constructor(init?: Partial<MaterialNodeDto>) {
        super()
        Object.assign(this, init)
    }
}

export class MaterialTotalDto extends MaterialQuantityDto {
    occurrenceCount: number = 0

    constructor(init?: Partial<MaterialTotalDto>) {
        super()
        Object.assign(this, init)
    }
}

export class QuantityResultDto {
    craftOrderId: number = 0
    craftOrderCode: string = ''
    craftOrderName: string = ''
    craftVersionId: number = 0
    craftVersion: string = ''
    product: MaterialInfoDto = new MaterialInfoDto()
    quantity: number = 0
    quantityUnitId: number = 0
    quantityUnit: string = ''
    quantityBasis: string = ''
    nodeCount: number = 0
    nodes: Array<MaterialNodeDto> = []
    materials: Array<MaterialTotalDto> = []

    constructor(init?: Partial<QuantityResultDto>) {
        Object.assign(this, init)
    }
}

export class ScheduleProductDto {
    code: string = ''
    name: string = ''
    customerCode: string = ''
    specification: string = ''
    technicalCode: string = ''
    unit: string = ''
    orderCode: string = ''
    version: string = ''
    owner: string = ''

    constructor(init?: Partial<ScheduleProductDto>) {
        Object.assign(this, init)
    }
}

export class RequirementSnapshotDto {
    revision: number = 0
    productQuantity: number = 0
    requiredDate: string = ''
    gross: number = 0
    available: number = 0
    incoming: number = 0
    net: number = 0
    parentId: string = ''
    source: string = ''

    constructor(init?: Partial<RequirementSnapshotDto>) {
        Object.assign(this, init)
    }
}

export class PlannedReceiptDto {
    id: string = ''
    quantity: number = 0
    arrival: string = ''
    supplier: string = ''
    status: ScheduleReceiptStatus = ScheduleReceiptStatus.待入库

    constructor(init?: Partial<PlannedReceiptDto>) {
        Object.assign(this, init)
    }
}

export class DispatchRowDto {
    id: number = 0
    operator: string = ''
    machine: string = ''
    quantity: number | string = 0

    constructor(init?: Partial<DispatchRowDto>) {
        Object.assign(this, init)
    }
}

export class ProcessSettingDto {
    code: string = ''
    name: string = ''
    craftCode: string = ''
    inbound: boolean = false
    outsource: boolean = false
    dispatches: Array<DispatchRowDto> = []
    start: string = ''
    end: string = ''
    predecessor: string = ''
    supplier: string = ''
    warehouse: string = ''
    setupHours: number = 0
    hoursPerUnit: number = 0

    constructor(init?: Partial<ProcessSettingDto>) {
        Object.assign(this, init)
    }
}

export class ScheduleMaterialDto {
    id: string = ''
    required: number = 0
    consumed: number = 0
    issued: boolean = false
    reserved: number = 0
    shortage: number = 0

    constructor(init?: Partial<ScheduleMaterialDto>) {
        Object.assign(this, init)
    }
}

export class ScheduleDetailsDto {
    immediateStart: boolean = false
    urgent: boolean = false
    circulationCard: boolean = false
    remark: string = ''
    processes: Array<ProcessSettingDto> = []
    materials: Array<ScheduleMaterialDto> = []

    constructor(init?: Partial<ScheduleDetailsDto>) {
        Object.assign(this, init)
    }
}

export class ScheduleSubmissionDto {
    nodeId: string = ''
    quantity: number = 0
    start: string = ''
    end: string = ''
    details: ScheduleDetailsDto = new ScheduleDetailsDto()

    constructor(init?: Partial<ScheduleSubmissionDto>) {
        Object.assign(this, init)
    }
}

export class ScheduleOrderDto {
    id: string = ''
    quantity: number = 0
    start: string = ''
    end: string = ''
    details?: ScheduleDetailsDto = undefined
    status?: ProductionScheduleStatus = undefined
    requirementId?: string = undefined
    snapshot?: RequirementSnapshotDto = undefined

    constructor(init?: Partial<ScheduleOrderDto>) {
        Object.assign(this, init)
    }
}

export class ScheduleNodeDto {
    id: string = ''
    name: string = ''
    code: string = ''
    specification: string = ''
    unit: string = ''
    kind: 'semi' | 'material' = 'material'
    //前端使用：独立规划模型的标准用量，真实查询保留服务端需求结果。
    baseDemand: number = 0
    stock: number = 0
    occupied: number = 0
    safetyStock?: number = undefined
    receipts?: Array<PlannedReceiptDto> = undefined
    orders: Array<ScheduleOrderDto> = []
    children: Array<ScheduleNodeDto> = []
    //前端使用：保留接口节点及真实来源 ID，不参与本地重新净算。
    serverMaterial?: MaterialNodeDto = undefined

    constructor(init?: Partial<ScheduleNodeDto>) {
        Object.assign(this, init)
    }
}

export class ScheduleTreeRowDto extends ScheduleNodeDto {
    //前端使用：TreeGrid 的节点状态和原始业务节点。
    children: Array<ScheduleTreeRowDto> = []
    source: ScheduleNodeDto = new ScheduleNodeDto()
    state: 'open' | 'closed' = 'open'
    iconCls: string = ''

    constructor(init?: Partial<ScheduleTreeRowDto>) {
        super()
        Object.assign(this, init)
    }
}

export class PlanContextDto {
    quantity: number = 0
    start: string = ''
    end: string = ''
    revision: number = 0
    product?: ScheduleProductDto = undefined

    constructor(init?: Partial<PlanContextDto>) {
        Object.assign(this, init)
    }
}

export class MaterialRequirementDto extends RequirementSnapshotDto {
    id: string = ''
    node: ScheduleNodeDto = new ScheduleNodeDto()
    physicalAvailable: number = 0
    reserved: number = 0
    scheduled: number = 0
    late: number = 0
    production: number = 0
    status: MaterialRequirementStatus = MaterialRequirementStatus.无需需求
    receiptSupplies: Array<{ quantity: number; arrival: string }> = []

    constructor(init?: Partial<MaterialRequirementDto>) {
        super()
        Object.assign(this, init)
    }
}

export class MaterialPlanDto {
    node: ScheduleNodeDto = new ScheduleNodeDto()
    perUnit: number = 0
    required: number = 0
    available: number = 0
    incoming: number = 0
    shortage: number = 0
    readyAt: string = ''

    constructor(init?: Partial<MaterialPlanDto>) {
        Object.assign(this, init)
    }
}

export class ResourceConflictDto {
    resource: string = ''
    first: string = ''
    second: string = ''

    constructor(init?: Partial<ResourceConflictDto>) {
        Object.assign(this, init)
    }
}

export class GanttEntryDto {
    key: string = ''
    node: ScheduleNodeDto = new ScheduleNodeDto()
    order: ScheduleOrderDto = new ScheduleOrderDto()
    process?: ProcessSettingDto = undefined
    start: string = ''
    end: string = ''
    risk: string = ''
    conflict: boolean = false

    constructor(init?: Partial<GanttEntryDto>) {
        Object.assign(this, init)
    }
}

export class TimelineTaskDto {
    id: string = ''
    parent: string | number = 0
    text: string = ''
    start: Date = new Date()
    end: Date = new Date()
    type: 'summary' | 'task' = 'task'
    open: boolean = true
    nodeId?: string = undefined
    orderId?: string = undefined
    processCode?: string = undefined
    quantity?: number = undefined
    status?: ProductionScheduleStatus = undefined
    //前端使用：节点分类或风险提示的展示文本。
    statusText: string = ''
    details?: string = undefined
    locked?: boolean = undefined

    constructor(init?: Partial<TimelineTaskDto>) {
        Object.assign(this, init)
    }
}

export class ScheduleTimeChangeDto {
    orderId: string = ''
    processCode?: string = undefined
    start: string = ''
    end: string = ''

    constructor(init?: Partial<ScheduleTimeChangeDto>) {
        Object.assign(this, init)
    }
}

export class TimelineDataDto {
    tasks: Array<TimelineTaskDto> = []
    links: Array<{ id: string; source: string; target: string; type: 'e2s' }> = []

    constructor(init?: Partial<TimelineDataDto>) {
        Object.assign(this, init)
    }
}

export class ScheduleListRowDto {
    id: string = ''
    title: string = ''
    statusText: string = ''
    quantity: number | null = null
    start: string = ''
    end: string = ''
    status: ProductionScheduleStatus = ProductionScheduleStatus.已排产
    //前端使用：排产单列表中的未保存空行。
    draft: boolean = false

    constructor(init?: Partial<ScheduleListRowDto>) {
        Object.assign(this, init)
    }
}

export class ScheduleDraftDto {
    //前端使用：按半成品保留未保存表单。
    id: string = ''
    nodeId: string = ''

    constructor(init?: Partial<ScheduleDraftDto>) {
        Object.assign(this, init)
    }
}

export class ScheduleFormDto {
    //前端使用：DateTimeBox 的日期对象，提交时转换为本地时间文本。
    quantity: number | null = null
    start: Date | null = null
    end: Date | null = null
    immediateStart: boolean = false
    urgent: boolean = false
    circulationCard: boolean = false
    remark: string = ''

    constructor(init?: Partial<ScheduleFormDto>) {
        Object.assign(this, init)
    }
}

export class ReceiptFormDto {
    //前端使用：采购建议编辑模型，当前尚未接入保存接口。
    quantity: number | null = null
    arrival: Date | null = null
    supplier: string = ''

    constructor(init?: Partial<ReceiptFormDto>) {
        Object.assign(this, init)
    }
}



/** 独立排产编辑器：字段与 ProductionScheduleDto.cs 对齐。 */
export class ProductionSchedulePostDto {
    rootCraftVersionId: number = 0
    rootDispatchOrderId: number | null = null
    departmentId: number | null = null
    craftVersionId: number = 0
    quantity: number = 1
    start: string = ''
    end: string = ''
    immediateStart: boolean = false
    urgent: boolean = false
    circulationCard: boolean = false
    remark: string = ''
    processs: Array<ProductionScheduleProcessDto> = []
    constructor(init?: Partial<ProductionSchedulePostDto>) { Object.assign(this, init) }
}

export class ProductionScheduleProcessDto {
    code: string = ''
    nodeSettingTemplateId: number = 0
    order: number = 0
    name: string = ''
    craftId: number = 0
    isOutsource: boolean = false
    isInbound: boolean = false
    isReportWork: boolean = false
    materials: Array<ProductionScheduleMaterialDto> = []
    machineRls: Array<ProductionScheduleMachineRlDto> = []
    constructor(init?: Partial<ProductionScheduleProcessDto>) { Object.assign(this, init) }
}

export class ProductionScheduleMaterialDto {
    order: number = 0
    dataId: number = 0
    dataType: number = 0
    quantity: number = 0
    unitId: number = 0
    unitConversionId: number | null = null
    convUnitInfo: string = '[]'
    constructor(init?: Partial<ProductionScheduleMaterialDto>) { Object.assign(this, init) }
}

export class ProductionScheduleMachineRlDto {
    machineId: number = 0
    userId: number = 0
    quantity: number = 0
    hours: number = 0
    constructor(init?: Partial<ProductionScheduleMachineRlDto>) { Object.assign(this, init) }
}

/** 数量为当前物料单位到产品主单位的比例，与领料需求的 ConvUnitInfo 口径一致。 */
export class ScheduleUnitOptionDto {
    id: number = 0
    unitId: number = 0
    unitName: string = ''
    ratio: number = 1
    quantity: number = 0
    constructor(init?: Partial<ScheduleUnitOptionDto>) { Object.assign(this, init) }
}

export class ScheduleMaterialEditDto extends ProductionScheduleMaterialDto {
    key: string = ''
    code: string = ''
    name: string = ''
    /** 每一排产单位的耗用系数；修改数量时同步更新此系数。 */
    quantityPerUnit: number = 0
    unitOptions: Array<ScheduleUnitOptionDto> = []
    constructor(init?: Partial<ScheduleMaterialEditDto>) { super(); Object.assign(this, init) }
}

export class SchedulePersonEditDto {
    userId: number = 0
    quantity: number = 0
    hours: number = 0
    constructor(init?: Partial<SchedulePersonEditDto>) { Object.assign(this, init) }
}

export class ScheduleMachineEditDto {
    machineId: number = 0
    quantity: number = 0
    hours: number = 0
    users: Array<SchedulePersonEditDto> = []
    constructor(init?: Partial<ScheduleMachineEditDto>) { Object.assign(this, init) }
}

export class ScheduleProcessEditDto extends ProductionScheduleProcessDto {
    key: string = ''
    materials: Array<ScheduleMaterialEditDto> = []
    machines: Array<ScheduleMachineEditDto> = []
    constructor(init?: Partial<ScheduleProcessEditDto>) { super(); Object.assign(this, init) }
}

export class ScheduleMachineOptionDto {
    id: number = 0
    text: string = ''
    craftIds: Array<number> = []
    users: Array<{ id: number; text: string }> = []
    constructor(init?: Partial<ScheduleMachineOptionDto>) { Object.assign(this, init) }
}

export class ProductionScheduleTemplateDto {
    craftVersionId: number = 0
    version: string = ''
    productCode: string = ''
    productName: string = ''
    quantityUnit: string = ''
    processs: Array<ScheduleProcessEditDto> = []
    crafts: Array<{ id: number; name: string }> = []
    departments: Array<{ value: number; text: string }> = []
    machines: Array<ScheduleMachineOptionDto> = []
    constructor(init?: Partial<ProductionScheduleTemplateDto>) { Object.assign(this, init) }
}


/** 前端库存试算结果，不代表锁定库存或实际领料。null 表示尚未查询或缺少单位关系。 */
export class ScheduleMaterialAvailabilityDto {
    allocated: number | null = null
    shortage: number | null = null
    constructor(init?: Partial<ScheduleMaterialAvailabilityDto>) { Object.assign(this, init) }
}

/** 实际排产单查询结果，状态文字取自后端枚举。 */
export class SavedScheduleMachineDto {
    id: number = 0
    machineId: number = 0
    machineName: string = ''
    userId: number | null = null
    userName: string = ''
    quantity: number = 0
    hours: number = 0
    actualWorkHours: number = 0
    constructor(init?: Partial<SavedScheduleMachineDto>) { Object.assign(this, init) }
}
export class SavedScheduleMaterialDto {
    id: number = 0
    code: string = ''
    name: string = ''
    demandQuantity: number = 0
    unitName: string = ''
    baseDemandQuantity: number = 0
    baseReceiveQuantity: number = 0
    baseReturnQuantity: number = 0

    constructor(init?: Partial<SavedScheduleMaterialDto>) { Object.assign(this, init) }
}

export class SavedScheduleProcessDto {
    materials: Array<SavedScheduleMaterialDto> = []
    id: number = 0
    order: number = 0
    name: string = ''
    craftName: string = ''
    planWorkHours: number = 0
    actualWorkHours: number = 0
    reciveQuantity: number = 0
    completeQuantity: number = 0
    materialCount: number = 0
    machines: SavedScheduleMachineDto[] = []
    constructor(init?: Partial<SavedScheduleProcessDto>) { Object.assign(this, init) }
}
export class SavedScheduleDto {
    rootDispatchOrderId: number | null = null
    canStart: boolean = false
    canDelete: boolean = false
    id: number = 0
    orderNo: string = ''
    craftVersionId: number = 0
    quantity: number = 0
    start: string = ''
    end: string | null = null
    statusText: string = ''
    dispatchUnit: string = '{}'
    departmentName: string = ''
    urgent: boolean = false
    remark: string | null = null
    processes: SavedScheduleProcessDto[] = []
    constructor(init?: Partial<SavedScheduleDto>) { Object.assign(this, init) }
}
