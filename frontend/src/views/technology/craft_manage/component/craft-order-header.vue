<template>
  <Loading v-show="loading"></Loading>

  <Panel :bodyStyle="{ padding: '0' }" class="craft-order-panel" :collapsible="true">
    <template #header>
      <div class="panel-header-row product-heading">
        <span class="order-code">工艺单 {{ craftOrder.code }}</span>
        <template v-if="localEditing">
          <TextBox v-model="draftName" :disabled="saving" style="width: 400px" placeholder="工艺单名称" />
          <LinkButton plain :disabled="saving" @click="saveHeader">保存名称</LinkButton>
          <LinkButton plain :disabled="saving" @click="cancel">取消</LinkButton>
        </template>
        <template v-else>
          <span class="order-name" :title="craftOrder.name">【{{ craftOrder.name }}】</span>
          <LinkButton v-if="headerEditable" plain @click="startHeaderEdit"
            style="color:var(--color-blue);padding-right: 10px;">编辑
            <font-awesome-icon icon="fa-solid fa-pen-to-square" style="margin-bottom:1px" />
          </LinkButton>
        </template>
        <div class="panel-tool">
          <a class="panel-tool-close" @click="$emit('cancel')" style="margin-right: 15px;">
          </a>
        </div>
      </div>
    </template>
    <KingZonShowForm :formData="showFormData" columnWidth="25%" lableWidth="78px" class="product-show-form" />
  </Panel>

  <!-- 版本和工序 Panel -->
  <Panel v-if="craftOrderId" :bodyStyle="{ padding: '0' }" class="version-process-panel" :collapsible="true">
    <template #header>
      <div class="panel-header-row">
        <span class="panel-title">版本和工序</span>
        <div class="version-list">
          <div v-for="version in versionList" :key="version.id ?? version.sequenceNo"
            class="version-item kingzon-pointer" :class="{ selected: activeVersionId === version.id }"
            @click="versionClick(version)">
            <span class="version-name">{{ version.version }}</span>
            <span v-if="version.isMain" class="version-main">主版本</span>
            <span v-if="version.status === CraftOrderVersionStatus.暂存" class="kingzon-tag kingzon-tag-info">{{
              CraftOrderVersionStatus[version.status] }}</span>
            <span v-else-if="version.status === CraftOrderVersionStatus.待审核" class="kingzon-tag kingzon-tag-warning">{{
              CraftOrderVersionStatus[version.status] }}</span>
            <span v-else-if="version.status === CraftOrderVersionStatus.已发布" class="kingzon-tag">{{
              CraftOrderVersionStatus[version.status] }}</span>
            <span v-else-if="version.status === CraftOrderVersionStatus.已废弃" class="kingzon-tag kingzon-tag-danger">{{
              CraftOrderVersionStatus[version.status] }}</span>
            <span v-if="mode === 'edit' && versionList.length > 1 && !version.isMain
              && (version.status === CraftOrderVersionStatus.暂存 || version.status === CraftOrderVersionStatus.待审核
                || version.status === CraftOrderVersionStatus.已废弃)" class="version-delete" title="删除版本"
              @click.stop="delVersion(version)">
              <font-awesome-icon :icon="['far', 'trash-can']" />
            </span>
          </div>

          <LinkButton v-if="mode === 'edit'" plain class="add-version-button" :disabled="versionLoading"
            @click="addVersion" style="color:var(--color-blue)">
            <font-awesome-icon icon="fa-solid fa-add" />
            新增版本
          </LinkButton>
          <LinkButton v-if="mode === 'edit'" plain :disabled="loading" @click="openCopyVersion"
            style="color:var(--color-blue)">
            <font-awesome-icon icon="fa-solid fa-copy" />
            复制版本
          </LinkButton>
        </div>
        <div class="panel-actions">
          <LinkButton v-if="canEditVersion" :disabled="saving" @click="saveVersion" plain>
            暂存
          </LinkButton>
          <span v-if="canEditVersion && versionChanges.length" class="kingzon-detail-text"
            :title="versionChanges.join('\n')" @click="showChange">已修改 {{ versionChanges.length }} 项</span>
          <LinkButton
            v-if="mode === 'edit' && activeVersion?.status === CraftOrderVersionStatus.已发布 && activeVersion?.isMain != true"
            plain :disabled="saving" @click="changeMainVersion">设为主版本</LinkButton>
          <LinkButton v-if="canEditVersion" plain :disabled="saving" @click="publishVersion"
            style="color:var(--color-blue)">发布</LinkButton>
          <LinkButton v-if="mode === 'edit' && activeVersion?.status === CraftOrderVersionStatus.已发布" plain
            :disabled="saving" @click="unpublishVersion">取消发布</LinkButton>
          <LinkButton v-if="mode === 'edit' && activeVersion?.status === CraftOrderVersionStatus.已废弃" plain
            :disabled="saving" @click="restoreVersion">恢复</LinkButton>
          <LinkButton
            v-if="mode === 'edit' && activeVersion?.status === CraftOrderVersionStatus.已发布 && !activeVersion?.isMain"
            plain class="danger-button" iconCls="icon-no" :disabled="saving" @click="obsoleteVersion">废弃当前版本
          </LinkButton>
        </div>
      </div>
    </template>

    <!-- <div v-if="canEditVersion && versionChanges.length"
      style="max-height: 140px; overflow-y: auto; padding: 4px 10px; font-size: 12px; line-height: 22px;">
      <div v-for="(change, index) in versionChanges" :key="index"
        style="white-space: pre-wrap; overflow-wrap: anywhere;">
        {{ index + 1 }}. {{ change }}</div>
    </div> -->

    <div class="version-route-content">
      <div class="version-line">
        <div v-if="activeVersion" class="version-editor">
          <label class="version-field">
            <span class="version-label">版本号：</span>

            <div class="version-input">
              <TextBox v-model="activeVersion.version" :disabled="!canEditVersion" placeholder="请输入版本名称" />
            </div>
          </label>
          <label class="version-field" style="width: 80%;">
            <span class="version-label">版本备注：</span>

            <div class="version-input">
              <TextBox v-model="activeVersion.remark" :multiline="true" style="width:100%;height:40px"
                :disabled="!canEditVersion" placeholder="请输入版本备注" />
            </div>
          </label>
        </div>
      </div>

      <div class="route-line">
        <strong>工艺路线</strong>

        <div class="route-scroll">
          <div class="route-list">
            <template v-for="(route, index) in routes" :key="route.uiKey">
              <button :class="['route-chip', { active: route.uiKey === selectedProcessKey }]" :draggable="canEditRoutes"
                @click="selectProcess(route)" @dragstart="startDrag(index)" @dragover.prevent @drop="dropRoute(index)">
                {{ route.code }} {{ route.name }}
              </button>
              <font-awesome-icon v-if="index < routes.length - 1" icon="fa-solid fa-arrow-right" />
            </template>
            <span v-if="!routes || routes.length === 0" class="empty-route">暂无工序</span>
          </div>
        </div>

        <div v-if="canEditRoutes" class="route-actions">
          <LinkButton plain @click="addProcess" style="color:var(--color-blue)">
            <font-awesome-icon icon="fa-solid fa-add" /> 新增工序
          </LinkButton>
          <LinkButton plain :disabled="!selectedProcessKey" @click="moveProcess(-1)" style="color:var(--color-blue)">
            <font-awesome-icon icon="fa-solid fa-arrow-up" />
            上移
          </LinkButton>
          <LinkButton plain :disabled="!selectedProcessKey" @click="moveProcess(1)" style="color:var(--color-blue)">
            <font-awesome-icon icon="fa-solid fa-arrow-down" />
            下移
          </LinkButton>
          <LinkButton plain :disabled="!selectedProcessKey" class="danger-button" @click="removeProcess"
            style="color:var(--color-red)">
            <font-awesome-icon icon="fa-solid fa-trash" />
            删除
          </LinkButton>
        </div>
      </div>
    </div>
  </Panel>
  <Dialog ref="copyVersionDialog" title="复制版本" :draggable="true" :resizable="true" :modal="true" :closed="true"
    :dialogStyle="{ width: '65%', height: '800px' }">
    <CopyCraftVersion :key="craftOrderId" :order="craftOrder" :versionId="activeVersionId" @copied="onVersionCopied" />
  </Dialog>
  <Dialog ref="versionChangeDialog" title="修改详情" :draggable="true" :resizable="true" :modal="true" :closed="true"
    :dialogStyle="{ width: '45%', height: '200px' }">
    <div class="kingzon-page-content">
      <div class="kingzon-page-fill" style="padding: 8px 20px; box-sizing: border-box;">
        <div v-for="(change, index) in versionChanges" :key="index">
          <strong>{{ change }}</strong>
        </div>
      </div>
      <div class="kingzon-btn-group kingzon-page-footer">
        <LinkButton v-if="canEditVersion && versionChanges.length" :disabled="loading || !originalEditData"
          @click="revertVersionChanges">撤回全部修改</LinkButton>
      </div>
    </div>
  </Dialog>

