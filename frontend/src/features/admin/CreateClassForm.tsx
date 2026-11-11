import { Button, NumberInput, Paper, Stack, TextInput, Title } from '@mantine/core'
import { useForm } from '@mantine/form'
import { notifications } from '@mantine/notifications'
import { ApiError } from '../../api/client.ts'
import { useCreateClass } from '../../api/queries.ts'
import { messageFor } from '../../messages.ts'
import { MAX_SEATS, MIN_SEATS, parseSeats, validateClassForm } from './classForm.ts'

export function CreateClassForm() {
  const createClass = useCreateClass()
  const form = useForm({
    mode: 'controlled',
    initialValues: { name: '', totalSeats: '' as number | string },
    validate: (values) => validateClassForm(values),
  })

  const submit = form.onSubmit((values) => {
    const totalSeats = parseSeats(values.totalSeats)
    if (totalSeats === null || createClass.isPending) return
    createClass.mutate(
      { name: values.name.trim(), totalSeats },
      {
        onSuccess: (created) => {
          form.reset()
          notifications.show({ color: 'green', title: 'Class created', message: `${created.name} is open for applications.` })
        },
        onError: (error) => {
          const code = error instanceof ApiError ? error.code : 'REQUEST_ERROR'
          notifications.show({ color: 'red', title: 'Could not create the class', message: messageFor(code) })
        },
      },
    )
  })

  return (
    <Paper withBorder p="lg" component="section" aria-labelledby="admin-title">
      <form onSubmit={submit} noValidate>
        <Stack>
          <Title order={2} size="h3" id="admin-title">School admin</Title>
          <TextInput label="Class name" placeholder="Class Six, Section A" {...form.getInputProps('name')} />
          <NumberInput
            label="Total seats"
            min={MIN_SEATS}
            max={MAX_SEATS}
            allowDecimal={false}
            allowNegative={false}
            {...form.getInputProps('totalSeats')}
          />
          <Button type="submit" loading={createClass.isPending}>
            Create class
          </Button>
        </Stack>
      </form>
    </Paper>
  )
}
