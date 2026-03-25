<script setup>
defineProps({
    show: {
        type: Boolean,
        default: false
    },
    title: {
        type: String,
        default: ''
    },
    message: {
        type: String,
        default: ''
    }
})

defineEmits(['close'])
</script>

<template>
    <div v-if="show" class="modal-overlay" @click.self="$emit('close')">
        <div class="modal-box">
            <div class="modal-title">{{ title }}</div>

            <div v-if="message" class="modal-message">
                {{ message }}
            </div>

            <slot v-else />

            <button v-if="message" class="modal-ok-btn" type="button" @click="$emit('close')">
                ตกลง
            </button>
        </div>
    </div>
</template>

<style scoped>
.modal-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.28);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 16px;
    z-index: 1000;
    box-sizing: border-box;
}

.modal-box {
    width: 100%;
    max-width: 430px;
    background: #ffffff;
    border-radius: 8px;
    box-shadow: 0 12px 30px rgba(0, 0, 0, 0.18);
    padding: 22px 18px 18px;
    box-sizing: border-box;
}

.modal-title {
    font-size: 18px;
    font-weight: 700;
    color: #2f2f2f;
    margin-bottom: 14px;
    line-height: 1.4;
}

.modal-message {
    font-size: 15px;
    color: #444444;
    line-height: 1.6;
    margin-bottom: 16px;
}

.modal-ok-btn {
    width: 100%;
    height: 42px;
    border: none;
    border-radius: 8px;
    background: #566173;
    color: #ffffff;
    font-size: 14px;
    font-weight: 700;
    cursor: pointer;
}

@media (max-width: 768px) {
    .modal-overlay {
        padding: 8px;
        align-items: center;
    }

    .modal-box {
        max-width: 92vw;
        border-radius: 6px;
        padding: 14px 14px 14px;
    }

    .modal-title {
        font-size: 15px;
        margin-bottom: 10px;
        line-height: 1.35;
    }

    .modal-message {
        font-size: 14px;
        margin-bottom: 12px;
        line-height: 1.5;
    }

    .modal-ok-btn {
        height: 38px;
        font-size: 14px;
    }
}
</style>