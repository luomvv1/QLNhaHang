import { createRouter, createWebHistory } from 'vue-router'
import { routeAccess } from '../auth/access.mjs'
import { validatedSession } from '../auth/session'

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
    { path: '/news', redirect: '/customer/news' },
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
        { path: 'reports', name: 'admin-reports', component: () => import('../views/admin/reports/AdminReportsView.vue') },
        { path: 'categories', name: 'admin-categories', component: CategoryManagementView },
        { path: 'foods', name: 'admin-foods', component: FoodManagementView },
        { path: 'tables', name: 'admin-tables', component: TableManagementView },
        { path: 'reservations', name: 'admin-reservations', component: ReservationManagementView },
        { path: 'users', name: 'admin-users', component: UserManagementView, meta: { roles: ['admin'] } },
        { path: 'payment-settings', name: 'admin-payment-settings', component: PaymentSettingsView, meta: { roles: ['admin'] } },
        { path: 'inventory', name: 'admin-inventory', component: InventoryManagementView },
        { path: 'daily-portions', name: 'admin-daily-portions', component: DailyPortionsView },
        { path: 'stock-receipts', name: 'admin-stock-receipts', component: StockReceiptsView },
        { path: 'kitchen-reconciliation', name: 'admin-kitchen-reconciliation', component: KitchenReconciliationView },
        { path: 'kitchen-decisions', name: 'admin-kitchen-decisions', component: () => import('../views/admin/kitchen/KitchenDecisionsHistoryView.vue') },
        { path: 'audit-logs', name: 'admin-audit-logs', component: () => import('../views/admin/audit/RestaurantAuditLogsView.vue') },
        { path: 'reviews', name: 'admin-reviews', component: ReviewManagementView },
        { path: 'crm-promotions', name: 'admin-crm-promotions', component: () => import('../views/admin/crm/CustomerPromotionManagementView.vue') },
      ]
    },

    // Staff / Waiter Route (Desktop POS Desk)
    {
      path: '/staff',
      name: 'staff',
      component: StaffWorkspaceView,
      meta: { roles: ['staff', 'chef', 'admin', 'manager'] }
    },

    // Staff / Waiter Mobile App Route (Smart POS Handheld App)
    {
      path: '/staff/app',
      name: 'staff-app',
      component: StaffAppView,
      meta: { roles: ['staff', 'chef', 'admin', 'manager'] }
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
        { path: 'news',         meta: { public: true }, component: () => import('../views/customer/NewsView.vue') },
        { path: 'reservation',  meta: { public: true }, component: () => import('../views/customer/ReservationView.vue') },
        { path: 'contact',      meta: { public: true }, component: () => import('../views/customer/ContactView.vue') },
        { path: 'profile',      meta: { requiresAuth: true, roles: ['customer', 'staff', 'chef', 'admin', 'manager'] }, component: () => import('../views/customer/ProfileView.vue') },
        { path: 'account',      meta: { requiresAuth: true, roles: ['customer', 'staff', 'chef', 'admin', 'manager'] }, component: () => import('../views/customer/CustomerAccountView.vue') },
      ]
    },
  ]
})

// Authentication & Role-based Navigation Guard (Vue Router 4 return syntax)
router.beforeEach(async (to) => {
  const protectedRoute = to.meta.requiresAuth || to.matched.some(record => record.meta?.roles);
  if (!protectedRoute && to.name !== 'login') return true;
  return routeAccess(to, await validatedSession());
});

export default router
