<template>
    <Loading v-if="isLoading"></Loading>
    <div class="kingzon-page-content">
        <KingZonShowForm :formData="formData" :columnWidth="'20%'"></KingZonShowForm>
        <Form ref="form" :model="submitData" :rules="rules" :messages="messages" labelAlign="right" :labelWidth="100"
            style="width:100%;justify-content: center;" class="kingzon-page-form" @validate="errors = $event">
            <FormField name="bomVersionId" label="物料清单:" style="width:600px;">
                <ComboBox v-model="submitData.bomVersionId" :data="bomOptions" :disabled="isLoading"
                    @selectionChange="getBomData">
                    <Addon>
                        <span v-if="submitData.bomVersionId" class="textbox-icon icon-clear"
                            @click.stop="clearBom"></span>
                    </Addon>
                </ComboBox>
            </FormField>
        </Form>
        <div class="kingzon-btn-group" style="margin-top: -30px;margin-bottom: 5px;margin-left: 5px;">
            <LinkButton @click="showSeleteDrawer">选择其他物料</LinkButton>
        </div>
        <TreeGrid class="kingzon-page-fill" :data="materialDetails" treeField="detailType" :selectionMode="'none'"
            id="id" idField="indexField" :dblclickToEdit="true" editMode="cell" :pagination="false" ref="treeGrid"
            :columnResizing="true" :lazy="true">
            <GridColumnGroup :frozen="true" align="left" width="420">
                <GridHeaderRow>
                    <GridColumn field="detailType" title="类型" width="150" align="left">
                        <template #body="scope">{{ typeText(scope.row) }}</template>
                    </GridColumn>
                    <GridColumn field="detailWithProductDto.productNo" title="物料编码" width="120" align="left">
                    </GridColumn>
                    <GridColumn field="detailWithProductDto.productName" title="物料名称" width="150" align="left"
                        :overflowTextEllipsis="true">

                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup align="center" :columnMoving="true">
                <GridHeaderRow>
                    <GridColumn field="detailWithProductDto.productTechCode" title="技术型号" width="100" align="left"
                        :rowspan="2" :overflowTextEllipsis="true">

                    </GridColumn>
                    <GridColumn field="detailWithProductDto.productCategoryName" title="物料类别" width="100" align="left"
                        :rowspan="2" :overflowTextEllipsis="true">

                    </GridColumn>
                    <GridColumn field="detailWithProductDto.productSpecification" title="规格型号" width="100" align="left"
                        :rowspan="2" :overflowTextEllipsis="true">

                    </GridColumn>
                    <GridColumn field="detailWithProductDto.productMaterialGrade" title="材质牌号" width="100" align="left"
                        :rowspan="2" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn title="标准用量" width="100" align="center" :colspan="2">
                    </GridColumn>
                    <GridColumn title="库存" width="100" align="center" :colspan="3">
                    </GridColumn>
                </GridHeaderRow>
                <GridHeaderRow>
                    <GridColumn field="quantity" title="用量" width="120" align="right">
                        <template #body="scope">{{
                            scope.row.isGroup || scope.row.isOther ? '—' : quantityText(scope.row.quantity)
                        }} {{ scope.row.isGroup || scope.row.isOther ? '' : scope.row.unit }}</template>
                    </GridColumn>
                    <GridColumn field="baseQuantity" title="底数" width="100" align="right">
                        <template #body="scope">{{
                            scope.row.isGroup || scope.row.isOther ? '—' : quantityText(scope.row.baseQuantity)
                            }} {{ scope.row.isGroup || scope.row.isOther ? '' :
                                scope.row.baseQuantityUnitName
                            }}</template>
                    </GridColumn>

                    <GridColumn field="totalStockQuantity" title="总库存" width="110" align="right">
                        <template #body="scope">
                            <div style="display: flex;flex-direction: column;">
                                <div style="z-index: 999;">
                                    <span
                                        :style="{ background: scope.row.abnormalQuantity ? 'white' : 'transparent', padding: scope.row.abnormalQuantity ? '0 0 0 2px' : '0' }">
                                        {{ $utils.formatThousandSeparator(scope.row.totalStockQuantity) }}
                                    </span>
                                </div>
                                <span v-if="scope.row.abnormalQuantity" style=" margin-top:-18px;display: flex;flex-direction: column;font-size:
                                    11px;text-align: left;">
                                    <span style="color: var(--color-red);">
                                        异{{ $utils.formatThousandSeparator(scope.row.abnormalQuantity) }}
                                    </span>
                                    <span style="color: var(--color-green);">
                                        正{{ $utils.formatThousandSeparator(scope.row.inventoryQuantity) }}
                                    </span>
                                </span>
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="occupyInventory" title="占用库存" width="100" align="right">
                    </GridColumn>
                    <GridColumn field="detailDataUnit" title="单位" width="40" align="left" :overflowTextEllipsis="true">
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup align="right" :frozen="true" width="800">
                <GridHeaderRow>
                    <GridColumn title="派工所需物料量" width="100" align="center" :overflowTextEllipsis="true" :colspan="2">
                    </GridColumn>
                    <GridColumn field="supplierId" title="供应商" width="150" align="left" :editable="true" :rowspan="2"
                        :cellCss="cellCss">
                        <template #body="scope">{{
                            supplierList.find((item) => item.value === scope.row.supplierId)?.text
                        }}</template>
                        <template #edit="scope">
                            <ComboBox v-model="scope.row.supplierId" :data="supplierList"
                                :disabled="!canEdit(scope.row)">
                                <Addon><span v-if="canEdit(scope.row) && scope.row.supplierId"
                                        class="textbox-icon icon-clear"
                                        @click.stop="scope.row.supplierId = undefined"></span></Addon>
                            </ComboBox>
                        </template>
                    </GridColumn>
                    <GridColumn field="dataStatus" title="物料状态" width="100" align="left" :editable="true" :rowspan="2"
                        :cellCss="cellCss">
                        <template #body="scope">{{
                            dataStatusList.find((item) => item.value === scope.row.dataStatus)?.text
                        }}</template>
                        <template #edit="scope">
                            <ComboBox v-model="scope.row.dataStatus" :data="dataStatusList"
                                :disabled="!canEdit(scope.row)">
                                <Addon><span v-if="canEdit(scope.row) && scope.row.dataStatus != null"
                                        class="textbox-icon icon-clear"
                                        @click.stop="scope.row.dataStatus = undefined"></span></Addon>
                            </ComboBox>
                        </template>
                    </GridColumn>
                    <GridColumn field="detailRemark" title="需求备注" width="150" align="left" :rowspan="2" :editable="true"
                        :cellCss="cellCss">
                        <template #edit="scope">
                            <TextBox v-model="scope.row.detailRemark" :disabled="!canEdit(scope.row)">
                            </TextBox>
                        </template>
                    </GridColumn>
                    <GridColumn field="op" title="操作" width="90" align="center" :rowspan="2">
                        <template #body="scope">
                            <ButtonGroup style="height: 24px">
                                <SwitchButton v-if="!scope.row.isOther && !scope.row.isGroup" :disabled="isLoading"
                                    style="width: 75px; height: 24px;" v-model="scope.row.isDemand" onText="领用"
                                    offText="不领用" @valueChange="changeDemandStatus(scope.row)">
                                </SwitchButton>
                                <LinkButton v-if="scope.row.isOther || scope.row.isGroup" :disabled="isLoading"
                                    @click="deleteRow(scope.row)">删除</LinkButton>
                            </ButtonGroup>
                        </template>
                    </GridColumn>
                </GridHeaderRow>
                <GridHeaderRow>

                    <GridColumn field="demandQuantity" title="用量" width="120" align="right" :editable="true"
                        :cellCss="cellCss">
                        <template #body="scope">{{
                            scope.row.isGroup ? '' : quantityText(scope.row.demandQuantity)
                        }}</template>
                        <template #edit="scope">
                            <NumberBox v-model="scope.row.demandQuantity" :min="0" :precision="6" :spinners="false"
                                :disabled="!canEdit(scope.row)" @valueChange="updateUnitInfo(scope.row)"></NumberBox>
                        </template>
                    </GridColumn>
                    <GridColumn field="unit" title="单位" width="80" align="left" :editable="true" :cellCss="cellCss">
                        <template #body="scope"><span :title="scope.row.unitError"
                                :style="{ color: scope.row.unitError ? 'var(--color-red)' : undefined }">{{
                                    scope.row.isGroup
                                        ? ''
                                        : scope.row.unitError ||
                                        scope.row.unitList.find(
                                            (item: BomDemandUnitDto) => item.value === scope.row.unitConversionId
                                        )?.text
                                }}</span>
                        </template>
                        <template #edit="scope">
                            <ComboBox v-model="scope.row.unitConversionId" :data="scope.row.unitList"
                                :disabled="!canEdit(scope.row)" @selectionChange="changeDemandUnit(scope.row)">
                            </ComboBox>
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
        </TreeGrid>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="$emit('close')">取消</LinkButton>
            <LinkButton btnCls="c8"
                :disabled="isLoading || (submitData.bomVersionId != null && loadedBomVersionId !== submitData.bomVersionId)"
                @click="submit">确定</LinkButton>
        </div>
    </div>

    <!-- 选择物料 -->
    <Drawer v-if="isShowSeleteDrawer" @collapse="isShowSeleteDrawer = false;" ref="seleteMaterial" title="选择其他物料"
        @expand="$utils.updateModal()" :drawerStyle="{ width: '95%' }" region="east" :maskClosable="false"
        :closable="true">
        <SelectMaterial :selectList="otherMaterials" @close="closeMaterialDrawer" @next="getOtherMaterials"
            :furmulaMaterialIds="bomProductIds">
        </SelectMaterial>
    </Drawer>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import { cloneDeep } from 'lodash'
