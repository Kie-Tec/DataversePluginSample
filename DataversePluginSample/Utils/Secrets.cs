using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataversePluginSample.Utils
{
    internal static class Secrets
    {
        internal static string ApiKey = "SECRET";

        internal static string Prompt = $@"
        Você é um assistente que analisa emails de solicitação de reembolso de viagens de trabalho. Dado o corpo do email abaixo, as informações do remetente e o texto extraído dos anexos em PDF, extraia as seguintes informações estruturadas em formato JSON sem nenhuma explicação ou complemento:

        1. Data do gasto (DT_BASE)
        2. Valor total do reembolso descrito no email (VL_DECLARADO) (Quando não houver anexos, retornar somente uma linha com o valor descrito no email)
        3. Valor total retirado dos comprovantes em anexo (VL_EXTRAIDO) (Quando não houver anexos, retornar somente uma linha com o valor descrito no email)
        4. Categoria do gasto (Categoria) (Quando não houver anexos, retornar uma tabela com um registro. Esse registro deve ser o motivo do reembolso)

        ### Corpo do Email:
        [corpoEmail]

        ### Informações do Remetente:
        Nome: [nomeRemetente]
        Email: [emailRemetente]
        Data: [dataEmail]
        DataFinal: [dataFinal] (Data do ultimo gasto encontrado, quando não houver mais de uma data, colocar a data encontrada)
        Cidade: [cidadeGasto]
        Estado: [estadoViagem]
        País: [paisviagem]

        ### Texto Extraído dos Anexos:
        [textoAnexos]

        ### Saída Esperada:
        TabelaFato:
        DT_BASE, VL_DECLARADO, VL_EXTRAIDO

        TabelaCategoria:
        Categoria

        A Tabela de Categoria e Tabela Fato devem obrigatóriamente ter o mesmo número de linhas. A Categoria pode se repitir na tabela.

        ### Exemplo 1:

        Corpo do Email:
        Olá, por favor, veja os comprovantes anexados para os gastos da minha viagem a São Paulo. Totalizando R$ 1000,00. Obrigado.

        Informações do Remetente:
        Nome: João Silva
        Email: joao.silva@empresa.com
        Data: 03/07/2024
        Cidade: São Paulo
        Estado: SP
        Pais: Brasil        

        Texto Extraído dos Anexos:
        Comprovante de Uber - Data: 01/07/2024, Valor: R$ 100,00
        Comprovante de Hotel - Data: 02/07/2024, Valor: R$ 400,00
        Comprovante de Alimentação - Data: 03/07/2024, Valor: R$ 500,00

        Saída:
        
        Cidade: São Paulo
        Estado: SP
        Pais: Brasil
        DataFinal: 03/07/2024

        TabelaFato:
        01/07/2024, 1000, 100
        02/07/2024, 1000, 400
        03/07/2024, 1000, 500

        TabelaCategoria:
        Uber
        Hotel
        Alimentação

        ### Exemplo 2:

        Corpo do Email:
        Olá, por favor, veja os comprovantes anexados para os gastos da minha viagem a São Paulo. Totalizando R$ 500,00. Obrigado.

        Informações do Remetente:
        Nome: João Silva
        Email: joao.silva@empresa.com
        Data: 03/07/2024
        Cidade: São Paulo
        Estado: SP
        País: Brasil

        Texto Extraído dos Anexos:
        Esse email não contém anexos

        Saída:
        Cidade: São Paulo
        Estado: SP
        Pais: Brasil
        DataFinal: 03/07/2024

        TabelaFato:
        03/07/2024, 500, 500

        TabelaCategoria:
        Viagem

        Agora forneça as informações estruturadas para o seguinte email e anexos:

        Texto do Email:
            [PLACEHOLDER EMAIL]

        Texto dos Anexos: 
            [PLACEHOLDER ANEXOS]
        ";
    }
}
