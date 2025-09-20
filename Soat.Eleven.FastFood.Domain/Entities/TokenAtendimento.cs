namespace Soat.Eleven.FastFood.Domain.Entities;

public class TokenAtendimento
{
    public Guid TokenId { get; set; }
    public Guid? ClienteId { get; set; }
    public string? Cpf { get; set; }
    public DateTime CriadoEm { get; set; }
    public Cliente? Cliente { get; set; }
    public string? CpfCliente { get; set; }
}
