// Sqlx TODO 应用主脚本
// 展示 Vue 3 + Element Plus + Sqlx WebAPI 的完整集成

const { createApp } = Vue;
const { ElMessage, ElMessageBox, ElLoading } = ElementPlus;

// API 客户端配置
const api = axios.create({
    baseURL: '/api',
    timeout: 10000,
    headers: {
        'Content-Type': 'application/json'
    }
});

// 请求拦截器
api.interceptors.request.use(
    config => {
        // 可以在这里添加认证 token 等
        return config;
    },
    error => Promise.reject(error)
);

// 响应拦截器
api.interceptors.response.use(
    response => response,
    error => {
        const message = error.response?.data?.error || error.message || '请求失败';
        ElMessage.error(message);
        return Promise.reject(error);
    }
);

// 主应用
const app = createApp({
    data() {
        return {
            // 应用状态
            loading: true,
            todosLoading: false,
            activeTab: 'todos',

            // 数据
            todos: [],
            stats: {
                totalTodos: 0,
                completedTodos: 0,
                pendingTodos: 0,
                highPriorityTodos: 0,
                overdueTodos: 0,
                completionRate: 0,
                totalEstimatedMinutes: 0,
                totalActualMinutes: 0
            },

            // 分页和过滤
            currentPage: 1,
            pageSize: 20,
            totalTodos: 0,
            searchText: '',
            statusFilter: '',
            priorityFilter: '',
            searchDebounceTimer: null,

            // 选择的 TODO
            selectedTodos: [],

            // 对话框
            dialogVisible: false,
            dialogMode: 'create', // 'create' | 'edit'
            currentTodo: null,

            // 图表实例
            completionChart: null,
            priorityChart: null
        };
    },

    async mounted() {
        try {
            // 初始化应用
            await this.initializeApp();
        } catch (error) {
            console.error('应用初始化失败:', error);
            ElMessage.error('应用初始化失败，请刷新页面重试');
        } finally {
            this.loading = false;
        }
    },

    methods: {
        // === 初始化方法 ===
        async initializeApp() {
            // 并行加载数据
            await Promise.all([
                this.loadStats(),
                this.loadTodos()
            ]);

            // 初始化图表
            this.$nextTick(() => {
                this.initCharts();
            });
        },

        // === 数据加载方法 ===
        async loadStats() {
            try {
                const response = await api.get('/todos/stats');
                this.stats = response.data;
            } catch (error) {
                console.error('加载统计数据失败:', error);
            }
        },

        async loadTodos() {
            this.todosLoading = true;
            try {
                const params = {
                    skip: (this.currentPage - 1) * this.pageSize,
                    take: this.pageSize
                };

                // 添加过滤条件
                if (this.searchText.trim()) {
                    params.search = this.searchText.trim();
                }
                if (this.statusFilter !== '') {
                    params.isCompleted = this.statusFilter;
                }
                if (this.priorityFilter !== '') {
                    params.priority = this.priorityFilter;
                }

                const response = await api.get('/todos', { params });
                this.todos = response.data;

                // 更新总数（这里简化处理，实际应该从 API 获取总数）
                if (this.currentPage === 1) {
                    this.totalTodos = Math.max(response.data.length, this.stats.totalTodos);
                }
            } catch (error) {
                console.error('加载 TODO 列表失败:', error);
                this.todos = [];
            } finally {
                this.todosLoading = false;
            }
        },

        async refreshData() {
            const loading = ElLoading.service({
                lock: true,
                text: '刷新数据中...',
                background: 'rgba(0, 0, 0, 0.7)'
            });

            try {
                await this.initializeApp();
                ElMessage.success('数据刷新成功');
            } catch (error) {
                ElMessage.error('数据刷新失败');
            } finally {
                loading.close();
            }
        },

        // === TODO 操作方法 ===
        showCreateDialog() {
            this.currentTodo = null;
            this.dialogMode = 'create';
            this.dialogVisible = true;
        },

        editTodo(todo) {
            this.currentTodo = { ...todo };
            this.dialogMode = 'edit';
            this.dialogVisible = true;
        },

        async handleTodoSave(todoData) {
            try {
                if (this.dialogMode === 'create') {
                    await api.post('/todos', todoData);
                    ElMessage.success('任务创建成功');
                } else {
                    await api.put(`/todos/${this.currentTodo.id}`, todoData);
                    ElMessage.success('任务更新成功');
                }

                this.dialogVisible = false;
                await this.refreshData();
            } catch (error) {
                console.error('保存任务失败:', error);
            }
        },

        async completeTodo(todo) {
            try {
                await api.patch(`/todos/${todo.id}/complete`);
                ElMessage.success('任务已标记为完成');
                await this.refreshData();
            } catch (error) {
                console.error('完成任务失败:', error);
            }
        },

        async deleteTodo(todo) {
            try {
                await ElMessageBox.confirm(
                    `确定要删除任务"${todo.title}"吗？`,
                    '删除确认',
                    {
                        confirmButtonText: '确定',
                        cancelButtonText: '取消',
                        type: 'warning'
                    }
                );

                await api.delete(`/todos/${todo.id}`);
                ElMessage.success('任务删除成功');
                await this.refreshData();
            } catch (error) {
                if (error !== 'cancel') {
                    console.error('删除任务失败:', error);
                }
            }
        },

        // === 批量操作方法 ===
        async batchComplete() {
            if (!this.selectedTodos.length) {
                ElMessage.warning('请选择要完成的任务');
                return;
            }

            try {
                await ElMessageBox.confirm(
                    `确定要完成选中的 ${this.selectedTodos.length} 个任务吗？`,
                    '批量完成确认',
                    {
                        confirmButtonText: '确定',
                        cancelButtonText: '取消',
                        type: 'info'
                    }
                );

                const ids = this.selectedTodos.map(todo => todo.id);
                await api.patch('/todos/complete-batch', { ids });

                ElMessage.success(`成功完成 ${ids.length} 个任务`);
                this.selectedTodos = [];
                await this.refreshData();
            } catch (error) {
                if (error !== 'cancel') {
                    console.error('批量完成失败:', error);
                }
            }
        },

        async batchDelete() {
            if (!this.selectedTodos.length) {
                ElMessage.warning('请选择要删除的任务');
                return;
            }

            try {
                await ElMessageBox.confirm(
                    `确定要删除选中的 ${this.selectedTodos.length} 个任务吗？此操作不可恢复！`,
                    '批量删除确认',
                    {
                        confirmButtonText: '确定',
                        cancelButtonText: '取消',
                        type: 'warning'
                    }
                );

                const ids = this.selectedTodos.map(todo => todo.id);
                await api.delete('/todos/batch', { data: { ids } });

                ElMessage.success(`成功删除 ${ids.length} 个任务`);
                this.selectedTodos = [];
                await this.refreshData();
            } catch (error) {
                if (error !== 'cancel') {
                    console.error('批量删除失败:', error);
                }
            }
        },

        // === 事件处理方法 ===
        handleTabClick(tab) {
            if (tab.paneName === 'stats' && !this.completionChart) {
                this.$nextTick(() => {
                    this.initCharts();
                });
            }
        },

        handleSelectionChange(selection) {
            this.selectedTodos = selection;
        },

        handleSizeChange(val) {
            this.pageSize = val;
            this.currentPage = 1;
            this.loadTodos();
        },

        handleCurrentChange(val) {
            this.currentPage = val;
            this.loadTodos();
        },

        debounceSearch() {
            clearTimeout(this.searchDebounceTimer);
            this.searchDebounceTimer = setTimeout(() => {
                this.currentPage = 1;
                this.loadTodos();
            }, 500);
        },

        // === 工具方法 ===
        getPriorityType(priority) {
            const types = { 1: '', 2: 'info', 3: 'warning', 4: 'danger' };
            return types[priority] || '';
        },

        getPriorityText(priority) {
            const texts = { 1: '低', 2: '中', 3: '高', 4: '紧急' };
            return texts[priority] || '未知';
        },

        formatDate(dateString) {
            if (!dateString) return '';
            return new Date(dateString).toLocaleDateString('zh-CN');
        },

        formatDateTime(dateString) {
            if (!dateString) return '';
            return new Date(dateString).toLocaleString('zh-CN');
        },

        getEfficiencyRatio() {
            if (!this.stats.totalEstimatedMinutes) return 0;
            return (this.stats.totalEstimatedMinutes / this.stats.totalActualMinutes) * 100;
        },

        // === 外部链接方法 ===
        showApiDocs() {
            window.open('/swagger', '_blank');
        },

        openGithub() {
            window.open('https://github.com/cricle/Sqlx', '_blank');
        },

        viewSourceCode() {
            window.open('https://github.com/cricle/Sqlx/tree/main/samples/TodoWebApi', '_blank');
        },

        // === 图表初始化方法 ===
        initCharts() {
            this.initCompletionChart();
            this.initPriorityChart();
        },

        initCompletionChart() {
            const canvas = document.getElementById('completionChart');
            if (!canvas) return;

            const ctx = canvas.getContext('2d');

            if (this.completionChart) {
                this.completionChart.destroy();
            }

            this.completionChart = new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: ['已完成', '待办事项', '过期任务'],
                    datasets: [{
                        data: [
                            this.stats.completedTodos,
                            this.stats.pendingTodos - this.stats.overdueTodos,
                            this.stats.overdueTodos
                        ],
                        backgroundColor: [
                            '#67C23A',
                            '#409EFF',
                            '#F56C6C'
                        ],
                        borderWidth: 0
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'bottom'
                        }
                    }
                }
            });
        },

        initPriorityChart() {
            const canvas = document.getElementById('priorityChart');
            if (!canvas) return;

            const ctx = canvas.getContext('2d');

            if (this.priorityChart) {
                this.priorityChart.destroy();
            }

            // 计算优先级分布（简化版，实际应该从 API 获取）
            const priorityData = this.todos.reduce((acc, todo) => {
                acc[todo.priority] = (acc[todo.priority] || 0) + 1;
                return acc;
            }, {});

            this.priorityChart = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: ['低', '中', '高', '紧急'],
                    datasets: [{
                        label: '任务数量',
                        data: [
                            priorityData[1] || 0,
                            priorityData[2] || 0,
                            priorityData[3] || 0,
                            priorityData[4] || 0
                        ],
                        backgroundColor: [
                            '#909399',
                            '#409EFF',
                            '#E6A23C',
                            '#F56C6C'
                        ],
                        borderRadius: 4,
                        borderSkipped: false
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: {
                                stepSize: 1
                            }
                        }
                    }
                }
            });
        }
    },

    watch: {
        // 监听过滤条件变化
        statusFilter() {
            this.currentPage = 1;
            this.loadTodos();
        },

        priorityFilter() {
            this.currentPage = 1;
            this.loadTodos();
        },

        // 监听统计数据变化，更新图表
        stats: {
            handler() {
                if (this.completionChart) {
                    this.initCompletionChart();
                }
            },
            deep: true
        },

        todos: {
            handler() {
                if (this.priorityChart) {
                    this.initPriorityChart();
                }
            },
            deep: true
        }
    }
});

// 注册 Element Plus 组件和图标
for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
    app.component(key, component);
}

// 使用 Element Plus
app.use(ElementPlus);

// 挂载应用
app.mount('#app');

// 全局错误处理
window.addEventListener('error', (event) => {
    console.error('全局错误:', event.error);
    ElMessage.error('应用发生错误，请刷新页面重试');
});

window.addEventListener('unhandledrejection', (event) => {
    console.error('未处理的 Promise 拒绝:', event.reason);
    event.preventDefault();
});
