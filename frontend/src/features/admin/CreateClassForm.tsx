import { Button, NumberInput, Paper, Stack, TextInput, Title } from '@mantine/core'
import { useForm } from '@mantine/form'
import { errorCode } from '../../api/client.ts'
import { useCreateClass } from '../../api/queries.ts'
import { messageFor } from '../../messages.ts'
import { notifyError, notifySuccess } from '../../notify.ts'
import { MAX_SEATS, MIN_SEATS, parseSeats, validateClassForm } from './classForm.ts'

export function CreateClassForm() {
  const createClass = useCreateClass()
  const form = useForm({
    mode: 'uncontrolled',
    initialValues: { name: '', totalSeats: '' as number | string },
    validate: validateClassForm,
  })

  const submit = form.onSubmit((values) => {
    const totalSeats = parseSeats(values.totalSeats)
    if (totalSeats === null || createClass.isPending) return
    createClass.mutate(
      { name: values.name.trim(), totalSeats },
      {
        onSuccess: (created) => {
          form.reset()
          notifySuccess('Class created', `${created.name} is open for applications.`)
        },
        onError: (error) => {
          const code = errorCode(error)
          notifyError('Could not create the class', messageFor(code))
        },
      },
    )
  })

  return (
    <Paper withBorder p="lg" component="section" aria-labelledby="admin-title">
      <form onSubmit={submit} noValidate>
        <Stack>
          <Title order={2} size="h3" id="admin-title">School admin</Title>
          <TextInput label="Class name" placeholder="Class Six, Section A" key={form.key('name')} {...form.getInputProps('name')} />
          <NumberInput
            label="Total seats"
            min={MIN_SEATS}
            max={MAX_SEATS}
            allowDecimal={false}
            allowNegative={false}
            clampBehavior="none"
            key={form.key('totalSeats')}
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
