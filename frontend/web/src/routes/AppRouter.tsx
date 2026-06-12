import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from '../pages/auth/LoginPage';
import { RegisterPage } from '../pages/auth/RegisterPage';
import { TrackedListPage } from '../pages/tracked/TrackedListPage';
import { DeceasedDetailsPage } from '../pages/tracked/DeceasedDetailsPage';
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
import { ProtectedRoute } from './ProtectedRoute';

/**
 * F1. Корневой роутер. Публичные маршруты (auth) — снаружи
 * ProtectedRoute, остальные — внутри. В F2.1 здесь появится
 * AppLayout с sidebar'ом для всех приватных маршрутов.
 */
export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Публичные */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        {/* Приватные */}
        <Route element={<ProtectedRoute />}>
          <Route path="/" element={<Navigate to="/tracked" replace />} />

          <Route path="/tracked" element={<TrackedListPage />} />
          <Route path="/tracked/:id" element={<DeceasedDetailsPage />} />

          <Route path="/search" element={<SearchPage />} />
          <Route path="/at-grave" element={<AtGravePage />} />
          <Route path="/preview/:id" element={<PreviewPage />} />

          <Route path="/route" element={<RoutePage />} />

          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/change-password" element={<ChangePasswordPage />} />

          {/* В F1 проверки роли нет — это придёт в F17.
              Сейчас любой залогиненный увидит /admin. */}
          <Route path="/admin" element={<AdminPage />} />
          <Route path="/admin/all" element={<AdminAllPage />} />
          <Route path="/admin/users" element={<AdminUsersPage />} />
        </Route>

        {/* 404 */}
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  );
}
