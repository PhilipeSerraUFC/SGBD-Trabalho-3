// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EscalonadorApp.Models;

namespace EscalonadorApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string caminhoArquivo = "in.txt";

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

            // 3. Timestamps
            var tsLinha = linhas[2].Trim().TrimEnd(';');
            var timestamps = tsLinha.Split(',').Select(t => int.Parse(t.Trim())).ToList();

            // Criar lista de transações
            var transacoes = new List<Transacao>();
            for (int i = 0; i < transacoesIds.Count; i++)
            {
                transacoes.Add(new Transacao(transacoesIds[i], timestamps[i]));
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
            foreach (var t in transacoes)
                Console.WriteLine($"  {t.Id} - TS: {t.Timestamp}");
            Console.WriteLine("Escalonamentos:");
            foreach (var esc in escalonamentos)
                Console.WriteLine("  " + esc);

            // Parsear operações
            var todasOperacoesPorEscalonamento = new List<List<Operacao>>();

            foreach (var escalonamento in escalonamentos)
            {
                var split = escalonamento.Split('-', 2);
                string idEscalonamento = split[0];
                string operacoesStr = split[1];

                var operacoesTokens = operacoesStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var operacoesList = new List<Operacao>();

                for (int momento = 0; momento < operacoesTokens.Length; momento++)
                {
                    string token = operacoesTokens[momento];

                    if (token.StartsWith("c"))
                    {
                        // Commit operation
                        string numTransacao = new string(token.Skip(1).TakeWhile(char.IsDigit).ToArray());
                        string transacaoId = "t" + numTransacao;
                        operacoesList.Add(new Operacao("c", transacaoId, null, momento));
                    }
                    else
                    {
                        char tipo = token[0];
                        int idxAbrePar = token.IndexOf('(');

                        string numTransacao = new string(token.Skip(1).TakeWhile(char.IsDigit).ToArray());
                        string transacaoId = "t" + numTransacao;

                        int idxFechaPar = token.IndexOf(')');
                        string objeto = token.Substring(idxAbrePar + 1, idxFechaPar - idxAbrePar - 1);

                        operacoesList.Add(new Operacao(tipo.ToString(), transacaoId, objeto, momento));
                    }
                }

                todasOperacoesPorEscalonamento.Add(operacoesList);

                Console.WriteLine($"\nEscalonamento {idEscalonamento}:");
                foreach (var op in operacoesList)
                {
                    Console.WriteLine($" Momento {op.Momento}: Tipo={op.Tipo} Transação={op.TransacaoId} Objeto={op.Objeto}");
                }
            }

            // Avaliar escalonamentos e gerar resultados
            var resultados = new List<string>();

            for (int i = 0; i < todasOperacoesPorEscalonamento.Count; i++)
            {
                var idEsc = $"E_{i + 1}";
                var operacoes = todasOperacoesPorEscalonamento[i];

                bool valido = VerificarEscalonamento(operacoes, transacoes, out int momento);

                if (valido)
                    resultados.Add($"{idEsc}-OK");
                else
                    resultados.Add($"{idEsc}-ROLLBACK-{momento}");
            }

            Console.WriteLine("\nResultados:");
            foreach (var r in resultados)
                Console.WriteLine(r);

            File.WriteAllLines("out.txt", resultados);
        }

        static bool VerificarEscalonamento(List<Operacao> operacoes, List<Transacao> transacoes, out int momentoFalha)
        {
            Dictionary<string, ItemDado> controleDados = new();

            foreach (var op in operacoes)
            {
                if (op.Objeto != null && !controleDados.ContainsKey(op.Objeto))
                {
                    controleDados[op.Objeto] = new ItemDado(op.Objeto);
                }
            }

            foreach (var op in operacoes)
            {
                var transacao = transacoes.First(t => t.Id == op.TransacaoId);
                int tsT = transacao.Timestamp;

                if (op.Tipo == "r" && op.Objeto != null)
                {
                    var dado = controleDados[op.Objeto];
                    if (tsT < dado.TSWrite)
                    {
                        momentoFalha = op.Momento;
                        return false;
                    }
                    dado.TSRead = Math.Max(dado.TSRead, tsT);
                }
                else if (op.Tipo == "w" && op.Objeto != null)
                {
                    var dado = controleDados[op.Objeto];
                    if (tsT < dado.TSRead || tsT < dado.TSWrite)
                    {
                        momentoFalha = op.Momento;
                        return false;
                    }
                    dado.TSWrite = tsT;
                }
            }

            momentoFalha = -1;
            return true;
        }
    }
}
