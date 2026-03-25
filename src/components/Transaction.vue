<script setup>
import { computed, ref, watch } from 'vue'
import { onMounted } from 'vue'
import PopupModal from './PopupModal.vue'

onMounted(() => {
    document.title = 'Transaction | Clicknext Bank'
})

const props = defineProps({
    transactions: {
        type: Array,
        default: () => []
    }
})

const emit = defineEmits(['update-transaction', 'delete-transaction'])

const isEditOpen = ref(false)
const isDeleteOpen = ref(false)
const showErrorModal = ref(false)

const selectedItem = ref(null)
const editAmount = ref('')

const errorTitle = ref('')
const errorMessage = ref('')

const currentPage = ref(1)
const itemsPerPage = 8

const openErrorModal = (title, message) => {
    errorTitle.value = title
    errorMessage.value = message
    showErrorModal.value = true
}

const closeErrorModal = () => {
    showErrorModal.value = false
}

const sanitizeEditAmount = (event) => {
    editAmount.value = String(event.target.value || '').replace(/\D/g, '')
}

const formatDate = (value) => {
    if (!value) return '-'

    const raw = String(value).trim()
    const hasTimeZone = /([zZ]|[+\-]\d{2}:\d{2})$/.test(raw)
    const normalized = hasTimeZone ? raw : `${raw}Z`

    const date = new Date(normalized)

    if (Number.isNaN(date.getTime())) return '-'

    return new Intl.DateTimeFormat('th-TH-u-ca-gregory', {
        timeZone: 'Asia/Bangkok',
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: false
    }).format(date)
}

const formattedTransactions = computed(() =>
    props.transactions.map((item) => ({
        ...item,
        displayDate: formatDate(item.createdAt),
        displayAmount: Number(item.amount || 0).toLocaleString(),
        displayStatus: item.type === 'deposit' ? 'ฝาก' : 'ถอน'
    }))
)

const totalItems = computed(() => formattedTransactions.value.length)

const totalPages = computed(() => {
    const total = Math.ceil(totalItems.value / itemsPerPage)
    return total > 0 ? total : 1
})

const paginatedTransactions = computed(() => {
    const start = (currentPage.value - 1) * itemsPerPage
    return formattedTransactions.value.slice(start, start + itemsPerPage)
})

const startItem = computed(() => {
    if (totalItems.value === 0) return 0
    return (currentPage.value - 1) * itemsPerPage + 1
})

const endItem = computed(() => {
    if (totalItems.value === 0) return 0
    return Math.min(currentPage.value * itemsPerPage, totalItems.value)
})

const openEdit = (item) => {
    if (item.type !== 'deposit') return
    selectedItem.value = item
    editAmount.value = String(item.amount)
    isEditOpen.value = true
}

const closeEdit = () => {
    isEditOpen.value = false
    selectedItem.value = null
    editAmount.value = ''
}

const saveEdit = () => {
    const value = Number(editAmount.value)

    if (!editAmount.value || Number.isNaN(value) || value <= 0) {
        openErrorModal('ข้อมูลไม่ถูกต้อง', 'กรุณากรอกจำนวนเงินที่ถูกต้อง')
        return
    }

    if (value < 1 || value > 100000) {
        openErrorModal('ข้อมูลไม่ถูกต้อง', 'จำนวนเงินต้องอยู่ระหว่าง 1 ถึง 100000 บาท')
        return
    }

    emit('update-transaction', {
        id: selectedItem.value.id,
        amount: value
    })

    closeEdit()
}

const openDelete = (item) => {
    if (item.type !== 'withdraw') return
    selectedItem.value = item
    isDeleteOpen.value = true
}

const closeDelete = () => {
    isDeleteOpen.value = false
    selectedItem.value = null
}

const confirmDelete = () => {
    emit('delete-transaction', { id: selectedItem.value.id })
    closeDelete()
}

const goToPage = (page) => {
    if (page < 1 || page > totalPages.value) return
    currentPage.value = page
}

