import { useMemo } from 'react';
import { Alert, Anchor, Container, List, Loader, Stack, Title } from '@mantine/core';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Cloud } from 'lucide-react';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  TitleLabel,
} from '../../components/ui';
import { legalApi } from '../../api/endpoints/legalApi';
import { formatError } from '../../auth/errorMessages';
import { cloudColors } from '../../design/theme';

/**
 * F24 / D19 / D19.9. Публичная страница юридического документа.
 *
 * Текст приходит с бэка (GET /api/legal/*, поле bodyMarkdown) — раньше
 * он лежал .md-файлом в бандле web, а версия документа при этом жила в
 * appsettings бэка, и ничто не мешало им разъехаться. Теперь источник
 * один: backend/docs/legal/*.md, и мобилка читает тот же текст.
 *
 * Рендер простой: H1/H2, параграфы и маркированные списки. Полноценный
 * markdown-парсер не тянем — из-за одного экрана.
 *
 * Страница публичная (эндпоинт AllowAnonymous): её открывают из формы
 * регистрации ещё до появления токена.
 */
type Kind = 'privacy' | 'terms';

const TITLES: Record<Kind, string> = {
  privacy: 'Политика конфиденциальности',
  terms: 'Условия использования',
};

export function LegalPage({ kind }: { kind: Kind }) {
  const title = TITLES[kind];

  const query = useQuery({
    queryKey: ['legal-document', kind],
    queryFn: () =>
      kind === 'privacy'
        ? legalApi.getPrivacyPolicy()
        : legalApi.getTermsOfUse(),
    staleTime: 60 * 60 * 1000, // документ меняется раз в год
  });

  const markdown = query.data?.bodyMarkdown ?? '';
  const blocks = useMemo(() => parseMarkdown(markdown), [markdown]);

  return (
    <Container size="md" pt={48} pb={48}>
      <Stack gap="md" mb="lg" align="center">
        <Cloud size={40} color={cloudColors.azureDeep} />
        <TitleLabel>{title}</TitleLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
          {query.isLoading && (
            <Stack align="center" py="xl">
              <Loader color="azure" />
            </Stack>
          )}

          {query.isError && (
            <Alert color="red" variant="light">
              Не удалось загрузить документ: {formatError(query.error)}
            </Alert>
          )}

          {/* Текст есть на бэке, но пустой — деградируем до ссылки на
              публичную версию, а не показываем пустую карточку. */}
          {query.isSuccess && !markdown && (
            <BodyLabel>
              Документ временно недоступен.{' '}
              <Anchor href={query.data.url} c={cloudColors.azureDeep}>
                Открыть опубликованную версию
              </Anchor>
            </BodyLabel>
          )}

          {blocks.map((b, i) => renderBlock(b, i))}

          <CaptionLabel>
            Другие документы:{' '}
            {kind === 'privacy' ? (
              <Anchor component={Link} to="/legal/terms" c={cloudColors.azureDeep}>
                Условия использования
              </Anchor>
            ) : (
              <Anchor component={Link} to="/legal/privacy" c={cloudColors.azureDeep}>
                Политика конфиденциальности
              </Anchor>
            )}
          </CaptionLabel>
        </Stack>
      </CloudCard>
    </Container>
  );
}

/* ─── Мини-парсер ─── */

type Block =
  | { type: 'h1'; text: string }
  | { type: 'h2'; text: string }
  | { type: 'p'; text: string }
  | { type: 'ul'; items: string[] };

function parseMarkdown(md: string): Block[] {
  const lines = md.replace(/\r\n/g, '\n').split('\n');
  const blocks: Block[] = [];
  let paraBuf: string[] = [];
  let listBuf: string[] = [];

  const flushPara = () => {
    if (paraBuf.length > 0) {
      blocks.push({ type: 'p', text: paraBuf.join(' ') });
      paraBuf = [];
    }
  };
  const flushList = () => {
    if (listBuf.length > 0) {
      blocks.push({ type: 'ul', items: listBuf });
      listBuf = [];
    }
  };

  for (const raw of lines) {
    const line = raw.trim();
    if (line === '') {
      flushPara();
      flushList();
      continue;
    }
    if (line.startsWith('# ')) {
      flushPara();
      flushList();
      blocks.push({ type: 'h1', text: line.slice(2) });
      continue;
    }
    if (line.startsWith('## ')) {
      flushPara();
      flushList();
      blocks.push({ type: 'h2', text: line.slice(3) });
      continue;
    }
    if (line.startsWith('- ')) {
      flushPara();
      listBuf.push(line.slice(2));
      continue;
    }
    flushList();
    paraBuf.push(line);
  }
  flushPara();
  flushList();

  return blocks;
}

function renderBlock(block: Block, key: number) {
  switch (block.type) {
    case 'h1':
      return (
        <Title key={key} order={2} c={cloudColors.inkBlue} mt="md">
          {block.text}
        </Title>
      );
    case 'h2':
      return (
        <Title key={key} order={3} c={cloudColors.inkBlue} mt="sm">
          {block.text}
        </Title>
      );
    case 'p':
      return <BodyLabel key={key}>{block.text}</BodyLabel>;
    case 'ul':
      return (
        <List key={key} spacing="xs" withPadding>
          {block.items.map((item, i) => (
            <List.Item key={i}>
              <BodyLabel>{item}</BodyLabel>
            </List.Item>
          ))}
        </List>
      );
  }
}
