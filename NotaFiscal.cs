using System;

public enum TipoNota
{
    NFe,
    CTe,
    NFCe,
    CFe
}

public class NotaFiscal
{
    public TipoNota Tipo { get; private set; }
    public int NumeroNota { get; private set; }
    private string chaveNota;
    private string cnpjEmitente;
    private string nomeEmitente;
    private double valorNota;
    private DateTime dataEmissao;

    public NotaFiscal(TipoNota tipo, int numeroNota, string chave, DateTime data, string cnpj, string nome, double valor)
    {
        Tipo = tipo;
        NumeroNota = numeroNota;
        chaveNota = chave;
        dataEmissao = data;
        cnpjEmitente = cnpj;
        nomeEmitente = nome;    
        valorNota = valor;
    }

    public string getChaveNota()
    {
        return chaveNota;
    }

    public string getCNPJEmitente()
    {
        return cnpjEmitente;
    }

    public string getNomeEmitente()
    {
        return nomeEmitente;
    }

    public double getValorNota()
    {
        return valorNota;
    }

    public DateTime getDataEmissao()
    {
        return dataEmissao;
    }
}
