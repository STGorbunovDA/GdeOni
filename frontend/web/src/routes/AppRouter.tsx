import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from '../pages/auth/LoginPage';
import { RegisterPage } from '../pages/auth/RegisterPage';
import { TrackedListPage } from '../pages/tracked/TrackedListPage';
import { DeceasedDetailsPage } from '../pages/tracked/DeceasedDetailsPage';
import { ArchivePage } from '../pages/tracked/ArchivePage';
import { SearchPage } from '../pages/search/SearchPage';
import { AtGravePage } from '../pages/search/AtGravePage';
import { PreviewPage } from '../pages/search/PreviewPage';
import { RoutePage } from '../pages/route/RoutePage';
import { ProfilePage } from '../pages/profile/ProfilePage';
import { ChangePasswordPage } from '../pages/profile/ChangePasswordPage';
import { AdminPage } from '../pages/admin/AdminPage';
import { AdminAllPage } from '../pages/admin/AdminAllPage';
import { AdminUsersPage } from '../pages/admin/AdminUsersPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { StyleDemoPage } from '../pages/StyleDemoPage';
import { ProtectedRoute } from './ProtectedRoute';
import { AppLayout } from '../components/layout/AppLayout';

/**
 * F1 / F2.1. Корневой роутер.
 *  - Публичные роуты (auth, demo) — без layout, центровка hero.
 *  - Приватные роуты — за ProtectedRoute + AppLayout (sidebar).
 *
 * Порядок /tracked/archive ПЕРЕД /tracked/:id принципиален:
 * React Router 6 разрешает первый подходящий маршрут, и без этого
 * 'archive' попадал бы в :id как невалидный GUID.
 */
export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Публичные */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        {/* F2. Demo дизайн-системы — публичный роут для визуальной проверки. */}
        <Route path="/style-demo" element={<StyleDemoPage />} />

        {/* Приватные: ProtectedRoute → AppLayout → конкретная страница */}
        <Route element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>
            <Route path="/" element={<Navigate to="/tracked" replace />} />

            <Route path="/tracked" element={<TrackedListPage />} />
            <Route path="/tracked/archive" element={<ArchivePage />} />
            <Route path="/tracked/:id" element={<DeceasedDetailsPage />} />

            <Route path="/search" element={<SearchPage />} />
            <Route path="/at-grave" element={<AtGravePage />} />
            <Route path="/preview/:id" element={<PreviewPage />} />

            <Route path="/route" element={<RoutePage />} />

            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/change-password" element={<ChangePasswordPage />} />

            {/* F1 / F2.1: проверка роли на UI только в AppLayout (sidebar
                не показывает пункт). Защита роута в коде — F17.
                Сейчас юзер может зайти по прямому URL — это OK. */}
            <Route path="/admin" element={<AdminPage />} />
            <Route path="/admin/all" element={<AdminAllPage />} />
            <Route path="/admin/users" element={<AdminUsersPage />} />
          </Route>
        </Route>

        {/* 404 — без layout, отдельная страница */}
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  );
}
