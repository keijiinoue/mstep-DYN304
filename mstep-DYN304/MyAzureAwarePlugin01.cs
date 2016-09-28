using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;

namespace mstep_DYN304
{
    /// <summary>
    /// Azure 連携用のプラグインのサンプル
    /// </summary>
    public class MyAzureAwarePlugin01 : IPlugin
    {
        private Guid serviceEndpointId;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MyAzureAwarePlugin01(string config)
        {
            if (String.IsNullOrEmpty(config) || !Guid.TryParse(config, out serviceEndpointId))
            {
                throw new InvalidPluginExecutionException("Service endpoint ID should be passed as config.");
            }
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            // Retrieve the execution context.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Extract the tracing service.
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            if (tracingService == null)
                throw new InvalidPluginExecutionException("Failed to retrieve the tracing service.");

            IServiceEndpointNotificationService cloudService = (IServiceEndpointNotificationService)serviceProvider.GetService(typeof(IServiceEndpointNotificationService));
            if (cloudService == null)
                throw new InvalidPluginExecutionException("Failed to retrieve the service bus service.");

            try
            {
                // ここにプラグインのビジネスロジックを記述する
                //
                // ・・・カスタムの処理を実装したり、
                // ・・・Azure 側に渡すカスタムのデータを context に追加したり、
                //

                tracingService.Trace("Posting the execution context.");
                string response = cloudService.Execute(new EntityReference("serviceendpoint", serviceEndpointId), context);
                if (!String.IsNullOrEmpty(response))
                {
                    tracingService.Trace("Response = {0}", response);
                }
                tracingService.Trace("Done.");
            }
            catch (Exception e)
            {
                tracingService.Trace("Exception: {0}", e.ToString());
                throw;
            }
        }
    }
}