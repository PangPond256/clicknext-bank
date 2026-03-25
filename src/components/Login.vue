<script setup>
import { ref, onMounted } from 'vue'
import PopupModal from './PopupModal.vue'
import { API_BASE_URL } from '../config'

onMounted(() => {
    document.title = 'Login | Clicknext Bank'
})

const emit = defineEmits(['login-success'])

const email = ref('')
const password = ref('')

const showPopup = ref(false)
const popupTitle = ref('')
const popupMessage = ref('')
const isLoading = ref(false)

const openPopup = (title, message) => {
    popupTitle.value = title
    popupMessage.value = message
    showPopup.value = true
}

const closePopup = () => {
    showPopup.value = false
}

const login = async () => {
    if (!email.value.trim() || !password.value.trim()) {
        openPopup('ข้อผิดพลาดในการตรวจสอบ', 'กรุณากรอกข้อมูลให้ครบทุกช่อง')
        return
    }

    isLoading.value = true

    try {
        const response = await fetch(`${API_BASE_URL}/api/Auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                email: email.value.trim(),
                password: password.value
            })
        })

        const data = await response.json().catch(() => null)

        if (!response.ok) {
            openPopup(
                'เข้าสู่ระบบไม่สำเร็จ',
                data?.message || 'อีเมลหรือรหัสผ่านไม่ถูกต้อง'
            )
            return
        }

        sessionStorage.setItem('token', data?.token || '')
        sessionStorage.setItem('isLoggedIn', 'true')
        sessionStorage.setItem('userEmail', data?.email || email.value.trim())

        emit('login-success', {
            token: data?.token || '',
            email: data?.email || email.value.trim()
        })
    } catch (error) {
        console.error(error)
        openPopup('เชื่อมต่อไม่สำเร็จ', 'ไม่สามารถเชื่อมต่อกับเซิร์ฟเวอร์ได้')
    } finally {
        isLoading.value = false
    }
}
</script>

<template>
    <div class="login-page">
        <form class="login-form" @submit.prevent="login">
            <label class="form-label">Email *</label>
            <input v-model="email" class="form-input" type="text" placeholder="Email" autocomplete="username" />

            <label class="form-label password-label">Password *</label>
            <input v-model="password" class="form-input" type="password" placeholder="Password"
                autocomplete="current-password" />

            <button class="login-button" type="submit" :disabled="isLoading">
                {{ isLoading ? 'กำลังเข้าสู่ระบบ...' : 'Login' }}
            </button>
        </form>

        <PopupModal :show="showPopup" :title="popupTitle" :message="popupMessage" @close="closePopup" />
    </div>
</template>

<style scoped>
button,
input {
    font-family: inherit;
}

.login-page {
    min-height: 100vh;
    background: #f1f1f1;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 24px;
}

.login-form {
    width: 100%;
    max-width: 320px;
}

.form-label {
    display: block;
    margin-bottom: 10px;
    color: #2f2f2f;
    font-size: 18px;
    font-weight: 600;
    line-height: 1.3;
}

.password-label {
    margin-top: 24px;
}

.form-input {
    width: 100%;
    height: 52px;
    border: 1px solid #bdbdbd;
    border-radius: 6px;
    background: #fff;
    padding: 0 16px;
    color: #333;
    font-size: 18px;
    outline: none;
    box-sizing: border-box;
}

.form-input::placeholder {
    color: #9aa0a6;
}

.form-input:focus {
    border-color: #8f8f8f;
}

.login-button {
    width: 100%;
    height: 52px;
    margin-top: 38px;
    border: none;
    border-radius: 6px;
    background: #313744;
    color: #fff;
    font-size: 22px;
    font-weight: 700;
    cursor: pointer;
}

.login-button:disabled {
    opacity: 0.7;
    cursor: not-allowed;
}
</style>