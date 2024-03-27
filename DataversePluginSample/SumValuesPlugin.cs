using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DataversePluginSample.Model;

namespace DataversePluginSample
{
    public class SumValuesPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService) serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext) serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity) context.InputParameters["Target"];

                // Obtain the IOrganizationService instance which you will need for  
                // web service calls.  
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory) serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    // Plug-in business logic goes here.
                    var queryInput = new QueryExpression(kietec_Item.EntityLogicalName)
                    {
                        ColumnSet = new ColumnSet(kietec_Item.Fields.kietec_Valor)
                    };
                    
                    tracingService.Trace("Get all rows from input table");
                    EntityCollection inputs = service.RetrieveMultiple(queryInput); 
                        
                    var queryOutput = new QueryExpression(kietec_Resultado.EntityLogicalName)
                    {
                        TopCount = 1
                    };
                    
                    tracingService.Trace("Get first row from output table");
                    Entity outputEntity = (Entity) service.RetrieveMultiple(queryOutput).Entities.First();


                    double sumFinal = inputs.Entities.ToList().Sum(x => Convert.ToDouble(x[kietec_Item.Fields.kietec_Valor]));

                    outputEntity[kietec_Resultado.Fields.kietec_ResultadoFinal] = sumFinal;

                    service.Update(outputEntity);
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
    }
}