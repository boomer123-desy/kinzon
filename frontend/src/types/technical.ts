import {
  FormulaStatus,
  FormulaDetailType,
  FrequencyType,
  InspValueMode,
  ProductType
} from '@/types/enums'
import { CustomAttribute } from '@/types/types'
import type { SystemParaTreeDto } from './common'

export class FormulaDto {
  id?: number = undefined
  formulaCode: string = ''
  formulaName: string = ''
  remark?: string = undefined
  productId?: number = undefined
  productCategory?: string = undefined
  productName?: string = undefined
  productTechnicalCode?: string = undefined
  productRemark?: string = undefined
  customAttributeList?: Array<CustomAttribute> = undefined

  constructor(init?: Partial<FormulaDto>) {
    Object.assign(this, init)
  }
}

export class FormulaVersionDataDto {
  version: FormulaVersionDto = new FormulaVersionDto()
  detailList: Array<FormulaDetailDto> = [new FormulaDetailDto({})]
  index: number = 0

  constructor(init?: Partial<FormulaVersionDataDto>) {
    Object.assign(this, init)
  }
}

export class FormulaVersionDto {
  id?: number = undefined
  version: string = 'V1'
  isMain: boolean = false
  status: FormulaStatus = FormulaStatus.待审核
  totalQuantity?: number = undefined
  remark?: string = undefined
  isDeleted: boolean = false
  customAttributeList?: Array<CustomAttribute> = undefined

  //前端使用
  customParaList?: Array<SystemParaTreeDto> = undefined

  constructor(init?: Partial<FormulaVersionDto>) {
    Object.assign(this, init)
  }
}

export class FormulaDetailDto {
  id?: number = undefined
  versionId?: number = undefined
  detailType: FormulaDetailType = FormulaDetailType.Product
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
  detailProductType?: ProductType = undefined
  detailFormulaId?: number = undefined

  children: Array<FormulaDetailDto> = []

  indexField: string = '' //前端使用
  baseQuantity: number = 0 //底数 前端使用
  //当前用量 前端使用
  curQuantity: number = 0
  curRatio: number = 0
  curBaseQuantity: number = 0
  editable: boolean = true //是否可编辑 前端使用

  isEdit: boolean = false

  constructor(init?: Partial<FormulaDetailDto>) {
    Object.assign(this, init)
  }
}

export class FormulaResourceDto {
  code?: string = undefined
  name?: string = undefined
  unit?: string = undefined
  techCode?: string = undefined
  version?: string = undefined
  specification?: string = undefined
  remark?: string = undefined
  detailType: FormulaDetailType = FormulaDetailType.Product
  detailId: number = 0
  formulaCode?: string = undefined
  formulaName?: string = undefined
  formulaVersion?: string = undefined
  formulaRemark?: string = undefined
  productCode?: string = undefined
  productName?: string = undefined
  productRemark?: string = undefined
  productType?: ProductType = undefined
  formulaId?: number = undefined

  constructor(init?: Partial<FormulaResourceDto>) {
    Object.assign(this, init)
  }
}

export class FormulaInspectionDataDto {
  inspection: FormulaInspectionDto = new FormulaInspectionDto()
  detailList: Array<InspectionItemDto> = []
  index: number = 0

  constructor(init?: Partial<FormulaVersionDataDto>) {
    Object.assign(this, init)
  }
}

export class FormulaInspectionDto {
  id?: number = undefined
  name: string = '检验方案'
  isDefault: boolean = false
  customerId?: number = undefined
  qualityStandardId?: number = undefined
  remark?: string = undefined
  isDeleted: boolean = false

  standardCode?: string = undefined
  standardName?: string = undefined
  customerName?: string = undefined
  customerNo?: string = undefined

  constructor(init?: Partial<FormulaInspectionDto>) {
    Object.assign(this, init)
  }
}

export class InspectionItemDto {
  id?: number = undefined
  characteristicId?: number = undefined
  characteristicName?: string = undefined
  inspectionName: string = ''
  inspectionMethod: string = ''
  frequencyType: FrequencyType = FrequencyType.每批
  frequency: number = 1
  equipmentList?: Array<string> = undefined
  equipment?: string = undefined
  safetyTips?: string = undefined
  remark?: string = undefined

  valueList: Array<InspectionItemValueDto> = [new InspectionItemValueDto()] //前端使用
  valueText?: string = undefined //表格中的文本显示

  customerList?: Array<number> = undefined
  customerText?: string = undefined //表格中的文本显示
  groupId?: number = undefined
  isGroupMember: boolean = false

  indexField?: number = undefined //前端使用

  order: number = 1

  constructor(init?: Partial<InspectionItemDto>) {
    Object.assign(this, init)
  }
}

export class InspectionItemValueDto {
  textValue?: string = undefined
  valueMode: InspValueMode = InspValueMode.等于
  standardValue?: number = undefined
  minValue?: number = undefined
  maxValue?: number = undefined
  upperTolerance?: number = undefined
  lowerTolerance?: number = undefined
  unitValue?: string = undefined

  index: number = 0 //前端使用

  constructor(init?: Partial<InspectionItemValueDto>) {
    Object.assign(this, init)
  }
}

export class FormulaIndexDto {
  versionId: number = 0
  formulaId: number = 0

  formulaCode: string = ''
  formulaName: string = ''

  formulaRemark: string | null = null

  formulaStatus: FormulaStatus = FormulaStatus.待审核

  version: string = ''

  isMain: boolean = false

  remark: string | null = null

  status: FormulaStatus = FormulaStatus.待审核

  productId: number | null = null

  productTechnicalCode: string | null = null

  productName: string | null = null

  productRemark: string | null = null

  createUserName: string | null = null

  createTime: Date = new Date()

  isDeleted: boolean = false

  formulaCustomAttribute: string | null = null

  versionCustomAttribute: string | null = null

  formulaCustomAttributeList?: Array<CustomAttribute> = undefined
  versuCustomAttributeList?: Array<CustomAttribute> = undefined

  constructor(init?: Partial<FormulaIndexDto>) {
    Object.assign(this, init)
  }
}
