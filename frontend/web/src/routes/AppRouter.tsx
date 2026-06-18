import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from '../pages/auth/LoginPage';
import { RegisterPage } from '../pages/auth/RegisterPage';
import { TrackedListPage } from '../pages/tracked/TrackedListPage';
import { DeceasedDetailsPage } from '../pages/tracked/DeceasedDetailsPage';
import { EditCoordsPage } from '../pages/tracked/EditCoordsPage';
import { ArchivePage } from '../pages/tracked/ArchivePage';
import { SearchPage } from '../pages/search/SearchPage';
import { AtGravePage } from '../pages/search/AtGravePage';
import { PreviewPage } from '../pages/search/PreviewPage';
import { RoutePage } from '../pages/route/RoutePage';
import { ProfilePage } from '../pages/profile/ProfilePage';
import { ChangePasswordPage } from '../pages/profile/ChangePasswordPage';
import { AdminPage } from '../pages/admin/AdminPage';
import { AdminEditsPage } from '../pages/admin/AdminEditsPage';
import { AdminUsersPage } from '../pages/admin/AdminUsersPage';
import { AdminPaymentsPage } from '../pages/admin/AdminPaymentsPage';
import { AdminSupportPage } from '../pages/admin/AdminSupportPage';
import { AdminFindDeceasedPage } from '../pages/admin/AdminFindDeceasedPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { StyleDemoPage } from '../pages/StyleDemoPage';
import { GeoDemoPage } from '../pages/GeoDemoPage';
import { ProtectedRoute } from './ProtectedRoute';
import { AdminRoute } from './AdminRoute';
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
        {/* F5. Demo геолокации — публичный, для ручного теста. */}
        <Route path="/geo-demo" element={<GeoDemoPage />} />

        {/* Приватные: ProtectedRoute → AppLayout → конкретная страница */}
        <Route element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>
            <Route path="/" element={<Navigate to="/tracked" replace />} />

            <Route path="/tracked" element={<TrackedListPage />} />
            <Route path="/tracked/archive" element={<ArchivePage />} />
            <Route path="/tracked/:id" element={<DeceasedDetailsPage />} />
            <Route path="/tracked/:id/edit-coords" element={<EditCoordsPage />} />

            <Route path="/search" element={<SearchPage />} />
            <Route path="/at-grave" element={<AtGravePage />} />
            <Route path="/preview/:id" element={<PreviewPage />} />

            <Route path="/route" element={<RoutePage />} />

            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/change-password" element={<ChangePasswordPage />} />

            {/* F4. Admin-роуты защищены AdminRoute — не-админа редиректит
                на /tracked. На бэке те же эндпоинты дополнительно охраняются
                [Authorize(Roles="SuperAdmin,Admin")] на контроллерах.
                Структура и названия — строго как в mobile AdminPage. */}
            <Route element={<AdminRoute />}>
              <Route path="/admin" element={<AdminPage />} />
              <Route path="/admin/edits" element={<AdminEditsPage />} />
              <Route path="/admin/users" element={<AdminUsersPage />} />
              <Route path="/admin/payments" element={<AdminPaymentsPage />} />
              <Route path="/admin/support-tickets" element={<AdminSupportPage />} />
              <Route path="/admin/find-deceased" element={<AdminFindDeceasedPage />} />
            </Route>
          </Route>
        </Route>

        {/* 404 — без layout, отдельная страница */}
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  );
}
