using System.Diagnostics.Contracts; 

class Data
{
    public string data_id = "";
    public int TS_Read = 0;
    public int TS_Write = 0;

    public Data(string data_id)
    {
        this.data_id = data_id;
    }

    public void Reset()
    {
        TS_Read = 0;
        TS_Write = 0;
    }
}
/*
public enum TransactionStatus
    {
        Active,
        Abort,
        Commit
    }
    */

class Transaction
{
    public string transaction_id;
    public int timestamp;

    //public TransactionStatus transactionStatus;

    public Transaction(string transaction_id, int timestamp)
    {
        //transactionStatus = TransactionStatus.Active;
        this.transaction_id = transaction_id;
        this.timestamp = timestamp;
    }
}


enum Command { READ, WRITE, COMMIT };

class Operation
{
    public string scheduler_id;
    public Data? data;

    public Transaction transaction;
    public Command command;


    public Operation(string scheduler_id, Data? data, Transaction transaction, Command command)
    {
        this.scheduler_id = scheduler_id;
        this.data = data;
        this.transaction = transaction;
        this.command = command;
    }

    private bool IsValid()
    {

        if (command == Command.COMMIT)
            return true;

        if (data == null)
            return false;

        if (command == Command.READ)
            return transaction.timestamp >= data.TS_Write;

        if (command == Command.WRITE)
            return (transaction.timestamp >= data.TS_Write) && (transaction.timestamp >= data.TS_Read);


        return false;
    }

    public void ResetData()
    {
        if (data != null) data.Reset();
    }

    private void LogData(int time)
    {
        if (data == null) return;
        string out_path = this.data.data_id + ".txt";
        string str_command;
        if (command == Command.WRITE) {
            str_command = "Write";
        }
        else {
            str_command = "Read";
        }
        File.AppendAllText(out_path, this.scheduler_id + ", " + str_command + ", " + time + Environment.NewLine);

    }

    public bool Apply(int time) //Retorna verdadeiro se concluiu e falso se exige RollBack
    {
        if (!this.IsValid()) return false;


        if (data == null) return true; //Desnecessario, pois se é valido, quer dizer que é um commit.

        if (command == Command.READ)
        {
            this.data.TS_Read = Math.Max(this.data.TS_Read, transaction.timestamp);
        }

        else if (command == Command.WRITE)
        {
            this.data.TS_Write = transaction.timestamp;
        }

        this.LogData(time);

        return true;
    }

    public void Print()
    {
        Console.WriteLine("------Operation-------");
        Console.WriteLine(scheduler_id);
        if (data != null)
            Console.WriteLine(data.data_id);
        else
            Console.WriteLine("Null");
        Console.WriteLine(transaction.transaction_id);
        Console.WriteLine(command);
    }   

}

class Scheduler //Escalonamento
{
    public string scheduler_id;
    public List<Operation> operations = [];
    public bool rollback = false;
    public int time = 0;

    public Scheduler(string scheduler_id, List<Operation> operations)
    {
        this.scheduler_id = scheduler_id;
        this.operations = operations;
    }

    private void ResetData()
    {
        foreach (Operation operation in this.operations)
            operation.ResetData();

    }

    public void Operate()
    {
        foreach (Operation operation in this.operations)
        {
            if (!operation.Apply(time))
            {
                rollback = true;
                this.ResetData();
                return;
            }
            this.time++;
        }


        this.ResetData();
    }

}

class Program
{
    public List<Data> datas = new List<Data>();
    public List<Transaction> transactions = new List<Transaction>();
    public List<Scheduler> schedulers = new List<Scheduler>();
    private void Parser(string caminhoArquivo)
    {

        if (!File.Exists(caminhoArquivo))
        {
            Console.WriteLine("Arquivo in.txt não encontrado.");
            return;
        }

        string[] linhas = File.ReadAllLines(caminhoArquivo);

        if (linhas.Length < 4)
        {
            Console.WriteLine("Arquivo in.txt está incompleto.");
            return;
        }

        // 1. Objetos de dados
        var objetosLinha = linhas[0].Trim().TrimEnd(';');
        var objetos = objetosLinha.Split(',').Select(o => o.Trim()).ToList();

        // 2. Transações
        var transacoesLinha = linhas[1].Trim().TrimEnd(';');
        var transacoesIds = transacoesLinha.Split(',').Select(t => t.Trim()).ToList();

        Console.WriteLine(transacoesIds);

        // 3. Timestamps
        var tsLinha = linhas[2].Trim().TrimEnd(';');
        var timestamps = tsLinha.Split(',').Select(t => int.Parse(t.Trim())).ToList();

        for (int i = 0; i < transacoesIds.Count; i++)
        {
            transactions.Add(new Transaction(transacoesIds[i], timestamps[i]));
        }

        foreach (string objeto in objetos)
        {
           datas.Add(new Data(objeto));

        }

        // 4. Escalonamentos
        var escalonamentos = new List<string>();
        for (int i = 3; i < linhas.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(linhas[i]))
                escalonamentos.Add(linhas[i].Trim());
        }

