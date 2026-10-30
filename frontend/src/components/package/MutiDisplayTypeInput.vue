<template>
  <div class="multi-display-input">
    <!-- 下拉选择 -->
    <ComboBox style="width: 100%;" v-if="para.systemPara.displayType === DisplayType.Select" v-model="value"
      :data="selections">
      <Addon>
        <span v-if="value != null || value != undefined" class="textbox-icon icon-clear"
          @click="value = undefined"></span>
      </Addon>
    </ComboBox>
    <!-- 数字 -->
    <NumberBox style="width: 100%;" v-if="para.systemPara.displayType === DisplayType.Number" v-model="value"
      :spinners="true">
      <Addon>
        <span v-if="para.systemPara.unit && para.systemPara.unit !== ''" class="kingzon-input-addon">
          {{ para.systemPara.unit }}
        </span>
      </Addon>
    </NumberBox>
    <!-- 日期时间 -->
    <DateBox v-if="para.systemPara.displayType === DisplayType.DateTime" v-model="value" format="yyyy-MM-dd"
      :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }" style="width: 100%;">
      <Addon>
        <span v-if="value != null || value != undefined" class="textbox-icon icon-clear"
          @click="value = undefined"></span>
      </Addon>
    </DateBox>
    <!-- 开关 -->
    <SwitchButton v-if="para.systemPara.displayType === DisplayType.Switch" style="width: 64px; height: 24px"
      v-model="value" onText="是" offText="否">
    </SwitchButton>
    <!-- 单选 -->
    <div v-if="para.systemPara.displayType === DisplayType.RadioButton"
      style="display: flex;justify-content: left;width: 100%;">
      <div v-for="(option, index) in selections" :key="index" style="margin-right: 5px;">
        <RadioButton :value="option.value" v-model="value"></RadioButton>
        <Label style="width: auto;">{{ option.text }}</Label>
      </div>
    </div>
    <!-- 多选 -->
    <div v-if="para.systemPara.displayType === DisplayType.CheckBox"
      style="display: flex;justify-content: left;width: 100%;">
      <div v-for="(option, index) in selections" :key="index" style="margin-right: 5px;">
        <CheckBox :multiple="true" v-model="value" :value="option.value"></CheckBox>
        <Label style="width: auto;">{{ option.text }}</Label>
      </div>
    </div>
    <!-- 文本框-->
    <div v-if="para.systemPara.displayType === DisplayType.TextBox">
      <TextBox v-if="!para.childrenSystemPara || !para.childrenSystemPara.length" v-model="value"
        :style="{ width: '100%' }">
        <Addon>
          <span v-if="para.systemPara.unit && para.systemPara.unit !== ''" class="kingzon-input-addon">
            {{ para.systemPara.unit }}
          </span>
        </Addon>
      </TextBox>
      <div v-else style="display: flex;flex-direction: column;">
        <div v-for="child in para.childrenSystemPara" :key="child.systemPara.id"
          style="display: flex;align-items: center;margin-top: 2px;">
          <span v-if="child.systemPara.dataValue" style="margin-right: 4px;"> {{ child.systemPara.dataValue }}</span>
          <MutiDisplayTypeInput style="flex: 1;" :para="child" @update:value="child.systemPara.value = $event">
          </MutiDisplayTypeInput>
        </div>
      </div>
    </div>
    <!-- 多选文件列表 -->
    <div v-if="para.systemPara.displayType === DisplayType.SelectFile"
      class="file-select-row">
      <ComboBox class="file-select-box" v-model="value" :data="para.fileSelections" :multiple="true">
      </ComboBox>
      <div v-for="child in para.childrenSystemPara" :key="child.systemPara.id"
        class="file-child-field">
        <span v-if="child.systemPara.dataValue" class="file-child-label">{{ child.systemPara.dataValue }}</span>
        <MutiDisplayTypeInput class="file-child-input" :compact="true" :para="child"
          @update:value="child.systemPara.value = $event">
        </MutiDisplayTypeInput>
      </div>
    </div>
    <!-- 其他类型 -->
    <TextBox v-if="para.systemPara.displayType === DisplayType.Textarea" v-model="value"
      :multiline="para.systemPara.displayType == DisplayType.Textarea"
      :style="{ width: '100%', height: compact ? '48px' : '80px' }">
      <Addon>
        <span v-if="para.systemPara.unit && para.systemPara.unit !== ''" class="kingzon-input-addon">
          {{ para.systemPara.unit }}
        </span>
      </Addon>
    </TextBox>
  </div>
</template>

<script lang="ts">
import type { SystemParaTreeDto } from '@/types/common';
import { DisplayType } from '@/types/enums';
import { defineComponent } from 'vue';
export default defineComponent({
  props: {
    para: {
      type: Object as () => SystemParaTreeDto,
      required: true,
      default: {} as SystemParaTreeDto,
    },
    compact: {
      type: Boolean,
      default: false,
    },
  },
  emits: ['update:value'],
  data() {
    return {
      value: undefined as any,
    };
  },
  computed: {
    DisplayType() {
      return DisplayType;
    },
    selections() {
      if (this.para.childrenSystemPara) {
        return this.para.childrenSystemPara.map((item) => {
          return {
            text: item.systemPara.dataValue,
            value: item.systemPara.dataValue,
          };
        });
      }
      return [];
    },
  },
  watch: {
    value: {
      handler(newValue) {
        this.$emit('update:value', newValue);
      },
      deep: true,
    },
    para: {
      handler() {
        this.init();
      },
      deep: true,
    },
  },
  mounted() {

  },
  created() {
    this.init();
  },
  methods: {

    init() {
      switch (this.para.systemPara.displayType) {
        case DisplayType.Number:
          this.value = this.para.systemPara.value ? Number(this.para.systemPara.value ?? 0) : undefined;
          break;
        case DisplayType.DateTime:
          this.value = this.para.systemPara.value ? new Date(this.para.systemPara.value ?? "") : undefined;
          break;
        case DisplayType.Switch:
          this.value = (this.para.systemPara.value ?? '').toString() === '1' || (this.para.systemPara.value ?? "").toLowerCase() === 'true';
          break;
        case DisplayType.CheckBox:
          //console.log(typeof this.para.systemPara.value,this.para.systemPara.value,'dd')
          if (typeof this.para.systemPara.value === 'string') {
            this.value = this.para.systemPara.value ? this.para.systemPara.value.split(",") : [];
          }

          break;
        default:
          this.value = this.para.systemPara.value;
      }


    },
  },
});
</script>

<style scoped>
.multi-display-input {
  width: 100%;
  min-width: 0;
}

.file-select-row {
  display: flex;
  align-items: flex-start;
  width: 100%;
  min-width: 0;
  gap: 5px;
}

.file-select-box {
  flex: 1;
  min-width: 0;
}

.file-child-field {
  display: flex;
  align-items: flex-start;
  flex: 1;
  min-width: 0;
}

.file-child-label {
  flex: 0 0 auto;
  margin: 5px 4px 0 0;
  white-space: nowrap;
}

.file-child-input {
  flex: 1;
  min-width: 0;
}
</style>
