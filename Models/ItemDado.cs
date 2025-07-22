namespace EscalonadorApp.Models
{
    public class ItemDado
    {
        public string Id { get; set; }
        public int TSRead { get; set; }
        public int TSWrite { get; set; }

        public ItemDado(string id)
        {
            Id = id;
            TSRead = 0;
            TSWrite = 0;
        }
    }
}