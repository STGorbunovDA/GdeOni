import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from '../pages/auth/LoginPage';
import { RegisterPage } from '../pages/auth/RegisterPage';
import { ForgotPasswordPage } from '../pages/auth/ForgotPasswordPage';
import { ResetPasswordPage } from '../pages/auth/ResetPasswordPage';
import { ConfirmEmailPage } from '../pages/auth/ConfirmEmailPage';
import { TrackedListPage } from '../pages/tracked/TrackedListPage';
import { DeceasedDetailsPage } from '../pages/tracked/DeceasedDetailsPage';
import { EditCoordsPage } from '../pages/tracked/EditCoordsPage';
import { ArchivePage } from '../pages/tracked/ArchivePage';
import { SearchPage } from '../pages/search/SearchPage';
import { NearbyPage } from '../pages/search/NearbyPage';
import { AtGravePage } from '../pages/search/AtGravePage';
import { PreviewPage } from '../pages/search/PreviewPage';
import { ShareImportPage } from '../pages/share/ShareImportPage';
import { RoutePage } from '../pages/route/RoutePage';
import { EventsPage } from '../pages/events/EventsPage';
import { RelativesPage } from '../pages/relatives/RelativesPage';
import { ProfilePage } from '../pages/profile/ProfilePage';
import { ChangePasswordPage } from '../pages/profile/ChangePasswordPage';
import { AdminPage } from '../pages/admin/AdminPage';
import { AdminDeceasedPage } from '../pages/admin/AdminDeceasedPage';
import { AdminDeceasedViewPage } from '../pages/admin/AdminDeceasedViewPage';
import { EditDeceasedPage } from '../pages/admin/EditDeceasedPage';
import { AdminFindDeceasedPage } from '../pages/admin/AdminFindDeceasedPage';
import { AdminInfoPage } from '../pages/admin/AdminInfoPage';
import { AdminDeceasedEditsPage } from '../pages/admin/AdminDeceasedEditsPage';
import { AdminEditsPage } from '../pages/admin/AdminEditsPage';
import { AdminUsersPage } from '../pages/admin/AdminUsersPage';
import { AdminUserDetailsPage } from '../pages/admin/AdminUserDetailsPage';
import { AdminUserTrackedPage } from '../pages/admin/AdminUserTrackedPage';
import { AdminPaymentsPage } from '../pages/admin/AdminPaymentsPage';
import { AdminSupportPage } from '../pages/admin/AdminSupportPage';
import { AdminSupportTicketDetailsPage } from '../pages/admin/AdminSupportTicketDetailsPage';
import { SupportNewPage } from '../pages/support/SupportNewPage';
import { SupportMinePage } from '../pages/support/SupportMinePage';
import { SupportTicketPage } from '../pages/support/SupportTicketPage';
import { SubscriptionPage } from '../pages/subscription/SubscriptionPage';
import { SubscriptionRequiredPage } from '../pages/subscription/SubscriptionRequiredPage';
import { PaymentReturnPage } from '../pages/subscription/PaymentReturnPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { StyleDemoPage } from '../pages/StyleDemoPage';
import { GeoDemoPage } from '../pages/GeoDemoPage';
import { DownloadPage } from '../pages/DownloadPage';
import { LegalPage } from '../pages/legal/LegalPage';
import { ProtectedRoute } from './ProtectedRoute';
import { AdminRoute } from './AdminRoute';
import { SuperAdminRoute } from './SuperAdminRoute';
import { RequireSubscription } from './RequireSubscription';
import { AppLayout } from '../components/layout/AppLayout';
import { AdminLayout } from '../components/layout/AdminLayout';

