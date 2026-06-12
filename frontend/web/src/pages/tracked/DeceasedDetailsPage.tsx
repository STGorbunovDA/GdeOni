import { useParams } from 'react-router-dom';
import { PageStub } from '../../components/PageStub';

export function DeceasedDetailsPage() {
  const { id } = useParams();
  return <PageStub title={`Карточка умершего (${id})`} fBlock="F11" />;
}
