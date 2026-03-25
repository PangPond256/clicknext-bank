<script setup>
import { ref, watch, onMounted, onBeforeUnmount } from 'vue'
import Login from './components/Login.vue'
import DepositWithdraw from './components/DepositWithdraw.vue'
import Transaction from './components/Transaction.vue'
import { API_BASE_URL } from './config'

const isLoggedIn = ref(false)
const currentPage = ref('deposit')
const isSidebarOpen = ref(false)
const isMobile = ref(false)

const token = ref(sessionStorage.getItem('token') || '')
const userEmail = ref(sessionStorage.getItem('userEmail') || '')

const balance = ref(0)
const transactions = ref([])

const isLoading = ref(false)
const errorMessage = ref('')

const safeJson = async (response) => {
  try {
    return await response.json()
  } catch {
    return null
  }
}

const normalizeTransaction = (item) => ({
  id: item?.id,
  type: String(item?.type ?? item?.status ?? '').toLowerCase(),
  amount: Number(item?.amount ?? 0),
  createdAt:
    item?.createdAt ??
    item?.createAt ??
    item?.datetime ??
    new Date().toISOString(),
  email: item?.email ?? userEmail.value ?? ''
})

const logout = () => {
  isLoggedIn.value = false
  currentPage.value = 'deposit'
  isSidebarOpen.value = false
  token.value = ''
  userEmail.value = ''
  balance.value = 0
  transactions.value = []
  errorMessage.value = ''

  sessionStorage.removeItem('token')
  sessionStorage.removeItem('userEmail')
  sessionStorage.removeItem('isLoggedIn')
  sessionStorage.removeItem('currentPage')
}

const handleUnauthorized = () => {
  logout()
}

const fetchWithAuth = async (url, options = {}) => {
  const response = await fetch(url, {
    ...options,
    headers: {
      ...(options.headers || {}),
      ...(token.value ? { Authorization: `Bearer ${token.value}` } : {})
    }
  })

  if (response.status === 401) {
    handleUnauthorized()
    throw new Error('Unauthorized')
  }

  return response
}

const fetchTransactions = async () => {
  const response = await fetchWithAuth(`${API_BASE_URL}/api/Transactions`)
  if (!response.ok) throw new Error('Failed to fetch transactions')

  const data = await safeJson(response)
  const items = Array.isArray(data) ? data : (data?.transactions || [])
  transactions.value = items.map(normalizeTransaction)
}

const fetchBalance = async () => {
  const response = await fetchWithAuth(`${API_BASE_URL}/api/Transactions/balance`)
  if (!response.ok) throw new Error('Failed to fetch balance')

  const data = await safeJson(response)

  if (typeof data === 'number') {
    balance.value = data
  } else if (typeof data?.balance === 'number') {
    balance.value = data.balance
  } else {
    balance.value = Number(data?.balance ?? 0)
  }
}

const fetchData = async () => {
  if (!token.value) return

  isLoading.value = true
  errorMessage.value = ''

  try {
    await Promise.all([fetchTransactions(), fetchBalance()])
  } catch (error) {
    console.error(error)
    if (error.message !== 'Unauthorized') {
      errorMessage.value = 'Failed to load data from server.'
    }
  } finally {
    isLoading.value = false
  }
}

const updateScreenState = () => {
  isMobile.value = window.innerWidth <= 768

  if (!isMobile.value) {
    isSidebarOpen.value = false
  }
}

const toggleSidebar = () => {
  if (!isMobile.value) return
  isSidebarOpen.value = !isSidebarOpen.value
}

const closeSidebar = () => {
  isSidebarOpen.value = false
}

const goTo = (page) => {
  currentPage.value = page
  sessionStorage.setItem('currentPage', page)

  if (isMobile.value) {
    closeSidebar()
  }
}

const handleLoginSuccess = async ({ token: newToken, email }) => {
  token.value = newToken || ''
  userEmail.value = email || ''
  isLoggedIn.value = true
  errorMessage.value = ''

  sessionStorage.setItem('token', token.value)
  sessionStorage.setItem('userEmail', userEmail.value)
  sessionStorage.setItem('isLoggedIn', 'true')
  sessionStorage.setItem('currentPage', currentPage.value)

  await fetchData()
}

const addTransaction = async (payload) => {
  errorMessage.value = ''

  try {
    const response = await fetchWithAuth(`${API_BASE_URL}/api/Transactions`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        type: String(payload?.type ?? '').toLowerCase(),
        amount: Number(payload?.amount ?? 0)
      })
    })

    const result = await safeJson(response)

    if (!response.ok) {
      errorMessage.value = result?.message || 'Failed to add transaction.'
      return
    }

    await fetchData()
  } catch (error) {
    console.error(error)
    if (error.message !== 'Unauthorized') {
      errorMessage.value = 'Cannot connect to server.'
    }
  }
}

