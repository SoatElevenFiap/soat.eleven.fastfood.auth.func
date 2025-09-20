namespace Soat.Eleven.FastFood.Domain.Entities;

public class Cliente : Usuario
{
    public string Cpf { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
}
