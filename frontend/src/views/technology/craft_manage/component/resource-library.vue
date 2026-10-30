<template>
  <aside class="panel library-panel" v-if="selectTabIndex == 1 || selectTabIndex == 2">
    <div class="panel-header"><strong>查找资料库</strong><span class="muted">点击行多选，再点击图标或拖拽加入</span></div>
    <SearchPanel :key="selectedTab === 'machine' ? 'machine' : 'material'" :searchParams="searchParams"
      :searchParamsHidden="selectedTab === 'machine' ? [] : searchParamsHidden" @search="search"
      style="margin: 0 5px" />
    <Tabs :key="selectTabIndex" class="resource-tabs" :plain="true" @tabSelect="tabSelect">
      <TabPanel title="物料" data-theme="material" v-if="selectTabIndex == 1" />
      <TabPanel title="成品" data-theme="product" v-if="selectTabIndex == 1" />
      <TabPanel title="半成品" data-theme="semiProduct" v-if="selectTabIndex == 1" />
      <TabPanel title="设备" data-theme="machine" v-if="selectTabIndex == 2" />
    </Tabs>
    <div v-if="selectedTab === 'machine'" class="machine-library-tip">
      {{ !craftId ? '请先选择加工工艺' : machineLoading ? '正在加载工艺对应的机器…' : machineError ? '机器加载失败，请重试' :
        '仅显示当前工艺的启用机器，可拖入“机器选择”' }}
      <LinkButton v-if="machineError" :plain="true" iconCls="icon-reload" @click="loadMachines">重试</LinkButton>
    </div>
    <KingZonDataGrid ref="dataGrid" :data="gridRows" :loading="selectedTab === 'machine' ? machineLoading : isLoading"
      :pagination="selectedTab !== 'machine'" selectionMode="multiple" :selection="librarySelection" id="resource"
      :isCheckBoxSelection="false" :showFieldFilter="false" @selectionChange="onSelectionChange" class="resource-grid"
      @pageChange="refreshTable" @sortChange="refreshTable">
      <GridColumnGroup align="left" :frozen="true" width="220">
        <GridHeaderRow>
          <GridColumn field="code" title="编码" width="92">
            <template #body="{ row }">
              <span class="resource-drag"
                :draggable="!disabled && !(selectedTab === 'machine' ? machineLoading : isLoading)"
                :title="selectedTab === 'machine' ? '拖入机器选择' : '拖入左侧物料表'" @dragstart="startDrag($event, row)">{{
                  row.code }}</span>
            </template>
          </GridColumn>
          <GridColumn field="name" title="名称" width="115" :overflowTextEllipsis="true">
            <template #body="{ row }">
              <span class="resource-drag"
                :draggable="!disabled && !(selectedTab === 'machine' ? machineLoading : isLoading)"
                :title="resourceTitle(row)" @dragstart="startDrag($event, row)">{{ row.name }}</span>
            </template>
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
      <GridColumnGroup align="center" :columnMoving="true">
        <GridHeaderRow>
          <GridColumn v-if="selectedTab === 'machine'" field="model" title="型号" width="85" />
          <GridColumn v-if="selectedTab === 'machine'" field="categoryName" title="类别" width="75" />
          <GridColumn v-if="selectedTab !== 'machine'" field="unitName" title="单位" width="48" />
          <GridColumn v-if="selectedTab !== 'machine'" field="techCode" title="技术型号" width="110"
            :overflowTextEllipsis="true" />
          <GridColumn v-if="selectedTab !== 'machine'" field="specification" title="规格型号" width="110"
            :overflowTextEllipsis="true" />
          <GridColumn v-if="selectedTab !== 'machine'" field="materialGrade" title="材质牌号" width="100"
            :overflowTextEllipsis="true" />
          <GridColumn v-if="selectedTab !== 'machine'" field="version" title="工艺版本" width="80" />
          <GridColumn v-if="selectedTab !== 'machine'" field="remark" title="备注" width="160"
            :overflowTextEllipsis="true" />
        </GridHeaderRow>
      </GridColumnGroup>
      <GridColumnGroup align="right" :frozen="true" width="94">
        <GridHeaderRow>
          <GridColumn title="操作" width="94" align="center">
            <template #body="{ row }">
              <div v-if="selectedTab === 'machine'" class="resource-actions">
                <LinkButton plain title="加入机器" aria-label="加入机器" :disabled="disabled || machineLoading || !craftId"
                  @click.stop="chooseMachines(row)">
                  <font-awesome-icon icon="fa-solid fa-plus" class="kingzon-detail-text" />
                </LinkButton>
              </div>
              <div v-else class="resource-actions">
                <LinkButton plain title="加入领用" aria-label="加入领用" :disabled="disabled"
                  @click.stop="choose(row, 'input')">
                  <font-awesome-icon icon="fa-solid fa-right-to-bracket" style="color:var(--color-yellow);" />
                </LinkButton>
                <LinkButton plain title="加入入库（需工序勾选入库或外协）" aria-label="加入入库" :disabled="disabled || !allowOutput"
                  @click.stop="choose(row, 'output')">
                  <font-awesome-icon icon="fa-solid fa-right-from-bracket" class="kingzon-detail-text" />
                </LinkButton>
                <LinkButton plain title="加入替料（先选中主料）" aria-label="加入替料" :disabled="disabled || !hasMaterialSelection"
                  @click.stop="choose(row, 'substitute')">
                  <font-awesome-icon icon="fa-solid fa-right-left" style="color:var(--color-green)" />
                </LinkButton>
              </div>
            </template>
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
    </KingZonDataGrid>
    <div class="library-selection-bar">
      <span v-if="selectedTab === 'machine' ? machineLoading : isLoading" role="status"
        aria-live="polite">正在加载资料库…</span>
      <span v-else>已选
        <strong>{{ librarySelection.length }}</strong> 项 ·
        <span v-if="librarySelection.length != 0">再次点击可取消</span>
      </span>
      <LinkButton plain iconCls="icon-clear" :disabled="!librarySelection.length" @click="librarySelection = []">
        清空选择
      </LinkButton>
    </div>
  </aside>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import type { CraftResourceDto, CraftMachineResource, CraftResourceTarget, SearchGroup } from '@/types/craft'
