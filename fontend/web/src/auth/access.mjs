export function roleHome(role) {
  return ({ 
    admin: '/admin', 
    manager: '/admin', 
    staff: '/staff', 
    chef: '/chef', 
    customer: '/customer/account' 
  })[role] || '/login';
}

export function routeAccess(to, session) {
  const records = to.matched || [];
  const roleSets = records.map(record => record.meta?.roles).filter(Boolean);
  if (to.meta?.roles && !roleSets.includes(to.meta.roles)) roleSets.push(to.meta.roles);
  const requiresAuth = to.meta?.requiresAuth || roleSets.length > 0;

  // Nếu route yêu cầu đăng nhập mà người dùng chưa đăng nhập
  if (requiresAuth && (!session.token || !session.role)) {
    return { name: 'login', query: { redirect: to.fullPath } };
  }

  // 1. ADMIN CÓ TOÀN QUYỀN - Vào bất kỳ đâu cũng được (Admin, Bếp, Phục Vụ, Trang Khách)
  if (session.role === 'admin') {
    if (to.name === 'login') return roleHome(session.role);
    return true;
  }

  // 2. BẾP (CHEF): Vào được trang Bếp (/chef), trang Phục Vụ (/staff, /staff/app), và trang Người Dùng (/customer, /menu...)
  if (session.role === 'chef') {
    if (to.name === 'login') return roleHome(session.role);
    // Bếp không vào được trang quản trị Admin
    const isAdminRoute = to.matched.some(r => r.path.startsWith('/admin'));
    if (isAdminRoute) return '/chef';
    // Được phép vào /chef, /staff, /staff/app, /customer/*
    return true;
  }

  // 3. NHÂN VIÊN PHỤC VỤ (STAFF): Vào được App Phục Vụ (/staff, /staff/app) và trang Người Dùng (/customer, /menu...)
  if (session.role === 'staff') {
    if (to.name === 'login') return roleHome(session.role);
    const isAdminRoute = to.matched.some(r => r.path.startsWith('/admin'));
    const isChefRoute = to.matched.some(r => r.path === '/chef');
    if (isAdminRoute || isChefRoute) return '/staff';
    // Được phép vào /staff, /staff/app, /customer/*
    return true;
  }

  // 4. MANAGER: Vào được Admin, Staff, Chef, Customer
  if (session.role === 'manager') {
    if (to.name === 'login') return roleHome(session.role);
    return true;
  }

  // 5. KHÁCH HÀNG (CUSTOMER): Chỉ vào được trang khách hàng và trang công khai
  if (roleSets.some(roles => !roles.includes(session.role))) {
    return roleHome(session.role);
  }

  if (to.name === 'login') return session.token && session.role ? roleHome(session.role) : true;
  return true;
}
