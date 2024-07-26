using Microsoft.Xrm.Sdk;
using System;
using System.Activities.Statements;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Linq;
using System.ServiceModel;
using OpenAI;
using OpenAI.Chat;
using IronPdf;
using DataversePluginSample.Utils;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Crm.Sdk.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using PdfDocument = UglyToad.PdfPig.PdfDocument;

namespace DataversePluginSample
{
    public class TabelaFato
    {
        public string DT_BASE;
        public string VL_DECLARADO;
        public string VL_EXTRAIDO;
    }
    
    public class TabelaCategoria
    {
        public string Categoria;
    }

    public class Fato
    {
        public TabelaFato[] TabelaFato;
        public TabelaCategoria[] TabelaCategoria;
        public string Cidade;
        public string Estado;
        public string Pais;
        public string DataFinal;
    }
    
    public class SolicitacaoReembolso : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];

                // Obtain the IOrganizationService instance which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                // Plug-in business logic inside the try operation.
                try
                {
                    var queryNewSolicitacaoEntity = new QueryExpression("crfd9_solicitacaorembolso")
                    {
                        ColumnSet = new ColumnSet("crfd9_solicitacaoid", "crfd9_corpoemail", "crfd9_email", "crfd9_dataemail"),
                    };

                    Entity newSolicitacaoEntity = service.RetrieveMultiple(queryNewSolicitacaoEntity).Entities.Last();
                    
                    string solicitacaoID = (string)newSolicitacaoEntity["crfd9_solicitacaoid"];
                    string solicitacaoCorpoEmail = (string)newSolicitacaoEntity["crfd9_corpoemail"];
                    string solicitacaoEmail = (string)newSolicitacaoEntity["crfd9_email"];
                    string solicitacaoNome = solicitacaoEmail.Split('@').First();
                    DateTime solicitacaoDate = (DateTime)newSolicitacaoEntity["crfd9_dataemail"];

                    tracingService.Trace("Get attachments informations");
                    
                    // Texto extraído dos anexos em PDF
                    var queryAnexos = new QueryExpression("crfd9_ia_solicitacaoreembolso_anexos")
                    {
                        ColumnSet = new ColumnSet("crfd9_index", "kietec_bytecontent"),
                        Criteria = new FilterExpression(LogicalOperator.And)
                        {
                            Conditions =
                            {
                                new ConditionExpression(
                                    attributeName: "crfd9_solicitacaoreferencia",
                                    conditionOperator: ConditionOperator.Equal,
                                    value: entity.Id.ToString()
                                )
                            }
                        }
                    };

                    List<string> textoAnexos = new List<string>();
                    
                    var anexosResponse = service.RetrieveMultiple(queryAnexos);

                    if (anexosResponse.Entities.Count > 0)
                    {
                        foreach (var anexo in anexosResponse.Entities)
                        {
                            string documentBody = anexo["kietec_bytecontent"].ToString();
                            byte[] pdfBytes = Convert.FromBase64String(documentBody);
                            
                            string pdfText = ExtractTextFromPdf(pdfBytes);
                            textoAnexos.Add(pdfText);
                        }
                    }
                    else
                    {
                        textoAnexos.Add("Esse email não contém anexos");
                    }
                    
                    tracingService.Trace("Get solicitante Id");

                    var querySolicitante = new QueryExpression("kietec_ia_gold_solicitante")
                    {
                        ColumnSet = new ColumnSet("kietec_name", "kietec_nome_solicitante", "kietec_email_solicitante"),
                        Criteria = new FilterExpression(LogicalOperator.And)
                        {
                            Conditions =
                            {
                                new ConditionExpression(
                                        attributeName: "kietec_email_solicitante",
                                        conditionOperator: ConditionOperator.Equal,
                                        value: solicitacaoEmail
                                )
                            }
                        }
                    };

                    var solicitanteResponse = service.RetrieveMultiple(querySolicitante);

                    int? solicitanteId = null;
                    
                    if (solicitanteResponse.Entities.Count > 0)
                    {
                        Entity solicitante = solicitanteResponse.Entities.First();
                        
                        solicitanteId = int.Parse(solicitante["kietec_name"].ToString());
                    }
                    else
                    {
                        Entity newSolicitante = new Entity("kietec_ia_gold_solicitante");
                        newSolicitante["kietec_nome_solicitante"] = solicitacaoNome;
                        newSolicitante["kietec_email_solicitante"] = solicitacaoEmail;
                        
                        Guid newSolicitanteId = service.Create(newSolicitante);

                        Entity solicitante = service.Retrieve(
                            "kietec_ia_gold_solicitante", 
                            newSolicitanteId,
                            new ColumnSet("kietec_name")
                        );

                        solicitanteId = int.Parse(solicitante["kietec_name"].ToString());
                    }
                    
                    // Informações dinâmicas
                    //string nomeRemetente = (string)solicitante["kietec_nome_solicitante"];
                    string emailRemetente = solicitacaoEmail;
                    //string cidadeRemetente = (string)entity["crfd9_cidade_solicitacao"];
                    //string estadoRemetente = (string)entity["crfd9_ud_solicitacao"];
                    //string paisRemetente = (string)entity["crfd9_pais_solicitacao"];

                    tracingService.Trace("Open AI API Request");
                    ChatClient client = new ChatClient("gpt-4o", Secrets.ApiKey);

                    string prompt = Secrets.Prompt;

                    ChatCompletion chatCompletion = client.CompleteChat(
                        prompt.Replace("[PLACEHOLDER EMAIL]", solicitacaoCorpoEmail)
                            .Replace("[PLACEHOLDER ANEXOS]", string.Join("\n", textoAnexos))
                    );
                    
                    string apiResult = chatCompletion.ToString().Replace("```json", "").Replace("```", "");

                    var json = JsonConvert.DeserializeObject<Fato>(apiResult);
                    
                    tracingService.Trace("Create Solicitação");

                    decimal valorDeclarado = decimal.Parse(json.TabelaFato.First().VL_DECLARADO);
                    decimal valorExtraido = json.TabelaFato.Sum(x => decimal.Parse(x.VL_EXTRAIDO));

                    Entity newSolicitacao = new Entity("crfd9_ia_gold_solicitacao");
                    newSolicitacao["kietec_cd_solicitante"] = solicitanteId.ToString();
                    newSolicitacao["kietec_inicio_solicitacao"] = solicitacaoDate;
                    newSolicitacao["kietec_fim_solicitacao"] = DateTime.Parse(json.DataFinal);
                    newSolicitacao["crfd9_cidade_solicitacao"] = json.Cidade;
                    newSolicitacao["crfd9_ud_solicitacao"] = json.Estado;
                    newSolicitacao["crfd9_pais_solicitacao"] = json.Pais;
                    newSolicitacao["kietec_status"] = (valorDeclarado == valorExtraido) ? "Aprovado" : "Pendente";
                    
                    Guid solicitacaoGuid = service.Create(newSolicitacao);

                    string solicitacaoId = (string)service.Retrieve("crfd9_ia_gold_solicitacao", solicitacaoGuid, new ColumnSet("crfd9_name"))["crfd9_name"];

                    tracingService.Trace("Add Tabela Output");
                    
                    Entity apiOutput = new Entity("kietec_ia_outputprompt");
                    apiOutput["kietec_output"] = apiResult;
                    apiOutput["kietec_solicitacaoid"] = solicitacaoID;

                    Guid outputId = service.Create(apiOutput);
                    
                    string categoriaId = null;
                    
                    
                    tracingService.Trace("Add Tabela Fato");

                    int index = 0;
                    
                    foreach (var fato in json.TabelaFato)
                    {
                        if (!(index + 1 > json.TabelaFato.Length))
                        {
                            tracingService.Trace("Get Categoria Id");

                            var queryCategory = new QueryExpression("kietec_ia_gold_")
                            {
                                ColumnSet = new ColumnSet("kietec_codigo_categoria", "kietec_name"),
                                Criteria = new FilterExpression(LogicalOperator.And)
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression(
                                                attributeName: "kietec_descricao_categoria",
                                                conditionOperator: ConditionOperator.Equal,
                                                value: json.TabelaCategoria[index].Categoria
                                        )
                                    }
                                }
                            };

                            var categoriaResponse = service.RetrieveMultiple(queryCategory);
                            
                            if (categoriaResponse.Entities.Count > 0)
                            {
                                Entity categoria = categoriaResponse.Entities.First();
                                
                                categoriaId = categoria["kietec_name"].ToString();
                            }
                            else
                            {
                                Entity newCategoria = new Entity("kietec_ia_gold_");
                                newCategoria["kietec_descricao_categoria"] = json.TabelaCategoria[index].Categoria;
                                
                                Guid newCategoriaId = service.Create(newCategoria);

                                Entity newCateogoria = service.Retrieve(
                                    "kietec_ia_gold_", 
                                    newCategoriaId,
                                    new ColumnSet("kietec_name")
                                );

                                categoriaId = newCateogoria["kietec_name"].ToString();
                            }
                            
                            Entity fatoEntity = new Entity("kietec_ia_gold_fato");
                            fatoEntity["kietec_data_recibo"] =  DateTime.Parse(fato.DT_BASE);
                            fatoEntity["kietec_vl_declarado"] = decimal.Parse(fato.VL_DECLARADO);
                            fatoEntity["kietec_vl_extraido"] = decimal.Parse(fato.VL_EXTRAIDO);
                            fatoEntity["kietec_cd_solicitante"] = solicitanteId.ToString();
                            fatoEntity["kietec_cd_solicitacao"] = solicitacaoId;
                            fatoEntity["kietec_cd_categoria"] = categoriaId;

                            Guid fatoId = service.Create(fatoEntity);

                            index++;
                        }
                        
                    }
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in FollowUpPlugin.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("FollowUpPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }
        
        public string ExtractTextFromPdf(byte[] pdfBytes)
        {
            using (var stream = new MemoryStream(pdfBytes))
            {
                StringBuilder text = new StringBuilder();
                using (PdfDocument document = PdfDocument.Open(stream))
                {
                    foreach (Page page in document.GetPages())
                    {
                        text.Append(page.Text);
                    }
                }
                return text.ToString();
            }
        }
    }
}