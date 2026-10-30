<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page-content">
        <Info v-if="inited" ref="info" :showTechCodeSearch="false" v-model:formula="formulaInfo"></Info>
        <div class="kingzon-page-fill" style="display:flex;flex-direction: row ;">
            <div style="flex: 1;">
                <Tabs v-if="inited" :plain="true" style="height:100%" ref="tabs" @tabSelect="tabSelect">
                    <TabPanel title="配方" data-theme="Formula">
                        <Version ref="version" :showVersionList="false" v-model:versions="formulaVersionList"></Version>
                    </TabPanel>
                    <TabPanel title="检验" data-theme="Inspection">
                        <Inspection ref="inspection"></Inspection>
                    </TabPanel>
                    <TabPanel title="文件">
                        <div>
                            <KingZonFileButton @onFileSelect="onFileSelect"
                                style="width: 80px;height: 26px;margin: 5px;">文件上传
                            </KingZonFileButton>
                            <FileUploadList v-model:uploadFileList="uploadFileData" style="width: 100%;">
                            </FileUploadList>
                        </div>
                    </TabPanel>
                </Tabs>
            </div>
            <Search v-show="selectedTab == 'Formula'" style=" width: 550px " :theme="'Formula'"
                @chooseData="chooseSearchData"></Search>
            <Search v-show="selectedTab == 'Inspection'" style="width: 400px" :theme="'Inspection'"
                @chooseData="chooseSearchData"></Search>
        </div>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="$emit('close')">取消</LinkButton>
            <LinkButton btnCls="c8" @click="confirm">确定</LinkButton>
        </div>
    </div>
    <!-- 文件上传 -->
    <Dialog ref="fileUpload" :title="'配方文件上传'" :dialogStyle="{ width: '600px', maxHeight: '600px' }" :draggable="true"
        :resizable="true" :modal="true" :closed="true">
        <FileUpload @close="fileDialogClose" :uploadFileList="uploadFileList" :upload="false"
            @submit="uploadFileSubmit">
        </FileUpload>
    </Dialog>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import Info from './component/formula-info.vue'
import Version from './component/formula-version.vue'
import Inspection from './component/formula-inspection.vue'
import Search from './component/formula-search.vue'
import type { UploadFile, uploadFilesType } from '@/types/types';
import { FileStorageSourceType, FileBase } from '@/types/enums';
import { FormulaDto, FormulaVersionDataDto, FormulaVersionDto, FormulaDetailDto } from '@/types/technical';

