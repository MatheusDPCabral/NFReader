using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;
using SQLitePCL;

class Program
{
    static void Main(string[] args)
    {
        string caminhoPasta = @"C:\Users\mathe\Downloads\XMLs";
        List<NotaFiscal> listaNotas = new List<NotaFiscal>();

        // Processar arquivos XML
        if (Directory.Exists(caminhoPasta))
        {
            var arquivosXml = Directory.GetFiles(caminhoPasta, "*.xml");

            foreach (var arquivo in arquivosXml)
            {
                try
                {
                    using (FileStream fs = new FileStream(arquivo, FileMode.Open, FileAccess.Read))
                    {
                        XDocument xml = XDocument.Load(fs);
                        var allElements = xml.Descendants().ToList();

                        var mod = allElements.FirstOrDefault(e => e.Name.LocalName == "mod")?.Value;
                        var cnpjEmitente = allElements.FirstOrDefault(e => e.Name.LocalName == "CNPJ")?.Value;
                        var nomeEmitente = allElements.FirstOrDefault(e => e.Name.LocalName == "xNome")?.Value;
                        var valorNota = allElements.FirstOrDefault(e => e.Name.LocalName == "vNF" || e.Name.LocalName == "vTPrest" || e.Name.LocalName == "vCFe")?.Value;

                        string numeroNota = null, chaveNota = null, dataEmissao = null;

                        if (mod == "59") // CFe
                        {
                            numeroNota = allElements.FirstOrDefault(e => e.Name.LocalName == "nCFe")?.Value;
                            chaveNota = allElements.FirstOrDefault(e => e.Name.LocalName == "infCFe")?.Attribute("Id")?.Value?.Substring(3);
                            dataEmissao = allElements.FirstOrDefault(e => e.Name.LocalName == "dEmi")?.Value;

                            if (!string.IsNullOrEmpty(dataEmissao) && dataEmissao.Length == 8)
                            {
                                dataEmissao = $"{dataEmissao.Substring(0, 4)}-{dataEmissao.Substring(4, 2)}-{dataEmissao.Substring(6, 2)}";
                            }
                        }
                        else if (mod == "57") // CTe
                        {
                            numeroNota = allElements.FirstOrDefault(e => e.Name.LocalName == "nCT")?.Value;
                            chaveNota = allElements.FirstOrDefault(e => e.Name.LocalName == "infCte")?.Attribute("Id")?.Value?.Substring(3);
                            dataEmissao = allElements.FirstOrDefault(e => e.Name.LocalName == "dhEmi")?.Value;
                        }
                        else if (mod == "55" || mod == "65") // NFe ou NFCe
                        {
                            numeroNota = allElements.FirstOrDefault(e => e.Name.LocalName == "nNF")?.Value;
                            chaveNota = allElements.FirstOrDefault(e => e.Name.LocalName == "infNFe")?.Attribute("Id")?.Value?.Substring(3);
                            dataEmissao = allElements.FirstOrDefault(e => e.Name.LocalName == "dhEmi" || e.Name.LocalName == "dEmi")?.Value;
                        }

                        TipoNota tipoNota = mod switch
                        {
                            "55" => TipoNota.NFe,
                            "57" => TipoNota.CTe,
                            "65" => TipoNota.NFCe,
                            "59" => TipoNota.CFe
                        };

                        if (!string.IsNullOrEmpty(numeroNota) && !string.IsNullOrEmpty(dataEmissao))
                        {
                            listaNotas.Add(new NotaFiscal(
                                tipoNota,
                                int.TryParse(numeroNota, out int n) ? n : 0,
                                chaveNota,
                                DateTime.TryParse(dataEmissao, out DateTime data) ? data : DateTime.MinValue,
                                cnpjEmitente,
                                nomeEmitente,
                                double.TryParse(valorNota, out double v) ? v : 0
                            ));
                        }
                        else
                        {
                            Console.WriteLine($"Aviso: Arquivo {Path.GetFileName(arquivo)} possui dados incompletos e foi ignorado.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar o arquivo {arquivo}: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine("Caminho da pasta não encontrado.");
        }

        // Imprimir as notas processadas
        foreach (var nota in listaNotas)
        {
            Console.WriteLine(
                $"Tipo: {nota.Tipo}, " +
                $"Número: {nota.NumeroNota}, " +
                $"Chave: {nota.getChaveNota()}, " +
                $"Data: {nota.getDataEmissao():yyyy-MM-dd}, " +
                $"CNPJ: {nota.getCNPJEmitente()}, " +
                $"Nome: {nota.getNomeEmitente()}, " +
                $"Valor: {nota.getValorNota():F2}"
            );
        }



        SQLitePCL.Batteries.Init(); // Inicialização do SQLite
        string connectionString = "Data Source=NotasFiscais.db";

        // Criar a tabela, se não existir
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS NotasFiscais (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Tipo TEXT NOT NULL,
                    NumeroNota INTEGER NOT NULL,
                    ChaveNota TEXT NOT NULL,
                    DataEmissao TEXT NOT NULL,
                    CnpjEmitente TEXT NOT NULL,
                    NomeEmitente TEXT NOT NULL,
                    ValorNota REAL NOT NULL
                );
            ";

            using (var command = new SqliteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
                Console.WriteLine("Tabela criada com sucesso!");
            }

            // Apagar todos os registros da tabela
            string deleteQuery = "DELETE FROM NotasFiscais";
            using (var deleteCommand = new SqliteCommand(deleteQuery, connection))
            {
                deleteCommand.ExecuteNonQuery();
                Console.WriteLine("Todos os registros foram apagados da tabela.");
            }

            // Inserir os novos dados na tabela
            foreach (var nota in listaNotas)
            {
                string insertQuery = @"
                    INSERT INTO NotasFiscais (Tipo, NumeroNota, ChaveNota, DataEmissao, CnpjEmitente, NomeEmitente, ValorNota) 
                    VALUES (@Tipo, @NumeroNota, @ChaveNota, @DataEmissao, @CnpjEmitente, @NomeEmitente, @ValorNota);
                ";

                using (var insertCommand = new SqliteCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@Tipo", nota.Tipo.ToString());
                    insertCommand.Parameters.AddWithValue("@NumeroNota", nota.NumeroNota);
                    insertCommand.Parameters.AddWithValue("@ChaveNota", nota.getChaveNota());
                    insertCommand.Parameters.AddWithValue("@DataEmissao", nota.getDataEmissao().ToString("yyyy-MM-dd"));
                    insertCommand.Parameters.AddWithValue("@CnpjEmitente", nota.getCNPJEmitente());
                    insertCommand.Parameters.AddWithValue("@NomeEmitente", nota.getNomeEmitente());
                    insertCommand.Parameters.AddWithValue("@ValorNota", nota.getValorNota());

                    insertCommand.ExecuteNonQuery();
                }
            }

            Console.WriteLine("Novos dados inseridos na tabela.");
        }
    }
}
