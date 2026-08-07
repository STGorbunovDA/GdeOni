import { useState } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Button,
  Container,
  Group,
  SimpleGrid,
  Stack,
  Text,
  Title,
} from '@mantine/core';
import { Cloud, Heart, MapPin, Search, Users } from 'lucide-react';
import { ThemeToggle } from '../../components/ui';
import { cloudColors } from '../../design/theme';
import { useIsAuthenticated } from '../../auth/authStore';
import { appApi } from '../../api/endpoints/appApi';
import styles from './LandingPage.module.css';

/** Русский формат чисел: 12400 → «12 400» (с неразрывным пробелом). */
const numberFormat = new Intl.NumberFormat('ru-RU');

/**
 * F40. Стартовая (публичная) страница «Ясное небо».
 *
 * Показывается ТОЛЬКО гостю: залогиненного сразу отправляем в его
 * рабочую область /tracked (раньше на / стоял этот редирект). Настоящий
 * поиск и «у могилы» — под авторизацией, поэтому кнопки героя ведут на
 * /search и /at-grave: гостя ProtectedRoute уведёт на вход, после
 * которого он попадёт на нужную функцию.
 *
 * Тема переключается штатным ThemeToggle справа от логотипа; все
 * поверхности покрашены токенами --cloud-* (styles.css), поэтому тёмная
 * тема перекрашивает лендинг сама.
 */
