<template>
    <!-- 成品外包装 -->
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page-content">
        <KingZonShowForm :formData="showFormData" :columnWidth="'25%'"></KingZonShowForm>
        <div class="kingzon-page-fill package-page">
            <div v-for="(pack, index) in packageList" :key="pack.id ?? pack.index ?? index" class="package-card">
                <div class="package-card-header">
                    <div class="package-card-title">
                        <span>包装方案 {{ index + 1 }}</span>
                        <span v-if="pack.packageSpecification" class="package-card-subtitle">{{
                            pack.packageSpecification }}</span>
                    </div>

                    <div class="package-card-actions">
                        <template v-if="pack.id">
                            <LinkButton plain @click="downLoad(pack.id)">下载</LinkButton>
                            <FilePreview>
                                <template v-slot="preview">
                                    <LinkButton plain @click="preViewPdf(preview, pack.id)">预览</LinkButton>
                                </template>
                            </FilePreview>
                            <LinkButton plain @click="print(pack.id)">打印</LinkButton>
                        </template>
                        <LinkButton plain class="package-delete-btn" @click="deletePackage(pack, index)">删除</LinkButton>
                    </div>
                </div>

                <div class="package-form-grid">
                    <div class="package-field package-field-2">
                        <span class="package-label required-label">包装方案：</span>
                        <TextBox v-model="pack.packageSpecification" class="package-control"
                            :style="{ borderColor: pack.packageSpecification ? '' : '#ffa8a8' }" placeholder="请输入包装方案">
                        </TextBox>
                    </div>

                    <div class="package-field package-field-2">
                        <span class="package-label">客户：</span>
                        <TagBox v-model="pack.customerIds" class="package-control" valueField="value" textField="text"
                            placeholder="选择客户" :limitToList="true" :hasDownArrow="true" :data="customerList">
                        </TagBox>
                    </div>

                    <div v-if="specList.length" class="package-field package-field-2">
                        <span class="package-label">选择规格：</span>
                        <ComboBox v-model="pack.specificationId" class="package-control" :data="specList"
                            textField="showText" valueField="specificationId">
                        </ComboBox>
                    </div>

                    <div
                        :class="!specList.length ? 'package-field package-field-full' : 'package-field package-field-2'">
                        <span class="package-label">包装模板：</span>
                        <ComboBox v-model="pack.packTempLateId" class="package-control" :data="tempLateList"
                            textField="dataValue" valueField="id" @valueChange="handlePackPara(pack, customerParaList)">
                        </ComboBox>
                    </div>

                    <div class="package-field package-field-2">
                        <span class="package-label">包装单位：</span>
                        <div class="package-unit-pair">
                            <div class="measure-cell">
                                <ComboBox v-model="pack.packageUnitId" class="measure-control" :data="unitList"
                                    textField="name" valueField="id" @valueChange="unitChange(pack)">
                                </ComboBox>
                            </div>
                            <div class="measure-cell">
                                <span class="measure-label">主单位换算：</span>
                                <span class="measure-equation">1 {{ pack.packageUnit || '包装单位' }} =</span>
                                <NumberBox v-model="pack.mainUnitRatio" class="measure-input" :min="0" :precision="4"
                                    :style="{ borderColor: (pack.packageUnitId && !pack.mainUnitRatio) ? '#ffa8a8' : '' }">
                                </NumberBox>
                                <span class="measure-unit">{{ product.productUnit }}</span>
                            </div>
                        </div>
                    </div>
                    <div class="package-field package-field-2">
                        <span class="package-label">包装重量：</span>
                        <div class="package-weight-triple">
                            <div class="measure-cell">
                                <NumberBox v-model="pack.packageWegiht" class="measure-control" :min="0" :precision="4">
                                </NumberBox>
                                <span class="measure-unit">kg</span>
                            </div>
                            <div class="measure-cell">
                                <span class="measure-label">托盘换算：</span>
                                <NumberBox v-model="pack.palletPackageRadio" class="measure-control" :min="0"
                                    :precision="4"
                                    :style="{ borderColor: (pack.packageUnitId && !pack.palletPackageRadio) ? '#ffa8a8' : '' }">
                                </NumberBox>
                                <span class="measure-unit">盘</span>
                            </div>
                            <div class="measure-cell">
                                <span class="measure-label">托盘单重：</span>
                                <NumberBox v-model="pack.palletWegiht" class="measure-control" :min="0" :precision="4">
                                </NumberBox>
                                <span class="measure-unit">kg</span>
                            </div>
                        </div>
                    </div>
                </div>

                <div v-if="(pack.packTempLateId && tempLateList.length) || !tempLateList.length"
                    class="package-para-grid">
                    <div v-for="para in getOrderedParaList(pack.customerParaList)"
                        :key="(pack.id ?? pack.index).toString() + para.systemPara.id"
                        class="package-field package-para-item" :class="{
                            'package-field-2':
                                para.systemPara.displayType == DisplayType.Textarea ||
                                para.systemPara.displayType == DisplayType.SelectFile
                        }">

                        <span class="package-label">{{ para.systemPara.dataValue }}：</span>

                        <MutiDisplayTypeInput class="package-control" :para="para"
                            @update:value="para.systemPara.value = $event">
                        </MutiDisplayTypeInput>
                    </div>
                </div>

                <div class="package-form-grid package-remark-row">
                    <div class="package-field package-field-2">
                        <span class="package-label">备注：</span>
                        <TextBox v-model="pack.remark" class="package-control"></TextBox>
                    </div>
                </div>

                <div class="package-file-section">
                    <div class="package-file-header">
                        <span class="package-section-title">附件</span>
                        <KingZonFileButton @onFileSelect="onFileSelect($event, pack)" style="width:80px;height:26px;">
                            文件上传
                        </KingZonFileButton>
                    </div>

                    <FileUploadList v-if="pack.uploadFileData?.length" v-model:uploadFileList="pack.uploadFileData"
                        class="package-upload-list">
                    </FileUploadList>

                    <div v-if="pack.fileList?.length != 0" class="package-file-list">
                        <FileList ref="fileList" :sourceType="FileStorageSourceType.成品包装文件" :sourceId="pack.id"
                            :fileBase="FileBase.ProductPack" :showSearchPanel="false" :showUpload="false"
                            :showFieldFilter="false" :showClose="false" :showPage="false"
                            @changeFileList="handleFileList($event, pack)">
                        </FileList>
                    </div>
                </div>

                <div class="package-file-section package-tag-file-section">
                    <div class="package-file-header">
                        <span class="package-section-title">标签模板文件（仅支持 .xlsx）</span>
                        <KingZonFileButton accept="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                            @onFileSelect="onTagFileSelect($event, pack)" style="width:80px;height:26px;">
                            模板上传
                        </KingZonFileButton>
                    </div>

                    <FileUploadList v-if="pack.tagUploadFileData?.length"
                        v-model:uploadFileList="pack.tagUploadFileData" class="package-upload-list">
                    </FileUploadList>

                    <div v-if="pack.id" class="package-file-list">
                        <FileList :sourceType="FileStorageSourceType.成品标签文件" :sourceId="pack.id"
                            :fileBase="FileBase.ProductTag" :showSearchPanel="false" :showUpload="false"
                            :showFieldFilter="false" :showClose="false" :showPage="false">
                        </FileList>
                    </div>
                </div>
            </div>

            <div class="package-add">
                <LinkButton plain class="package-add-btn" @click="add()">＋ 添加包装方案</LinkButton>
            </div>
        </div>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="$emit('close')">取消</LinkButton>
            <LinkButton btnCls="c8" @click="confirm">确定</LinkButton>
        </div>
    </div>
    <Dialog ref="fileUpload" :title="uploadDialogTitle" :dialogStyle="{ width: '600px', maxHeight: '600px' }"
        :draggable="true" :resizable="true" :modal="true" :closed="true">
        <FileUpload @close="fileDialogClose" :uploadFileList="uploadFileList" :upload="false"
            @submit="uploadFileSubmit">
        </FileUpload>
    </Dialog>