</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { cloneDeep } from 'lodash'
import { CraftOrderDto, CraftProcessDto, CraftVersionDto, type CraftProcessMaterialDto } from '@/types/craft'
import { CraftOrderVersionStatus, ShowFormDataType } from '@/types/enums'
import { ShowFormData } from '@/types/types'
import CopyCraftVersion from './copy-craft-version.vue'

export default defineComponent({
  name: 'ProcessOrderHeader',
  components: { CopyCraftVersion },
  props: {
    mode: {
      type: String,
      default: 'view',
      validator(value) {
        return ['edit', 'view'].includes(value as string)
      }
    },
    craftOrderId: {
      type: Number,
      default: null
    },
    modelValue: {
      type: Object,
      default: () => ({})
    },
    activeVersionId: {
      type: Number,
      default: null
    },
    routes: {
      type: Array<CraftProcessDto>,
      default: () => []
    },
    selectedProcessKey: {
      type: String,
      default: ''
    },

  },
  emits: [
    'update:modelValue',
    'update:activeVersionId',
    'update:selectedProcessKey',
    'saved',
    'editabilityChange',
    'versionSaved',
    'beforeSave',
    'loaded',
    'update:routes',
    'cancel'
  ],
  computed: {
    CraftOrderVersionStatus() {
      return CraftOrderVersionStatus
    },

    headerEditable(): boolean {
      return this.mode === 'edit' && !this.localEditing
    },

    activeVersion() {
      return this.versionList.find(item => item.id === this.activeVersionId)
    },

    versionChanges(): string[] {
      if (this.loadedVersionId !== this.activeVersionId || !this.originalVersion) return []
      const original = this.originalVersion
      const current = this.getVersionSnapshot(this.routes)
      const changes = new Map<string, string[]>()
      for (const key of new Set([...Object.keys(original), ...Object.keys(current)])) {
        const before = original[key]
        const after = current[key]
        if (before?.value === after?.value) continue
        const group = key.includes(':material:') ? key.slice(0, key.lastIndexOf(':')) : key
        const originalRow = original[`${group}:position`]
        const currentRow = current[`${group}:position`]
        // 一条物料的多个字段合为一项；新增、删除仅显示物料名称。
        if (key.includes(':material:') && (!originalRow || !currentRow)) {
          const label = (currentRow ?? originalRow).label.replace(/顺序$/, '')
          changes.set(group, [`${currentRow ? '新增' : '删除'}${label}`])
          continue
        }
        const label = after?.label ?? before.label
        const text = !before ? `新增${label}：${after.value || '（空）'}`
          : !after ? `删除${label}：${before.value || '（空）'}`
            : `${label}：${before.value || '（空）'} → ${after.value || '（空）'}`
        const items = changes.get(group) ?? []
        items.push(text)
        changes.set(group, items)
      }
      return [...changes.values()].map(items => items.join('；'))
    },
    canEditRoutes() {
      return this.canEditVersion
    },
    loading(): boolean {
      return this.orderLoading || this.versionLoading || this.routeLoading || this.saving
    },
    canEditVersion() {
      return this.mode === 'edit'
        && this.loadedVersionId === this.activeVersionId && !this.versionLoading
        && (this.activeVersion?.status === CraftOrderVersionStatus.暂存
          || this.activeVersion?.status === CraftOrderVersionStatus.待审核)
    },

    // 已是主版本时不允许取消勾选，避免出现没有主版本的无效状态；
    // 选择其它草稿版本后勾选并保存即可完成主版本切换。
    canSetMainVersion() {
      return this.canEditVersion && !this.activeVersion?.isMain
    },

    productInfo() {
      return this.craftOrder.materialInfo ?? {}
    },
    showFormData() {
      const info = this.productInfo
      return [
        ['类型', info.dataProductType], ['技术型号', info.dataTechnicalCode],
        ['规格型号', info.dataSpecification], ['材质', info.dataMaterialGrade],
        ['产品类别', info.dataCategory], ['单位', info.dataUnit || info.dataBaseUnitName],
        ['客户料号', info.dataCustomerMaterialNo], ['产品备注', info.dataRemark]
      ].map(([label, value]) => ({
        label, value: value || '-', type: ShowFormDataType.文本,
        validate: true, tagClass: '', timeFormat: ''
      })) as ShowFormData[]
    }
  },

  data() {
    return {
      localEditing: false,
      draftName: '',
      craftOrder: {
        ...this.modelValue
      } as CraftOrderDto,
      dragIndex: null as number | null,
      versionList: [] as CraftVersionDto[],
      versionLoading: false,
      orderLoading: false,
      routeLoading: false,
      orderRequestToken: 0,
      versionRequestToken: 0,
      routeRequestToken: 0,
      loadedVersionId: null as number | null,
      originalVersion: null as Record<string, { label: string; value: string }> | null,
      originalEditData: null as { version: CraftVersionDto; routes: CraftProcessDto[] } | null,
      saving: false,
    }
  },

  watch: {
    canEditVersion: {
      immediate: true,
      handler(value) { this.$emit('editabilityChange', value) }
    },
    modelValue: {
      deep: true,
      handler(value) {
        if (!this.isSameCraftOrder(value, this.craftOrder)) {
          this.craftOrder = { ...value }
        }
      }
    },
    craftOrder: {
      deep: true,
      handler(value) {
        if (!this.isSameCraftOrder(value, this.modelValue)) {
          this.$emit('update:modelValue', { ...value })
        }
      }
    },
    craftOrderId: {
      immediate: true,
      async handler(value) {
        this.localEditing = false
        this.orderRequestToken++
        this.versionRequestToken++
        this.routeRequestToken++
        this.orderLoading = false
        this.versionLoading = false
        this.routeLoading = false
        this.loadedVersionId = null
        this.originalVersion = null
        this.originalEditData = null
        this.updateRoutes([])
        this.$emit('update:selectedProcessKey', '')
        if (value) {
          if (this.craftOrder.id !== value) {
            await this.load(value)
          } else {
            await this.init()
          }
        } else {
          this.versionList = []
        }
      }
    }
  },
  beforeUnmount() {
    this.orderRequestToken++
    this.versionRequestToken++
    this.routeRequestToken++
  },
  methods: {
    // 保存原始字段值，而非记录编辑次数；改回原值后自然移除提示。
    getVersionSnapshot(routes: CraftProcessDto[]): Record<string, { label: string; value: string }> {
      const snapshot: Record<string, { label: string; value: string }> = {}
      const add = (key: string, label: string, value: unknown) => {
        snapshot[key] = { label, value: typeof value === 'boolean' ? (value ? '是' : '否') : String(value ?? '') }
      }
      add('version', '版本号', this.activeVersion?.version)
      add('remark', '版本备注', this.activeVersion?.remark)
      add('isMain', '主版本', !!this.activeVersion?.isMain)
      routes.filter(route => !route.isDeleted).forEach((route, index) => {
        const key = `process:${route.uiKey}`
        const label = `工序【${route.code} ${route.name}】`
        add(`${key}:position`, `${label}顺序`, index + 1)
        const fields = {
          code: '编号', name: '名称', craftId: '加工工艺', description: '描述',
          theoreticalWorkHours: '理论工时', isReportWork: '是否报工', isInbound: '入库', isOutsource: '外协'
        } as const
        for (const field of Object.keys(fields) as Array<keyof typeof fields>) {
          add(`${key}:${field}`, `${label}${fields[field]}`, route[field])
        }
        const machineIds = [...new Set([...(route.machineIds ?? []),
        ...(route.machineSettings ?? []).map(machine => machine.machineId)])].sort((a, b) => a - b)
        for (const id of machineIds) {
          add(`${key}:machine:${id}`, `${label}机器 ${id} 理论工时`,
            route.machineSettings?.find(machine => machine.machineId === id)?.theoreticalWorkHours ?? 0)
        }
        const addMaterials = (rows: CraftProcessMaterialDto[], parent: string, parentLabel: string) => {
          rows.forEach((row, position) => {
            const materialKey = `${parent}:material:${row.id || `${row.dataType}-${row.dataId}-${position}`}`
            const materialLabel = `${parentLabel}【${row.materialNo || row.dataId} ${row.materialName || ''}】`
            add(`${materialKey}:position`, `${materialLabel}顺序`, position + 1)
            const materialFields = {
              dataType: '物料类型', dataId: '物料', ioType: '领用/产出类型', quantity: '数量',
              unitId: '单位', unitConversionId: '单位换算', lossRate: '损耗率',
              refCraftOrderVersionId: '工艺版本', remark: '备注'
            } as const
            for (const field of Object.keys(materialFields) as Array<keyof typeof materialFields>) {
              add(`${materialKey}:${field}`, `${materialLabel}${materialFields[field]}`, row[field])
            }
            addMaterials(row.substitutes ?? [], materialKey, `${materialLabel}替料`)
          })
        }
        addMaterials(route.materials ?? [], key, `${label}物料`)
      })
      return snapshot
    },
    isSameCraftOrder(left: object | null | undefined, right: object | null | undefined) {
      const leftData = (left ?? {}) as Record<string, Date>
      const rightData = (right ?? {}) as Record<string, Date>
      const keys = new Set([...Object.keys(left ?? {}), ...Object.keys(right ?? {})])
      return [...keys].every(key => {
        const leftValue = leftData[key] instanceof Date ? leftData[key].getTime() : leftData[key]
        const rightValue = rightData[key] instanceof Date ? rightData[key].getTime() : rightData[key]
        return leftValue === rightValue
      })
    },
    async init(selectVersionId?: number | null) {
      const craftOrderId = this.craftOrder.id ?? this.craftOrderId
      if (!craftOrderId) {
        this.versionList = []
        this.updateRoutes([])
        return
      }
      this.versionLoading = true
      const requestToken = ++this.versionRequestToken
      try {
        const result = await this.getVersionList()
        if (requestToken !== this.versionRequestToken) {
          return
        }
        this.versionList = result?.data ?? []

        const nextVersionId = selectVersionId !== undefined
          ? (selectVersionId ?? this.versionList[0]?.id)
          : (this.activeVersionId ?? this.versionList[0]?.id)
        if (nextVersionId && this.versionList.some(item => item.id === nextVersionId)) {
          await this.activateVersion(nextVersionId)
        } else {
          this.$emit('update:activeVersionId', null)
          this.loadedVersionId = null
          this.updateRoutes([])
        }
      } finally {
        if (requestToken === this.versionRequestToken) {
          this.versionLoading = false
        }
      }
    },
    async refreshVersions(selectVersionId?: number | null) {
      await this.init(selectVersionId)
    },
    showChange() {
      (this.$refs.versionChangeDialog as EasyUI.Dialog).open()
    },
    revertVersionChanges() {
      if (!this.canEditVersion || this.loading || !this.versionChanges.length
        || !this.originalEditData || this.originalEditData.version.id !== this.activeVersionId) return
      const original = cloneDeep(this.originalEditData)
      Object.assign(this.activeVersion!, original.version)
      this.dragIndex = null
      this.updateRoutes(original.routes)
      const selected = original.routes.some(route => route.uiKey === this.selectedProcessKey)
        ? this.selectedProcessKey : (original.routes[0]?.uiKey ?? '')
      this.$emit('update:selectedProcessKey', selected)
    },
    async load(craftOrderId: number) {
      if (!craftOrderId) return
      const requestToken = ++this.orderRequestToken
      this.orderLoading = true
      try {
        const result = await this.$http.get('/api/technical/CraftOrderManage/GetCraftOrderInfo', { craftOrderId })
        if (requestToken !== this.orderRequestToken) return
        this.craftOrder = { ...this.craftOrder, ...(result?.data ?? {}) }
        this.$emit('loaded', { ...this.craftOrder })
        await this.$nextTick()
        if (requestToken !== this.orderRequestToken) return
        await this.init()
      } catch (error) {
        if (requestToken === this.orderRequestToken) this.$kingzon_messager.showError(error)
      } finally {
        if (requestToken === this.orderRequestToken) this.orderLoading = false
      }
    },
    async getVersionList() {
      return await this.$http.get('/api/technical/CraftOrderManage/GetCraftVersionList', {
        craftOrderId: this.craftOrder.id ?? this.craftOrderId
      })
    },
    async saveHeader() {
      if (this.saving) return
      const name = this.draftName.trim()
      if (!name || name.length > 100) {
        this.$kingzon_messager.showInfo('请输入不超过100个字符的工艺单名称')
        return
      }
      this.saving = true
      try {
        const result = await this.$http.post('/api/technical/CraftOrderManage/UpdateCraftOrder', {
          id: this.craftOrder.id, name
        })
        this.craftOrder = { ...this.craftOrder, ...(result?.data ?? {}), name }
        this.localEditing = false
        this.$emit('saved', { craftOrder: { ...this.craftOrder }, isNew: false })
      } catch (error) {
        this.$kingzon_messager.showError(error)
      } finally {
        this.saving = false
      }
    },
    startHeaderEdit() {
      this.draftName = this.craftOrder.name ?? ''
      this.localEditing = true
    },
    cancel() {
      this.localEditing = false
      this.draftName = ''
    },
    async versionClick(item: CraftVersionDto | null) {
      if (!item || this.saving || this.versionLoading || this.routeLoading) {
        return
      }
      if (this.versionChanges.length != 0) {
        var isConfirm = false
        await this.$kingzon_messager.showConfim("当前版本有未保存的修改，确认切换吗？").then(() => {
          isConfirm = true
        })
        if (!isConfirm) {
          return false
        }
      }
      await this.activateVersion(item.id)
    },

    async delVersion(item: CraftVersionDto | null) {
      if (!item || this.saving) {
        return
      }

      if (this.versionList.length <= 1) {
        this.$kingzon_messager?.showError('至少保留一个版本')
        return
      }
      return this.$kingzon_messager.showConfim('确认删除该版本？').then(async () => {
        this.saving = true
        try {
          await this.$http.post('/api/technical/CraftOrderManage/DeleteCraftVersion', { id: item.id })
          await this.init(this.activeVersionId === item.id ? null : this.activeVersionId)
        } catch (err) {
          this.$kingzon_messager.showError(err)
        }
        finally {
          this.saving = false
        }
      })
    },

    validateVersion(): boolean {
      const version = this.activeVersion
      if (!version) return false

      const versionName = (version.version ?? '').trim()

      if (!versionName) {
        this.$kingzon_messager.showError('版本号不能为空')
        return false
      }

      // 检查同一工艺单下的其他版本，排除当前版本自身
      const isDuplicate = this.versionList.some(item =>
        item.id !== version.id &&
        (item.version ?? '').trim().toLowerCase() === versionName.toLowerCase()
      )

      var any = this.routes.filter(it => {
        if (it.isInbound || it.isOutsource) {
          if (it.materials.filter(x => x.ioType == 2).length == 0) {
            return true
          }
        }
      })

      if (any.length != 0) {
        this.$kingzon_messager.showError(`工序 [ "${any.map(it => it.code + '-' + it.name).join(' , ')}" ]设置为外协或入库，但是未配置入库物料，请检查`)
        return false
      }

      if (isDuplicate) {
        this.$kingzon_messager.showError(`版本号 "${versionName}" 已存在，请使用其他版本号`)
        return false
      }

      return true
    },

    openCopyVersion() {
      const craftOrderId = this.craftOrderId ?? this.craftOrder.id
      if (this.mode !== 'edit' || !craftOrderId || this.loading) return
      if (this.canEditVersion && this.versionChanges.length) {
        this.$kingzon_messager.showInfo('请先暂存当前版本的修改，再复制版本')
        return
      }
      const dialog = this.$refs.copyVersionDialog as EasyUI.Dialog
      dialog.open()
    },

    async onVersionCopied(result: { craftOrderId: number; versionId: number }) {
      if (result.craftOrderId !== (this.craftOrderId ?? this.craftOrder.id)) return
      this.saving = true
      try {
        await this.init(result.versionId)
        const dialog = this.$refs.copyVersionDialog as EasyUI.Dialog
        dialog.close()
        this.$emit('versionSaved')
      } catch (error) {
        this.$kingzon_messager.showError(error)
      } finally {
        this.saving = false
      }
    },

    async addVersion() {
      const craftOrderId = this.craftOrder.id ?? this.craftOrderId
      if (!craftOrderId || this.saving) {
        return
      }

      this.saving = true
      try {
        const result = await this.$http.post('/api/technical/CraftOrderManage/CreateCraftVersion', { craftOrderId })
        await this.init(result?.data?.id)
      } finally {
        this.saving = false
      }
    },

    selectProcess(route: CraftProcessDto | null) {
      if (!route) {
        return
      }

      this.$emit('update:selectedProcessKey', route.uiKey)
    },

    startDrag(index: number) {
      if (!this.canEditRoutes) {
        return
      }

      this.dragIndex = index
    },

    dropRoute(index: number) {
      if (!this.canEditRoutes) {
        return
      }

      if (this.dragIndex === null || this.dragIndex === index) {
        return
      }

      const next = [...this.routes]

      const [moving] = next.splice(this.dragIndex, 1)

      next.splice(index, 0, moving)

      this.dragIndex = null

      this.updateRoutes(next)
    },

    async publishVersion() {
      if (!this.canEditVersion || this.saving || !this.activeVersion?.id) return
      const versionId = this.activeVersion.id
      const lastRoute = this.routes[this.routes.length - 1]
      if (!lastRoute || !lastRoute.isReportWork || !(lastRoute.isInbound || lastRoute.isOutsource)) {
        var errs = [] as string[]
        if (!lastRoute.isReportWork) {
          errs.push('最后一道工序非报工工序')
        }
        if (!(lastRoute.isInbound || lastRoute.isOutsource)) {
          errs.push('不是入库或外协工序，会导致没有产出物料')
        }
        errs.push("确认发布吗？")
        this.$kingzon_messager.showConfim(errs.join("，"), '请检查').then(() => {
          this.putVersion(versionId)
        })
      } else {
        await this.putVersion(versionId)
      }

    },

    async putVersion(versionId) {
      if (!this.validateVersion()) return
      if (!await this.saveVersion() || this.activeVersion?.id !== versionId) return
      this.saving = true
      try {
        await this.$http.post('/api/technical/CraftOrderManage/PublishCraftVersion', { id: versionId })
        await this.init(versionId)
      } catch (error) {
        this.$kingzon_messager.showError(error)
      } finally { this.saving = false }
    },

    async obsoleteVersion() {
      if (!this.activeVersion?.id || this.saving) return
      this.saving = true
      try {
        await this.$http.post('/api/technical/CraftOrderManage/ObsoleteCraftVersion', { id: this.activeVersion.id })
        await this.init(this.activeVersion.id)
      } catch (error) {
        this.$kingzon_messager.showError(error)
      } finally { this.saving = false }
    },

    async restoreVersion() {
      if (!this.activeVersion?.id || this.saving || this.activeVersion.status !== CraftOrderVersionStatus.已废弃) return
      const versionId = this.activeVersion.id
      this.saving = true
      try {
        await this.$http.post('/api/technical/CraftOrderManage/RestoreCraftVersion', { id: versionId })
        await this.init(versionId)
      } catch (error) {
        this.$kingzon_messager.showError(error)
      } finally { this.saving = false }
    },

    async unpublishVersion() {
      if (this.saving) return
      return this.$kingzon_messager.showConfim("确认取消发布该版本吗？").then(async () => {
        if (!this.activeVersion?.id) return
        this.saving = true
        try {
          await this.$http.post('/api/technical/CraftOrderManage/UnpublishCraftVersion', { id: this.activeVersion.id })
          await this.init(this.activeVersion.id)
        } catch (err) {
          this.$kingzon_messager.showError(err);
        } finally { this.saving = false }
      })
    },

    async changeMainVersion() {
      if (this.saving) return
      return this.$kingzon_messager.showConfim("确认设置当前版本为主版本吗？").then(async () => {
        if (!this.activeVersion?.id) return
        this.saving = true
        try {
          await this.$http.get('/api/technical/CraftOrderManage/ChangeMainVersion', { versionId: this.activeVersion.id })
          await this.init(this.activeVersion.id)
        } catch (err) {
          this.$kingzon_messager.showError(err);
        } finally { this.saving = false }
      })
    },

    async activateVersion(versionId: number | null | undefined) {
      if (!versionId) {
        return
      }
      this.$emit('update:activeVersionId', versionId)
      await this.loadRoutes(versionId)
    },

    async loadRoutes(versionId: number | null | undefined) {
      const requestToken = ++this.routeRequestToken
      this.routeLoading = !!versionId
      this.originalVersion = null
      this.originalEditData = null
      this.loadedVersionId = null
      this.updateRoutes([])
      if (!versionId) {
        this.loadedVersionId = null
        this.updateRoutes([])
        this.$emit('update:selectedProcessKey', '')
        return
      }

      try {
        const result = await this.$http.get('/api/technical/CraftOrderManage/GetCraftVersion', { versionId })
        if (requestToken !== this.routeRequestToken) return
        const routes = ((result?.data?.nodeSettings ?? []) as CraftProcessDto[]).map(item => {
          const process = new CraftProcessDto(item)
          if (process.id) process.uiKey = `saved-${process.id}`
          return process
        })
        const version = this.versionList.find(item => item.id === versionId)
        this.originalEditData = version ? cloneDeep({ version, routes }) : null
        this.originalVersion = this.getVersionSnapshot(routes)
        this.loadedVersionId = versionId
        this.updateRoutes(routes)
        const selected = routes.some(item => item.uiKey === this.selectedProcessKey)
          ? this.selectedProcessKey
          : (routes[0]?.uiKey ?? '')
        this.$emit('update:selectedProcessKey', String(selected))
      } catch (error) {
        if (requestToken === this.routeRequestToken) this.$kingzon_messager.showError(error)
      } finally {
        if (requestToken === this.routeRequestToken) this.routeLoading = false
      }
    },

    async saveVersion(): Promise<boolean> {
      if (!this.canEditVersion || this.saving) return false
      const craftOrderId = this.craftOrder.id ?? this.craftOrderId
      const version = this.activeVersion
      if (!craftOrderId || !version?.id) {
        return false
      }

      this.$emit('beforeSave')
      const routes = this.routes.map(item => ({ ...item }))
      const codes = routes.map(item => item.code.trim())
      if (codes.some(code => !code || code.length > 50) || new Set(codes).size !== codes.length) {
        this.$kingzon_messager.showInfo('工序编号不能为空、不能重复，且最多50个字符')
        return false
      }
      if (routes.some(item => !Number.isFinite(item.theoreticalWorkHours ?? 0) || item.theoreticalWorkHours < 0 || item.theoreticalWorkHours > 99999999.9999
        || (item.machineSettings ?? []).some(machine => !Number.isFinite(machine.theoreticalWorkHours) || machine.theoreticalWorkHours < 0 || machine.theoreticalWorkHours > 99999999.9999))) {
        this.$kingzon_messager.showInfo('理论加工工时必须介于0与99999999.9999小时之间')
        return false
      }
      this.saving = true
      try {
        await this.$http.post('/api/technical/CraftOrderManage/SaveCraftVersion', {
          id: version.id,
          craftOrderId,
          version: version.version,
          remark: version.remark,
          isMain: version.isMain,
          nodeSettings: routes.map((item, index) => ({
            ...item,
            uiKey: undefined,
            code: item.code.trim(),
            theoreticalWorkHours: item.theoreticalWorkHours ?? 0,
            materials: (item.materials ?? []).filter(material => item.isInbound || item.isOutsource || Number(material.ioType) !== 2),
            id: item.id ?? 0,
            sequence: (index + 1) * 10,
            machineIds: item.machineIds ?? [],
            isDeleted: false
          }))
        })

        // 重新查询版本列表：同步版本描述、主版本状态及新建工序的真实 Id。
        await this.init(version.id)
        this.$emit('versionSaved', { versionId: version.id })
        return true
      } catch (error) {
        this.$kingzon_messager.showError(error)
        return false
      } finally {
        this.saving = false
      }
    },

    updateRoutes(routes: CraftProcessDto[]) {
      const next = routes.map((item, index) => new CraftProcessDto({
        ...item,
        uiKey: item.uiKey || (item.id ? `saved-${item.id}` : new CraftProcessDto().uiKey),
        sequence: (index + 1) * 10
      }))
      this.$emit('update:routes', next)
    },

    addProcess() {
      const maxCode = Math.max(0, ...this.routes
        .map(item => Number(item.code))
        .filter(value => !Number.isNaN(value)))
      const process = new CraftProcessDto({
        code: String(maxCode + 10),
        name: '新工序',
        sequence: (this.routes.length + 1) * 10
      })
      this.updateRoutes([...this.routes, process])
      this.$emit('update:selectedProcessKey', process.uiKey)
    },

    moveProcess(step: number) {
      const index = this.routes.findIndex(item => item.uiKey === this.selectedProcessKey)
      const target = index + step
      if (index < 0 || target < 0 || target >= this.routes.length) {
        return
      }
      const routes = [...this.routes]
        ;[routes[index], routes[target]] = [routes[target], routes[index]]
      this.updateRoutes(routes)
    },

    removeProcess() {
      const index = this.routes.findIndex(item => item.uiKey === this.selectedProcessKey)
      if (index < 0) {
        return
      }
      const routes = this.routes.filter((_, routeIndex) => routeIndex !== index)
      this.updateRoutes(routes)
      this.$emit('update:selectedProcessKey', String(routes[Math.min(index, routes.length - 1)]?.uiKey ?? ''))
    }
  }
})
</script>

