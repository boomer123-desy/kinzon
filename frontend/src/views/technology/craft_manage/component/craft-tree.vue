<template>
  <aside class="panel tree-panel">
    <div class="panel-header">
      <strong>工艺结构树</strong>
    </div>
    <TreeGrid ref="treeGrid" class="tree-grid" :data="treeRows" id-field="id" tree-field="name" :animate="true"
      :columnResizing="true" @selection-change="selectRow" @row-expand="onNodeExpand" @row-collapse="onNodeCollapse">
      <GridColumnGroup align="left" :columnMoving="true">
        <GridHeaderRow>
          <GridColumn field="name" title="工序 / 物料" :width="nameColumnWidth">
            <template #body="{ row }">
              <span class="tree-node-content" :title="row.name" :class="{ 'node-message': row.isPlaceholder }">
                <font-awesome-icon v-if="getFontAwesomeIcon(row)" :icon="getFontAwesomeIcon(row)"
                  :style="{ color: getFontAwesomeColor(row) }" class="tree-node-fa-icon" />
                <span>{{ row.name }}</span>
              </span>
            </template>
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
      <GridColumnGroup align="right" :frozen="true" width="125">
        <GridHeaderRow>
          <GridColumn field="typeText" title="类型" width="58" align="center" />
          <GridColumn field="quantityText" title="用量" width="50" align="center" />
          <!-- <GridColumn field="code" title="编码" width="90" /> -->
        </GridHeaderRow>
      </GridColumnGroup>
    </TreeGrid>
    <div class="panel-foot">共 {{ routes.length }} 道工序，{{ materialCount }} 条主物料</div>
  </aside>
</template>

<script lang="ts">
import { CraftTreeNodeType, type CraftProcessDto, type CraftProcessMaterialDto, type CraftTreeRow, type CraftTreeSource, type TreeGridRef } from '@/types/craft'
import { ProductType } from '@/types/enums'
import { defineComponent, type PropType } from 'vue';

type MaterialSource = CraftProcessMaterialDto & { name?: string; code?: string }
type MessageKind = 'idle' | 'loading' | 'empty' | 'error'
// 改用 font-awesome-icon 带的图标类，所有层级（含懒加载节点）共用这一映射。
const TYPE_FA_ICONS: Record<CraftTreeNodeType, string> = {
  [CraftTreeNodeType.Process]: 'fa-solid fa-gears',
  [CraftTreeNodeType.Input]: 'fa-solid fa-right-to-bracket',
  [CraftTreeNodeType.Output]: 'fa-solid fa-right-from-bracket',
  [CraftTreeNodeType.Product]: 'fa-solid fa-box',
  [CraftTreeNodeType.SemiProduct]: 'fa-solid fa-cube',
  [CraftTreeNodeType.Material]: 'fa-solid fa-box-open',
  [CraftTreeNodeType.Substitute]: 'fa-solid fa-right-left'
}

const TYPE_FA_COLORS: Record<CraftTreeNodeType, string> = {
  [CraftTreeNodeType.Process]: 'var(--color-blue)',
  [CraftTreeNodeType.Input]: 'var(--color-yellow)',
  [CraftTreeNodeType.Output]: 'var(--color-blue)',
  [CraftTreeNodeType.Product]: 'var(--color-green)',
  [CraftTreeNodeType.SemiProduct]: 'var(--color-blue)',
  [CraftTreeNodeType.Material]: 'var(--color-grey)',
  [CraftTreeNodeType.Substitute]: 'var(--color-green)'
}

const PRODUCT_NODE_TYPES: Record<string, CraftTreeNodeType> = {
  [ProductType[ProductType.成品]]: CraftTreeNodeType.Product,
  [ProductType[ProductType.半成品]]: CraftTreeNodeType.SemiProduct,
  [ProductType[ProductType.原材料]]: CraftTreeNodeType.Material
}

function canLoadChildren(row: CraftTreeSource): boolean {
  // 只有引用了外部工艺版本的半成品/成品才有下级工艺，其余节点保持叶子状态。
  return row.refCraftOrderVersionId != null
    && (row.nodeType === CraftTreeNodeType.Product || row.nodeType === CraftTreeNodeType.SemiProduct)
    && Number(row.dataId) > 0 && row.dataType != null
}

function getMaterialNodeType(materialType?: string): CraftTreeNodeType {
  return PRODUCT_NODE_TYPES[materialType ?? ''] ?? CraftTreeNodeType.Material
}

