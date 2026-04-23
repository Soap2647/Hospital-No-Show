using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HospitalNoShow.Application.Validations;

public class TcKimlikAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string tcKimlik || string.IsNullOrWhiteSpace(tcKimlik))
        {
            return new ValidationResult("TC Kimlik No boş bırakılamaz.");
        }

        if (tcKimlik.Length != 11 || !Regex.IsMatch(tcKimlik, @"^\d{11}$"))
        {
            return new ValidationResult("TC Kimlik No 11 haneli rakamlardan oluşmalıdır.");
        }

        if (tcKimlik[0] == '0')
        {
            return new ValidationResult("TC Kimlik No 0 ile başlayamaz.");
        }

        int[] digits = tcKimlik.Select(c => int.Parse(c.ToString())).ToArray();

        int sumOdd = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        int sumEven = digits[1] + digits[3] + digits[5] + digits[7];

        int digit10 = ((sumOdd * 7) - sumEven) % 10;
        if (digit10 < 0) digit10 += 10; // C#'ta modulo negatif çıkabilir

        if (digits[9] != digit10)
        {
            return new ValidationResult("Geçersiz TC Kimlik No algoritması (Hane 10 hatası).");
        }

        int totalSum = digits.Take(10).Sum();
        int digit11 = totalSum % 10;

        if (digits[10] != digit11)
        {
            return new ValidationResult("Geçersiz TC Kimlik No algoritması (Hane 11 hatası).");
        }

        return ValidationResult.Success;
    }
}
