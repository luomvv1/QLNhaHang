<template>
  <main class="customer-register-page d-flex align-items-center justify-content-center p-3">
    <div class="register-shell card-modern shadow-lg overflow-hidden">
      <section class="register-intro p-4 p-md-5 text-white">
        <router-link to="/customer/intro" class="brand-link">HƯƠNG SEN</router-link>
        <span class="register-mark">✦</span>
        <p class="eyebrow-light mt-5 mb-2">Thanh vien Hương Sen</p>
        <h1>Dat ban de dang,<br><span>phuc vu chu dao.</span></h1>
        <p class="intro-copy">Tao tai khoan de quan ly lich dat ban, hoa don va diem tich luy cua ban.</p>
        <div class="intro-points"><span>✓ Luu thong tin dat ban</span><span>✓ Theo doi lich su dung bua</span><span>✓ Tich diem thanh vien</span></div>
      </section>

      <section class="register-form p-4 p-md-5">
        <div class="mb-4"><span class="eyebrow">Tai khoan khach hang</span><h2 class="mb-1">Tao tai khoan</h2><p class="text-muted small mb-0">Thong tin dung de xac nhan dat ban va gui thong bao.</p></div>
        <div v-if="errorMessage" class="alert alert-danger py-2 small">{{ errorMessage }}</div>
        <form @submit.prevent="register">
          <label class="form-label">Ho va ten</label>
          <input v-model.trim="form.fullName" class="form-control mb-3" placeholder="Nguyen Van An" required />
          <label class="form-label">So dien thoai</label>
          <input v-model.trim="form.phone" class="form-control mb-3" type="tel" placeholder="0901 234 567" required />
          <label class="form-label">Email</label>
          <input v-model.trim="form.email" class="form-control mb-3" type="email" placeholder="ban@email.com" required />
          <label class="form-label">Mat khau</label>
          <input v-model="form.password" class="form-control mb-4" type="password" minlength="6" placeholder="It nhat 6 ky tu" required />
          <button class="register-submit w-100" type="submit" :disabled="loading">{{ loading ? 'Dang tao tai khoan...' : 'Tao tai khoan' }}</button>
        </form>
        <p class="text-center small text-muted mt-4 mb-0">Da co tai khoan? <router-link to="/login">Dang nhap ngay</router-link></p>
      </section>
    </div>
  </main>
</template>

<script setup>
import { reactive, ref } from 'vue';
import axios from 'axios';
import { useRouter } from 'vue-router';

const router = useRouter();
const loading = ref(false);
const errorMessage = ref('');
const form = reactive({ fullName: '', phone: '', email: '', password: '' });

async function register() {
  loading.value = true;
  errorMessage.value = '';
  try {
    await axios.post('http://localhost:5079/api/Auth/register', form);
    await axios.post('http://localhost:5079/api/Auth/login', { email: form.email, password: form.password }).then(({ data }) => {
      localStorage.setItem('token', data.token ?? data.Token);
      localStorage.setItem('role', (data.roleCode ?? data.RoleCode ?? 'customer').toLowerCase());
      localStorage.setItem('fullName', data.fullName ?? data.FullName ?? form.fullName);
    });
    router.push('/customer/account');
  } catch (error) {
    errorMessage.value = error.response?.data?.message || 'Khong the tao tai khoan. Vui long thu lai.';
  } finally {
    loading.value = false;
  }
}
</script>

<style scoped>
.customer-register-page { min-height: 100vh; background: radial-gradient(circle at 18% 20%, rgba(214,156,82,.2), transparent 35%), var(--dola-canvas); }
.register-shell { display: grid; grid-template-columns: minmax(260px, .9fr) minmax(320px, 1fr); width: min(900px, 100%); border-radius: 10px; overflow: hidden; }
.register-intro { display: flex; flex-direction: column; justify-content: center; background: linear-gradient(145deg, var(--dola-surface-alt), #0d2824); }
.brand-link { color: var(--dola-primary); font-weight: 800; letter-spacing: .15em; text-decoration: none; }.register-mark { color: var(--dola-primary); font-size: 5rem; line-height: 1; margin-top: 28px; }.eyebrow, .eyebrow-light { color: var(--dola-primary); font-size: .68rem; font-weight: 800; letter-spacing: .14em; text-transform: uppercase; }.eyebrow-light { color: #edc886; }.register-intro h1 { font-size: clamp(2rem, 4vw, 3.3rem); line-height: 1.08; margin-bottom: 18px; }.register-intro h1 span { color: var(--dola-primary); }.intro-copy { color: var(--dola-body); line-height: 1.7; }.intro-points { display: grid; gap: 10px; color: #d7e3dc; font-size: .82rem; margin-top: 24px; }.register-form { background: #fff; }.register-form h2 { color: #18332f; font-weight: 800; }.register-form .form-control { min-height: 45px; border-radius: 6px; border-color: #d7e1dd; }.register-form .form-control:focus { border-color: var(--dola-primary); box-shadow: 0 0 0 3px rgba(214,156,82,.18); }.register-submit { min-height: 46px; border: 0; border-radius: 6px; background: var(--dola-canvas); color: #fff; font-weight: 700; transition: transform .2s ease, background .2s ease; }.register-submit:hover { background: var(--dola-primary); color: #222; transform: translateY(-2px); }.register-submit:disabled { opacity: .6; transform: none; }.register-form a { color: #9b6d2d; font-weight: 700; }
@media (max-width: 700px) { .register-shell { grid-template-columns: 1fr; }.register-intro { padding: 28px !important; }.register-mark { font-size: 3rem; margin-top: 16px; }.register-intro .eyebrow-light { margin-top: 20px !important; } }
</style>
