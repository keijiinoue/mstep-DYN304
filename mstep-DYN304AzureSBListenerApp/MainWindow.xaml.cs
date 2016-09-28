using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.Xrm.Sdk;

/// <summary>
/// Dynamics CRM から Azure Service Bus の Queue 経由で受け取ったメッセージを表示するデモ用アプリケーション
/// 取引先企業 account エンティティに対する Create および Update メッセージを受け取ることを想定しています。
/// また、Update メッセージの場合には、Pre Image および Post Image として "Target" と Entity Alias で指定された Image を受け取ることを想定しています。
/// Dynamics CRM 2016 SDK に付属のサンプルコード"Sample: Persistent queue listener"のコードを利用しています。
/// </summary>
namespace mstep_DYN304AzureSBListenerApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Azure Service Bus の Queue の SAS 認証でアクセスするための接続文字列
        const string connectionString = "＜＜ここにご自身で取得した接続文字列を入力してください。＞＞";
        //const string connectionString = "Endpoint=sb://xxxxxxxxxxxxxxxx.servicebus.windows.net/;SharedAccessKeyName=CrmQueueSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx;EntityPath=myqueue";
        #endregion

        private QueueClient queueClient;
        private MessagesFromCRM messageList;
        public MainWindow()
        {
            InitializeComponent();

            messageList = new MessagesFromCRM();
            MyDataGrid.ItemsSource = messageList;

            CreateQueueClient(connectionString);
            ProcessMessages();
        }
        public void CreateQueueClient(string connectionString)
        {
            this.queueClient = QueueClient.CreateFromConnectionString(connectionString);
        }
        public void ProcessMessages()
        {
            MyRawMessageTB.Text += "メッセージの受信を待っています。" + Environment.NewLine;

            queueClient.OnMessage(message =>
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    RemoteExecutionContext context = message.GetBody<RemoteExecutionContext>();

                    MyRawMessageTB.Text += "--------------------------------" + Environment.NewLine;
                    MyRawMessageTB.Text += string.Format("Message received: Id = {0}", message.MessageId) + Environment.NewLine;
                    // Display message properties that are set on the brokered message.
                    MyRawMessageTB.Text += Utility.GetPrintMessageProperties(message.Properties);
                    // Display body details.
                    MyRawMessageTB.Text += Utility.GetPrint(context);
                    MyRawMessageTB.Text += "--------------------------------" + Environment.NewLine;
                    MySV.ScrollToBottom();

                    messageList.Add(new MessageFromCRM
                    {
                        MessageId = message.MessageId,
                        EnqueuedTimeUtc = message.EnqueuedTimeUtc,
                        Size = message.Size,
                        Properties = message.Properties,
                        InputEntity = (Entity)context.InputParameters["Target"],
                        PreEntity = context.PreEntityImages.Keys.Contains("Target") ? context.PreEntityImages?["Target"]: null,
                        PostEntity = context.PostEntityImages.Keys.Contains("Target") ? context.PostEntityImages["Target"]: null
                    });
                }));
            });
        }

        private void MyDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MessageFromCRM message = e.AddedItems[0] as MessageFromCRM;
            MyFormattedMessageFromCRMTB.Inlines.Clear();
            string requestName = (string) message.Properties["http://schemas.microsoft.com/xrm/2011/Claims/RequestName"];
            MyFormattedMessageFromCRMTB.Inlines.Add(string.Format("MessageId: {0}\n", message.MessageId));
            MyFormattedMessageFromCRMTB.Inlines.Add(string.Format("EnqueuedTimeUtc: {0}\n", message.EnqueuedTimeUtc));
            MyFormattedMessageFromCRMTB.Inlines.Add(string.Format("Size: {0}\n", message.Size));
            MyFormattedMessageFromCRMTB.Inlines.Add(string.Format("Organization: {0}\n", message.Properties["http://schemas.microsoft.com/xrm/2011/Claims/Organization"]));
            MyFormattedMessageFromCRMTB.Inlines.Add(string.Format("EntityLogicalName: {0}\n", message.Properties["http://schemas.microsoft.com/xrm/2011/Claims/EntityLogicalName"]));
            MyFormattedMessageFromCRMTB.Inlines.Add(string.Format("RequestName: "));
            MyFormattedMessageFromCRMTB.Inlines.Add(new Run() { Foreground = new SolidColorBrush(Colors.Red), Text = string.Format("{0}\n", requestName) });
            if(requestName == "Update")
            {
                List<string> updatedAttributes = new List<string>();
                foreach(var attr in 
                message.InputEntity.Attributes.Where(
                    a => a.Key != "accountid" && a.Key != "modifiedon" && a.Key != "modifiedby" && a.Key != "modifiedonbehalfby"))
                {
                    MyFormattedMessageFromCRMTB.Inlines.Add(string.Format("Attribute \"{0}\": Pre: {1} ⇒ Post: {2}\n",
                        attr.Key,
                        message.PreEntity.Attributes.Keys.Contains(attr.Key) ? message.PreEntity[attr.Key]: "null",
                        message.PostEntity.Attributes.Keys.Contains(attr.Key) ? message.PostEntity[attr.Key]: "null"
                        ));
                }
            }
        }
        /// <summary>
        /// Dynamics CRM 2016 SDK に付属のものを修正
        /// </summary>
        internal static class Utility
        {
            public static string GetPrint(RemoteExecutionContext context)
            {
                string text = "";
                if (context == null)
                {
                    text += "Context is null." + Environment.NewLine;
                    return text;
                }

                text += string.Format("UserId: {0}\n", context.UserId);
                text += string.Format("OrganizationId: {0}\n", context.OrganizationId);
                text += string.Format("OrganizationName: {0}\n", context.OrganizationName);
                text += string.Format("MessageName: {0}\n", context.MessageName);
                text += string.Format("Stage: {0}\n", context.Stage);
                text += string.Format("Mode: {0}\n", context.Mode);
                text += string.Format("PrimaryEntityName: {0}\n", context.PrimaryEntityName);
                text += string.Format("SecondaryEntityName: {0}\n", context.SecondaryEntityName);

                text += string.Format("BusinessUnitId: {0}\n", context.BusinessUnitId);
                text += string.Format("CorrelationId: {0}\n", context.CorrelationId);
                text += string.Format("Depth: {0}\n", context.Depth);
                text += string.Format("InitiatingUserId: {0}\n", context.InitiatingUserId);
                text += string.Format("IsExecutingOffline: {0}\n", context.IsExecutingOffline);
                text += string.Format("IsInTransaction: {0}\n", context.IsInTransaction);
                text += string.Format("IsolationMode: {0}\n", context.IsolationMode);
                text += string.Format("Mode: {0}\n", context.Mode);
                text += string.Format("OperationCreatedOn: {0}\n", context.OperationCreatedOn.ToString());
                text += string.Format("OperationId: {0}\n", context.OperationId);
                text += string.Format("PrimaryEntityId: {0}\n", context.PrimaryEntityId);
                text += string.Format("OwningExtension LogicalName: {0}\n", context.OwningExtension.LogicalName);
                text += string.Format("OwningExtension Name: {0}\n", context.OwningExtension.Name);
                text += string.Format("OwningExtension Id: {0}\n", context.OwningExtension.Id);
                text += string.Format("SharedVariables: {0}\n", (context.SharedVariables == null ? "NULL" :
                    SerializeParameterCollection(context.SharedVariables)));
                text += string.Format("InputParameters: {0}\n", (context.InputParameters == null ? "NULL" :
                    SerializeParameterCollection(context.InputParameters)));
                text += string.Format("OutputParameters: {0}\n", (context.OutputParameters == null ? "NULL" :
                    SerializeParameterCollection(context.OutputParameters)));
                text += string.Format("PreEntityImages: {0}\n", (context.PreEntityImages == null ? "NULL" :
                    SerializeEntityImageCollection(context.PreEntityImages)));
                text += string.Format("PostEntityImages: {0}\n", (context.PostEntityImages == null ? "NULL" :
                    SerializeEntityImageCollection(context.PostEntityImages)));

                return text;
            }

            #region Private methods.
            private static string SerializeEntity(Entity e)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Environment.NewLine);
                sb.Append(" LogicalName: " + e.LogicalName);
                sb.Append(Environment.NewLine);
                sb.Append(" EntityId: " + e.Id);
                sb.Append(Environment.NewLine);
                sb.Append(" Attributes: [");
                foreach (KeyValuePair<string, object> parameter in e.Attributes)
                {
                    sb.Append(parameter.Key + ": " + parameter.Value + "; ");
                }
                sb.Append("]");
                return sb.ToString();
            }

            private static string SerializeParameterCollection(ParameterCollection parameterCollection)
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, object> parameter in parameterCollection)
                {
                    if (parameter.Value != null && parameter.Value.GetType() == typeof(Entity))
                    {
                        Entity e = (Entity)parameter.Value;
                        sb.Append(parameter.Key + ": " + SerializeEntity(e));
                    }
                    else
                    {
                        sb.Append(parameter.Key + ": " + parameter.Value + "; ");
                    }
                }
                return sb.ToString();
            }
            private static string SerializeEntityImageCollection(EntityImageCollection entityImageCollection)
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, Entity> entityImage in entityImageCollection)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(entityImage.Key + ": " + SerializeEntity(entityImage.Value));
                }
                return sb.ToString();
            }
            #endregion

            internal static string GetPrintMessageProperties(IDictionary<string, object> iDictionary)
            {
                string text = "";
                if (iDictionary.Count == 0)
                {
                    text += "No Message properties found." + Environment.NewLine;
                    return text;
                }
                foreach (var item in iDictionary)
                {
                    String key = (item.Key != null) ? item.Key.ToString() : "";
                    String value = (item.Value != null) ? item.Value.ToString() : "";
                    text += key + " " + value + Environment.NewLine;
                }
                return text;
            }
        }
    }
}
