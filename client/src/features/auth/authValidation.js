/** Українське пластикове посвідчення водія: 3 літери + 6 цифр. */
export const normalizeLicense = (value) => value.replace(/\s/g, '').toUpperCase();

export const phoneDigitsOf = (value) => value.replace(/\D/g, '');

/**
 * Перевірка форми реєстрації. Повертає текст помилки або null, якщо все гаразд.
 * @param {{ email: string, password: string, phone: string, role: 'passenger' | 'driver' | null, license: string }} form
 */
export function validateSignUp({ email, password, phone, role, license }) {
  if (password.length < 8) {
    return 'Некоректний ввід: пароль має містити щонайменше 8 символів.';
  }

  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    return 'Некоректний ввід: введіть справжню електронну пошту.';
  }

  if (!/^[3-9]\d{8}$/.test(phoneDigitsOf(phone))) {
    return 'Некоректний ввід: номер телефону має бути 9 цифр після +380 (не починається з 0-2).';
  }

  if (role === 'driver' && !/^[A-ZА-ЯҐЄІЇ]{3}\d{6}$/.test(normalizeLicense(license))) {
    return 'Некоректний ввід: номер посвідчення водія має бути 3 літери + 6 цифр (напр. ВХХ123456).';
  }

  return null;
}
