using System;
using System.ServiceModel;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace mstep_DYN304
{
    /// <summary>
    /// 営業案件が作成された際に、業務プロセス フローが関連付いていて、かつ、予測クローズ日フィールドに値が入っている場合に、
    /// タスクレコードを作成する。作成されるタスクレコードはその業務プロセス フローのステージの個数と同じ数だけ作成される。
    /// 各々のタスクレコードの期限フィールドの日付は自動算出される値がセットされる。その期限フィールドの日付の値は、
    /// 予測クローズ日から按分された日付であるが、曜日により調整される。
    /// その調整とは、その日付が土曜日あるいは日曜日の場合には、その前の金曜日に設定される。
    /// なお、作成されるタスクレコードのうち、最後のものについてはこの調整はされない。
    /// 
    /// 前提条件1
    /// このプラグインでは、プラグイン登録ツールにより設定されて渡されるイメージとして、PostEntityImageのEntity Aliasに
    /// "Target"と設定されたものが渡されることを想定している。
    /// 
    /// 前提条件2
    /// ステージは40 (Post-operation) を想定している。
    /// 
    /// </summary>
    public class MyAddTasksPluginSample: IPlugin
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

            tracingService.Trace("MyAddTasksPluginSample: PostEntityImages からEntity Aliasに\"Target\"と設定されたものが渡されているかどうかをチェック");
            // InputParametersコレクションは、メッセージリクエストで渡されるすべてのデータを含む
            if (context.PostEntityImages.Contains("Target") &&
                context.PostEntityImages["Target"] is Entity)
            {
                // PostEntityImagesからターゲットエンティティを取得
                Entity postImage = (Entity)context.PostEntityImages["Target"];

                // エンティティが意図したものであるかどうかを検証。もし違っていたらプラグインは正しく登録されていない。
                if (postImage.LogicalName != Opportunity.EntityLogicalName) {
                    tracingService.Trace("MyAddTasksPluginSample: このプラグインは {0} エンティティで動作しており、opportunity(営業案件)エンティティではないため、終了します。", postImage.LogicalName);
                    return;
                }

                Opportunity opp = postImage.ToEntity<Opportunity>();

                if (!opp.ProcessId.HasValue)
                {
                    tracingService.Trace("MyAddTasksPluginSample: 関連付いている業務プロセス フローがないため、終了します。");
                    return;
                }
                if (!opp.EstimatedCloseDate.HasValue)
                {
                    tracingService.Trace("MyAddTasksPluginSample: 予測クローズ日フィールドに値がないため、終了します。");
                    return;
                }

                // Web サービスをコールするための組織サービスへの参照を取得
                IOrganizationServiceFactory serviceFactory =
                    (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    // 関連付いている業務プロセス フローのデータを取得
                    ColumnSet cols = new ColumnSet(new String[] {  "name", "xaml" });
                    Workflow retrievedFlow = (Workflow)service.Retrieve(Workflow.EntityLogicalName, opp.ProcessId.Value, cols);
                    tracingService.Trace("MyAddTasksPluginSample: 業務プロセス フロー\"{0}\"のデータを取得しました。", retrievedFlow.Name);

                    // 業務プロセス フローのデータから、ステージ名のリストを取得
                    List<string> stageNameList = GetStageNameList(retrievedFlow);

                    // 作成すべきタスクレコード群のエンティティ データを作成
                    List<Task> taskList = GetTakListToCreate(opp, stageNameList, opp.EstimatedCloseDate.Value, tracingService);

                    // タスクレコード群を作成
                    foreach(Task task in taskList)
                    {
                        service.Create(task);
                    }
                }
                catch (FaultException<OrganizationServiceFault>)
                {
                    throw new InvalidPluginExecutionException("MyAddTasksPluginSample でエラーが発生しました。");
                }
                catch (Exception ex)
                {
                    tracingService.Trace("MyAddTasksPluginSample: {0}", ex.ToString());
                    throw;
                }
            }
        }
        /// <summary>
        /// 作成すべきタスクレコード群のエンティティ データを作成
        /// </summary>
        /// <param name="opp"></param>
        /// <param name="stageNameList"></param>
        /// <param name="estimatedCloseDate"></param>
        /// <returns></returns>
        private List<Task> GetTakListToCreate(Opportunity opp, List<string> stageNameList, DateTime estimatedCloseDate, ITracingService tracingService)
        {
            List<Task> taskList = new List<Task>();
            DateTime now = DateTime.Now;
            TimeSpan span = estimatedCloseDate - now;
            for(int i = 0; i < stageNameList.Count - 1; i++)
            {
                int daysFromNow = (i + 1) * span.Days / stageNameList.Count;
                DateTime scheduledend = now.AddDays(daysFromNow);
                if (scheduledend.DayOfWeek == DayOfWeek.Saturday)
                {
                    tracingService.Trace("MyAddTasksPluginSample: {0} が土曜日であるため、金曜日に変更します。", scheduledend.ToLongDateString());
                    scheduledend = scheduledend.AddDays(-1);
                }
                if (scheduledend.DayOfWeek == DayOfWeek.Sunday)
                {
                    tracingService.Trace("MyAddTasksPluginSample: {0} が日曜日であるため、金曜日に変更します。", scheduledend.ToLongDateString());
                    scheduledend = scheduledend.AddDays(-2);
                }
                    
                Task task = new Task();
                task.Subject = string.Format("\"{0}\"ステージ完了期日", stageNameList[i]);
                task.ScheduledEnd = scheduledend;
                task.RegardingObjectId = new EntityReference
                {
                    Id = opp.OpportunityId.Value,
                    LogicalName = Opportunity.EntityLogicalName,
                    Name = opp.Name
                };
                taskList.Add(task);
            }
            DateTime lastScheduledend = now;
            Task lastTask = new Task();
            lastTask.Subject = string.Format("\"{0}\"ステージ完了期日", stageNameList[stageNameList.Count - 1]);
            lastTask.ScheduledEnd = estimatedCloseDate;
            lastTask.RegardingObjectId = new EntityReference
            {
                Id = opp.OpportunityId.Value,
                LogicalName = Opportunity.EntityLogicalName,
                Name = opp.Name
            };
            taskList.Add(lastTask);

            return taskList;
        }

        /// <summary>
        /// 業務プロセス フローのデータから、ステージ名のリストを取得
        /// </summary>
        /// <param name="retrievedFlow"></param>
        /// <returns></returns>
        private List<string> GetStageNameList(Workflow retrievedFlow)
        {
            List<string> stageNameList = new List<string>();
            System.IO.StringReader txtReader = new System.IO.StringReader((string)retrievedFlow["xaml"]);
            XmlDocument xaml = new XmlDocument();
            xaml.Load(txtReader);
            return GetStageNameList(xaml);
        }
        /// <summary>
        /// 業務プロセス フローの xaml フィールドの値を解析し、ステージ名のリストを取得
        /// </summary>
        /// <param name="xamlInBusinessProcessFlow"></param>
        /// <returns></returns>
        private List<string> GetStageNameList(XmlDocument xamlInBusinessProcessFlow)
        {
            List<string> list = new List<string>();
            XmlNodeList stepLabelList = xamlInBusinessProcessFlow.GetElementsByTagName("mcwo:StepLabel");
            foreach (XmlNode sl in stepLabelList)
            {
                var p = sl.ParentNode.ParentNode.ParentNode;
                foreach (XmlAttribute att in p.Attributes)
                {
                    if (att.Name == "AssemblyQualifiedName" && att.InnerText.StartsWith("Microsoft.Crm.Workflow.Activities.StageComposite"))
                    {
                        foreach (XmlAttribute slAtt in sl.Attributes)
                        {
                            if (slAtt.Name == "Description")
                            {
                                list.Add(slAtt.Value);
                            }
                        }
                    }
                }
            }
            return list;
        }

        public MyAddTasksPluginSample(string unsecure, string secure)
        {
            this.unsecure = unsecure;
            this.secure = secure;
        }
    }
}
