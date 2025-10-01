// TODO 对话框组件
// 用于创建和编辑 TODO 项目

const TodoDialog = {
    template: `
        <el-dialog
            :model-value="visible"
            @update:model-value="$emit('update:visible', $event)"
            :title="isEdit ? '编辑任务' : '新建任务'"
            width="600px"
            :close-on-click-modal="false"
            destroy-on-close>

            <el-form
                ref="formRef"
                :model="form"
                :rules="rules"
                label-width="100px"
                label-position="top">

                <!-- 标题 -->
                <el-form-item label="任务标题" prop="title">
                    <el-input
                        v-model="form.title"
                        placeholder="请输入任务标题"
                        maxlength="200"
                        show-word-limit
                        clearable>
                        <template #prefix>
                            <el-icon><Edit /></el-icon>
                        </template>
                    </el-input>
                </el-form-item>

                <!-- 描述 -->
                <el-form-item label="任务描述" prop="description">
                    <el-input
                        v-model="form.description"
                        type="textarea"
                        placeholder="请输入任务描述（可选）"
                        :rows="3"
                        maxlength="500"
                        show-word-limit
                        clearable>
                    </el-input>
                </el-form-item>

                <!-- 第一行：优先级和状态 -->
                <el-row :gutter="20">
                    <el-col :span="12">
                        <el-form-item label="优先级" prop="priority">
                            <el-select v-model="form.priority" placeholder="选择优先级" style="width: 100%">
                                <el-option label="低" :value="1">
                                    <span style="color: #909399">● 低</span>
                                </el-option>
                                <el-option label="中" :value="2">
                                    <span style="color: #409eff">● 中</span>
                                </el-option>
                                <el-option label="高" :value="3">
                                    <span style="color: #e6a23c">● 高</span>
                                </el-option>
                                <el-option label="紧急" :value="4">
                                    <span style="color: #f56c6c">● 紧急</span>
                                </el-option>
                            </el-select>
                        </el-form-item>
                    </el-col>

                    <el-col :span="12" v-if="isEdit">
                        <el-form-item label="完成状态">
                            <el-switch
                                v-model="form.isCompleted"
                                active-text="已完成"
                                inactive-text="待办">
                            </el-switch>
                        </el-form-item>
                    </el-col>
                </el-row>

                <!-- 第二行：截止日期和估计时间 -->
                <el-row :gutter="20">
                    <el-col :span="12">
                        <el-form-item label="截止日期">
                            <el-date-picker
                                v-model="form.dueDate"
                                type="date"
                                placeholder="选择截止日期"
                                style="width: 100%"
                                format="YYYY-MM-DD"
                                value-format="YYYY-MM-DD"
                                clearable>
                            </el-date-picker>
                        </el-form-item>
                    </el-col>

                    <el-col :span="12">
                        <el-form-item label="估计时间（分钟）">
                            <el-input-number
                                v-model="form.estimatedMinutes"
                                placeholder="预估工作时间"
                                :min="0"
                                :max="9999"
                                :step="15"
                                controls-position="right"
                                style="width: 100%">
                            </el-input-number>
                        </el-form-item>
                    </el-col>
                </el-row>

                <!-- 第三行：实际时间和标签 -->
                <el-row :gutter="20" v-if="isEdit">
                    <el-col :span="12">
                        <el-form-item label="实际时间（分钟）">
                            <el-input-number
                                v-model="form.actualMinutes"
                                placeholder="实际工作时间"
                                :min="0"
                                :max="9999"
                                :step="15"
                                controls-position="right"
                                style="width: 100%">
                            </el-input-number>
                        </el-form-item>
                    </el-col>

                    <el-col :span="12">
                        <el-form-item label="标签">
                            <el-input
                                v-model="form.tags"
                                placeholder="输入标签，如：工作,重要"
                                clearable>
                                <template #prefix>
                                    <el-icon><PriceTag /></el-icon>
                                </template>
                            </el-input>
                        </el-form-item>
                    </el-col>
                </el-row>

                <!-- 只有编辑时显示标签 -->
                <el-row v-else>
                    <el-col :span="24">
                        <el-form-item label="标签">
                            <el-input
                                v-model="form.tags"
                                placeholder="输入标签，如：工作,重要"
                                clearable>
                                <template #prefix>
                                    <el-icon><PriceTag /></el-icon>
                                </template>
                            </el-input>
                        </el-form-item>
                    </el-col>
                </el-row>

                <!-- 快捷标签 -->
                <el-form-item label="快捷标签">
                    <div class="quick-tags">
                        <el-tag
                            v-for="tag in quickTags"
                            :key="tag"
                            @click="addQuickTag(tag)"
                            class="quick-tag"
                            size="small"
                            effect="plain"
                            style="cursor: pointer; margin-right: 8px; margin-bottom: 8px">
                            {{ tag }}
                        </el-tag>
                    </div>
                </el-form-item>

                <!-- 预设优先级组合 -->
                <el-form-item label="快速设置">
                    <div class="quick-settings">
                        <el-button-group>
                            <el-button size="small" @click="setQuickSetting('low')">
                                <el-icon><Clock /></el-icon>
                                普通任务
                            </el-button>
                            <el-button size="small" @click="setQuickSetting('medium')" type="primary">
                                <el-icon><Warning /></el-icon>
                                重要任务
                            </el-button>
                            <el-button size="small" @click="setQuickSetting('high')" type="warning">
                                <el-icon><SemiSelect /></el-icon>
                                紧急任务
                            </el-button>
                            <el-button size="small" @click="setQuickSetting('urgent')" type="danger">
                                <el-icon><WarnTriangleFilled /></el-icon>
                                重要紧急
                            </el-button>
                        </el-button-group>
                    </div>
                </el-form-item>
            </el-form>

            <!-- 对话框底部 -->
            <template #footer>
                <div class="dialog-footer">
                    <el-button @click="cancel">取消</el-button>
                    <el-button type="primary" @click="confirm" :loading="saving">
                        <el-icon><Check /></el-icon>
                        {{ isEdit ? '更新' : '创建' }}
                    </el-button>
                </div>
            </template>
        </el-dialog>
    `,

    props: {
        visible: {
            type: Boolean,
            default: false
        },
        todo: {
            type: Object,
            default: null
        },
        mode: {
            type: String,
            default: 'create', // 'create' | 'edit'
            validator: (value) => ['create', 'edit'].includes(value)
        }
    },

    emits: ['update:visible', 'confirm'],

    data() {
        return {
            saving: false,
            form: {
                title: '',
                description: '',
                priority: 2,
                isCompleted: false,
                dueDate: null,
                estimatedMinutes: null,
                actualMinutes: null,
                tags: ''
            },

            // 表单验证规则
            rules: {
                title: [
                    { required: true, message: '请输入任务标题', trigger: 'blur' },
                    { min: 1, max: 200, message: '标题长度在 1 到 200 个字符', trigger: 'blur' }
                ],
                priority: [
                    { required: true, message: '请选择优先级', trigger: 'change' }
                ]
            },

            // 快捷标签
            quickTags: [
                '工作', '个人', '学习', '购物', '健康',
                '重要', '紧急', '今日', '本周', '长期'
            ]
        };
    },

    computed: {
        isEdit() {
            return this.mode === 'edit';
        }
    },

    watch: {
        visible(val) {
            if (val) {
                this.initForm();
            } else {
                this.resetForm();
            }
        },

        todo: {
            handler() {
                if (this.visible) {
                    this.initForm();
                }
            },
            deep: true
        }
    },

    methods: {
        // 初始化表单
        initForm() {
            if (this.isEdit && this.todo) {
                // 编辑模式：填充现有数据
                this.form = {
                    title: this.todo.title || '',
                    description: this.todo.description || '',
                    priority: this.todo.priority || 2,
                    isCompleted: this.todo.isCompleted || false,
                    dueDate: this.todo.dueDate ? this.todo.dueDate.split('T')[0] : null,
                    estimatedMinutes: this.todo.estimatedMinutes || null,
                    actualMinutes: this.todo.actualMinutes || null,
                    tags: this.todo.tags || ''
                };
            } else {
                // 创建模式：重置表单
                this.resetForm();
            }
        },

        // 重置表单
        resetForm() {
            this.form = {
                title: '',
                description: '',
                priority: 2,
                isCompleted: false,
                dueDate: null,
                estimatedMinutes: null,
                actualMinutes: null,
                tags: ''
            };

            // 清除验证结果
            this.$nextTick(() => {
                if (this.$refs.formRef) {
                    this.$refs.formRef.clearValidate();
                }
            });
        },

        // 添加快捷标签
        addQuickTag(tag) {
            const currentTags = this.form.tags ? this.form.tags.split(',').map(t => t.trim()) : [];

            if (!currentTags.includes(tag)) {
                currentTags.push(tag);
                this.form.tags = currentTags.join(',');

                ElMessage.success(`已添加标签: ${tag}`);
            } else {
                ElMessage.info(`标签 "${tag}" 已存在`);
            }
        },

        // 快速设置
        setQuickSetting(type) {
            const settings = {
                low: {
                    priority: 1,
                    dueDate: this.getDateAfterDays(7),
                    estimatedMinutes: 60
                },
                medium: {
                    priority: 2,
                    dueDate: this.getDateAfterDays(3),
                    estimatedMinutes: 120
                },
                high: {
                    priority: 3,
                    dueDate: this.getDateAfterDays(1),
                    estimatedMinutes: 180
                },
                urgent: {
                    priority: 4,
                    dueDate: new Date().toISOString().split('T')[0],
                    estimatedMinutes: 240
                }
            };

            const setting = settings[type];
            if (setting) {
                Object.assign(this.form, setting);

                const typeNames = {
                    low: '普通任务',
                    medium: '重要任务',
                    high: '紧急任务',
                    urgent: '重要紧急'
                };

                ElMessage.success(`已应用 "${typeNames[type]}" 设置`);
            }
        },

        // 获取几天后的日期
        getDateAfterDays(days) {
            const date = new Date();
            date.setDate(date.getDate() + days);
            return date.toISOString().split('T')[0];
        },

        // 确认操作
        async confirm() {
            try {
                // 验证表单
                const valid = await this.$refs.formRef.validate();
                if (!valid) return;

                this.saving = true;

                // 准备数据
                const todoData = { ...this.form };

                // 处理日期格式
                if (todoData.dueDate) {
                    todoData.dueDate = new Date(todoData.dueDate).toISOString();
                }

                // 处理标签
                if (todoData.tags) {
                    todoData.tags = todoData.tags.trim();
                }

                // 清理空值
                Object.keys(todoData).forEach(key => {
                    if (todoData[key] === null || todoData[key] === '') {
                        if (key !== 'description' && key !== 'tags') {
                            delete todoData[key];
                        }
                    }
                });

                // 发送确认事件
                this.$emit('confirm', todoData);

            } catch (error) {
                console.error('表单验证失败:', error);
            } finally {
                this.saving = false;
            }
        },

        // 取消操作
        cancel() {
            this.$emit('update:visible', false);
        }
    }
};

// 将组件注册到全局应用
if (typeof app !== 'undefined') {
    app.component('TodoDialog', TodoDialog);
}
