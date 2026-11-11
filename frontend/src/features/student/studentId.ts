const STUDENT_ID_PATTERN = /^[0-9]{17}$/

export function isValidStudentId(value: string): boolean {
  return STUDENT_ID_PATTERN.test(value.trim())
}