</template>
<script lang="ts">
import { AddPackageDto, FileStorage, ProductIndexTreeDto, SystemParaTreeDto } from '@/types/common';
import { defineComponent } from 'vue';
import { FileStorageSourceType, FileBase, UnitType, SystemParaType, DisplayType, FileStorageContentType } from '@/types/enums';
import { ShowFormData, type UploadFile, type uploadFilesType, CustomAttribute, type SelectOption } from '@/types/types';
import { cloneDeep } from 'lodash';
import FilePreview from '@/components/file/file-preview.vue';

type PackageEditDto = AddPackageDto & {
    tagUploadFileData?: UploadFile[];
};

const WeightParaType = 'weight'

export default defineComponent({
    name: 'Package',
    emits: ['close', 'submit'],
    data() {
        return {
            isLoading: false,
            packageList: [] as PackageEditDto[],
            unitList: [] as any[],
            uploadFileList: [] as UploadFile[],
            uploadFileData: [] as UploadFile[],  //上传文件数据
            uploadFileDialog: {} as EasyUI.Dialog,
            selectedPack: {} as PackageEditDto,
            uploadFileType: 'attachment' as 'attachment' | 'tag',
            showFormData: [] as ShowFormData[],
            specList: [] as ProductIndexTreeDto[],
            customerParaList: [] as SystemParaTreeDto[],//包装扩展参数
            customerList: [] as SelectOption[],
            maxIndex: 1,
            tempLateList: [] as any[], //包装模板列表
            defaultWeight: 0
        }
    },
    props: {
        product: {
            type: Object as () => ProductIndexTreeDto,
            required: true,
        },
    },
    computed: {
        FileStorageSourceType() {
            return FileStorageSourceType;
        },
        FileBase() {
            return FileBase
        },
        DisplayType() {
            return DisplayType
        },
        uploadDialogTitle() {
            return this.uploadFileType == 'tag' ? '标签文件上传（仅支持 .xlsx）' : '产品文件上传'
        }
    },
    created() {
        this.init();
        this.specList = cloneDeep(this.product.children);
        this.specList.forEach(item => {
            item.showText = (item.productNo ?? '') + '-' + item.productName + '【' + item.productSpecification + '】'
        })
    },
    mounted() {
        this.uploadFileDialog = this.$refs.fileUpload as EasyUI.Dialog;
    },
    methods: {
        init() {
            var head = "成品";
            if (this.product.specificationId) {
                head = "规格"
            }
            this.showFormData = [
                new ShowFormData(head + '编码', this.product.productNo as string),
                new ShowFormData(head + '名称', this.product.productName as string),
                new ShowFormData(head + '规格', this.product.productSpecification as string),
                new ShowFormData(head + '单位', this.product.productUnit as string),
                new ShowFormData('技术型号', this.product.productTechnicalCode as string),
                new ShowFormData('产品型号', this.product.productCategory as string),
                new ShowFormData('备注', this.product.remark as string, undefined, undefined, undefined, undefined, "50%")
            ];
            this.isLoading = true;
            //获取产品单位
            this.getProductUnitList()
                .then((res) => {
                    this.unitList = res.data as [];
                    return this.getCustomerParamData()
                }).then((res) => {
                    this.handlePara(res.data as SystemParaTreeDto[]);
                    // this.customerParaList = res.data as SystemParaTreeDto[];
                    return this.getData();
                }).then((res: any) => {
                    this.packageList = res.data;
                    if (!this.packageList.length) {
                        this.add();
                    }
                    this.packageList.forEach(pack => {
                        pack.palletWegiht ??= this.defaultWeight
                        this.handlePackPara(pack, this.customerParaList);
                    })
                    return this.getCustomerList()
                })
                .then((res: any) => {
                    this.customerList = res.data.map(x => {
                        return {
                            text: x.customerName,
                            value: x.id
                        }
                    })
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                })
                .finally(() => {
                    this.isLoading = false;
                })
        },
        getData() {
            return this.$http.get('/api/base/Product/GetProductPackageList',
                {
                    productId: this.product.productId,
                    specId: this.product.specificationId,
                })
        },
        getCustomerList() {
            return this.$http.post('/api/base/Customer/GetCustomer', []);
        },
        getProductUnitList() {
            return this.$http.get('/api/base/Unit/GetUnitByType', { unitType: UnitType.Package })
        },
        getCustomerParamData() {
            return this.$http.get('/api/base/SystemPara/GetAdditionalParam', { type: SystemParaType.包装附加参数 });
        },
        preViewPdf(perview: any, id: number) {
            this.isLoading = true
            this.$http
                .getFile('/api/base/Product/PreviewPackagePdf', {
                    packegeId: id
                }).then((res) => {
                    this.isLoading = false
                    var file = new FileStorage()
                    file.tmpUrl = res.url
                    file.contentType = FileStorageContentType.pdf
                    perview.preview(file)
                }).finally(() => {
                    this.isLoading = false
                })
        },
        downLoad(id: number) {
            this.isLoading = true
            this.$http
                .getFile('/api/base/Product/DownloadPackageExcel', {
                    packegeId: id
                })
                .then((res) => {
                    if (res.url) {
                        const a = document.createElement('a')
                        // 获取文件名fileName
                        a.download = res.name
                        a.href = res.url
                        document.body.appendChild(a)
                        a.click()
                        document.body.removeChild(a)
                    } else {
                        this.$kingzon_messager.showError('文件无法下载')
                    }
                }).finally(() => {
                    this.isLoading = false
                })
        },
        print(id: number) {
            this.isLoading = true;
            this.$http.getFile('/api/base/Product/PrintPackegeById', {
                packegeId: id
            })
                .then(async (res: any) => {
                    this.$utils.printAsposeHtml(res.content);
                    this.isLoading = false;
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                    this.isLoading = false;
                })
        },
        handlePara(systemPara: SystemParaTreeDto[]) {
            //判断取得的包装附加参数是否只有一级结构
            var hasTwoLevel = systemPara.some(x => x.childrenSystemPara && x.childrenSystemPara.length > 0);
            if (!hasTwoLevel) {
                this.customerParaList = systemPara;
            } else {
                var weight = systemPara.find(x => x.systemPara.code == WeightParaType)
                if (weight != null) {
                    this.defaultWeight = Number(weight.systemPara.dataValue)
                }
                this.tempLateList = systemPara.filter(x => x.systemPara.code != WeightParaType).map(item => {
                    return {
                        ...item.systemPara,
                        childrenSystemPara: item.childrenSystemPara
                    } as any;
                });
            }
        },
        handlePackPara(pack: AddPackageDto, customerParaList: SystemParaTreeDto[]) {
            //选中模板时根据模板带出参数值 
            if (pack.packTempLateId && this.tempLateList.length) {
                var tempLate = this.tempLateList.find(x => x.id == pack.packTempLateId);
                if (tempLate) {
                    var data = cloneDeep(tempLate.childrenSystemPara ?? []);
                    pack.customerParaList = data.map(x => {
                        var item = pack.customAttributeList.find(e => e.systemParaId == x.systemPara.id);
                        x.systemPara.value = (item?.value ?? undefined) as string | undefined;
                        if (x.childrenSystemPara && x.childrenSystemPara.length) {
                            x.childrenSystemPara = x.childrenSystemPara.map(e => {
                                var subItem = pack.customAttributeList.find(a => a.systemParaId == e.systemPara.id);
                                e.systemPara.value = (subItem?.value ?? undefined) as string | undefined;
                                return e;
                            })
                        }
                        return x;
                    })
                    this.handleFileList(pack.fileList, pack);
                }
                return;
            }
            //取消选中 删除所有参数
            if (!pack.packTempLateId && this.tempLateList.length) {
                pack.customerParaList = [];
                return;
            }
            //没有模板时
            if (!this.tempLateList.length) {
                var data2 = cloneDeep(customerParaList);
                pack.customerParaList = data2.map(x => {
                    var item = pack.customAttributeList.find(e => e.systemParaId == x.systemPara.id);
                    x.systemPara.value = (item?.value ?? undefined) as string | undefined;
                    if (x.childrenSystemPara && x.childrenSystemPara.length) {
                        x.childrenSystemPara = x.childrenSystemPara.map(e => {
                            var subItem = pack.customAttributeList.find(a => a.systemParaId == e.systemPara.id);
                            e.systemPara.value = (subItem?.value ?? undefined) as string | undefined;
                            return e;
                        })
                    }
                    return x;
                })
                this.handleFileList(pack.fileList, pack);
            }
        },
        handleFileList(fileList, pack) {
            //为文件下拉框添加选项
            pack.customerParaList?.forEach(item => {
                if (item.systemPara.displayType == DisplayType.SelectFile) {
                    item.fileSelections = fileList.map(x => {
                        return {
                            id: x.id,
                            text: x.fileName,
                            value: x.id
                        }
                    });
                }
            })
        },
        getOrderedParaList(paraList: SystemParaTreeDto[] | undefined) {
            const list = paraList ?? [];
            return [
                ...list.filter(item => item.systemPara.displayType != DisplayType.SelectFile),
                ...list.filter(item => item.systemPara.displayType == DisplayType.SelectFile),
            ];
        },
        unitChange(row: AddPackageDto) {
            row.packageUnit = this.unitList.find(x => x.id == row.packageUnitId)?.name;
        },
        add() {
            var data = new AddPackageDto() as PackageEditDto;
            data.index = this.maxIndex++;
            data.palletWegiht = this.defaultWeight;
            this.packageList.push(data);
        },
        deletePackage(pack: AddPackageDto, index) {
            this.packageList.splice(index, 1);
        },
        onFileSelect(files, pack) {
            this.selectedPack = pack;
            this.uploadFileType = 'attachment';
            this.uploadFileList = files;
            this.uploadFileDialog.open();
        },
        onTagFileSelect(files, pack) {
            this.selectedPack = pack;
            this.uploadFileType = 'tag';
            this.uploadFileList = files;
            this.uploadFileDialog.open();
        },
        fileDialogClose() {
            this.uploadFileDialog.close();
        },
        uploadFileSubmit(files: UploadFile[]) {
            //此时文件未上传后端 新增产品时再添加文件
            if (this.uploadFileType == "tag") {
                this.selectedPack.tagUploadFileData = files;
            } else {
                this.selectedPack.uploadFileData = files;
            }
            this.uploadFileDialog.close();
            //为文件下拉框添加选项
            if (this.selectedPack.customerParaList) {
                this.selectedPack.customerParaList.forEach(item => {
                    if (item.systemPara.displayType == DisplayType.SelectFile) {
                        var newFileList = files.map(x => {
                            return {
                                id: undefined,
                                text: x.name,
                                value: x.url
                            }
                        });
                        //按value去重
                        item.fileSelections = [...(item.fileSelections ?? []), ...newFileList];
                        //按value去重
                        item.fileSelections = item.fileSelections.filter((v, i, a) => a.findIndex(t => t.value === v.value) === i);
                    }
                })
            }
        },
        confirm() {
            var valid = true;
            //表单验证
            for (let index = 0; index < this.packageList.length; index++) {
                const pack = this.packageList[index];
                if (!pack.packageSpecification) {
                    this.$kingzon_messager.showConfim("请输入包装方案")
                    valid = false;
                    break;
                }
                if (pack.packageUnitId && !pack.mainUnitRatio) {
                    this.$kingzon_messager.showConfim("请输入主单位换算")
                    valid = false;
                    break;
                }
            }
            if (!valid) {
                return;
            }
            //提交后端
            this.isLoading = true;

            this.packageList.forEach(pack => {
                pack.customAttributeList = pack.customerParaList?.map(x => {
                    var data = [{
                        systemParaId: x.systemPara.id,
                        value: x.systemPara.value,
                    }];
                    if (x.childrenSystemPara && x.childrenSystemPara.length) {
                        var childData = x.childrenSystemPara.map(e => {
                            return {
                                systemParaId: e.systemPara.id,
                                value: e.systemPara.value,
                            }
                        })
                        data = [...data, ...childData];
                    }
                    return data;
                }).flat() ?? [];
            })
            var uploadFileData = this.packageList.filter(x => x.uploadFileData && x.uploadFileData.length);
            var tagUploadFileData = this.packageList.filter(x => x.tagUploadFileData && x.tagUploadFileData.length);
            const hasPendingUpload = uploadFileData.length > 0 || tagUploadFileData.length > 0;
            return this.$http.post('/api/base/Product/ModifyPackage',
                {
                    productId: this.product.productId,
                    specId: this.product.specificationId,
                    packageList: this.packageList
                })
                .then((res: any) => {
                    var data = res.data as AddPackageDto[];
                    //上传文件
                    var fileData = [] as uploadFilesType[];
                    uploadFileData.forEach(item => {
                        var entity = data.find(x => x.index == item.index || x.id == item.id);
                        if (entity) {
                            fileData.push({
                                sourceId: entity.id,
                                files: item.uploadFileData,
                                sourceType: FileStorageSourceType.成品包装文件,
                                fileBase: FileBase.ProductPack
                            } as uploadFilesType);
                        }
                    });
                    tagUploadFileData.forEach(item => {
                        var entity = data.find(x => x.index == item.index || x.id == item.id);
                        if (entity) {
                            fileData.push({
                                sourceId: entity.id,
                                files: item.tagUploadFileData,
                                sourceType: FileStorageSourceType.成品标签文件,
                                fileBase: FileBase.ProductTag
                            } as uploadFilesType);
                        }
                    });
                    if (fileData.length > 0) {
                        //上传多组文件
                        this.$utils.uploadFiles(fileData,
                            () => {
                                this.isLoading = false; //关闭loading
                                this.$kingzon_messager.showInfo("文件上传成功");
                                this.$emit('submit');
                            }, true, (sourceId, file, res) => {
                                //单个文件上传成功回调
                                var newFileId = res.data;
                                var entity = data.find(x => x.id == sourceId); //文件对应的包装方案
                                var originEntity = this.packageList.find(x => x.id == entity?.id || x.index == entity?.index); //原包装方案数据
                                var isAttachment = originEntity?.uploadFileData?.some(x =>
                                    x === file || (!!x.url && x.url == file.url)
                                );
                                if (!isAttachment) {
                                    return;
                                }
                                var isModify = false;
                                if (entity) {
                                    entity.customAttributeList.forEach(element => {
                                        var para = originEntity?.customerParaList?.find(x => x.systemPara.id == element.systemParaId);

                                        if (para && para.systemPara.displayType == DisplayType.SelectFile) {
                                            //将新上传的文件id替换掉参数值中对应的旧文件id
                                            if (Array.isArray(element.value)) {
                                                // 只有数组才执行 map
                                                element.value = element.value.map(f => {
                                                    if (f == file.url) {
                                                        isModify = true;
                                                        return newFileId;
                                                    }
                                                    return f;
                                                });
                                            }
                                        }
                                    });
                                    if (isModify) {
                                        //修改
                                        this.$http.post('/api/base/Product/ModifyPackCustomAttributeById', entity)
                                    }
                                }
                            })
                    } else {
                        //不上传文件
                        this.$emit('submit');
                    }

                })
                .catch((err) => {
                    this.isLoading = false;
                    this.$kingzon_messager.showError(err);
                })
                .finally(() => {
                    if (!hasPendingUpload) { //不上传文件
                        this.isLoading = false;
                    }
                })
        },

    }
})
</script>
<style scoped>
.package-page {
    padding: 10px 12px 16px;
}