import { ShowFormData, type SelectOption } from '@/types/types'
import {
    BomDetailType,
    BomStatus,
    DispatchBasisType,
    InventoryDataStatus,
    InventoryDataType,
    ProductType
} from '@/types/enums'
import {
    BomDetailDto,
    BomIndexDto,
    BomVersionDataDto,
    BomDemandCurrentRowDto,
    BomDemandDetailDto,
    BomDemandUnitDto,
    BomMaterialDemandsDto,
    InventoryUnitOutboundDto,
    GetProductListByProductionDto,
    DetailWithProductDto,
    type GetProductUnitDto
} from '@/types/production'
import SelectMaterial from './select-material.vue'

type ProductUnits = {
    productId: number
    specificationId?: number
    idField: string
    unitList: GetProductUnitDto[]
}

export default defineComponent({
    name: 'MaterialDemandByBom',
    components: { SelectMaterial },
    props: { currentRow: { type: Object as PropType<BomDemandCurrentRowDto>, required: true } },
    emits: ['close', 'submit'],
    data() {
        return {
            isLoading: false,
            formData: [] as ShowFormData[],
            rules: {},
            messages: {},
            errors: {},
            boms: [] as BomIndexDto[],
            submitData: new BomMaterialDemandsDto(),
            materialDetails: [] as BomDemandDetailDto[],
            supplierList: [] as SelectOption[],
            outputUnits: [] as BomDemandUnitDto[],
            outputUnitsLoaded: false,
            isShowSeleteDrawer: false,
            loadedBomVersionId: undefined as number | undefined,
            requestId: 0
        }
    },
    computed: {
        bomOptions(): SelectOption[] {
            return this.boms.map((item) => ({
                value: item.versionId,
                text: `${item.isMain ? '【主版本】' : ''}${item.version}
                ${item.remark ? '（' + item.remark + '）' : ''}`
                // （${BomStatus[item.status] ?? '未知状态'}）
            }))
        },
        bomVersion(): BomIndexDto | undefined {
            return this.boms.find((item) => item.versionId === this.submitData.bomVersionId)
        },
        bomUnits(): BomDemandUnitDto[] {
            const units = this.bomVersion?.unitInfoList ?? []
            const versionUnits = units
                .filter((unit) => unit.unitId > 0 && Number.isFinite(unit.ratio) && unit.ratio > 0)
                .map((unit) => new BomDemandUnitDto({
                    ...unit, value: unit.id, text: unit.unitName
                }))
            return [...versionUnits, ...this.outputUnits]
                .filter((unit, index, items) => items.findIndex(item => item.id === unit.id) === index)
        },
        bomQuantity(): number {
            const unit = this.bomUnits.find((item) => item.id === this.submitData.bomUnitConversionId)
            return this.getBomQuantity(unit)
        },
        dataStatusList(): SelectOption[] {
            return this.$utils
                .getEnumKeyValueArray(InventoryDataStatus)
                .filter((item) => Number(item.value) !== InventoryDataStatus.None)
                .map((item) => ({ value: Number(item.value), text: item.text }))
        },
        bomProductIds(): number[] {
            return [
                ...new Set(
                    this.allRows()
                        .filter((row) => !row.isGroup && !row.isOther)
                        .map((row) => row.productId)
                        .filter((id) => id > 0)
                )
            ]
        },
        otherMaterials(): GetProductListByProductionDto[] {
            return this.allRows()
                .filter((row) => row.isOther)
                .map((row) =>
                    Object.assign(new GetProductListByProductionDto(), {
                        id: row.id,
                        productId: row.productId,
                        specificationId: row.specificationId,
                        productType: row.detailProductType,
                        productNo: row.detailDataNo,
                        productName: row.detailDataName,
                        productTechnicalCode: row.detailDataTechCode,
                        productSpecification: row.detailDataSpecification,
                        materialSpecification: row.detailDataSpecification,
                        materialGrade: row.detailDataMaterialGrade,
                        productCategory: row.detailDataCategoryName,
                        productUnit: row.detailDataUnit,
                        productUnitId: row.detailDataUnitId,
                        inventoryQuantity: row.inventoryQuantity,
                        abnormalQuantity: row.abnormalQuantity,
                        occupyInventory: row.occupyInventory
                    })
                )
        }
    },
    watch: {
        currentRow() {
            this.init()
        }
    },
    created() {
        this.init()
    },
    beforeUnmount() {
        this.requestId++
    },
    methods: {
        init() {
            this.formData = [
                new ShowFormData('技术型号', this.currentRow?.productTechnicalCode ?? ""),
                new ShowFormData('产品编码', this.currentRow?.productNo ?? ""),
                new ShowFormData('产品名称', this.currentRow?.productName ?? ""),
                new ShowFormData('规格型号', this.currentRow?.specification ?? ""),
                new ShowFormData('包装方式', this.currentRow?.productPackageWay ?? ''),
                new ShowFormData('产品类别', this.currentRow?.productCategory ?? ""),
                new ShowFormData('派工部门', this.currentRow?.departmentName ?? ''),
                new ShowFormData('生产单位派工', (this.currentRow?.dispatchQuantity ?? 0) + (this.currentRow?.dispatchUnitInfo?.productionUnitName ?? '')),
                new ShowFormData('产品单位派工', (this.currentRow?.baseQuantity ?? 0) + (this.currentRow?.dispatchUnitInfo?.baseUnitName ?? '')),
                new ShowFormData('产品备注', this.currentRow?.remark ?? ''),
            ] as ShowFormData[];
            const request = ++this.requestId
            this.isLoading = true
            this.boms = []
            this.outputUnits = []
            this.outputUnitsLoaded = false
            this.materialDetails = []
            this.submitData = new BomMaterialDemandsDto()
            this.loadedBomVersionId = undefined
            if (this.isShowSeleteDrawer) this.closeMaterialDrawer()
            return Promise.all([
                this.$http.get('/api/production/BomManage/GetProductionBomOptions', {
                    productId: this.currentRow.productId,
                    specificationId: this.currentRow.specificationId
                }),
                this.$http.get('/api/base/Supplier/GetAllSupplier')
            ])
                .then(([bomRes, supplierRes]) => {
                    if (request !== this.requestId) return
                    this.boms = (bomRes.data as any).map((item: BomIndexDto) => new BomIndexDto(item))
                    this.supplierList = (supplierRes.data as any).map(
                        (item: { id: number; customerName: string }) => ({
                            value: item.id,
                            text: item.customerName
                        })
                    )
                    const draft = this.currentRow.materialDemands
                    if (draft?.materialDetails) {
                        this.submitData = new BomMaterialDemandsDto(cloneDeep(draft))
                        this.materialDetails = this.copyRows(draft.materialDetails)
                        if (
                            draft.bomVersionId != null &&
                            !this.boms.some((item) => item.versionId === draft.bomVersionId)
                        )
                            throw new Error('原物料清单版本已不可用，请重新选择物料清单')
                        this.loadedBomVersionId = draft.bomVersionId
                        return this.loadOutputUnits(request).then(() => {
                            if (request !== this.requestId) return
                            const selected = this.bomUnits.find(unit => unit.id === draft.bomUnitConversionId)
                            if (draft.bomVersionId != null &&
                                (draft.dispatchQuantity !== this.currentRow.dispatchQuantity ||
                                    draft.dispatchUnitId !== this.currentRow.dispatchUnitInfo?.productionUnitId ||
                                    draft.dispatchUnitConversionId !== this.currentRow.dispatchUnitInfo?.unitConversionId ||
                                    draft.convRatio !== this.getBomRatio(selected))) {
                                this.loadedBomVersionId = undefined
                                this.isLoading = false
                                return this.getBomData(true)
                            }
                            return this.getMaterialUnits(this.materialDetails)
                        }).then(() => {
                            if (request === this.requestId) this.materialDetails = [...this.materialDetails]
                        })
                    }
                    const main = this.boms.find((item) => item.isMain) ?? this.boms[0]
                    this.submitData.bomVersionId = main?.versionId
                    this.submitData.bomUnitConversionId = main?.unitConversionId
                    this.isLoading = false
                    if (main) return this.getBomData()
                })
                .catch((err) => {
                    if (request === this.requestId) this.$kingzon_messager.showError(err)
                })
                .finally(() => {
                    if (request === this.requestId) this.isLoading = false
                })
        },
        findDispatchUnit(units: BomDemandUnitDto[]): BomDemandUnitDto | undefined {
            const info = this.currentRow.dispatchUnitInfo
            if (!info?.productionUnitId) return undefined
            const matches = units.filter(unit => unit.unitId === info.productionUnitId)
            return info.unitConversionId != null
                ? matches.find(unit => unit.id === info.unitConversionId)
                : matches.length === 1 ? matches[0] : undefined
        },
        getBomRatio(unit?: BomDemandUnitDto): number {
            return this.resolveBomConversion(unit)?.ratio ?? 0
        },
        resolveBomConversion(unit?: BomDemandUnitDto): { ratio: number; unit: BomDemandUnitDto } | undefined {
            if (!unit) return undefined
            // 两端必须使用同一来源的换算率，不能混用版本快照和最新单位的底数。
            const versionUnits = (this.bomVersion?.unitInfoList ?? [])
                .map(item => new BomDemandUnitDto({ ...item, value: item.id, text: item.unitName }))
            for (const units of [versionUnits, this.outputUnits]) {
                const source = this.findDispatchUnit(units)
                let target = units.find(item => item.id === unit.id && item.unitId === unit.unitId)
                if (!target && units === this.outputUnits) {
                    // 旧 BOM 的 Id=0/规格转换 ID 与接口不同；基础/主单位按物理单位对应。
                    const matches = units.filter(item => item.unitId === unit.unitId &&
                        (item.id === -1 || item.isMainUnit))
                    if (matches.length === 1) target = matches[0]
                }
                if (!source || !target || !Number.isFinite(source.ratio) || source.ratio <= 0 ||
                    !Number.isFinite(target.ratio) || target.ratio <= 0) continue
                const ratio = source.ratio / target.ratio
                if (Number.isFinite(ratio) && ratio > 0) return { ratio, unit: target }
            }
            return undefined
        },
        loadOutputUnits(request: number): Promise<void> {
            const version = this.bomVersion
            if (!version || this.outputUnitsLoaded) return Promise.resolve()
            const defaultUnit = this.bomUnits.find(unit => unit.id === version.unitConversionId)
            const selectedUnit = this.bomUnits.find(unit => unit.id === this.submitData.bomUnitConversionId) ?? defaultUnit
            if (this.getBomRatio(defaultUnit) > 0 && this.getBomRatio(selectedUnit) > 0) return Promise.resolve()
            const { productId, specificationId } = this.currentRow
            const productIdList = [{
                productId, specificationId,
                idField: `${productId}${specificationId != null ? '_' + specificationId : ''}`
            }]
            return this.$http.post('/api/sale/SaleOrderManage/GetProductUnitList', productIdList)
                .then(res => {
                    if (request !== this.requestId) return
                    const group = (res.data as ProductUnits[]).find(item => item.productId === productId &&
                        (item.specificationId ?? null) === (specificationId ?? null))
                    this.outputUnits = (group?.unitList ?? []).map(item => new BomDemandUnitDto({
                        id: Number(item.code), value: Number(item.code), unitId: item.unitId,
                        unitName: item.text ?? '', text: item.text ?? '', ratio: Number(item.value),
                        isMainUnit: item.isMainUnit
                    })).filter(item => Number.isFinite(item.id) && item.unitId > 0 &&
                        Number.isFinite(item.ratio) && item.ratio > 0)
                    this.outputUnitsLoaded = true
                })
        },
        getBomQuantity(unit?: BomDemandUnitDto): number {
            const quantity = this.currentRow.dispatchQuantity
            if (!Number.isFinite(quantity) || quantity <= 0) return 0
            const converted = quantity * this.getBomRatio(unit)
            return Number.isFinite(converted) ? this.roundQuantity(converted) : 0
        },
        getBomData(force: unknown = false) {
            const version = this.bomVersion
            if (
                this.isLoading ||
                !version ||
                (force !== true && this.loadedBomVersionId === version.versionId)
            )
                return
            const request = ++this.requestId
            this.isLoading = true
            return this.loadOutputUnits(request)
                .then(() => {
                    if (request !== this.requestId) return
                    const standardUnit = this.bomUnits.find(unit => unit.id === version.unitConversionId)
                    const quantity = this.getBomQuantity(standardUnit)
                    if (quantity <= 0 || !Number.isFinite(version.totalQuantity) || version.totalQuantity <= 0)
                        throw new Error('请检查产品派工单位到主单位的换算，以及物料清单中对应主单位、清单单位和总量的配置')
                    if (!this.bomUnits.some(unit => unit.id === this.submitData.bomUnitConversionId))
                        this.submitData.bomUnitConversionId = version.unitConversionId
                    return this.$http.get('/api/production/BomManage/GetProductionBomDataById', {
                        bomVersionId: version.versionId, quantity
                    })
                })
                .then((res) => {
                    if (request !== this.requestId || !res) return
                    const details = this.createBomRows(new BomVersionDataDto(res.data as any).detailList)
                    const others = this.materialDetails.filter((row) => row.isGroup)
                    const rows = [...details, ...this.copyRows(others)]
                    return this.getMaterialUnits(rows).then(() => {
                        if (request !== this.requestId) return
                        this.materialDetails = rows
                        this.loadedBomVersionId = version.versionId
                    })
                })
                .catch((err) => {
                    if (request === this.requestId) this.$kingzon_messager.showError(err)
                })
                .finally(() => {
                    if (request === this.requestId) this.isLoading = false
                })
        },
        changeBomUnit() {
            if (!this.isLoading) this.submitData.bomQuantity = this.bomQuantity
        },
        createBomRows(details: BomDetailDto[]): BomDemandDetailDto[] {
            return details.map((detail, index) => {
                const key = `bom-${index}-${detail.id ?? 0}`
                const main = this.createRow(detail, key, key)
                main.isDemand = true
                const alternatives: BomDemandDetailDto[] = []
                const addAlternatives = (items: BomDetailDto[]) => {
                    for (const item of items ?? []) {
                        const row = this.createRow(
                            item,
                            `${key}-sub-${alternatives.length}-${item.id ?? 0}`,
                            key
                        )
                        row.isSubstitute = true
                        alternatives.push(row)
                        addAlternatives(item.replaceData)
                    }
                }
                addAlternatives(detail.replaceData)
                main.children = alternatives
                return main
            })
        },
        createRow(detail: BomDetailDto, key: string, groupKey: string): BomDemandDetailDto {
            const row = new BomDemandDetailDto({
                ...cloneDeep(detail),
                indexField: key,
                groupKey,
                children: [],
                replaceData: []
            })
            row.productId =
                detail.detailWithProductDto?.productId ??
                (detail.detailType === BomDetailType.Product ? detail.detailId : 0)
            row.specificationId =
                detail.detailWithProductDto?.specificationId ??
                (detail.detailType === BomDetailType.Specication ? detail.detailId : undefined)
            row.dataId = row.specificationId ?? row.productId
            row.dataType =
                row.specificationId != null ? InventoryDataType.Specification : InventoryDataType.Product
            row.totalStockQuantity = Number(
                detail.totalStockQuantity ??
                Number(detail.inventoryQuantity ?? 0) + Number(detail.abnormalQuantity ?? 0)
            )
            row.standardQuantity = detail.curQuantity
            return row
        },
        allRows(rows?: BomDemandDetailDto[]): BomDemandDetailDto[] {
            return (rows ?? this.materialDetails).flatMap((row) => [
                row,
                ...this.allRows(row.children ?? [])
            ])
        },
        getMaterialUnits(rows: BomDemandDetailDto[]) {
            const materials = this.allRows(rows).filter((row) => !row.isGroup)
            materials
                .filter((row) => !row.productId)
                .forEach((row) => {
                    row.unitError = '缺少关联产品'
                })
            const products = [
                ...new Map(
                    materials
                        .filter((row) => row.productId > 0)
                        .map((row) => {
                            const idField = `${row.productId}${row.specificationId != null ? '_' + row.specificationId : ''}`
                            return [
                                idField,
                                { idField, productId: row.productId, specificationId: row.specificationId }
                            ]
                        })
                ).values()
            ]
            if (!products.length) return Promise.resolve()
            return this.$http
                .post('/api/sale/SaleOrderManage/GetProductUnitList', products)
                .then((res) => {
                    const units = res.data as ProductUnits[]
                    for (const row of materials) {
                        const group = units.find(
                            (item) =>
                                item.productId === row.productId &&
                                (item.specificationId ?? null) === (row.specificationId ?? null)
                        )
                        if (!row.productId) continue
                        row.unitList = (group?.unitList ?? [])
                            .map(
                                (unit) =>
                                    new BomDemandUnitDto({
                                        id: Number(unit.code),
                                        value: Number(unit.code),
                                        text: unit.text ?? '',
                                        unitName: unit.text ?? '',
                                        unitId: unit.unitId,
                                        ratio: Number(unit.value),
                                        isMainUnit: unit.isMainUnit
                                    })
                            )
                            .filter(
                                (unit) =>
                                    Number.isFinite(unit.id) &&
                                    unit.unitId > 0 &&
                                    Number.isFinite(unit.ratio) &&
                                    unit.ratio > 0
                            )
                        this.prepareRowUnits(row)
                    }
                })
        },
        prepareRowUnits(row: BomDemandDetailDto) {
            row.unitError = ''
            const unitName = (row.unit || row.detailDataUnit || '').trim().toLowerCase()
            const matches = row.unitList.filter(
                (unit) => unit.unitName.trim().toLowerCase() === unitName
            )
            const standard =
                matches.find((unit) => unit.id === row.unitConversionId) ??
                (matches.length === 1 ? matches[0] : undefined)
            if (!row.isOther && !standard) row.unitError = '缺少清单用量单位换算'
            row.standardRatio = standard?.ratio ?? 0
            const selected =
                row.unitList.find(
                    (unit) => unit.id === row.unitConversionId && unit.unitId === row.unitId
                ) ??
                standard ??
                row.unitList.find((unit) => unit.unitId === row.detailDataUnitId && unit.ratio === 1)
            if (!selected) {
                row.unitError = '物料单位未配置'
                return
            }
            row.unitConversionId = selected.id
            row.unitId = selected.unitId
            row.demandRatio = selected.ratio
            if (row.demandQuantity == null && !row.isOther && standard)
                row.demandQuantity = this.roundQuantity(
                    (row.standardQuantity * standard.ratio) / selected.ratio
                )
            this.updateUnitInfo(row)
        },
        changeDemandUnit(row: BomDemandDetailDto) {
            const unit = row.unitList.find((item) => item.id === row.unitConversionId)
            if (!unit) return
            if (row.demandQuantity != null && row.demandRatio > 0)
                row.demandQuantity = this.roundQuantity((row.demandQuantity * row.demandRatio) / unit.ratio)
            row.unitId = unit.unitId
            row.demandRatio = unit.ratio
            this.updateUnitInfo(row)
        },
        updateUnitInfo(row: BomDemandDetailDto) {
            const quantity = Number(row.demandQuantity)
            row.baseDemandQuantity =
                Number.isFinite(quantity) && row.demandRatio > 0
                    ? this.roundQuantity(quantity * row.demandRatio)
                    : 0
            const units = row.unitList.filter(
                (unit) => unit.id === row.unitConversionId || unit.id === -1 || unit.isMainUnit
            )
            row.convUnitInfo = units.map(
                (unit) =>
                    new InventoryUnitOutboundDto({
                        ...unit,
                        outboundQuantity:
                            unit.id === row.unitConversionId
                                ? Number.isFinite(quantity)
                                    ? quantity
                                    : 0
                                : this.roundQuantity(row.baseDemandQuantity / unit.ratio)
                    })
            )
        },
        changeDemandStatus(row: BomDemandDetailDto) {
            this.$nextTick(() => {
                if (!row.isDemand) return
                this.allRows()
                    .filter((item) => item.indexField !== row.indexField && item.groupKey === row.groupKey)
                    .forEach((item) => {
                        item.isDemand = false
                    })
            })
        },
        clearBom() {
            if (this.isLoading) return
            this.requestId++
            this.submitData = new BomMaterialDemandsDto()
            this.loadedBomVersionId = undefined
            this.materialDetails = this.materialDetails.filter((row) => row.isGroup)
        },
        showSeleteDrawer() {
            if (this.isLoading) return
            this.isShowSeleteDrawer = true
            this.$nextTick(() => {
                ; (this.$refs.seleteMaterial as EasyUI.Drawer)?.expand()
            })
        },
        closeMaterialDrawer() {
            ; (this.$refs.seleteMaterial as EasyUI.Drawer)?.collapse()
            this.isShowSeleteDrawer = false
        },
        getOtherMaterials(list: GetProductListByProductionDto[]) {
            if (this.isLoading) return
            const existing = this.allRows().filter((row) => row.isOther)
            const children = list
                .filter((item) => !this.bomProductIds.includes(item.productId))
                .map((item) => {
                    const key = `other-${item.productId}-${item.specificationId ?? 'product'}`
                    const saved = existing.find((row) => row.indexField === key)
                    if (saved) return this.copyRows([saved])[0]
                    return new BomDemandDetailDto({
                        id: item.id,
                        indexField: key,
                        groupKey: key,
                        isOther: true,
                        isDemand: true,
                        productId: item.productId,
                        specificationId: item.specificationId,
                        dataId: item.specificationId ?? item.productId,
                        dataType:
                            item.specificationId != null
                                ? InventoryDataType.Specification
                                : InventoryDataType.Product,
                        detailType:
                            item.specificationId != null ? BomDetailType.Specication : BomDetailType.Product,
                        detailId: item.specificationId ?? item.productId,
                        detailProductType: item.productType,
                        detailWithProductDto: new DetailWithProductDto({
                            productId: item.productId,
                            specificationId: item.specificationId,
                            productType: item.productType,
                            productNo: item.productNo,
                            productName: item.productName,
                            productTechCode: item.productTechnicalCode,
                            productCategoryName: item.productCategory,
                            productSpecification: item.productSpecification ?? item.materialSpecification,
                            productMaterialGrade: item.materialGrade,
                            productUnit: item.productUnit
                        }),
                        detailDataNo: item.productNo,
                        detailDataName: item.productName,
                        detailDataTechCode: item.productTechnicalCode,
                        detailDataSpecification: item.productSpecification ?? item.materialSpecification,
                        detailDataCategoryName: item.productCategory,
                        detailDataMaterialGrade: item.materialGrade,
                        detailDataUnit: item.productUnit,
                        detailDataUnitId: item.productUnitId,
                        inventoryQuantity: item.inventoryQuantity,
                        abnormalQuantity: item.abnormalQuantity,
                        totalStockQuantity: item.inventoryQuantity + item.abnormalQuantity,
                        occupyInventory: item.occupyInventory
                    })
                })
            const rows = this.copyRows(this.materialDetails.filter((row) => !row.isGroup))
            if (children.length)
                rows.push(new BomDemandDetailDto({ indexField: 'other-group', isGroup: true, children }))
            const request = ++this.requestId
            this.isLoading = true
            return this.getMaterialUnits(rows)
                .then(() => {
                    if (request !== this.requestId) return
                    this.materialDetails = rows
                    this.closeMaterialDrawer()
                })
                .catch((err) => {
                    if (request === this.requestId) this.$kingzon_messager.showError(err)
                })
                .finally(() => {
                    if (request === this.requestId) this.isLoading = false
                })
        },
        deleteRow(row: BomDemandDetailDto) {
            if (this.isLoading) return
            this.materialDetails = this.materialDetails.filter(
                (item) => item.indexField !== row.indexField
            )
            for (const item of this.materialDetails.filter((item) => item.isGroup))
                item.children = item.children.filter((child) => child.indexField !== row.indexField)
            this.materialDetails = this.materialDetails.filter(
                (item) => !item.isGroup || item.children.length > 0
            )
        },
        copyRows(rows: BomDemandDetailDto[]): BomDemandDetailDto[] {
            return rows.map((row) => {
                // TreeGrid 注入 parent；返回父页面前去除循环引用，保留原始明细 ID。
                const { parent, children, replaceData, ...fields } = row as BomDemandDetailDto & {
                    parent?: unknown
                }
                void parent
                void replaceData
                return new BomDemandDetailDto({
                    ...cloneDeep(fields),
                    children: this.copyRows(children ?? []),
                    replaceData: []
                })
            })
        },
        canEdit(row: BomDemandDetailDto): boolean {
            return !this.isLoading && !row.isGroup && row.isDemand
        },
        cellCss(row: BomDemandDetailDto): string {
            return this.canEdit(row) ? 'kingzon-cell-editable' : ''
        },
        typeText(row: BomDemandDetailDto): string {
            if (row.isGroup) return '其他物料'
            if (row.isOther) return '其他物料'
            const type =
                row.detailType === BomDetailType.Bom
                    ? '子清单'
                    : row.detailType === BomDetailType.Formula
                        ? '配方'
                        : ProductType[row.detailProductType ?? ProductType.成品]
            return `${row.isSubstitute ? '替料 · ' : ''}${type}`
        },
        quantityText(value?: number): string {
            return value == null || !Number.isFinite(Number(value))
                ? '—'
                : Number(value).toLocaleString('zh-CN', { maximumFractionDigits: 6 })
        },
        roundQuantity(value: number): number {
            return Number(value.toFixed(6))
        },
        async submit() {
            if (this.isLoading) return
            await this.$nextTick()
            if (
                this.submitData.bomVersionId != null &&
                this.loadedBomVersionId !== this.submitData.bomVersionId
            ) {
                this.$kingzon_messager.showError('请先加载所选物料清单')
                return
            }
            if (
                this.submitData.bomVersionId != null &&
                (!this.bomUnits.some((unit) => unit.id === this.submitData.bomUnitConversionId) ||
                    !Number.isFinite(this.bomQuantity) || this.bomQuantity <= 0)
            ) {
                this.$kingzon_messager.showError('请选择有效的清单单位并检查派工数量')
                return
            }
            const selected = this.allRows().filter((row) => !row.isGroup && row.isDemand)
            const groups = new Set<string>()
            for (const row of selected) {
                if (groups.has(row.groupKey)) {
                    this.$kingzon_messager.showError('同组主料和替料只能领用一种')
                    return
                }
                groups.add(row.groupKey)
                if (
                    row.unitError ||
                    !row.productId ||
                    !row.unitId ||
                    !Number.isFinite(row.demandQuantity) ||
                    (row.demandQuantity ?? 0) <= 0
                ) {
                    this.$kingzon_messager.showError(
                        `请检查【${row.detailDataNo}-${row.detailDataName}】的需求用量及单位${row.unitError ? '：' + row.unitError : ''}`
                    )
                    return
                }
                this.updateUnitInfo(row)
                if (!Number.isFinite(row.baseDemandQuantity) || row.baseDemandQuantity <= 0) {
                    this.$kingzon_messager.showError(
                        `【${row.detailDataNo}-${row.detailDataName}】换算后的基础需求量无效或小于六位小数精度`
                    )
                    return
                }
            }
            if (!selected.length) {
                this.$kingzon_messager.showError('请选择至少一种领用物料')
                return
            }
            const materialDetails = this.copyRows(this.materialDetails)
            const unit = this.bomUnits.find((unit) => unit.id === this.submitData.bomUnitConversionId)
            this.$emit(
                'submit',
                new BomMaterialDemandsDto({
                    basisType:
                        this.submitData.bomVersionId != null
                            ? DispatchBasisType.物料清单
                            : DispatchBasisType.未指定,
                    basisId: this.submitData.bomVersionId,
                    bomVersionId: this.submitData.bomVersionId,
                    bomUnitConversionId: this.submitData.bomUnitConversionId,
                    bomUnitInfo: cloneDeep(this.resolveBomConversion(unit)?.unit),
                    dispatchQuantity: this.currentRow.dispatchQuantity,
                    dispatchUnitId: this.currentRow.dispatchUnitInfo?.productionUnitId,
                    dispatchUnitConversionId: this.currentRow.dispatchUnitInfo?.unitConversionId,
                    bomQuantity: this.bomQuantity,
                    convRatio: this.getBomRatio(unit),
                    materialDetails,
                    materials: this.allRows(materialDetails)
                        .filter((row) => !row.isGroup && row.isDemand)
                        .map((row) => new BomDemandDetailDto({ ...row, children: [], replaceData: [] }))
                })
            )
        }
    }
})
</script>
