import { createRouter, createWebHistory } from 'vue-router'

// 1. Auth Actor
import LoginView from '../views/auth/LoginView.vue'

// 2. Admin Actor (Layout & Features)
import AdminLayoutView from '../views/admin/layout/AdminLayoutView.vue'
import AdminDashboardView from '../views/admin/dashboard/AdminDashboardView.vue'
import CategoryManagementView from '../views/admin/categories/CategoryManagementView.vue'
import FoodManagementView from '../views/admin/foods/FoodManagementView.vue'
import TableManagementView from '../views/admin/tables/TableManagementView.vue'
import UserManagementView from '../views/admin/users/UserManagementView.vue'
import PaymentSettingsView from '../views/admin/payment/PaymentSettingsView.vue'
import ReservationManagementView from '../views/admin/reservations/ReservationManagementView.vue'
import InventoryManagementView from '../views/admin/inventory/InventoryManagementView.vue'
import DailyPortionsView from '../views/admin/inventory/DailyPortionsManagementView.vue'
import StockReceiptsView from '../views/admin/inventory/StockReceiptsView.vue'
import KitchenReconciliationView from '../views/admin/inventory/KitchenReconciliationView.vue'
import ReviewManagementView from '../views/admin/reviews/ReviewManagementView.vue'

// 3. Staff Actor
import StaffWorkspaceView from '../views/staff/StaffWorkspaceView.vue'
import StaffAppView from '../views/staff/StaffAppView.vue'

// 4. Chef Actor
import ChefWorkspaceView from '../views/chef/ChefWorkspaceView.vue'



const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/customer/intro' },
    { path: '/login', name: 'login', component: LoginView },
    { path: '/register', name: 'register', component: () => import('../views/auth/RegisterView.vue'), meta: { public: true } },

    // Direct aliases for convenience
    { path: '/intro', redirect: '/customer/intro' },
    { path: '/area', redirect: '/customer/area' },
    { path: '/menu', redirect: '/customer/menu' },
    { path: '/reservation', redirect: '/customer/reservation' },
    { path: '/contact', redirect: '/customer/contact' },
    { path: '/profile', redirect: '/customer/profile' },
    { path: '/staff-app', redirect: '/staff/app' },

    // Admin / Manager Routes
    {
      path: '/admin',
      component: AdminLayoutView,
      meta: { roles: ['admin', 'manager'] },
      children: [
        { path: '', name: 'admin-dashboard', component: AdminDashboardView },
        { path: 'categories', name: 'admin-categories', component: CategoryManagementView },
        { path: 'foods', name: 'admin-foods', component: FoodManagementView },
        { path: 'tables', name: 'admin-tables', component: TableManagementView },
        { path: 'reservations', name: 'admin-reservations', component: ReservationManagementView },
        { path: 'users', name: 'admin-users', component: UserManagementView },
        { path: 'payment-settings', name: 'admin-payment-settings', component: PaymentSettingsView },
        { path: 'inventory', name: 'admin-inventory', component: InventoryManagementView },
        { path: 'daily-portions', name: 'admin-daily-portions', component: DailyPortionsView },
        { path: 'stock-receipts', name: 'admin-stock-receipts', component: StockReceiptsView },
        { path: 'kitchen-reconciliation', name: 'admin-kitchen-reconciliation', component: KitchenReconciliationView },
        { path: 'reviews', name: 'admin-reviews', component: ReviewManagementView },
      ]
    },

    // Staff / Waiter Route (Desktop POS Desk)
    {
      path: '/staff',
      name: 'staff',
      component: StaffWorkspaceView,
      meta: { roles: ['staff', 'admin', 'manager'] }
    },

    // Staff / Waiter Mobile App Route (Smart POS Handheld App)
    {
      path: '/staff/app',
      name: 'staff-app',
      component: StaffAppView,
      meta: { roles: ['staff', 'admin', 'manager'] }
    },

    // Chef / Kitchen Route
    {
      path: '/chef',
      name: 'chef',
      component: ChefWorkspaceView,
      meta: { roles: ['chef', 'admin', 'manager'] }
    },

    // Customer Public Portal
    {
      path: '/customer',
      component: () => import('../components/CustomerLayout.vue'),
      meta: { public: true },
      children: [
        { path: '', redirect: '/customer/intro' },
        { path: 'intro',        meta: { public: true }, component: () => import('../views/customer/IntroView.vue') },
        { path: 'area',         meta: { public: true }, component: () => import('../views/customer/AreaView.vue') },
        { path: 'menu',         meta: { public: true }, component: () => import('../views/customer/MenuView.vue') },
        { path: 'reservation',  meta: { public: true }, component: () => import('../views/customer/ReservationView.vue') },
        { path: 'contact',      meta: { public: true }, component: () => import('../views/customer/ContactView.vue') },
        { path: 'profile',      meta: { public: true }, component: () => import('../views/customer/ProfileView.vue') },
        { path: 'account',      meta: { requiresAuth: true }, component: () => import('../views/customer/CustomerAccountView.vue') },
      ]
    },
  ]
})

// Authentication & Role-based Navigation Guard (Vue Router 4 return syntax)
router.beforeEach((to) => {
  const token = localStorage.getItem('token');
  const role = (localStorage.getItem('role') || '').toLowerCase();

  // The account dashboard contains private data and must be checked before
  // the public parent layout is considered.
  if (to.meta.requiresAuth && !token) {
    return { name: 'login', query: { redirect: to.fullPath } };
  }

  // 1. Allow public customer routes without authentication
  if (to.meta.public || to.matched.some(r => r.meta?.public)) {
    return true;
  }

  // 2. Handling Login Route
  if (to.name === 'login') {
    if (!token) return true;
    // Already authenticated -> redirect to their role home
    if (role === 'chef') return '/chef';
    if (role === 'staff') return '/staff';
    if (role === 'customer') return '/customer/intro';
    return '/admin';
  }

  // 3. Protected routes require token
  if (!token) {
    return { name: 'login' };
  }

  // 4. Role authorization check
  const requiredRoles = to.meta.roles || to.matched.find(r => r.meta?.roles)?.meta?.roles;
  if (requiredRoles && role && !requiredRoles.includes(role)) {
    if (role === 'chef') return '/chef';
    if (role === 'staff') return '/staff';
    if (role === 'customer') return '/customer/intro';
    return '/admin';
  }

  return true;
});

export default router
