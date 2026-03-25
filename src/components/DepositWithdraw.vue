<script setup>
import { ref } from 'vue'
import { onMounted } from 'vue'
import PopupModal from './PopupModal.vue'

onMounted(() => {
    document.title = 'Deposit/Withdraw | Clicknext Bank'
})

const props = defineProps({
    balance: {
        type: Number,
        default: 0
    }
})

const emit = defineEmits(['add-transaction'])

const amount = ref('')
const selectedType = ref('')

const showConfirmModal = ref(false)
const showErrorModal = ref(false)

const errorTitle = ref('')
const errorMessage = ref('')

const sanitizeAmount = (event) => {
    amount.value = String(event.target.value || '').replace(/\D/g, '')
}

const openErrorModal = (title, message) => {
    errorTitle.value = title
    errorMessage.value = message
    showErrorModal.value = true
}

const closeErrorModal = () => {
    showErrorModal.value = false
}

const openConfirm = (type) => {
    const value = Number(amount.value)

    if (!amount.value || Number.isNaN(value) || value <= 0) {
        openErrorModal('ข้อมูลไม่ถูกต้อง', 'กรุณากรอกจำนวนเงินที่ถูกต้อง')
        return
    }

    if (value < 1 || value > 100000) {
        openErrorModal('ข้อมูลไม่ถูกต้อง', 'จำนวนเงินต้องอยู่ระหว่าง 1 ถึง 100000 บาท')
        return
    }

    if (type === 'withdraw' && value > Number(props.balance || 0)) {
        openErrorModal('ยอดเงินไม่เพียงพอ', 'จำนวนเงินถอนไม่สามารถมากกว่ายอดคงเหลือได้')
        return
    }

    selectedType.value = type
    showConfirmModal.value = true
}

const closeConfirmModal = () => {
    showConfirmModal.value = false
}

const confirmTransaction = () => {
    emit('add-transaction', {
        type: selectedType.value,
        amount: Number(amount.value)
    })

    amount.value = ''
    selectedType.value = ''
    showConfirmModal.value = false
}
</script>

<template>
    <div class="deposit-page">
        <div class="deposit-container">
            <h1 class="balance-text">
                จำนวนเงินคงเหลือ
                <br class="mobile-balance-break" />
                {{ Number(balance || 0).toLocaleString() }} บาท
            </h1>

            <div class="form-block">
                <label class="amount-label">จำนวนเงิน *</label>

                <input :value="amount" class="amount-input" type="text" inputmode="numeric" placeholder="กรอกจำนวนเงิน"
                    @input="sanitizeAmount" />

                <div class="action-buttons">
                    <button class="deposit-btn" type="button" @click="openConfirm('deposit')">
                        ฝาก
                    </button>

                    <button class="withdraw-btn" type="button" @click="openConfirm('withdraw')">
                        ถอน
                    </button>
                </div>
            </div>
        </div>

        <PopupModal :show="showConfirmModal" title="ยืนยันการฝาก-ถอน" @close="closeConfirmModal">
            <div class="popup-body">
                <div class="popup-text">
                    จำนวนเงิน {{ Number(amount || 0).toLocaleString() }} บาท
                </div>

                <div class="popup-actions">
                    <button class="popup-primary-btn" type="button" @click="confirmTransaction">
                        ยืนยัน
                    </button>
                    <button class="popup-secondary-btn" type="button" @click="closeConfirmModal">
                        ยกเลิก
                    </button>
                </div>
            </div>
        </PopupModal>

        <PopupModal :show="showErrorModal" :title="errorTitle" :message="errorMessage" @close="closeErrorModal" />
    </div>
</template>

<style scoped>
.deposit-page {
    width: 100%;
    display: flex;
    justify-content: center;
    padding: 72px 24px 32px;
    box-sizing: border-box;
}

.deposit-container {
    width: 100%;
    max-width: 420px;
}

.balance-text {
    margin: 0 0 34px 0;
    text-align: center;
    font-size: 20px;
    font-weight: 700;
    line-height: 1.45;
    color: #222222;
}

.mobile-balance-break {
    display: none;
}

.form-block {
    width: 100%;
}

.amount-label {
    display: block;
    font-size: 14px;
    font-weight: 600;
    color: #222222;
    margin-bottom: 10px;
    text-align: left;
}

.amount-input {
    width: 100%;
    height: 48px;
    box-sizing: border-box;
    border: 1px solid #bcc3ca;
    border-radius: 10px;
    background: #ffffff;
    padding: 0 16px;
    font-size: 15px;
    color: #222222;
}

.amount-input::placeholder {
    color: #9aa1a9;
    font-weight: 600;
}

.amount-input:focus {
    outline: none;
    border-color: #98a1aa;
}

.action-buttons {
    display: flex;
    justify-content: center;
    gap: 18px;
    margin-top: 24px;
}

.deposit-btn,
.withdraw-btn {
    flex: 1;
    height: 46px;
    border: none;
    border-radius: 8px;
    color: #ffffff;
    font-size: 15px;
    font-weight: 700;
    cursor: pointer;
}

.deposit-btn {
    background: #35a94b;
}

.withdraw-btn {
    background: #e32545;
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