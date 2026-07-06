import { useMemo } from 'react';
import { Anchor, Container, List, Stack, Title } from '@mantine/core';
import { Link } from 'react-router-dom';
import { Cloud } from 'lucide-react';
import privacyMarkdown from './privacyPolicy.md?raw';
import termsMarkdown from './termsOfUse.md?raw';
import {
  BodyLabel,
  CaptionLabel,
  CloudCard,
  TitleLabel,
} from '../../components/ui';
import { cloudColors } from '../../design/theme';

/**
 * F24 / D19. Публичная страница юридического документа. Использует
 * markdown-заглушку из .md?raw импорта — так текст лежит в бандле
 * без дополнительной статики. Рендер простой: H1/H2, параграфы и
 * маркированные списки. Полноценный markdown-парсер не тянем —
 * из-за одного экрана.
 *
 * Один компонент на оба документа (privacy / terms), контент
 * выбирается по пропсу.
 */
type Kind = 'privacy' | 'terms';

const CONTENT: Record<Kind, { title: string; markdown: string }> = {
  privacy: { title: 'Политика конфиденциальности', markdown: privacyMarkdown },
  terms: { title: 'Условия использования', markdown: termsMarkdown },
};

export function LegalPage({ kind }: { kind: Kind }) {
  const { title, markdown } = CONTENT[kind];
  const blocks = useMemo(() => parseMarkdown(markdown), [markdown]);

  return (
    <Container size="md" pt={48} pb={48}>
      <Stack gap="md" mb="lg" align="center">
        <Cloud size={40} color={cloudColors.azureDeep} />
        <TitleLabel>{title}</TitleLabel>
      </Stack>

      <CloudCard>
        <Stack gap="md">
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
        <Title key={key} order={2} c={cloudColors.inkBlue} mt="sm">
          {block.text}
        </Title>
      );
    case 'h2':
      return (
        <Title key={key} order={4} c={cloudColors.inkBlue} mt="sm">
          {block.text}
        </Title>
      );
    case 'p':
      return <BodyLabel key={key}>{block.text}</BodyLabel>;
    case 'ul':
      return (
        <List key={key} spacing={4}>
          {block.items.map((item, i) => (
            <List.Item key={i}>{item}</List.Item>
          ))}
        </List>
      );
  }
}
