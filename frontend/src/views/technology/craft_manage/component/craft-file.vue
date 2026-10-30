<template>
    <div class="kingzon-page-content" style="min-height: 0; padding: 8px; box-sizing: border-box; gap: 12px; overflow-y: auto">
        <div v-if="!sourceId" class="kingzon-text-grey">请先保存版本，生成工序记录后再上传文件。</div>
        <div v-for="group in fileGroups" :key="group.sourceType"
            style="display: flex; flex-direction: column; min-height: 0"
            :style="{ flex: sourceId ? '1 0 240px' : '0 0 auto' }">
            <div style="display: flex; align-items: center; justify-content: space-between; padding-bottom: 8px">
                <strong>{{ group.title }}</strong>
                <KingZonFileButton v-if="sourceId && !readonly" :multiple="true"
                    @onFileSelect="onFileSelect($event, group.sourceType)">
                    文件上传
                </KingZonFileButton>
            </div>
            <div v-if="sourceId" :key="`${sourceId}-${group.sourceType}-${group.listKey}`"
                style="flex: 1; min-height: 0; overflow: hidden">
                <FileList :sourceType="group.sourceType" :sourceId="sourceId" :fileBase="group.fileBase"
                    :showSearchPanel="false" :showUpload="false" :showFieldFilter="false" :showClose="false"
                    :showPage="false" />
            </div>
        </div>

        <Dialog ref="fileUploadDialog" :title="`${uploadGroup.title}上传`"
            :dialogStyle="{ width: '600px', maxHeight: '600px' }" :draggable="true" :resizable="true"
            :modal="true" :closed="true" @close="clearUpload">
            <FileUpload v-if="uploadFileList.length && uploadSourceId" :key="`${uploadSourceId}-${uploadSourceType}`"
                :uploadFileList="uploadFileList" :sourcId="uploadSourceId" :sourceType="uploadGroup.sourceType"
                :fileBase="uploadGroup.fileBase" @close="closeUpload" @submit="uploadComplete" />
        </Dialog>
    </div>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import { FileBase, FileStorageSourceType } from '@/types/enums'
import type { UploadFile } from '@/types/types'
import type { CraftProcessDto, DialogRef } from '@/types/craft'

export default defineComponent({
    name: 'CraftFile',
    props: {
        process: { type: Object as PropType<CraftProcessDto>, required: true },
        readonly: Boolean
    },
    data() {
        return {
            fileGroups: [
                { title: '工序作业指导书', sourceType: FileStorageSourceType.工序作业指导书,
                    fileBase: FileBase.CraftInstruction, listKey: 0 },
                { title: '其他工序文件', sourceType: FileStorageSourceType.工序文件,
                    fileBase: FileBase.Craft, listKey: 0 }
            ],
            uploadFileList: [] as UploadFile[],
            uploadSourceId: 0,
            uploadSourceType: FileStorageSourceType.工序文件
        }
    },
    computed: {
        sourceId(): number { return Number(this.process.id) || 0 },
        uploadGroup() {
            return this.fileGroups.find(group => group.sourceType === this.uploadSourceType) ?? this.fileGroups[1]
        }
    },
    watch: {
        sourceId() {
            this.closeUpload()
            this.fileGroups.forEach(group => group.listKey++)
        },
        readonly(value: boolean) {
            if (value) this.closeUpload()
        }
    },
    methods: {
        async onFileSelect(files: UploadFile[], sourceType = FileStorageSourceType.工序文件) {
            if (this.readonly || !this.sourceId || !files?.length) return
            if (!this.fileGroups.some(group => group.sourceType === sourceType)) return
            this.uploadSourceType = sourceType
            this.uploadSourceId = this.sourceId
            this.uploadFileList = files
            await this.$nextTick()
            if (this.readonly || this.uploadSourceId !== this.sourceId || !this.uploadFileList.length) return
            const dialog = this.$refs.fileUploadDialog as DialogRef | undefined
            dialog?.open()
        },
        clearUpload() {
            this.uploadFileList = []
            this.uploadSourceId = 0
        },
        closeUpload() {
            const dialog = this.$refs.fileUploadDialog as DialogRef | undefined
            dialog?.close()
            this.clearUpload()
        },
        uploadComplete() {
            if (!this.uploadFileList.length || this.uploadSourceId !== this.sourceId) return
            const group = this.uploadGroup
            this.closeUpload()
            group.listKey++
            this.$kingzon_messager.showInfo(`${group.title}上传成功`)
        }
    }
})
</script>
