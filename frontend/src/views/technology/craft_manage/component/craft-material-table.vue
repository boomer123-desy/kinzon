<template>
  <div class="material-drop-zone"
    :class="{ 'drop-active': dropState, 'drop-append': dropState?.position === 'append' && !dropState?.highlight }"
    @dragenter="onDragEnter" @dragover.stop="onDragOver" @dragleave.stop="onDragLeave" @drop.stop="onDrop">
    <div v-if="dropState" class="drop-hint" role="status">{{ dropHint }}</div>
    <KingZonDataGrid ref="grid" :id="`craft-material-${ioType}-${parentRow ? 'substitute' : 'main'}`"
      :showFieldFilter="false" :columnMoving="false" :virtualScroll="false" :lazy="false" class="material-table"
      :data="rows" :pagination="false" :columnResizing="true" selectionMode="single" :selection="selectedRow"
      :dblclickToEdit="!readonly" editMode="row" :rowHeight="30" :rowCss="rowClass" @selectionChange="selectRow">
      <GridColumn v-if="!parentRow" :expander="true" width="28" />
      <GridColumn field="materialNo" title="编码" width="100">
        <template #body="{ row }"><span :data-material-index="rows.indexOf(row)" :title="row.materialNo">{{
          row.materialNo }}</span></template>
      </GridColumn>
      <GridColumn field="materialName" :title="parentRow ? '替料名称' : '物料名称'" width="140">
        <template #body="{ row }"><span :title="row.materialName">{{ row.materialName }}</span></template>
      </GridColumn>
      <GridColumn field="materialType" title="类型" width="60" />
      <GridColumn field="quantity" title="数量" width="85" :editable="!readonly" cellCss="kingzon-cell-editable">
        <template #edit="{ row }">
          <NumberBox :modelValue="row.quantity" @update:modelValue="updateMaterial(row, { quantity: $event })" :min="0"
            :precision="4" />
        </template>
      </GridColumn>
      <GridColumn field="unitName" title="单位" width="50" />
      <GridColumn v-if="ioType === 1" field="lossRate" title="损耗率%" width="85" :editable="!readonly"
        cellCss="kingzon-cell-editable">
        <template #edit="{ row }">
          <NumberBox :modelValue="row.lossRate" @update:modelValue="updateMaterial(row, { lossRate: $event })" :min="0"
            :precision="4" />
        </template>
      </GridColumn>
      <GridColumn v-if="ioType === 1" title="工艺版本" width="130">
        <template #body="{ row }">
          <div v-if="['成品', '半成品'].includes(row.materialType)" class="version-picker">
            <ComboBox :modelValue="row.refCraftOrderVersionId"
              @update:modelValue="updateMaterial(row, { refCraftOrderVersionId: $event })"
              :data="versionOptions[row.dataId] || []" valueField="id" textField="text" :disabled="readonly"
              style="width: 126px" placeholder="已发布版本" @click="$emit('loadVersions', row)">
              <template #item="scope">
                <div style="display: flex; align-items: center; gap: 5px;">
                  <span>{{ scope.row.text }}</span>
                  <span v-if="scope.row.isMain" title="主版本" aria-label="主版本" class="version-main-badge"
                    :class="{ selected: scope.row.id === row.refCraftOrderVersionId }"
                    style="display: inline-flex; align-items: center; justify-content: center; width: 16px; height: 16px; border-radius: 50%; font-size: 11px; border: 1px solid currentColor;">主</span>
                </div>
              </template>
            </ComboBox>
            <LinkButton title="新增工艺版本" aria-label="新增工艺版本" :plain="true" :disabled="readonly"
              @click.stop="$emit('createCraftVersion', row)">
              <font-awesome-icon icon="fa-solid fa-pen-to-square" class="kingzon-detail-text" />
            </LinkButton>
          </div>
          <span v-else>-</span>
        </template>
      </GridColumn>
      <GridColumn v-if="!parentRow" title="替料" width="48" align="center">
        <template #body="{ row }">{{ row.substitutes?.length || 0 }}</template>
      </GridColumn>
      <GridColumn title="操作" width="48" align="center">
        <template #body="{ row }">
          <LinkButton :plain="true" :disabled="readonly" title="删除物料" aria-label="删除物料"
            @click.stop="$emit('remove', { row, main: parentRow || row })">
            <font-awesome-icon icon="fa-solid fa-trash-can" class="kingzon-detail-text" />
          </LinkButton>
        </template>
      </GridColumn>
      <template v-if="!parentRow" #detail="{ row }">
        <div class="substitute-detail" :data-main-index="rows.indexOf(row)">
          <div class="substitute-heading">{{ row.materialName }} · 替换料</div>
          <CraftMaterialTable v-if="row.substitutes?.length" :rows="row.substitutes" :parentRow="row" :ioType="ioType"
            :readonly="readonly" :selectedRow="selectedRow" :versionOptions="versionOptions"
            :style="{ height: `${Math.min(210, 48 + row.substitutes.length * 32)}px` }"
            @updateMaterial="$emit('updateMaterial', $event)" @select="$emit('select', $event)"
            @remove="$emit('remove', $event)" @loadVersions="$emit('loadVersions', $event)"
            @createCraftVersion="$emit('createCraftVersion', $event)" @dropResources="$emit('dropResources', $event)" />
          <div v-else class="empty-substitute">暂无替料，将资料库物料拖到这里，或选中主料后点击“加入替料”。</div>
        </div>
      </template>
    </KingZonDataGrid>
  </div>
