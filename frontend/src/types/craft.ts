/**
 * 标准工序单（craft）模块 DTO
 * 对应后端 technical/craft/dto/CraftDto.cs
 */

import type { Filter } from '@kingzon/base-pc'
import { CraftOrderVersionStatus } from './enums'

/** 对应 JoinWithData 返回的 KingZon.OLCM.Base.Service.Dto.DataInfo。 */
export interface DataInfo {
  dataNo?: string
  dataName?: string
  dataSpecification?: string
  dataMaterialGrade?: string
  dataProductType?: string
  dataCategory?: string
  dataCategoryId?: number
  dataUnit?: string
  dataUnitId?: number
  dataTechnicalCode?: string
  dataCustomerMaterialNo?: string
  dataMaterialNoCustomerName?: string
  dataRemark?: string
  dataMinStock?: number
  dataMaxStock?: number
  dataTotalStock?: number
  dataBaseUnitId?: number
  dataBaseUnitName?: string
  toBaseUnitRatio?: number
  toBaseUnitConversionId?: number
  dataProductId?: number
  currentPurchasePrice?: number
}

export type CraftResourceTarget = 'input' | 'output' | 'substitute'

/** 工艺结构树节点类别，仅用于界面结构和图标判断。 */
export enum CraftTreeNodeType {
  Process = 1,
  Input = 2,
  Output = 3,
  Product = 4,
  SemiProduct = 5,
  Material = 6,
  Substitute = 7
}

/**
 * 工艺单列表查询条件
 */
export class CraftOrderListFilterDTO {
  craftOrderFilterList: Array<Filter> = []
  productFilterList: Array<Filter> = []

  constructor(init?: Partial<CraftOrderListFilterDTO>) {
    Object.assign(this, init)
  }
}

/**
 * 工艺单基本信息
 */
export class CraftOrderDto {
  materialInfo?: DataInfo = undefined
  id?: number = undefined

  /** 工艺单编码 */
  code: string = ''

  /** 工艺单名称 */
  name: string = ''

  /** 工艺单说明 */
  description?: string = undefined

  /** 产品 Id */
  productId?: number = undefined

  /** 产品规格 Id */
  specificationId?: number = undefined

  /** 产品编码 */
  productNo: string = ''

  /** 产品名称 */
  productName: string = ''

  /** 规格名称 */
  specificationName?: string = undefined

  /** 生效日期 */
  effectiveDate?: string | Date = undefined

  /** 状态 */
  status?: number = undefined

  createUserName?: string = undefined
  createTime?: string | Date = undefined

  updateUserName?: string = undefined
  updateTime?: string | Date = undefined

  constructor(init?: Partial<CraftOrderDto>) {
    Object.assign(this, init)
  }
}

/**
 * 工艺单列表行
 */
export class CraftListOrderDto {
  id: number = 0

  /** 工艺单编码 */
  code: string = ''

  /** 工艺单名称 */
  name: string = ''

  /** 工艺单说明 */
  description?: string = undefined

  /** 产品 Id */
  productId: number = 0

  /** 规格 Id */
  specificationId?: number = undefined

  /** 产品编码 */
  productNo: string = ''

  /** 产品名称 */
  productName: string = ''

  /** 规格名称 */
  specificationName?: string = undefined

  /** 版本数量 */
  versionCount: number = 0

  /** 当前主版本，如 V2 */
  mainVersion?: string = undefined

  createUserName?: string = undefined
  createTime?: string | Date = undefined

  constructor(init?: Partial<CraftListOrderDto>) {
    Object.assign(this, init)
  }
}

/** 将来源版本复制为目标工艺单的新版本。 */
export class CopyCraftVersionDto {
  craftOrderId: number = 0
  sourceCraftOrderId: number = 0
  sourceVersionId: number = 0

  constructor(init?: Partial<CopyCraftVersionDto>) {
    Object.assign(this, init)
  }
}

/**
 * 工艺单版本列表项
 */
export class CraftVersionDto {
  id?: number = undefined

  /** 所属工艺单 Id */
  craftOrderId: number = 0

  /** 工艺单编码 */
  craftOrderCode: string = ''

  /** 工艺单名称 */
  craftOrderName: string = ''

  /** 数字版本号：1、2、3... */
  sequenceNo: number = 0

  /** 展示版本：V1、V2... */
  version: string = 'V1'

  /** 是否主版本 */
  isMain: boolean = false

  /** 版本状态 */
  status: CraftOrderVersionStatus = CraftOrderVersionStatus.暂存

  /** 备注 */
  remark?: string = undefined

  /** 工序数量 */
  nodeCount: number = 0

  createUserName?: string = undefined
  createTime?: string | Date = undefined

  constructor(init?: Partial<CraftVersionDto>) {
    Object.assign(this, init)
  }
}

/**
 * 工艺单版本完整详情
 */
export class CraftVersionDetailDto {
  id?: number = undefined

  craftOrderId: number = 0

  craftOrderCode: string = ''

  craftOrderName: string = ''

  sequenceNo: number = 0

  version: string = 'V1'