<style scoped>
.craft-order-panel,
.version-process-panel {
  flex: 0 0 auto;
  margin-bottom: 4px;
}

.craft-order-panel :deep(.panel-header),
.version-process-panel :deep(.panel-header) {
  padding: 0;
}

.panel-header-row {
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 32px;
  padding: 2px 10px;
  box-sizing: border-box;
  width: 100%;
}

.product-heading {
  flex-wrap: wrap;
}

.product-heading strong {
  font-size: 14px;
  color: #253449;
}

.product-code {
  font-size: 12px;
  color: var(--color-blue);
}

.order-meta {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-left: auto;
  color: #777;
  font-size: 12px;
  min-width: 0;
}

.order-code {
  white-space: nowrap;
}

.order-name {
  max-width: 220px;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.product-show-form {
  font-size: 12px;
}

.product-show-form :deep(.form-field) {
  margin-bottom: 2px;
}

.panel-title {
  font-size: 12px;
  white-space: nowrap;
}

.panel-actions,
.route-actions {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
  padding-right: 10px;
}

.version-list {
  display: flex;
  align-items: center;
  gap: 5px;
  flex: 1;
  min-width: 0;
  overflow-x: auto;
}

.version-item {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
  height: 34px;
  padding: 0 24px 0 7px;
  box-sizing: border-box;
  font-size: 12px;
  border: 1px solid #ddd;
  background: #fff;
  white-space: nowrap;
}

.version-item.selected {
  border-color: var(--color-blue);
  color: var(--color-blue);
  background: var(--color-blue-bg);
}

.version-name {
  font-weight: 600;
}

.version-main {
  color: #667085;
  font-size: 11px;
}

.version-delete {
  position: absolute;
  right: 4px;
  cursor: pointer;
  color: #999;
}

.version-delete:hover {
  color: #dc2626;
}

.version-header {
  min-height: 46px;
  flex-wrap: wrap;
}

.version-header .version-list {
  min-width: 180px;
}

.main-version-field {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
  white-space: nowrap;
  font-size: 12px;
}

.version-line {
  width: 100%;
  min-height: 44px;
  padding: 2px 10px;
  box-sizing: border-box;
  border-bottom: 1px solid var(--color-grey-bg);
}

.version-editor {
  display: flex;
  align-items: center;
  width: 100%;
  min-width: 0;
  gap: 10px;
}

.version-field {
  display: flex;
  align-items: center;
  flex: 1 1 auto;
  min-width: 0;
}

.version-label {
  flex: 0 0 auto;
  margin-right: 6px;
  white-space: nowrap;
}

.version-input {
  flex: 1 1 0;
  min-width: 0;
}

.main-version {
  display: flex;
  align-items: center;
  justify-content: flex-start;

  flex: 0 0 72px;
  width: 72px;

  gap: 4px;
  white-space: nowrap;
}

.route-line {
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 36px;
  padding: 2px 10px;
  font-size: 12px;
}

.route-scroll {
  flex: 1;
  min-width: 0;
  overflow-x: auto;
}

.route-list {
  display: flex;
  align-items: center;
  gap: 6px;
  width: max-content;
  min-width: 100%;
}

.route-list i {
  color: #777;
  font-style: normal;
}

.route-chip {
  flex-shrink: 0;
  height: 26px;
  padding: 0 8px;
  border: 1px solid #ddd;
  background: #fff;
  cursor: pointer;
}

.route-chip.active {
  color: var(--color-blue);
  background: var(--color-blue-bg);
  border-color: var(--color-blue);
}

.route-chip[draggable='true'] {
  cursor: grab;
}

.empty-route {
  color: #999;
}
</style>
