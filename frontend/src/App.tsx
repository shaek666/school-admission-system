import { Container, SimpleGrid, Stack } from '@mantine/core'
import { CreateClassForm } from './features/admin/CreateClassForm.tsx'
import { ClassList } from './features/classes/ClassList.tsx'
import { MyApplications } from './features/holds/MyApplications.tsx'
import { StudentBar } from './features/student/StudentBar.tsx'

export default function App() {
  return (
    <>
      <header className="app-header">
        <h1 className="app-title">School Admission</h1>
        <StudentBar />
      </header>
      <Container size="lg" py="lg">
        <Stack gap="lg">
          <SimpleGrid cols={{ base: 1, md: 2 }} spacing="lg">
            <CreateClassForm />
            <ClassList />
          </SimpleGrid>
          <MyApplications />
        </Stack>
      </Container>
    </>
  )
}