watch(
    () => props.transactions.length,
    () => {
        const maxPage = Math.max(1, Math.ceil(props.transactions.length / itemsPerPage))
        if (currentPage.value > maxPage) currentPage.value = maxPage
    }
)
</script>

<template>
    <div class="transaction-page">
        <div class="transaction-container">
            <h1 class="section-title">ประวัติรายการฝากถอน</h1>

            <div class="table-wrapper">
                <table class="transaction-table">
                    <thead>
                        <tr>
                            <th>Datetime</th>
                            <th>Amount</th>
                            <th>Status</th>
                            <th>Email</th>
                            <th>Action</th>
                        </tr>
                    </thead>

                    <tbody>
                        <tr v-if="paginatedTransactions.length === 0">
                            <td colspan="5" class="empty-cell">ไม่มีรายการ</td>
                        </tr>

                        <tr v-for="item in paginatedTransactions" :key="item.id">
                            <td>{{ item.displayDate }}</td>
                            <td>{{ item.displayAmount }}</td>
                            <td :class="['status-cell', item.type]">
                                {{ item.displayStatus }}
                            </td>
                            <td>{{ item.email }}</td>
                            <td>
                                <button v-if="item.type === 'deposit'" class="action-btn" type="button"
                                    @click="openEdit(item)">
                                    Edit
                                </button>

                                <button v-if="item.type === 'withdraw'" class="action-btn" type="button"
                                    @click="openDelete(item)">
                                    Delete
                                </button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <div class="table-footer">
                <div class="table-summary">
                    แสดง {{ startItem }} ถึง {{ endItem }} จาก {{ totalItems }} รายการ
                </div>

                <div v-if="totalPages > 1" class="pagination">
                    <button class="page-btn" type="button" :disabled="currentPage === 1"
                        @click="goToPage(currentPage - 1)">
                        ‹
                    </button>

                    <button v-for="page in totalPages" :key="page" class="page-btn"
                        :class="{ active: currentPage === page }" type="button" @click="goToPage(page)">
                        {{ page }}
                    </button>

                    <button class="page-btn" type="button" :disabled="currentPage === totalPages"
                        @click="goToPage(currentPage + 1)">
                        ›
                    </button>
                </div>
            </div>
        </div>

        <PopupModal :show="isEditOpen" title="แก้ไขจำนวนเงินฝาก" @close="closeEdit">
            <div v-if="selectedItem" class="popup-body">
                <div class="popup-text">ของวันที่ {{ formatDate(selectedItem.createdAt) }}</div>
                <div class="popup-text">จากอีเมล {{ selectedItem.email }}</div>

                <label class="popup-label">จำนวนเงิน *</label>
                <input :value="editAmount" class="popup-input" type="text" inputmode="numeric"
                    placeholder="กรอกจำนวนเงิน" @input="sanitizeEditAmount" />

                <div class="popup-actions">
                    <button class="popup-primary-btn" type="button" @click="saveEdit">
                        ยืนยัน
                    </button>
                    <button class="popup-secondary-btn" type="button" @click="closeEdit">
                        ยกเลิก
                    </button>
                </div>
            </div>
        </PopupModal>

        <PopupModal :show="isDeleteOpen" title="ยืนยันการลบ" @close="closeDelete">
            <div v-if="selectedItem" class="popup-body">
                <div class="popup-text">
                    จำนวนเงินถอน {{ Number(selectedItem.amount || 0).toLocaleString() }} บาท
                </div>
                <div class="popup-text">ของวันที่ {{ formatDate(selectedItem.createdAt) }}</div>
                <div class="popup-text">จากอีเมล {{ selectedItem.email }}</div>

                <div class="popup-actions">
                    <button class="popup-primary-btn" type="button" @click="confirmDelete">
                        ยืนยัน
                    </button>
                    <button class="popup-secondary-btn" type="button" @click="closeDelete">
                        ยกเลิก
                    </button>
                </div>
            </div>
        </PopupModal>

        <PopupModal :show="showErrorModal" :title="errorTitle" :message="errorMessage" @close="closeErrorModal" />
    </div>
</template>