  isMain: boolean = false

  status: CraftOrderVersionStatus = CraftOrderVersionStatus.暂存

  remark?: string = undefined

  /** 工序列表 */
  nodeSettings: Array<CraftProcessDto> = []

  constructor(init?: Partial<CraftVersionDetailDto>) {
    Object.assign(this, init)
  }
}

/**
 * 前端工序编辑模型。
 *
 * 与后端 NodeSettingTemplateDto 保持字段对应；页面、Header 和保存请求均使用此模型，
 * 避免在组件内散落匿名 object。
 */
let nextProcessKey = 0

export class CraftProcessDto {
  /** 仅用于界面身份，不随工序编号变化。 */
  uiKey: string = `draft-${++nextProcessKey}`
  theoreticalWorkHours: number = 0
  isReportWork: boolean = false
  machineSettings: Array<{ machineId: number; theoreticalWorkHours: number }> = []
  /** id undefined / 0 表示新增 */
  id?: number = undefined

  /** 所属工艺版本 Id，仅查询结果回填时使用 */
  craftOrderVersionId?: number = undefined

  /** 工序顺序，如 10、20、30 */
  sequence: number = 0

  /** 工序编码 */
  code: string = ''

  /** 工序名称 */
  name: string = ''

  /** 是否外协 */
  isOutsource: boolean = false

  /** 是否入库 */
  isInbound: boolean = false

  /** 工序说明 */
  description?: string = undefined

  /** 工序模板 Id */
  nodeTemplateId?: number = undefined

  /** Base_Craft Id */
  craftId?: number = undefined

  /** Base_Craft 名称（仅界面展示，保存时由 craftId 确定） */
  craftName?: string = undefined

  /** 设备 Id */
  machineIds: Array<number> = []

  /** 前端 / 保存接口软删除标志 */
  isDeleted: boolean = false

  /** 工序领用 / 产出物料 */
  materials: Array<CraftProcessMaterialDto> = []

  constructor(init?: Partial<CraftProcessDto>) {
    Object.assign(this, init)
  }
}

/** 领用物料可引用的已发布工艺版本。 */
export class CraftVersionOptionDto {
  id: number = 0
  text: string = ''
  isMain: boolean = false

  constructor(init?: Partial<CraftVersionOptionDto>) {
    Object.assign(this, init)
  }
}

/**
 * 工序资料库行。
 * 对应后端 CraftResourceDto；dataType 为 InventoryDataType 的数值。
 */
export class CraftResourceDto {
  dataId: number = 0
  dataType: number = 0
  code: string = ''
  name: string = ''
  unitId: number = 0
  unitName: string = ''
  /** 物料分类：物料、半成品或成品 */
  materialType?: string = undefined
  techCode?: string = undefined
  specification?: string = undefined
  materialGrade?: string = undefined
  version?: string = undefined
  remark?: string = undefined

  constructor(init?: Partial<CraftResourceDto>) {
    Object.assign(this, init)
  }
}

/** @deprecated 请使用 CraftProcessDto。保留别名以兼容既有引用。 */
export class NodeSettingTemplateDto extends CraftProcessDto {}

/**
 * 保存工艺单版本
 */
export class SaveCraftVersionDto {
  /** 版本 Id */
  id: number = 0

  /** 工艺单 Id */
  craftOrderId: number = 0

  /** 版本备注 */
  remark?: string = undefined

  /** 是否设置为主版本 */
  isMain: boolean = false

  /** 工序列表 */
  nodeSettings: Array<CraftProcessDto> = []

  constructor(init?: Partial<SaveCraftVersionDto>) {
    Object.assign(this, init)
  }
}

/**
 * 创建新版本
 */
export class CreateCraftVersionDto {
  craftOrderId: number = 0

  remark?: string = undefined

  constructor(init?: Partial<CreateCraftVersionDto>) {
    Object.assign(this, init)
  }
}

/**
 * 发布版本
 */
export class PublishCraftVersionDto {
  id: number = 0

  /** 是否废弃旧主版本 */
  obsoleteOldMain: boolean = true

  constructor(init?: Partial<PublishCraftVersionDto>) {
    Object.assign(this, init)
  }
}

/**
 * 工序物料
 */
export class CraftProcessMaterialDto {
  id?: number = undefined

  /** 所属工序 Id */
  nodeSettingTemplateId?: number = undefined

  /** InventoryDataType */
  dataType: number = 0

  /** 产品 / 物料 Id */
  dataId: number = 0

  /** NodeIOType */
  ioType: number = 0

  /**
   * 替料来源 Id
   * 当前行为替代料时，指向原物料行
   */
  insteadSourceId?: number = undefined

  /** 数量 */
  quantity: number = 0

  /** 单位 Id */
  unitId?: number = undefined

  /** 单位显示名称（查询结果字段） */
  unitName: string = ''

  /** 单位换算 Id */
  unitConversionId?: number = undefined

  /** 单位转换 Json */
  unitListInfo?: string = undefined

  /** 后端展开的可用单位列表 */
  unitList: Array<unknown> = []