</template>

<script lang="ts">
import type { MaterialGridWrapper, CraftProcessMaterialDto, CraftResourceTarget, CraftVersionOptionDto } from '@/types/craft'
import { defineComponent, type PropType } from 'vue'

type DropState = {
  target: CraftResourceTarget
  position: 'before' | 'after' | 'append'
  row?: CraftProcessMaterialDto
  main?: CraftProcessMaterialDto
  highlight?: CraftProcessMaterialDto
}

export default defineComponent({
  name: 'CraftMaterialTable',
  emits: ['select', 'remove', 'loadVersions', 'createCraftVersion', 'dropResources', 'updateMaterial'],
  props: {
    rows: { type: Array as PropType<CraftProcessMaterialDto[]>, default: () => [] },
    parentRow: Object as PropType<CraftProcessMaterialDto>,
    selectedRow: Object as PropType<CraftProcessMaterialDto>,
    ioType: { type: Number, default: 1 },
    readonly: Boolean,
    versionOptions: { type: Object as PropType<Record<number, CraftVersionOptionDto[]>>, default: () => ({}) }
  },
  data() { return { dropState: null as DropState | null } },
  computed: {
    dropHint(): string {
      const state = this.dropState
      if (!state) return ''
      const area = state.target === 'substitute' ? `${state.main?.materialName || '当前主料'}的替料` : this.ioType === 2 ? '入库物料' : '领用主料'
      return state.row
        ? `松开：在「${state.row.materialName || state.row.materialNo}」${state.position === 'before' ? '前' : '后'}插入${area}`
        : `松开：追加到${area}`
    }
  },
  mounted() {
    window.addEventListener('dragend', this.clearDrop)
    window.addEventListener('drop', this.clearDrop, true)
  },
  beforeUnmount() {
    window.removeEventListener('dragend', this.clearDrop)
    window.removeEventListener('drop', this.clearDrop, true)
  },
  watch: {
    readonly() { this.clearDrop() },
    rows() { this.clearDrop() }
  },
  methods: {
    updateMaterial(row: CraftProcessMaterialDto, changes: Partial<CraftProcessMaterialDto>) {
      if (!this.readonly && this.rows.includes(row)) this.$emit('updateMaterial', { row, changes })
    },
    clearDrop() { this.dropState = null },
    rowClass(row: CraftProcessMaterialDto) {
      const state = this.dropState
      if (!state || state.highlight !== row) return ''
      return state.target === 'substitute' && !this.parentRow
        ? 'material-drop-substitute' : `material-drop-${state.position}`
    },
    acceptsDrag(event: DragEvent) {
      return !this.readonly && Array.from(event.dataTransfer?.types ?? []).includes('application/x-craft-resources')
    },
    onDragEnter(event: DragEvent) {
      if ((event.target as HTMLElement)?.closest('.material-drop-zone') !== event.currentTarget) this.clearDrop()
    },
    resolveDrop(event: DragEvent): DropState | null {
      const element = event.target as HTMLElement
      if (!element?.closest || element.closest('.material-drop-zone') !== event.currentTarget) return null
      const body = element.closest('.datagrid-body')
      if (!body || body.closest('.material-drop-zone') !== event.currentTarget) return null
      const detail = element.closest<HTMLElement>('.substitute-detail')
      if (!this.parentRow && detail) {
        const main = this.rows[Number(detail.dataset.mainIndex)]
        return main ? { target: 'substitute', main, highlight: main, position: 'append' } : null
      }
      const tr = element.closest<HTMLElement>('tr.datagrid-row')
      const marker = tr?.querySelector<HTMLElement>('[data-material-index]')
      const row = marker ? this.rows[Number(marker.dataset.materialIndex)] : undefined
      const target = this.parentRow ? 'substitute' : this.ioType === 2 ? 'output' : 'input'
      if (!row || !tr) return { target, main: this.parentRow, position: 'append' }
      const rect = tr.getBoundingClientRect()
      const fraction = (event.clientY - rect.top) / Math.max(rect.height, 1)
      if (!this.parentRow && fraction >= 0.25 && fraction <= 0.75) {
        return { target: 'substitute', main: row, highlight: row, position: 'append' }
      }
      return { target, main: this.parentRow, row, highlight: row, position: fraction < 0.5 ? 'before' : 'after' }
    },
    onDragOver(event: DragEvent) {
      if (!this.acceptsDrag(event)) { this.clearDrop(); return }
      this.dropState = this.resolveDrop(event)
      if (!this.dropState) return
      event.preventDefault()
      event.dataTransfer!.dropEffect = 'copy'
      const body = (event.target as HTMLElement).closest<HTMLElement>('.datagrid-body')
      if (body) {
        const rect = body.getBoundingClientRect()
        if (event.clientY < rect.top + 28) body.scrollTop -= 18
        else if (event.clientY > rect.bottom - 28) body.scrollTop += 18
      }
    },
    onDragLeave(event: DragEvent) {
      if (!(event.currentTarget as HTMLElement).contains(event.relatedTarget as Node)) this.clearDrop()
    },
    onDrop(event: DragEvent) {
      // 重新解析落点，避免滚动、切换数据后使用过期的预览位置。
      const placement = this.acceptsDrag(event) ? this.resolveDrop(event) : null
      this.clearDrop()
      if (!placement) return
      event.preventDefault()
      try {
        const resources = JSON.parse(event.dataTransfer!.getData('application/x-craft-resources'))
        if (!Array.isArray(resources) || !resources.length || resources.some(row => !row || typeof row !== 'object')) return
        const { target, position, row, main } = placement
        this.$emit('dropResources', { resources, target, position, row, main })
      } catch { /* 忽略外部或已损坏的拖拽数据。 */ }
    },
    selectRow(row: CraftProcessMaterialDto) {
      if (row) this.$emit('select', { row, main: this.parentRow || row })
    },
    getGrid() {
      const wrapper = (this.$refs.grid as MaterialGridWrapper | undefined)
      return wrapper?.dataGrid ?? wrapper?.$refs?.dataGrid ?? wrapper
    },
    expandMaterial(row: CraftProcessMaterialDto) {
      this.getGrid()?.expandRow?.(row)
    },
    async focusMaterial(row: CraftProcessMaterialDto) {
      if (!this.rows.includes(row)) return false
      this.getGrid()?.selectRow?.(row)
      await this.$nextTick()
      // 依据真实行元素滚动；展开替料后不能再按行号推算 DOM 行位置。
      const index = this.rows.indexOf(row)
      const marker = Array.from((this.$el as HTMLElement).querySelectorAll<HTMLElement>('[data-material-index]')).find(element =>
        Number(element.dataset.materialIndex) === index && element.closest('.material-drop-zone') === this.$el) as HTMLElement | undefined
      const tr = marker?.closest('tr')
      const body = tr?.closest('.datagrid-body')
      if (tr && body) {
        const rect = tr.getBoundingClientRect(), bounds = body.getBoundingClientRect()
        if (rect.top < bounds.top) body.scrollTop += rect.top - bounds.top
        else if (rect.bottom > bounds.bottom) body.scrollTop += rect.bottom - bounds.bottom
      }
      return true
    }
  }
})
</script>

