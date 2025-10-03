using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Soat.Eleven.FastFood.AuthFunc;

public static class Common
{
    public static string GeneratePassword(string password, string saltKey)
    {
        var saltByte = Encoding.UTF8.GetBytes(saltKey);
        var hmacMD5 = new HMACMD5(saltByte);
        var passwordConvert = Encoding.UTF8.GetBytes(password!);
        return Convert.ToBase64String(hmacMD5.ComputeHash(passwordConvert));
    }

    public static string CleanCpf(string cpf) => Regex.Replace(cpf, @"[^\d]", "");

    public static bool IsCpfValido(string cpf)
    {
        cpf = CleanCpf(cpf);

        if (cpf.Length != 11)
            return false;

        if (cpf.Distinct().Count() == 1)
            return false;

        var soma = 0;
        for (int i = 0; i < 9; i++)
            soma += int.Parse(cpf[i].ToString()) * (10 - i);

        var resto = soma % 11;
        var digito1 = resto < 2 ? 0 : 11 - resto;

        if (int.Parse(cpf[9].ToString()) != digito1)
            return false;

        soma = 0;
        for (int i = 0; i < 10; i++)
            soma += int.Parse(cpf[i].ToString()) * (11 - i);

        resto = soma % 11;
        var digito2 = resto < 2 ? 0 : 11 - resto;

        return int.Parse(cpf[10].ToString()) == digito2;
    }
}
