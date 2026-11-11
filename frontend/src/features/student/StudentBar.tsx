import { Button, Group, Text, TextInput } from '@mantine/core'
import { useForm } from '@mantine/form'
import { isValidStudentId } from './studentId.ts'
import { useStudentId } from './useStudentId.ts'

export const STUDENT_ID_ERROR = 'Enter exactly 17 digits.'

export function StudentBar() {
  const [studentId, setStudentId] = useStudentId()
  const form = useForm({
    mode: 'uncontrolled',
    initialValues: { studentId: '' },
    validate: {
      studentId: (value) => (isValidStudentId(value) ? null : STUDENT_ID_ERROR),
    },
  })

  return (
    <form onSubmit={form.onSubmit((values) => setStudentId(values.studentId))} noValidate>
      <Group align="flex-start" gap="sm">
        <TextInput
          label="Student birth registration number"
          placeholder="17 digits"
          inputMode="numeric"
          maxLength={17}
          key={form.key('studentId')}
          {...form.getInputProps('studentId')}
        />
        <Button type="submit" mt={25}>
          Save
        </Button>
        {studentId !== null && (
          <Text mt={30} size="sm">
            Applying as <strong>{studentId}</strong>
          </Text>
        )}
      </Group>
    </form>
  )
}