.package-card {
    margin-bottom: 12px;
    border: 1px solid #d9e0e8;
    border-radius: 4px;
    background: #fff;
}

.package-card-header {
    height: 38px;
    padding: 0 12px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    border-bottom: 1px solid #e5e9ef;
    background: #f7f9fc;
}

.package-card-title {
    display: flex;
    align-items: center;
    gap: 10px;
    font-weight: 600;
    color: #333;
}

.package-card-subtitle {
    max-width: 380px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    font-size: 12px;
    font-weight: normal;
    color: #888;
}

.package-card-actions {
    display: flex;
    align-items: center;
    gap: 2px;
}

.package-delete-btn {
    margin-left: 6px;
}

.package-form-grid,
.package-para-grid {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    column-gap: 16px;
    row-gap: 8px;
    padding: 10px 12px 0;
}

.package-para-grid {
    padding-top: 8px;
}

.package-field {
    min-width: 0;
    display: flex;
    align-items: center;
}

.package-field-2 {
    grid-column: span 2;
}

.package-field-full {
    grid-column: 1 / -1;
    align-items: flex-start;
}

.package-label {
    width: 90px;
    flex-shrink: 0;
    text-align: right;
    color: #333;
}

.package-control {
    flex: 1;
    min-width: 0;
}

.required-label::before {
    content: '*';
    margin-right: 2px;
    color: #f56c6c;
}