function indexRows(rows: CraftTreeRow[], index = new Map<string, CraftTreeRow>()) {
  for (const row of rows) {
    index.set(row.id, row)
    indexRows(row.children, index)
  }
  return index
}

export default defineComponent({
  name: 'ProcessTree',
  props: { craftOrderId: Number, activeVersionId: Number, routes: { type: Array as PropType<CraftProcessDto[]>, default: () => [] }, selectedProcessKey: String },
  emits: ['selectProcess'],
  data() {
    return {
      treeRows: [] as CraftTreeRow[],
      treeContext: '',
      requestGeneration: 0
    }
  },
  computed: {
    contextKey(): string {
      return `${this.craftOrderId ?? ''}:${this.activeVersionId ?? ''}`
    },
    nameColumnWidth(): number {
      // EasyUI 每级缩进 16px；另留 260px 给图标、展开按钮和名称。
      // 只计入可见节点，未展开的缓存子树不占用额外宽度。
      let maxDepth = 0
      const pending = this.treeRows.map(row => ({ row, depth: 0 }))
      while (pending.length) {
        const { row, depth } = pending.pop()!
        maxDepth = Math.max(maxDepth, depth)
        if (row.state !== 'closed') {
          for (const child of row.children) pending.push({ row: child, depth: depth + 1 })
        }
      }
      return 260 + maxDepth * 16
    },
    materialCount(): number {
      return this.routes.reduce((count, process) => count + (process.materials ?? [])
        .filter(item => !item.insteadSourceId).length, 0)
    }
  },
  watch: {
    routes: {
      deep: true,
      immediate: true,
      handler() {
        this.rebuildTree()
      }
    },
    contextKey() {
      this.rebuildTree()
    },
    nameColumnWidth() {
      this.$nextTick(() => this.syncNameColumnWidth())
    }
  },
  mounted() {
    this.syncNameColumnWidth()
  },
  beforeUnmount() {
    this.requestGeneration++
  },
  methods: {
    syncNameColumnWidth() {
      const grid = (this.$refs.treeGrid as TreeGridRef | undefined)
      const column = grid?.findColumn('name')
      if (!grid || !column) return
      // GridColumn 仅在挂载时读取 width，后续需通过 EasyUI API 调整。
      // 保留用户手动加宽的列，收起节点时也不自动缩窄，避免滚动位置跳动。
      const currentWidth = Number.parseFloat(String(column.widthState)) || 0
      if (currentWidth < this.nameColumnWidth) grid.resizeColumn('name', this.nameColumnWidth)
    },
    getNodeIcon(row: CraftTreeRow): string {
      return 'tree-custom-icon-hidden'
    },
    getFontAwesomeIcon(row: CraftTreeRow): string {
      if (row.isPlaceholder) {
        switch (row.messageKind) {
          case 'loading':
            return 'fa-solid fa-spinner'

          case 'empty':
            return 'fa-regular fa-circle-question'

          case 'error':
            return 'fa-solid fa-circle-exclamation'

          default:
            return 'fa-solid fa-circle-info'
        }
      }

      return TYPE_FA_ICONS[
        row.nodeType ?? CraftTreeNodeType.Material
      ]
    },

    getFontAwesomeColor(row: CraftTreeRow): string {
      if (row.rootMaterial && row.refCraftOrderVersionId == null) {
        return 'var(--color-yellow)'
      }
      if (row.isPlaceholder) {
        switch (row.messageKind) {
          case 'error':
            return 'var(--color-red)'

          case 'loading':
            return 'var(--color-blue)'

          default:
            return '#999'
        }
      }

      return TYPE_FA_COLORS[
        row.nodeType ?? CraftTreeNodeType.Material
      ]
    },
    rebuildTree() {
      if (this.treeContext !== this.contextKey) {
        this.requestGeneration++
        this.treeContext = this.contextKey
        this.treeRows = []
      }
      // 只在业务数据变化时重建结构，复用已有节点以保留展开状态和进行中的请求。
      const previous = indexRows(this.treeRows)
      this.treeRows = this.routes.map((process, index) => {
        const materials = (process.materials ?? []).filter(item => !item.insteadSourceId)
        const processId = `process-${process.uiKey || process.id || index}`
        return this.createNode({
          id: processId,
          name: `${process.code ?? ''} ${process.name ?? ''}`.trim() || '未命名工序',
          code: process.code ?? '',
          nodeType: CraftTreeNodeType.Process,
          typeText: '',
          processCode: process.code,
          processKey: process.uiKey,
          children: [
            this.createMaterialGroup(`${processId}-input`, '领用主料', CraftTreeNodeType.Input, process.code,
              materials.filter(item => Number(item.ioType) !== 2)),
            ...((process.isInbound || process.isOutsource) ? [this.createMaterialGroup(`${processId}-output`, '入库物料', CraftTreeNodeType.Output, process.code,
              materials.filter(item => Number(item.ioType) === 2))] : [])
          ]
        }, processId, previous)
      })
    },
    createMaterialGroup(id: string, name: string, nodeType: CraftTreeNodeType, processCode: string, materials: MaterialSource[]): CraftTreeSource {
      return {
        id, name, code: '', nodeType, typeText: '', processCode,
        children: materials.map((material, index) => ({
          // 新增行的 id 为 0，同一物料可出现多次，不能只使用 dataId。
          id: `material-${material.id || `${material.dataType}-${material.dataId}-${index}`}`,
          name: (material.materialName ?? material.name ?? '未命名物料') + '【' + (material.materialNo ?? material.code ?? '') + '】',
          code: material.materialNo ?? material.code ?? '',
          nodeType: getMaterialNodeType(material.materialType),
          typeText: material.materialType || '物料',
          quantityText: material.quantity ?? '',
          processCode,
          refCraftOrderVersionId: material.refCraftOrderVersionId,
          rootMaterial: material,
          dataId: material.dataId,
          dataType: material.dataType,
          materialType: material.materialType,
          children: (material.substitutes ?? []).map((substitute: MaterialSource, substituteIndex) => ({
            id: `sub-${substitute.id || `${substitute.dataId}-${substituteIndex}`}`,
            name: (substitute.materialName ?? substitute.name ?? '未命名替料') + '  [替]',
            code: substitute.materialNo ?? substitute.code ?? '',
            nodeType: CraftTreeNodeType.Substitute,
            typeText: '替料',
            quantityText: substitute.quantity ?? '',
            processCode
          }))
        }))
      }
    },
    createNode(source: CraftTreeSource, id: string, previous = new Map<string, CraftTreeRow>()): CraftTreeRow {
      const old = previous.get(id)
      const materialType = source.materialType ?? source.typeText ?? '物料'
      // 相同物料行更换产品后，不能沿用旧产品的缓存或请求。
      const reusable = !!old && old.dataId === source.dataId
        && old.dataType === source.dataType && old.materialType === materialType
      const wasLazy = !!old && old.loadState !== undefined
      const row: CraftTreeRow = reusable ? old : {
        id, children: [], fixedChildren: [], lazyChildren: []
      }
      const previousState = reusable ? row.state : undefined
      const { children, ...fields } = source
      Object.assign(row, fields, {
        id, materialType,
        nodeType: source.nodeType ?? getMaterialNodeType(materialType)
      })
      row.fixedChildren = (children ?? []).map((child, index) =>
        this.createNode(child, `${id}/${encodeURIComponent(String(child.id ?? index))}`, previous))
      const lazy = canLoadChildren(row)
      // 引用关系变化会改变节点能否展开，此时重置展开状态而不是沿用旧状态。
      const state = reusable && wasLazy === lazy ? previousState : undefined
      row.state = lazy ? (state ?? 'closed') : (state ?? 'open')
      if (!lazy) {
        row.loadState = undefined
        row.lazyChildren = []
      } else if (!row.loadState) {
        row.loadState = 'idle'
        row.lazyChildren = [this.createMessageRow(id, '展开加载工艺单')]
      }
      row.children = [...row.fixedChildren, ...row.lazyChildren]
      row.iconCls = this.getNodeIcon(row)
      return row
    },
    createMessageRow(
      parentId: string,
      name: string,
      messageKind: MessageKind = 'idle'
    ): CraftTreeRow {
      return {
        id: `${parentId}/status`, name, code: '', typeText: '', state: 'open', children: [], fixedChildren: [], lazyChildren: [],
        isPlaceholder: true, messageKind, iconCls: 'tree-custom-icon-hidden'
      }
    },
    setLazyChildren(row: CraftTreeRow, children: CraftTreeRow[]) {
      row.lazyChildren = children
      // 始终提供非空的结果/提示节点，让 TreeGrid 退出内部 loading 状态。
      row.children = [...row.fixedChildren, ...children]
      row.iconCls = this.getNodeIcon(row)
    },
    setReferencedNodeTypes(source: CraftTreeSource): CraftTreeSource {
      if (source.dataId != null) {
        return {
          ...source,
          nodeType: getMaterialNodeType(source.materialType ?? source.typeText),
          children: (source.children ?? []).map(child => this.setReferencedNodeTypes(child))
        }
      }
      return {
        ...source,
        nodeType: CraftTreeNodeType.Process,
        children: (source.children ?? []).map((group, groupIndex) => ({
          ...group,
          nodeType: groupIndex === 0 ? CraftTreeNodeType.Input : CraftTreeNodeType.Output,
          children: (group.children ?? []).map(material => ({
            ...material,
            nodeType: getMaterialNodeType(material.materialType ?? material.typeText)
          }))
        }))
      }
    },
    async onNodeExpand(row: CraftTreeRow) {
      if (!row || row.isPlaceholder || !canLoadChildren(row)) return
      row.state = 'open'
      if (row.loadState === 'loaded' || row.loadState === 'loading') return

      const generation = this.requestGeneration
      const context = this.contextKey
      const isCurrent = () => generation === this.requestGeneration && context === this.contextKey
        && indexRows(this.treeRows).get(row.id) === row
      row.loadState = 'loading'
      this.setLazyChildren(row, [this.createMessageRow(row.id, '加载中...', 'loading')])
      try {
        const result = await this.$http.get(
          '/api/technical/CraftOrderManage/GetCraftTreeByProductId',
          { dataType: row.dataType, dataId: row.dataId }
        )
        if (!isCurrent()) return
        // 本项目的 $http 返回 data；同时兼容直接返回数组和空响应。
        const data = Array.isArray(result) ? result : result?.data
        if (data != null && !Array.isArray(data)) {
          throw new Error('工艺树数据格式异常，请重试')
        }
        const children: CraftTreeSource[] = data ?? []
        this.setLazyChildren(row, children.length
          ? children.map((child, index) => this.createNode(this.setReferencedNodeTypes(child),
            `${row.id}/ref/${encodeURIComponent(String(child.id ?? index))}`))
          : [this.createMessageRow(row.id, '暂无工艺单', 'empty')])
        row.loadState = 'loaded'
        row.iconCls = this.getNodeIcon(row)
      } catch (error) {
        if (!isCurrent()) return
        row.loadState = 'error'
        this.setLazyChildren(row, [this.createMessageRow(row.id, '加载失败，请收起后重新展开', 'error')])
        this.$kingzon_messager?.showError(error)
      }
    },
    onNodeCollapse(row: CraftTreeRow) {
      if (row) row.state = 'closed'
    },
    selectRow(row: CraftTreeRow) {
      if (!row || row.isPlaceholder) return
      // 只响应当前版本的顶层工序、分组和主物料，不定位引用工艺或替料。
      for (const process of this.treeRows) {
        console.log('Checking process:', process)
        if (row === process || process.fixedChildren.includes(row)) {
          this.$emit('selectProcess', { key: process.processKey, code: process.processCode, tab: row === process ? 0 : 1 })
          return
        }
        for (const group of process.fixedChildren) {
          if (group.fixedChildren.includes(row) && row.rootMaterial) {
            this.$emit('selectProcess', { key: process.processKey, code: process.processCode, tab: 1, material: row.rootMaterial })
            return
          }
        }
      }
    }
  }
})
</script>