const updateTransaction = async (payload) => {
  errorMessage.value = ''

  try {
    const response = await fetchWithAuth(`${API_BASE_URL}/api/Transactions/${payload.id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        amount: Number(payload?.amount ?? 0)
      })
    })

    const result = await safeJson(response)

    if (!response.ok) {
      errorMessage.value = result?.message || 'Failed to update transaction.'
      return
    }

    await fetchData()
  } catch (error) {
    console.error(error)
    if (error.message !== 'Unauthorized') {
      errorMessage.value = 'Cannot connect to server.'
    }
  }
}

const deleteTransaction = async (payload) => {
  errorMessage.value = ''

  try {
    const response = await fetchWithAuth(`${API_BASE_URL}/api/Transactions/${payload.id}`, {
      method: 'DELETE'
    })

    const result = await safeJson(response)

    if (!response.ok) {
      errorMessage.value = result?.message || 'Failed to delete transaction.'
      return
    }

    await fetchData()
  } catch (error) {
    console.error(error)
    if (error.message !== 'Unauthorized') {
      errorMessage.value = 'Cannot connect to server.'
    }
  }
}

watch(currentPage, (value) => {
  sessionStorage.setItem('currentPage', value)
})

onMounted(async () => {
  updateScreenState()
  window.addEventListener('resize', updateScreenState)

  const savedPage = sessionStorage.getItem('currentPage')
  if (savedPage === 'deposit' || savedPage === 'transaction') {
    currentPage.value = savedPage
  }

  const savedLogin = sessionStorage.getItem('isLoggedIn') === 'true'
  if (savedLogin && token.value) {
    isLoggedIn.value = true
    await fetchData()
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateScreenState)
})
</script>

<template>
  <Login v-if="!isLoggedIn" @login-success="handleLoginSuccess" />

  <div v-else class="app-shell">
    <header class="topbar">
      <div class="topbar-left">
        <button class="icon-btn mobile-toggle-btn" type="button"
          :aria-label="isSidebarOpen ? 'Close menu' : 'Open menu'" @click="toggleSidebar">
          <svg v-if="!isSidebarOpen" class="menu-icon" viewBox="0 0 24 24" aria-hidden="true">
            <path d="M4 7H20M4 12H20M4 17H20" fill="none" stroke="currentColor" stroke-linecap="round"
              stroke-width="2" />
          </svg>

          <svg v-else class="menu-icon" viewBox="0 0 24 24" aria-hidden="true">
            <path d="M6 6L18 18M18 6L6 18" fill="none" stroke="currentColor" stroke-linecap="round" stroke-width="2" />
          </svg>
        </button>

        <div class="brand">Clicknext</div>
      </div>

      <button class="logout-btn" type="button" @click="logout">Logout</button>
    </header>

    <div class="main-layout">
      <aside class="sidebar desktop-sidebar">
        <nav class="menu-list">
          <button type="button" class="menu-button" :class="{ active: currentPage === 'deposit' }"
            @click="goTo('deposit')">
            Deposit / Withdraw
          </button>

          <button type="button" class="menu-button" :class="{ active: currentPage === 'transaction' }"
            @click="goTo('transaction')">
            Transaction
          </button>
        </nav>
      </aside>

      <div v-if="isMobile && isSidebarOpen" class="mobile-backdrop" @click="closeSidebar"></div>

      <aside class="mobile-drawer" :class="{ open: isSidebarOpen }">
        <div class="mobile-drawer-panel">
          <nav class="mobile-drawer-nav">
            <button type="button" class="mobile-drawer-item" :class="{ active: currentPage === 'deposit' }"
              @click="goTo('deposit')">
              Deposit / Withdraw
            </button>

            <button type="button" class="mobile-drawer-item" :class="{ active: currentPage === 'transaction' }"
              @click="goTo('transaction')">
              Transaction
            </button>
          </nav>

          <button type="button" class="mobile-close-link" @click="closeSidebar">
            Close
          </button>
        </div>
      </aside>

      <main class="content-area">
        <div class="content-wrapper">
          <div v-if="isLoading" class="status-box">Loading...</div>

          <div v-else-if="errorMessage" class="status-box error-box">
            {{ errorMessage }}
          </div>

          <DepositWithdraw v-else-if="currentPage === 'deposit'" :balance="balance" @add-transaction="addTransaction" />

          <Transaction v-else :transactions="transactions" @update-transaction="updateTransaction"
            @delete-transaction="deleteTransaction" />
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
button {
  font-family: inherit;
}

.app-shell {
  width: 100%;
  min-height: 100vh;
  margin: 0;
  background: #f7f7f7;
  border: 1px solid #7d7d7d;
  box-sizing: border-box;
  overflow-x: hidden;
}

.topbar {
  height: 82px;
  border-bottom: 1px solid #9f9f9f;
  background: #f6f6f6;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 28px;
  box-sizing: border-box;
}

.topbar-left {
  display: flex;
  align-items: center;
  gap: 14px;
}

.brand {
  font-size: 28px;
  font-weight: 700;
  color: #313744;
}

.icon-btn {
  border: none;
  background: transparent;
  padding: 0;
  cursor: pointer;
}

.mobile-toggle-btn {
  display: none;
  width: 34px;
  height: 34px;
  align-items: center;
  justify-content: center;
}

.menu-icon {
  width: 24px;
  height: 24px;
  color: #313744;
}

.logout-btn {
  min-width: 110px;
  height: 42px;
  border: none;
  border-radius: 6px;
  background: #313744;
  color: #fff;
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
}

.main-layout {
  display: flex;
  min-height: calc(100vh - 82px);
}

.sidebar {
  width: 260px;
  border-right: 1px solid #9f9f9f;
  background: #f6f6f6;
  box-sizing: border-box;
  padding: 38px 20px;
}

.menu-list {
  display: flex;
  flex-direction: column;
  gap: 22px;
}

.menu-button {
  width: 100%;
  min-height: 66px;
  padding: 14px 16px;
  border: 1px solid transparent;
  border-radius: 4px;
  background: transparent;
  color: #2f2f2f;
  font-size: 21px;
  font-weight: 500;
  text-align: left;
  line-height: 1.25;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;
  word-break: break-word;
}

.menu-button:hover {
  background: #e5e5e5;
}

.menu-button.active {
  background: #4b4f58;
  color: #ffffff;
  font-weight: 700;
}

.content-area {
  flex: 1;
  min-width: 0;
  padding: 60px 80px;
  background: #f7f7f7;
  box-sizing: border-box;
}

.content-wrapper {
  width: 100%;
  margin: 0 auto;
}

.status-box {
  padding: 18px 20px;
  border: 1px solid #d5d5d5;
  background: #ffffff;
  color: #444444;
  font-size: 20px;
}

.error-box {
  color: #c0392b;
  border-color: #e4b2ab;
  background: #fff6f5;
}

.mobile-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.12);
  z-index: 20;
}

.mobile-drawer {
  position: fixed;
  top: 58px;
  left: 0;
  width: 100%;
  height: calc(100vh - 58px);
  z-index: 30;
  pointer-events: none;
}

.mobile-drawer-panel {
  width: 280px;
  max-width: 82%;
  height: 100%;
  background: #f5f5f5;
  border-right: 1px solid #d8d8d8;
  transform: translateX(-100%);
  transition: transform 0.25s ease;
  padding: 28px 18px 20px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  box-sizing: border-box;
}

.mobile-drawer.open {
  pointer-events: auto;
}

.mobile-drawer.open .mobile-drawer-panel {
  transform: translateX(0);
}

.mobile-drawer-nav {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.mobile-drawer-item {
  width: 100%;
  min-height: 50px;
  padding: 10px 14px;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: #2f2f2f;
  font-size: 16px;
  font-weight: 500;
  text-align: left;
  line-height: 1.25;
  cursor: pointer;
}

.mobile-drawer-item.active {
  background: #4b4f58;
  color: #ffffff;
  font-weight: 700;
}

.mobile-close-link {
  align-self: center;
  margin-bottom: 10px;
  border: none;
  background: transparent;
  color: #444444;
  font-size: 18px;
  cursor: pointer;
}

@media (max-width: 768px) {
  .app-shell {
    width: 100%;
    min-height: 100vh;
    border: none;
  }

  .desktop-sidebar {
    display: none;
  }

  .topbar {
    height: 58px;
    padding: 0 12px;
  }

  .topbar-left {
    gap: 10px;
  }

  .brand {
    font-size: 15px;
  }

  .icon-btn.mobile-toggle-btn {
    display: inline-flex;
  }

  .logout-btn {
    min-width: 58px;
    height: 26px;
    font-size: 11px;
    padding: 0 10px;
  }

  .content-area {
    padding: 18px 12px 24px;
    background: #fafafa;
  }

  .content-wrapper {
    width: 100%;
    max-width: none;
    margin: 0;
    border: none;
    background: transparent;
    min-height: auto;
    padding: 0;
    box-sizing: border-box;
  }

  .mobile-drawer {
    top: 58px;
    height: calc(100vh - 58px);
  }
}

@media (min-width: 769px) {

  .mobile-drawer,
  .mobile-backdrop {
    display: none;
  }
}
</style>