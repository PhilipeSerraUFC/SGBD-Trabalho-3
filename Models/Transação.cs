namespace EscalonadorApp.Models
{
    public enum StatusTransacao
    {
        Ativa,
        Abortada,
        Comitada
    }

    public class Transacao
    {
        public string Id { get; set; }
        public int Timestamp { get; set; }
        public StatusTransacao Status { get; set; }

        public Transacao(string id, int timestamp)
        {
            Id = id;
            Timestamp = timestamp;
            Status = StatusTransacao.Ativa;
        }
    }
}