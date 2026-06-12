import { useParams } from 'react-router-dom';
import { PageStub } from '../../components/PageStub';

export function PreviewPage() {
  const { id } = useParams();
  return <PageStub title={`Preview карточки (${id})`} fBlock="F7" />;
}