import { ProductType } from '@/types/enums'
import { Search } from '@/types/types'
import type { Filter } from '@kingzon/base-pc'
type LibraryRow = CraftResourceDto | CraftMachineResource

export default defineComponent({
  name: 'ResourceLibrary',
  emits: ['select-resources', 'select-machines', 'machine-options', 'drag-machines'],
  props: { craftId: Number, disabled: Boolean, allowOutput: Boolean, hasMaterialSelection: Boolean, selectTabIndex: Number },
  data() {
    return {
      isLoading: false,
      librarySelection: [] as Array<CraftResourceDto | CraftMachineResource>,
      resourceRequest: 0,
      machineRequest: 0,
      machineLoading: false,
      machineError: false,
      machineRows: [] as Array<CraftMachineResource>,
      selectedTab: 'material',
      filterList: [] as Filter[],
      dataList: [] as Array<CraftResourceDto>,
      searchParams: [new Search('code', '编码'), new Search('name', '名称')],
      searchParamsHidden: [new Search('techCode', '技术型号'), new Search('version', '工艺版本'), new Search('specification', '规格型号')]
    }
  },
  computed: {
    gridRows(): LibraryRow[] {
      if (this.selectedTab !== 'machine') return this.dataList
      return this.machineRows.filter(row => this.filterList.every(filter => {
        if (filter.key !== 'code' && filter.key !== 'name') return true
        return String(row[filter.key] ?? '').toLowerCase().includes(String(filter.value ?? '').toLowerCase())
      }))
    }
  },
  watch: {
    craftId: { immediate: true, handler() { this.loadMachines() } },
    gridRows(rows) { this.librarySelection = this.librarySelection.filter(row => rows.includes(row)) },
    selectTabIndex: {
      immediate: true,
      handler(value) {
        if (value === 1 || value === 2) {
          this.changeTab(value === 2 ? 'machine' : 'material')
        } else {
          ++this.resourceRequest
          this.isLoading = false
          this.librarySelection = []
        }
      }
    }
  },
  beforeUnmount() { ++this.resourceRequest; ++this.machineRequest },
  methods: {
    resourceTitle(row: LibraryRow): string {
      if ('id' in row) return `${row.name}\n型号：${row.model || '-'}\n类别：${row.categoryName || '-'}`
      return `${row.name}\n技术型号：${row.techCode || '-'}\n规格型号：${row.specification || '-'}\n材质牌号：${row.materialGrade || '-'}\n工艺版本：${row.version || '-'}\n备注：${row.remark || '-'}\n可拖拽加入`
    },
    clearSelection() { this.librarySelection = [] },
    onSelectionChange(rows: LibraryRow[] | LibraryRow | null) {
      this.librarySelection = (Array.isArray(rows) ? rows : rows ? [rows] : []).filter(row => this.gridRows.includes(row))
    },
    search(params: SearchGroup[]) {
      this.librarySelection = []
      this.filterList = params.find(item => item.table === 'default')?.filter ?? []
      this.refreshTable()
    },
    tabSelect(event: { $el?: HTMLElement } | null) {
      const tab = event?.$el?.dataset?.theme
      if (!tab || (this.selectTabIndex === 1 && tab === 'machine') ||
        (this.selectTabIndex === 2 && tab !== 'machine')) return
      return this.changeTab(tab)
    },
    async changeTab(tab: string) {
      this.selectedTab = tab
      this.filterList = []
      this.librarySelection = []
      const request = ++this.resourceRequest
      this.isLoading = false
      this.dataList = []
      // 外层 v-if 与标签切换后，等待表格 ref 和分页配置更新再请求。
      await this.$nextTick()
      if (request === this.resourceRequest && this.selectTabIndex === 1) return this.refreshTable()
    },
    async loadMachines() {
      const request = ++this.machineRequest
      const craftId = Number(this.craftId) || 0
      if (this.selectedTab === 'machine') this.librarySelection = []
      this.machineRows = []
      this.machineError = false
      this.machineLoading = craftId > 0
      this.$emit('machine-options', { craftId, rows: [], loading: this.machineLoading })
      if (!craftId) return
      try {
        const result = await this.$http.get('/api/technical/CraftOrderManage/GetCraftMachineList', { craftId })
        if (request !== this.machineRequest) return
        this.machineRows = ((result?.data ?? []) as CraftMachineResource[]).map(row => ({ ...row, id: Number(row.id), craftId }))
          .filter(row => row.id > 0)
        this.$emit('machine-options', { craftId, rows: this.machineRows, loading: false })
      } catch (error) {
        if (request !== this.machineRequest) return
        this.machineError = true
        this.$emit('machine-options', { craftId, rows: [], loading: false })
        this.$kingzon_messager.showError(error)
      } finally { if (request === this.machineRequest) this.machineLoading = false }
    },
    getSelectedMachines(row: LibraryRow): CraftMachineResource[] {
      return this.getSelectedRows(row).filter((item): item is CraftMachineResource => 'id' in item)
    },
    chooseMachines(row: LibraryRow) {
      if (this.disabled || this.machineLoading || !this.craftId) return
      this.$emit('select-machines', { craftId: Number(this.craftId), machines: this.getSelectedMachines(row) })
    },
    async refreshTable() {
      if (this.selectedTab === 'machine') return
      // 与 FormulaSearch 一致：请求参数使用 KingZonDataGrid 内部的 EasyUI DataGrid。
      const grid = this.$refs.dataGrid
      if (!grid) return
      const request = ++this.resourceRequest
      const materialType = this.getMaterialType()
      this.isLoading = true
      this.librarySelection = []
      try {
        const result = await this.$http.post(this.getResourceUrl(), this.getFilters(), grid)
        if (request === this.resourceRequest) this.dataList = ((result?.data ?? []) as CraftResourceDto[]).map(row => ({ ...row, materialType }))
      } catch (error) {
        if (request === this.resourceRequest) this.$kingzon_messager?.showError(error)
      } finally {
        if (request === this.resourceRequest) this.isLoading = false
      }
    },
    getFilters() {
      return this.filterList
    },
    getResourceUrl() {
      switch (this.selectedTab) {
        case 'product': return `/api/technical/CraftOrderManage/GetCraftResourceList?productType=${ProductType.成品}`
        case 'semiProduct': return `/api/technical/CraftOrderManage/GetCraftResourceList?productType=${ProductType.半成品}`
        default: return '/api/technical/CraftOrderManage/GetCraftResourceList'
      }
    },
    startDrag(event: DragEvent, row: LibraryRow) {
      if (this.selectedTab === 'machine') {
        if (this.disabled || this.machineLoading || !this.craftId || !event.dataTransfer) {
          event.preventDefault()
          return
        }
        const machines = this.getSelectedMachines(row)
        event.dataTransfer.effectAllowed = 'copy'
        event.dataTransfer.setData('application/x-craft-machines', JSON.stringify({ craftId: Number(this.craftId), machines }))
        event.dataTransfer.setData('text/plain', `加入 ${machines.length} 台机器`)
        this.$emit('drag-machines')
        return
      }
      if (this.disabled || this.isLoading || !event.dataTransfer) {
        event.preventDefault()
        return
      }
      const resources = this.getSelectedRows(row).filter((item): item is CraftResourceDto => 'dataId' in item).map(item => this.toProcessResource(item))
      event.dataTransfer.effectAllowed = 'copy'
      event.dataTransfer.setData('application/x-craft-resources', JSON.stringify(resources))
      event.dataTransfer.setData('text/plain', `加入 ${resources.length} 条物料`)
    },
    choose(row: CraftResourceDto, target: CraftResourceTarget) {
      if (this.disabled || this.isLoading || (target === 'output' && !this.allowOutput)
        || (target === 'substitute' && !this.hasMaterialSelection)) return
      this.$emit('select-resources', {
        resources: this.getSelectedRows(row).filter((item): item is CraftResourceDto => 'dataId' in item).map(item => this.toProcessResource(item)), target
      })
    },
    getSelectedRows(fallback?: LibraryRow): LibraryRow[] {
      const selected = this.librarySelection.filter(item => this.gridRows.includes(item))
      // 对未选中的行执行操作时只加入该行，避免误用另一个批次的选中项。
      return fallback && !selected.includes(fallback) ? [fallback] : selected.length ? selected : []
    },
    getMaterialType() {
      switch (this.selectedTab) {
        case 'product': return '成品'
        case 'semiProduct': return '半成品'
        default: return '物料'
      }
    },
    toProcessResource(row: CraftResourceDto) {
      return {
        // FormulaManage 的资源行使用 DetailId / DetailType；这里转换为工序物料保存 DTO。
        dataId: row.dataId,
        dataType: row.dataType,
        ioType: 1,
        materialNo: row.code ?? '',
        materialName: row.name ?? '',
        materialType: row.materialType ?? this.getMaterialType(),
        unitId: row.unitId,
        unitName: row.unitName ?? '',
        remark: row.remark
      }
    }
  }
})
</script>

