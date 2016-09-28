using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;

namespace mstep_DYN304
{
    public class MyPlugin01 : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // サンドボックスのプラグインをデバッグするために tracing service を展開
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // サービスプロバイダーからプラグイン実行コンテキストを取得
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // InputParametersコレクションは、メッセージリクエストで渡されるすべてのデータを含む
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // インプットパラメーターからターゲットエンティティを取得
                Entity entity = (Entity)context.InputParameters["Target"];

                // エンティティが意図したものであるかどうかを検証。もし違っていたらプラグインは正しく登録されていない。
                if (entity.LogicalName != "account")
                    return;

                // Web サービスをコールするための組織サービスへの参照を取得
                IOrganizationServiceFactory serviceFactory =
                    (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    // ここにプラグインのビジネスロジックを記述する
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("MyPlugin01 でエラーが発生しました。", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("MyPlugin01: {0}", ex.ToString());
                    throw;
                }
            }
        }
    }
}