export default defineComponent({
    name: 'CreateFormula',
    emits: ["close", "submit"],
    components: { Info, Version, Inspection, Search },
    props: {
        productId: {
            type: Number,
            required: false
        },
        quantity: {
            type: Number,
            required: false
        },
    },
    data() {
        return {
            isLoading: false,
            selectedTab: "Formula" as "Formula" | "Inspection" | undefined,
            uploadFileList: [] as UploadFile[],
            uploadFileData: [] as UploadFile[],  //上传文件数据
            uploadFileDialog: {} as EasyUI.Dialog,
            formulaInfo: undefined as FormulaDto | undefined,
            formulaVersionList: [] as FormulaVersionDataDto[],
            inited: false,
        }
    },
    async created() {
        await this.init();
    },
    mounted() {
        this.uploadFileDialog = this.$refs.fileUpload as EasyUI.Dialog;
    },
    methods: {
        async init() {
            this.formulaVersionList.push(new FormulaVersionDataDto({
                version: new FormulaVersionDto({
                    totalQuantity: this.quantity,
                    isMain: true
                }),
                detailList: [
                    new FormulaDetailDto({
                        baseQuantity: this.quantity,
                        curBaseQuantity: this.quantity,
                    })
                ]
            }));
            //获取产品信息
            this.isLoading = true;
            await this.$http.get('api/base/Product/GetProductById', { productId: this.productId })
                .then(res => {
                    var data = res.data as any;
                    this.formulaInfo = new FormulaDto({
                        productId: data.id,
                        productName: data.productName,
                        productCategory: data.productCategory,
                        productRemark: data.remark,
                        productTechnicalCode: data.technicalCode,
                    })
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                })
                .finally(() => {
                    this.isLoading = false;
                    this.inited = true;
                })
        },
        confirm() {
            //三个表单验证通过
            var info = this.$refs.info as InstanceType<typeof Info>;
            if (!info.getValid()) {
                this.$kingzon_messager.showConfim("请完善‘配方编码和名称’");
                return;
            }
            var version = this.$refs.version as InstanceType<typeof Version>;
            if (!version.getValid()) {
                this.$kingzon_messager.showConfim("请完善‘配方版本号和配方总量’");
                return;
            }

            var versionList = this.formulaVersionList;
            for (let i = 0; i < versionList.length; i++) {
                var curVersion = versionList[i];
                //版本号不能为空
                if (!curVersion.version.version) {
                    this.$kingzon_messager.showConfim("版本号不能为空");
                    return;
                }

                //配方总量不能为0
                if (!curVersion.version.totalQuantity) {
                    this.$kingzon_messager.showConfim("配方总量不能为0");
                    return;
                }
                curVersion.version.status = Number(curVersion.version.status);
                curVersion.detailList = curVersion.detailList.filter(x => x.detailId);
                curVersion.detailList = this.cloneTree(curVersion.detailList);
            }
            //必需有一个主版本
            var mainVersion = versionList.filter(item => item.version.isMain);
            if (mainVersion.length == 0) {
                this.$kingzon_messager.showConfim("请设置为主版本");
                return;
            }

            var inspection = this.$refs.inspection as InstanceType<typeof Inspection>;
            if (!inspection.getValid()) {
                this.$kingzon_messager.showConfim("请完善‘检验方案名称’");
                return;
            }
            var inspectionList = inspection.getInspectionData();
            for (let i = 0; i < inspectionList.length; i++) {
                var curInspection = inspectionList[i];
                //方案名称不能为空
                if (!curInspection.inspection.name) {
                    this.$kingzon_messager.showConfim("第" + (i + 1) + "检验方案的名称不能为空");
                    return;
                }
            }
            //必需有一个默认方案
            var defaultInspection = inspectionList.filter(item => item.inspection.isDefault);
            if (defaultInspection.length == 0) {
                this.$kingzon_messager.showConfim("请至少设置一个默认检验方案");
                return;
            } else if (defaultInspection.length > 1) {
                this.$kingzon_messager.showConfim("默认检验方案只能有一个");
                return;
            }

            //提交后端
            this.isLoading = true;
            this.postData(info.getInfoData(), versionList, inspectionList)
                .then(res => {
                    this.$kingzon_messager.showInfo("配方创建成功");
                    var data = res.data as any
                    var formulaId = data.formulaId as number;
                    var versionId = data.versionId as number;

                    var fileData = [] as uploadFilesType[];
                    if (this.uploadFileData.length > 0 && formulaId) {
                        fileData.push({
                            sourceId: formulaId,
                            files: this.uploadFileData,
                            sourceType: FileStorageSourceType.配方文件,
                            fileBase: FileBase.Formula
                        } as uploadFilesType)
                    }
                    if (fileData.length > 0) {
                        //上传多组文件
                        this.$utils.uploadFiles(fileData,
                            () => {
                                this.isLoading = false; //关闭loading
                                this.$kingzon_messager.showInfo("文件上传成功");
                                this.$emit('submit', {
                                    formulaId: formulaId,
                                    versionId: versionId
                                });
                            })
                    } else {
                        //不上传文件
                        this.$emit('submit', {
                            formulaId: formulaId,
                            versionId: versionId
                        });
                    }
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                    this.isLoading = false;
                })
                .finally(() => {
                    if (this.uploadFileData.length <= 0) { //不上传文件
                        this.isLoading = false;
                    }
                })
        },
        tabSelect($event) {
            this.selectedTab = $event.$el.dataset.theme;
        },
        chooseSearchData(row) {
            if (this.selectedTab == 'Formula') {
                var component = this.$refs.version as InstanceType<typeof Version>;
                component.setVersionData(row);
            } else if (this.selectedTab == 'Inspection') {
                var component2 = this.$refs.inspection as InstanceType<typeof Inspection>;
                component2.setInspectionData(row);
            }
        },
        onFileSelect(files) {
            this.uploadFileList = files;
            this.uploadFileDialog.open();
        },
        fileDialogClose() {
            this.uploadFileDialog.close();
        },
        uploadFileSubmit(files) {
            //此时文件未上传后端 新增产品时再添加文件
            this.uploadFileData = files;
            this.uploadFileDialog.close();
        },
        postData(info, versionList, inspectionList) {
            return this.$http.post("api/technical/FormulaManage/CreateFormula", {
                //配方信息
                info: info,
                //版本信息
                versionList: versionList,
                //检验方案信息
                inspectionList: inspectionList,
                returnVersionId: true
            });
        },
        cloneTree(list) {
            return list.map(item => ({
                ...item,
                parent: null,
                children: item.children
                    ? this.cloneTree(item.children)
                    : []
            }));
        },
    }
})
</script>