<style scoped>
.material-drop-zone {
  position: relative;
  display: flex;
  flex-direction: column;
  width: 100%;
  min-width: 0;
  min-height: 0;
  overflow: hidden;
}

/* KingZonDataGrid 将 class 透传给内部 DataGrid；同时撑满主料和固定高度的替料区域。 */
.material-drop-zone :deep(.material-table) {
  flex: 1 1 0;
  width: 100%;
  height: 100%;
  min-width: 0;
  min-height: 0;
  box-sizing: border-box;
  border: 1px solid var(--color-grey-bg);
}

.material-table :deep(.datagrid-view2) {
  min-width: 0;
}

.version-picker {
  display: flex;
  align-items: center;
  gap: 2px;
}

.version-main-badge {
  color: var(--color-blue);
}

.version-main-badge.selected {
  color: #fff;
}

.substitute-detail {
  padding: 6px 10px 8px 28px;
}

.substitute-heading {
  font-size: 12px;
  color: #64748b;
  margin-bottom: 4px;
}

.empty-substitute {
  padding: 8px 0;
  font-size: 12px;
  color: #888;
}

/* 使用单元格内阴影，插入线不会改变行高或被 EasyUI 选中行样式盖住。 */
.material-drop-zone :deep(.material-drop-before > td) {
  background: #eff6ff !important;
  box-shadow: inset 0 3px 0 #2563eb;
}

.material-drop-zone :deep(.material-drop-after > td) {
  background: #eff6ff !important;
  box-shadow: inset 0 -3px 0 #2563eb;
}

.material-drop-zone :deep(.material-drop-substitute > td) {
  background: #dbeafe !important;
  box-shadow: inset 0 2px 0 #60a5fa, inset 0 -2px 0 #60a5fa;
}

.drop-active {
  outline: 1px solid #93c5fd;
  outline-offset: -1px;
}

.drop-append::after {
  content: "";
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 3px;
  z-index: 9;
  background: #2563eb;
  pointer-events: none;
}

.drop-hint {
  position: absolute;
  z-index: 10;
  top: 2px;
  right: 6px;
  max-width: calc(100% - 12px);
  padding: 4px 8px;
  border-radius: 3px;
  background: #1d4ed8;
  color: #fff;
  font-size: 12px;
  pointer-events: none;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