        Console.WriteLine("Objetos de dados: " + string.Join(", ", objetos));
        Console.WriteLine("Transações e timestamps:");
        foreach (var t in transactions)
            Console.WriteLine($"  {t.transaction_id} - TS: {t.timestamp}");
        Console.WriteLine("Escalonamentos:");
        foreach (var esc in escalonamentos)
            Console.WriteLine("  " + esc);

        //////////////////// - Mudei algumas coisas - ////////////////////
        // Parsear operações
        //var todasOperacoesPorEscalonamento = new List<List<Operation>>();

        foreach (var escalonamento in escalonamentos)
        {
            var split = escalonamento.Split('-', 2);
            string idEscalonamento = split[0];
            string operacoesStr = split[1];

            var operacoesTokens = operacoesStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);



            var operacoesList = new List<Operation>();

            for (int momento = 0; momento < operacoesTokens.Length; momento++)
            {
                string token = operacoesTokens[momento];

                if (token.StartsWith("c"))
                {
                    // Commit operation
                    string numTransacao = new string(token.Skip(1).TakeWhile(char.IsDigit).ToArray());
                    string transacaoId = "t" + numTransacao;

                    Transaction? transaction = transactions.Find(x => x.transaction_id == transacaoId);
                    if (transaction != null)
                        operacoesList.Add(new Operation(idEscalonamento, null, transaction, Command.COMMIT));
                    else
                    {
                        throw new Exception("Tentando Escalonar uma Transação não indentificada!");
                    }
                }
                else
                {
                    char tipo = token[0];
                    Command command;
                    if (tipo == 'w')
                    {
                        command = Command.WRITE;
                    }
                    else
                    {
                        command = Command.READ;
                    }

                    int idxAbrePar = token.IndexOf('(');

                    string numTransacao = new string(token.Skip(1).TakeWhile(char.IsDigit).ToArray());
                    string transacaoId = "t" + numTransacao;

                    Transaction? transaction = transactions.Find(x => x.transaction_id == transacaoId);
                    if (transaction == null)
                        throw new Exception("Tentando Escalonar uma Transação não indentificada!");

                    int idxFechaPar = token.IndexOf(')');
                    string objeto = token.Substring(idxAbrePar + 1, idxFechaPar - idxAbrePar - 1);


                    Data? data = datas.Find(x => x.data_id == objeto);

                    if (data != null)
                        operacoesList.Add(new Operation(idEscalonamento, data, transaction, command));
                    else
                        throw new Exception("Tentando Ler/Escrever um objeto inexistente");
                }
            }

            this.schedulers.Add(new Scheduler(idEscalonamento, operacoesList));
        }


    }

    private void ExecuteSchedulers()
    {

        // Avaliar escalonamentos e gerar resultados
        var resultados = new List<string>();

        foreach (Scheduler scheduler in this.schedulers)
        {

            scheduler.Operate();

            if (!scheduler.rollback)
                resultados.Add($"{scheduler.scheduler_id}-OK");
            else
                resultados.Add($"{scheduler.scheduler_id}-ROLLBACK-{scheduler.time}");
        }

        Console.WriteLine("\nResultados:");
        foreach (var r in resultados)
            Console.WriteLine(r);

        File.WriteAllLines("out.txt", resultados);
    }

    public void Run()
    {
        foreach (Data data in datas) File.WriteAllText(data.data_id + ".txt", string.Empty);
        const string data_path = "in.txt";
        this.Parser(data_path);
        this.ExecuteSchedulers();
    }

    static void Main(string[] args) {
        Program program = new Program();
        program.Run();
    }


};





