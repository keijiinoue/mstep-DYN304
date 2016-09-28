using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;

namespace mstep_DYN304
{
    public class MyPlugin : IPlugin
    {
        private string unsecure;
        private string secure;

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
                //IOrganizationService service = serviceFactory.CreateOrganizationService(new Guid("62B0C976-630B-E611-80F9-3863BB36AD30")); // 竹田 悠弥ユーザー

                try
                {
                    // ここにプラグインのビジネスロジックを記述する

                    // Early-bound の例
                    Task task1 = new Task();
                    task1.Subject = "_タスクの件名です Early-bound";
                    task1.Description = "こちらはタスクの説明フィールドです。";
                    service.Create(task1);

                    // Late-bound の例
                    Entity task2 = new Entity("task");
                    task2["subject"] = string.Format("_タスクの件名です Late-bound {0}{1}", unsecure, secure);
                    task2["description"] = "こちらはタスクの説明フィールドです。";
                    service.Create(task2);

                    //// Pre エンティティイメージからエンティティの情報を取得する
                    //// "Target"はプラグイン登録ツールでイメージの Entity Alias として指定した文字列
                    //tracingService.Trace("MyPlugin: PreEntityImages からエンティティの情報を取得");
                    //Entity preImage = (Entity)context.PreEntityImages["Target"];
                    //if (preImage.Attributes.Contains("name"))
                    //{
                    //    tracingService.Trace("MyPlugin: preImage name = {0}", preImage["name"]);
                    //}

                    //// Post エンティティイメージからエンティティの情報を取得する
                    //// "Target"はプラグイン登録ツールでイメージの Entity Alias として指定した文字列
                    //tracingService.Trace("MyPlugin: PostEntityImages からエンティティの情報を取得");
                    //Entity postImage = (Entity)context.PostEntityImages["Target"];
                    //if (postImage.Attributes.Contains("name"))
                    //{
                    //    tracingService.Trace("MyPlugin: postImage name = {0}", postImage["name"]);
                    //}

                    // 例外を発生させる
                    //throw new InvalidPluginExecutionException(
                    //    string.Format("レコード更新処理時のプラグインの処理中ステージ{0}で例外が発生しました。", context.Stage));
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("MyPlugin でエラーが発生しました。", ex);
                }
                catch (Exception ex)
                {
                    tracingService.Trace("MyPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }

        public MyPlugin(string unsecure, string secure)
        {
            this.unsecure = unsecure;
            this.secure = secure;
        }
    }

    public class PreEventPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // 例えば、新規の取引先担当者レコードを作成したとする
            Guid contact = new Guid("{74882D5C-381A-4863-A5B9-B8604615C2D0}");

            // "PrimaryContact"と名付けた実行コンテキスト共有変数を追加
            context.SharedVariables.Add("PrimaryContact", (Object)contact.ToString());
        }
    }

    public class PostEventPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // 実行コンテキスト共有変数から取引先担当者の情報を取得
            if (context.SharedVariables.Contains("PrimaryContact"))
            {
                Guid contact =
                    new Guid((string)context.SharedVariables["PrimaryContact"]);

                // この取引先担当者に対して何かしらの処理をする
            }
        }
    }
}