<style scoped>
.panel {
  min-width: 0;
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background: #fff;
  border: 1px solid var(--color-grey-bg);
  border-radius: 0;
}

.panel-header {
  display: flex;
  flex: 0 0 auto;
  justify-content: space-between;
  align-items: center;
  height: 34px;
  padding: 0 14px;
  background-color: var(--color-grey-bg);
  border-bottom: 1px solid var(--color-grey-bg);
  font-size: 13px;
  font-weight: 700;
  box-sizing: border-box;
}

.muted {
  color: #777;
  font-size: 11px;
}

.resource-tabs {
  flex: 0 0 auto;
}

.resource-grid {
  flex: 1;
  min-height: 0;
  border: 0 !important;
}

.machine-library-tip {
  padding: 5px 8px;
  font-size: 12px;
  color: #64748b;
}

.library-selection-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 6px;
  padding: 5px 8px;
  border-top: 1px solid var(--color-grey-bg);
  font-size: 11px;
}

.resource-actions {
  display: flex;
  justify-content: center;
  gap: 2px;
}

.resource-actions :deep(.l-btn) {
  width: 26px;
  min-width: 26px;
  padding: 0;
}
</style>

<style scoped>
.resource-drag {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.resource-drag[draggable="true"] {
  cursor: grab;
}

.resource-drag[draggable="true"]:active {
  cursor: grabbing;
}
</style>