<style scoped>
.transaction-page {
    width: 100%;
    padding: 56px 24px 32px;
    box-sizing: border-box;
}

.transaction-container {
    width: 100%;
    max-width: 1380px;
    margin: 0 auto;
}

.section-title {
    margin: 0 0 22px 0;
    font-size: 28px;
    font-weight: 700;
    line-height: 1.35;
    color: #1f2937;
}

.table-wrapper {
    width: 100%;
    overflow-x: auto;
    border: 1px solid #bfc5cc;
    background: #ffffff;
}

.transaction-table {
    width: 100%;
    min-width: 980px;
    border-collapse: collapse;
    font-size: 15px;
    color: #222222;
}

.transaction-table th,
.transaction-table td {
    border: 1px solid #bfc5cc;
    padding: 14px 16px;
    text-align: left;
    vertical-align: middle;
    background: #ffffff;
}

.transaction-table th {
    font-size: 15px;
    font-weight: 700;
    color: #1f2937;
}

.empty-cell {
    text-align: center;
    color: #6b7280;
    padding: 28px 16px;
}

.status-cell {
    font-weight: 700;
}

.status-cell.deposit {
    color: #1f7a3e;
}

.status-cell.withdraw {
    color: #dc2626;
}

.action-btn {
    min-width: 78px;
    height: 38px;
    padding: 0 14px;
    border: none;
    border-radius: 8px;
    background: #59606b;
    color: #ffffff;
    font-size: 14px;
    font-weight: 700;
    cursor: pointer;
}

.table-footer {
    margin-top: 16px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 16px;
    flex-wrap: wrap;
}

.table-summary {
    font-size: 15px;
    color: #374151;
}

.pagination {
    display: flex;
    gap: 8px;
    align-items: center;
}

.page-btn {
    min-width: 36px;
    height: 36px;
    padding: 0 10px;
    border: 1px solid #d1d5db;
    background: #ffffff;
    color: #374151;
    font-size: 14px;
    cursor: pointer;
}

.page-btn.active {
    background: #59606b;
    border-color: #59606b;
    color: #ffffff;
}

.page-btn:disabled {
    opacity: 0.45;
    cursor: not-allowed;
}

.popup-body {
    display: flex;
    flex-direction: column;
    gap: 14px;
}

.popup-text {
    font-size: 15px;
    line-height: 1.5;
    color: #374151;
}

.popup-label {
    display: block;
    margin-top: 2px;
    margin-bottom: 6px;
    font-size: 14px;
    font-weight: 600;
    color: #111827;
}

.popup-input {
    width: 100%;
    height: 44px;
    padding: 0 14px;
    box-sizing: border-box;
    border: 1px solid #d1d5db;
    border-radius: 8px;
    background: #ffffff;
    color: #111827;
    font-size: 14px;
}

.popup-input:focus {
    outline: none;
    border-color: #6b7280;
}

.popup-actions {
    display: flex;
    justify-content: center;
    gap: 12px;
    margin-top: 6px;
}

.popup-primary-btn,
.popup-secondary-btn {
    min-width: 96px;
    height: 40px;
    padding: 0 16px;
    border-radius: 8px;
    font-size: 14px;
    font-weight: 700;
    cursor: pointer;
}

.popup-primary-btn {
    border: none;
    background: #4b5563;
    color: #ffffff;
}

.popup-secondary-btn {
    border: 1px solid #d1d5db;
    background: #f3f4f6;
    color: #374151;
}

@media (max-width: 768px) {
    .popup-body {
        gap: 10px;
    }

    .popup-text {
        font-size: 14px;
        line-height: 1.45;
    }

    .popup-label {
        font-size: 13px;
        margin-bottom: 4px;
    }

    .popup-input {
        height: 40px;
        font-size: 14px;
    }

    .popup-actions {
        flex-direction: column;
        gap: 8px;
        margin-top: 6px;
    }

    .popup-primary-btn,
    .popup-secondary-btn {
        width: 100%;
        height: 38px;
        font-size: 14px;
    }
}
</style>