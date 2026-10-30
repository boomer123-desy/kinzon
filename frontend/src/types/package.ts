export class AddPackageDto {
  id: number | undefined
  packageSpecification: string
  packageUnitId: number | undefined
  productId: number | undefined
  specificationId: number | undefined
  uploadFileData: Array<UploadFile>
  packageUnit: string
  fileList: []
  remark: string | undefined
  palletPackageRadio: number | undefined
  packageWegiht: number | undefined
  palletWegiht: number | undefined
  mainUnitRatio: number | undefined
  index: number
  customAttributeList: CustomAttribute[]
  customerParaList: SystemParaTreeDto[] | undefined
  packTempLateId?: number | undefined
  customerIds: Array<number> = []

  constructor(init?: Partial<AddPackageDto>) {
    this.id = undefined
    this.packageSpecification = ''
    this.packageUnitId = undefined
    this.productId = undefined
    this.specificationId = undefined
    this.uploadFileData = Array<UploadFile>()
    this.packageUnit = ''
    this.fileList = []
    this.remark = undefined
    this.palletPackageRadio = undefined
    this.packageWegiht = undefined
    this.palletWegiht = undefined
    this.mainUnitRatio = undefined
    this.index = 0
    this.customAttributeList = []
    this.customerParaList = undefined
    this.packTempLateId = undefined

    Object.assign(this, init)
  }
}