<style scoped>
.panel {
  position: relative;
  min-width: 0;
  overflow: hidden;
  background: #fff;
  border: 1px solid var(--color-grey-bg);
  border-radius: 0;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  height: 34px;
  padding: 0 14px;
  background-color: var(--color-grey-bg);
  border-bottom: 1px solid var(--color-grey-bg);
  font-size: 13px;
  font-weight: 700;
  flex-shrink: 0;
  box-sizing: border-box;
}

.tree-panel {
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.muted,
.panel-foot {
  color: #777;
  font-size: 11px;
}

.tree-grid {
  flex: 1 1 0;
  min-width: 0;
  min-height: 0;
  border: 0 !important;
}

.tree-grid :deep(.datagrid-view2) {
  min-width: 0;
}

.tree-grid :deep(.datagrid-body) {
  min-height: 0;
  overflow: auto;
}

.node-message {
  color: #777;
}

.panel-foot {
  box-sizing: border-box;
  flex-shrink: 0;
  height: 24px;
  padding: 5px 13px;
  border-top: 1px solid var(--color-grey-bg);
}

.tree-grid :deep(.tree-custom-icon-hidden) {
  background: none !important;
  background-image: none !important;
  width: 0 !important;
  height: 0 !important;
  margin: 0 !important;
}

.tree-node-content {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  min-width: 0;
}

.tree-node-fa-icon {
  width: 14px;
  height: 14px;
  flex-shrink: 0;
}

.tree-node-content>span:last-child {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