/**
 * F1 / F2.1 / F17.13 / F22. Корневой роутер.
 *  - Публичные роуты (auth, demo) — без layout, центровка hero.
 *  - Приватные роуты — за ProtectedRoute + AppLayout (sidebar основного
 *    приложения).
 *  - Admin-роуты — за ProtectedRoute + AdminRoute + AdminLayout
 *    (отдельный sidebar админки).
 *  - Paywall-гейт RequireSubscription оборачивает "оплачиваемые"
 *    приватные роуты (карточки, маршрут, архив). Whitelist —
 *    /profile, /change-password, /support/*, /subscription,
 *    /subscription-required, /payment/return — эти маунтятся до
 *    гейта, чтобы юзер без активной подписки мог зайти в профиль,
 *    оформить подписку и обратиться в поддержку.
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
        {/* D43. Восстановление пароля — публичные роуты: юзер по
            определению не может войти. Адрес /reset-password должен
            совпадать с PasswordReset:WebResetUrl в конфиге бэка,
            иначе ссылка из письма приведёт в никуда. */}
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        {/* D45. Подтверждение email по ссылке из письма — публичный роут:
            новый юзер под гейтом ещё не вошёл. Адрес должен совпадать с
            EmailConfirmation:WebConfirmUrl в конфиге бэка. */}
        <Route path="/confirm-email" element={<ConfirmEmailPage />} />
        {/* F27. Публичная страница скачивания APK — доступна анонимам. */}
        <Route path="/download" element={<DownloadPage />} />
        {/* F24 / D19. Публичные legal-документы — доступны до логина,
            ссылки из чекбоксов регистрации ведут сюда. */}
        <Route path="/legal/privacy" element={<LegalPage kind="privacy" />} />
        <Route path="/legal/terms" element={<LegalPage kind="terms" />} />
        {/* F2. Demo дизайн-системы — публичный роут для визуальной проверки. */}
        <Route path="/style-demo" element={<StyleDemoPage />} />
        {/* F5. Demo геолокации — публичный, для ручного теста. */}
        <Route path="/geo-demo" element={<GeoDemoPage />} />

        {/* Приватные: ProtectedRoute → AppLayout → whitelist / gated */}
        <Route element={<ProtectedRoute />}>
          <Route element={<AppLayout />}>
            {/* F22 whitelist: доступно без активной подписки. */}
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/change-password" element={<ChangePasswordPage />} />
            <Route path="/subscription" element={<SubscriptionPage />} />
            <Route
              path="/subscription-required"
              element={<SubscriptionRequiredPage />}
            />
            <Route path="/payment/return" element={<PaymentReturnPage />} />
            <Route path="/support/new" element={<SupportNewPage />} />
            <Route path="/support/mine" element={<SupportMinePage />} />
            <Route path="/support/:id" element={<SupportTicketPage />} />
            {/* D46. Импорт подборки по ссылке/QR. В whitelist (до paywall):
                список показываем и без активной подписки; сам импорт под
                подпиской — 403 subscription.required уведёт на paywall. */}
            <Route path="/s/:code" element={<ShareImportPage />} />

            {/* Функция «Родственники» — в whitelist (API BasicAuthenticated):
                социальная функция связи между близкими, доступна и без
                активной подписки. */}
            <Route path="/relatives" element={<RelativesPage />} />

            {/* F22 gated: требует активную подписку (или admin роль). */}
            <Route element={<RequireSubscription />}>
              <Route path="/" element={<Navigate to="/tracked" replace />} />

              <Route path="/tracked" element={<TrackedListPage />} />
              <Route path="/tracked/archive" element={<ArchivePage />} />
              <Route path="/tracked/:id" element={<DeceasedDetailsPage />} />
              <Route
                path="/tracked/:id/edit-coords"
                element={<EditCoordsPage />}
              />

              <Route path="/search" element={<SearchPage />} />
              {/* F36 / E21. Поиск по GPS «кто рядом» — вход с /tracked
                  и /search, отдельного пункта в sidebar нет. */}
              <Route path="/nearby" element={<NearbyPage />} />
              <Route path="/at-grave" element={<AtGravePage />} />
              <Route path="/preview/:id" element={<PreviewPage />} />

              <Route path="/route" element={<RoutePage />} />
              <Route path="/events" element={<EventsPage />} />
            </Route>
          </Route>

          {/* F17.13. Admin-роуты в отдельном AdminLayout с собственным
              sidebar. AdminRoute проверяет роль (Admin/SuperAdmin) и
              редиректит не-админа на /tracked. На бэке те же эндпоинты
              охраняются [Authorize(Roles="SuperAdmin,Admin")]. */}
          <Route element={<AdminRoute />}>
            <Route element={<AdminLayout />}>
              <Route path="/admin" element={<AdminPage />} />
              <Route path="/admin/deceased" element={<AdminDeceasedPage />} />
              <Route path="/admin/deceased/:id" element={<AdminDeceasedViewPage />} />
              <Route path="/admin/deceased/:id/edit" element={<EditDeceasedPage />} />
              <Route path="/admin/deceased/:id/edits" element={<AdminDeceasedEditsPage />} />
              <Route path="/admin/users" element={<AdminUsersPage />} />
              <Route path="/admin/users/:id" element={<AdminUserDetailsPage />} />
              <Route path="/admin/users/:id/tracked" element={<AdminUserTrackedPage />} />
              <Route path="/admin/payments" element={<AdminPaymentsPage />} />
              <Route path="/admin/edits" element={<AdminEditsPage />} />
              {/* D44. Обращения — только для SuperAdmin: в переписке
                  платёжные реквизиты и договорённости о переводах. */}
              <Route element={<SuperAdminRoute />}>
                <Route
                  path="/admin/support-tickets"
                  element={<AdminSupportPage />}
                />
                <Route
                  path="/admin/support-tickets/:id"
                  element={<AdminSupportTicketDetailsPage />}
                />
              </Route>
              <Route path="/admin/find-deceased" element={<AdminFindDeceasedPage />} />
              {/* F38. Справка по системе: счётчики людей, карточек, денег. */}
              <Route path="/admin/info" element={<AdminInfoPage />} />
            </Route>
          </Route>
        </Route>

        {/* 404 — без layout, отдельная страница */}
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  );
}