  /** 损耗率 */
  lossRate: number = 0

  /** 排序 */
  sortNo: number = 0

  /** 引用的外部半成品工艺版本 */
  refCraftOrderVersionId?: number = undefined

  /** 备注 */
  remark?: string = undefined

  // -------------------------
  // 前端显示 / 后端翻译字段
  // -------------------------

  /** 物料编码 */
  materialNo?: string = undefined

  /** 物料名称 */
  materialName?: string = undefined

  /** 物料分类：物料、半成品或成品 */
  materialType?: string = undefined

  /** 替代料 */
  substitutes: Array<CraftProcessMaterialDto> = []

  constructor(init?: Partial<CraftProcessMaterialDto>) {
    Object.assign(this, init)
  }
}

/** @deprecated 请使用 CraftProcessMaterialDto。 */
export class NodeMaterialDto extends CraftProcessMaterialDto {}

/** 当前加工工艺的可用机器，来自 GetCraftMachineList。 */
export interface CraftMachineResource {
  id: number
  craftId: number
  code: string
  name: string
  model?: string
  categoryName?: string
}

/** 工艺结构树的接口及本地节点。rootMaterial 仅由当前版本的顶层主料设置。 */
export interface CraftTreeSource {
  id?: string | number
  name?: string
  code?: string
  nodeType?: CraftTreeNodeType
  typeText?: string
  processKey?: string
  processCode?: string
  quantityText?: string | number
  dataId?: number
  dataType?: number
  materialType?: string
  /** 引用的外部半成品工艺版本；为空表示没有下级工艺，节点不可展开。 */
  refCraftOrderVersionId?: number
  state?: 'open' | 'closed'
  children?: CraftTreeSource[]
  rootMaterial?: CraftProcessMaterialDto
}
export interface CraftTreeRow extends Omit<CraftTreeSource, 'id' | 'children'> {
  id: string
  children: CraftTreeRow[]
  fixedChildren: CraftTreeRow[]
  lazyChildren: CraftTreeRow[]
  iconCls?: string
  isPlaceholder?: boolean
  messageKind?: 'idle' | 'loading' | 'empty' | 'error'
  loadState?: 'idle' | 'loading' | 'loaded' | 'error'
}


/** 工艺单编辑组件事件及第三方组件引用类型。 */
export interface SearchGroup { table: string; filter?: Filter[] }
export interface ProductOption {
    id?: number; productId?: number; specificationId?: number; dataId?: number; dataType?: number
    productName?: string; materialName?: string; name?: string
    productNo?: string; materialNo?: string; code?: string; materialInfo?: DataInfo
}
export type MaterialResource = Partial<CraftProcessMaterialDto> & { code?: string; name?: string }
export interface MaterialPlacement {
    row?: CraftProcessMaterialDto; main?: CraftProcessMaterialDto; position: 'before' | 'after' | 'append'
}
export interface MaterialChange { row: CraftProcessMaterialDto; changes: Partial<CraftProcessMaterialDto> }
export interface ResourceSelection { resources: MaterialResource[]; target: CraftResourceTarget }
export interface ResourceDrop extends ResourceSelection, MaterialPlacement {}
export interface MachineSelection { craftId: number; machines: Array<{ id: number }> }
export interface MachineOptions { craftId: number; rows: CraftMachineResource[]; loading: boolean }
export interface ProcessSelection { key?: string; tab?: number; code?: string; material?: CraftProcessMaterialDto }
export interface DialogRef { open(): void; close(): void }
export interface CreateDialogRef { open(product?: ProductOption | null): void }
export interface TabsRef { select(index: number): void }
export interface MaterialTableRef {
    expandMaterial(row: CraftProcessMaterialDto): void
    focusMaterial(row: CraftProcessMaterialDto): Promise<boolean>
}
export interface MaterialEditorRef {
    insertResources(resources: MaterialResource[], target: CraftResourceTarget, placement?: MaterialPlacement): number
    focusMaterial(row: CraftProcessMaterialDto): Promise<boolean>
}
export interface MachineEditorRef { insertMachines(payload: MachineSelection): number }
export interface ResourceLibraryRef { clearSelection(): void }
export interface ProcessEditorRef {
    showTab(index: number): void
    showMachines(): void
    focusMaterial(row: CraftProcessMaterialDto): Promise<boolean>
    addMachines(payload: MachineSelection): Promise<number>
    addResources(payload: ResourceSelection): Promise<number>
}
export interface MaterialGridRef {
    selectRow(row: CraftProcessMaterialDto): void
    expandRow(row: CraftProcessMaterialDto): void
}
export interface MaterialGridWrapper extends Partial<MaterialGridRef> {
    dataGrid?: MaterialGridRef
    $refs?: { dataGrid?: MaterialGridRef }
}
export interface TreeGridRef {
    findColumn(field: string): { widthState: number | string } | undefined
    resizeColumn(field: string, width: number): void
}
export interface DrawerRef { expand(): void }
export interface DrawerEntry { id: number; mode: string; craftOrderId: number }
export interface HeaderSaved { craftOrder: CraftOrderDto }