.package-remark-row {
    padding-bottom: 10px;
}

.package-file-section {
    margin: 8px 12px 10px;
    padding-top: 8px;
    border-top: 1px dashed #dcdfe6;
}

.package-file-header {
    height: 30px;
    display: flex;
    align-items: center;
    justify-content: space-between;
}

.package-section-title {
    padding-left: 4px;
    font-weight: 600;
    color: #555;
}

.package-upload-list {
    margin-top: 5px;
}

.package-file-list {
    height: 150px;
    margin-top: 5px;
}

.package-add {
    padding: 3px 0 8px;
    text-align: center;
}

.package-add-btn {
    width: 130px;
    color: var(--color-blue);
}

.package-unit-pair,
.package-weight-triple {
    flex: 1;
    min-width: 0;
    display: grid;
    gap: 8px;
}

.package-unit-pair {
    grid-template-columns: repeat(2, minmax(0, 1fr));
}

.package-weight-triple {
    grid-template-columns: repeat(3, minmax(0, 1fr));
}

.measure-cell {
    display: flex;
    align-items: center;
    min-width: 0;
    gap: 4px;
    white-space: nowrap;
}

.measure-cell+.measure-cell {
    padding-left: 8px;
    border-left: 1px solid #e1e6ec;
}

.measure-label {
    flex: 0 0 auto;
    color: #4b5563;
}

.measure-equation {
    flex: 0 0 auto;
    color: #5f6b78;
}

.measure-control {
    flex: 1;
    min-width: 0;
}

.measure-input {
    width: 76px;
    flex: 0 0 76px;
}

.measure-unit {
    flex: 0 0 auto;
    color: #1769aa;
    font-weight: 600;
}

@media (max-width: 1200px) {

    .package-form-grid,
    .package-para-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }

    .package-field-2,
    .package-field-full {
        grid-column: 1 / -1;
    }
}

:deep(input) {
    color: var(--color-deep-blue);
}

:deep(textarea) {
    color: var(--color-deep-blue);
}
</style>
