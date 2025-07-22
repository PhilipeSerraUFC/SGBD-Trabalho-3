namespace EscalonadorApp.Models
{
    public class Operacao
    {
        public string Tipo { get; set; } // r, w, c
        public string TransacaoId { get; set; }
        public string? Objeto { get; set; }    // X, Y, Z...
        public int Momento { get; set; }

        public Operacao(string tipo, string transacaoId, string? objeto, int momento)
        {
            Tipo = tipo;
            TransacaoId = transacaoId;
            Objeto = objeto;
            Momento = momento;
        }
    }
}