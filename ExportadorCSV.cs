using System.Text;

public static class ExportadorCSV
{
    public static void Exportar(List<NotaFiscal> listaNotas, string caminhoArquivoCSV)
    {
        var csvBuilder = new StringBuilder();

        // cabeçalho
        csvBuilder.AppendLine("Tipo,NumeroNota,ChaveNota,DataEmissao,CNPJEmitente,NomeEmitente,ValorNota");

        // Adiciona os dados
        foreach (var nota in listaNotas)
        {
            csvBuilder.AppendLine(
                $"{nota.Tipo}," +
                $"{nota.NumeroNota}," +
                $"{nota.getChaveNota()}," +
                $"{nota.getDataEmissao():yyyy-MM-dd}," +
                $"{nota.getCNPJEmitente()}," +
                $"{nota.getNomeEmitente()}," +
                $"{nota.getValorNota():F2}" 
            );
        }

        // Salva o conteúdo em um arquivo CSV
        File.WriteAllText(caminhoArquivoCSV, csvBuilder.ToString(), Encoding.UTF8);

        Console.WriteLine("Arquivo CSV gerado com sucesso em: " + caminhoArquivoCSV);
    }
}