export function LandingPage() {
  const isAuthenticated = useIsAuthenticated();
  const navigate = useNavigate();
  const [query, setQuery] = useState('');

  // Живые счётчики для героя (пользователи / карточки памяти). Публичный
  // endpoint, для гостя. staleTime 60 с — столько же кешируется на бэке.
  const statsQuery = useQuery({
    queryKey: ['app-stats'],
    queryFn: appApi.stats,
    enabled: !isAuthenticated,
    staleTime: 60_000,
  });

  // Гость видит лендинг; вошедший пользователь — сразу в приложение.
  if (isAuthenticated) {
    return <Navigate to="/tracked" replace />;
  }

  // Единый источник — реальный поиск. Аноним по пути пройдёт /login.
  const goSearch = () => navigate('/search');
  const goAtGrave = () => navigate('/at-grave');
  const scrollToHow = () =>
    document.getElementById('how')?.scrollIntoView({
      behavior: 'smooth',
      block: 'start',
    });

  return (
    <Box className={styles.page}>
      {/* Декоративные облака — чисто визуальный слой под контентом. */}
      <Box className={styles.clouds} aria-hidden>
        <span className={styles.cloud} style={{ width: 200, height: 66, left: 110, top: 170 }} />
        <span className={styles.cloud} style={{ width: 150, height: 52, right: 150, top: 130 }} />
        <span className={styles.cloud} style={{ width: 240, height: 78, right: 60, top: 470 }} />
        <span className={styles.cloud} style={{ width: 170, height: 58, left: 70, top: 520 }} />
      </Box>

      <Box className={styles.content}>
        {/* ── Шапка ── */}
        <Box component="header" className={styles.header}>
          {/* Логотип + название + переключатель темы (по просьбе — сразу
              справа от «ГдеОни»). */}
          <div className={styles.brand}>
            <Cloud size={30} color={cloudColors.azure} strokeWidth={2.2} />
            <Title
              order={3}
              c={cloudColors.inkBlue}
              style={{ fontSize: 22, fontWeight: 800, letterSpacing: '0.2px' }}
            >
              ГдеОни
            </Title>
            <ThemeToggle size="md" />
          </div>

          {/* На мобиле этот блок переносится под логотип (см. media в
              LandingPage.module.css) — кнопки перестают уезжать за край. */}
          <div className={styles.headerRight}>
            <span
              className={`${styles.navLink} ${styles.navLinksDesktop}`}
              onClick={scrollToHow}
              style={{ cursor: 'pointer' }}
            >
              Как это работает
            </span>
            <Link
              to="/download"
              className={`${styles.navLink} ${styles.navLinksDesktop}`}
            >
              Приложение
            </Link>
            <Button
              component={Link}
              to="/login"
              variant="default"
              radius={50}
              fw={600}
            >
              Войти
            </Button>
            <Button component={Link} to="/register" radius={50} fw={600}>
              Регистрация
            </Button>
          </div>
        </Box>

        {/* ── Герой ── */}
        <Container
          size="lg"
          px="md"
          pt={{ base: 40, md: 68 }}
          pb={40}
          ta="center"
        >
          <span className={styles.eyebrow}>
            🕊️ Каталог мест памяти с GPS-координатами
          </span>

          <Title
            order={1}
            mt="xl"
            mb="md"
            c={cloudColors.inkBlue}
            style={{
              fontSize: 'clamp(34px, 6vw, 58px)',
              lineHeight: 1.1,
              fontWeight: 800,
              letterSpacing: '-0.5px',
            }}
          >
            Место памяти —
            <br />
            <span style={{ color: cloudColors.azure }}>всегда рядом</span>
          </Title>

          <Text
            maw={620}
            mx="auto"
            c={cloudColors.text}
            style={{ fontSize: 'clamp(16px, 2.4vw, 19px)', lineHeight: 1.55 }}
          >
            Найдите близкого по имени или координатам, поделитесь местом и сохраните семейную память в одном месте.
          </Text>

          {/* Поиск. Текст в поле — визуальный хук: настоящий поиск у сайта
              многополевой, поэтому просто ведём на /search (через вход). */}
          <Box mt={40}>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                goSearch();
              }}
            >
              <div className={styles.searchWrap}>
                <Search
                  size={24}
                  color={cloudColors.captionGray}
                  style={{ flex: 'none' }}
                />
                <input
                  className={styles.searchInput}
                  placeholder="Имя, город или название кладбища"
                  aria-label="Поиск места памяти"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                />
                <Button type="submit" radius={12} size="md" fw={700} px={30}>
                  Найти
                </Button>
              </div>
            </form>
          </Box>

          <div
            className={styles.gpsHint}
            role="button"
            tabIndex={0}
            onClick={goAtGrave}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') goAtGrave();
            }}
          >
            <MapPin size={18} color={cloudColors.captionGray} />
            <span>
              Вы у могилы? <b>Определите место по GPS в один тап</b>
            </span>
          </div>

          {/* Реальные счётчики появляются, когда пришли с бэка — до этого
              показываем только GPS, без мигания нулями. */}
          <Group justify="center" gap={54} mt={56} wrap="wrap">
            {statsQuery.data && (
              <>
                <Stat
                  value={numberFormat.format(statsQuery.data.usersCount)}
                  label="пользователей"
                />
                <Stat
                  value={numberFormat.format(statsQuery.data.deceasedCount)}
                  label="мест памяти"
                />
                <Stat
                  value={numberFormat.format(statsQuery.data.citiesCount)}
                  label="городов"
                />
              </>
            )}
            <Stat value="2" label="до метров точности GPS" />
          </Group>
        </Container>

        {/* ── Как это работает ── */}
        <Container size="lg" px="md" id="how" py={{ base: 48, md: 72 }}>
          <Title
            order={2}
            ta="center"
            mb={6}
            c={cloudColors.inkBlue}
            style={{ fontSize: 'clamp(26px, 4vw, 38px)', fontWeight: 800 }}
          >
            Как это работает
          </Title>
          <Text
            ta="center"
            maw={560}
            mx="auto"
            mb={40}
            c={cloudColors.text}
            style={{ fontSize: 'clamp(15px, 2vw, 17px)', lineHeight: 1.55 }}
          >
            Три шага, чтобы память о близком оставалась рядом.
          </Text>

          <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="lg">
            <StepCard
              n="01"
              icon={<MapPin size={28} color={cloudColors.azure} />}
              title="Найдите по GPS"
              text="Стоя у захоронения, зафиксируйте точные координаты с точностью до полуметра — больше не потеряете место."
            />
            <StepCard
              n="02"
              icon={<Users size={28} color={cloudColors.azure} />}
              title="Делитесь с близкими"
              text="Отправьте карточку близким — они дойдут до места сами."
            />
            <StepCard
              n="03"
              icon={<Heart size={28} color={cloudColors.azure} />}
              title="Сохраните память"
              text="Даты рождения и памятные дни сервис напомнит сам, чтобы вы никогда не забыли о важном."
            />
          </SimpleGrid>
        </Container>

        {/* ── Подвал ── */}
        <Box
          px={{ base: 18, md: 48 }}
          py={30}
          style={{ borderTop: `1px solid ${cloudColors.cloudBorder}` }}
        >
          <Group justify="space-between" wrap="wrap" gap="md">
            <Group gap={8} wrap="nowrap">
              <Cloud size={20} color={cloudColors.azure} strokeWidth={2.2} />
              <Text fw={700} c={cloudColors.inkBlue}>
                ГдеОни
              </Text>
              <Text c={cloudColors.captionGray} style={{ fontSize: 13 }}>
                · каталог мест захоронений с GPS
              </Text>
            </Group>
            <Group gap="lg" wrap="wrap">
              <Link to="/legal/privacy" className={styles.navLink}>
                Политика
              </Link>
              <Link to="/legal/terms" className={styles.navLink}>
                Условия
              </Link>
              <Link to="/download" className={styles.navLink}>
                Приложение
              </Link>
              <Link to="/login" className={styles.navLink}>
                Войти
              </Link>
            </Group>
          </Group>
        </Box>
      </Box>
    </Box>
  );
}

function Stat({ value, label }: { value: string; label: string }) {
  return (
    <Stack gap={2} align="center">
      <Text fw={800} c={cloudColors.inkBlue} style={{ fontSize: 30 }}>
        {value}
      </Text>
      <Text c={cloudColors.captionGray} style={{ fontSize: 14.5 }}>
        {label}
      </Text>
    </Stack>
  );
}

function StepCard({
  n,
  icon,
  title,
  text,
}: {
  n: string;
  icon: React.ReactNode;
  title: string;
  text: string;
}) {
  return (
    <div className={styles.stepCard}>
      <Text
        fw={800}
        c={cloudColors.captionGray}
        mb={8}
        style={{ fontSize: 13, letterSpacing: '1px' }}
      >
        ШАГ {n}
      </Text>
      <div className={styles.stepIcon}>{icon}</div>
      <Title
        order={3}
        c={cloudColors.inkBlue}
        mb={10}
        style={{ fontSize: 21, fontWeight: 800 }}
      >
        {title}
      </Title>
      <Text c={cloudColors.text} style={{ fontSize: 15.5, lineHeight: 1.6 }}>
        {text}
      </Text>
    </div>
  );
}
