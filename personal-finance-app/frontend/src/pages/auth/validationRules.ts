export const passwordRules = [
  { label: 'Between 8 and 100 characters', test: (value: string) => value.length >= 8 && value.length <= 100 },
  { label: 'One uppercase and one lowercase letter', test: (value: string) => /[A-Z]/.test(value) && /[a-z]/.test(value) },
  { label: 'One number', test: (value: string) => /\d/.test(value) },
]
