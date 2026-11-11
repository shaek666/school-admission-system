import { Container, SimpleGrid, Stack } from '@mantine/core'
import { CreateClassForm } from './features/admin/CreateClassForm.tsx'
import { ClassList } from './features/classes/ClassList.tsx'
import { StudentBar } from './features/student/StudentBar.tsx'

export default function App() {
  return (
    <>
      <header className="app-header">
        <h1 className="app-title">School Admission</h1>
        <StudentBar />
      </header>
      <Container size="lg" py="lg">
        <SimpleGrid cols={{ base: 1, md: 2 }} spacing="lg">
          <Stack>
            <CreateClassForm />
          </Stack>
          <Stack>
            <ClassList />
          </Stack>
        </SimpleGrid>
      </Container>
    </>
  )
}
