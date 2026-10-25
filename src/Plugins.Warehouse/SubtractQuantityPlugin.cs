using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using TALXIS.DevKit.Build.Dataverse.Plugin;

namespace Plugins.Warehouse
{
    /// <summary>
    /// Adds or subtracts the item's available quantity whenever a transaction is created.
    /// Runs Post-operation so the stock change commits or rolls back with the transaction record.
    /// </summary>
    [PluginStep("Create", "lab_warehousetransaction", PluginStage.PostOperation)]
    public class SubtractQuantityPlugin : PluginBase
    {
        public SubtractQuantityPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SubtractQuantityPlugin))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;

            if (!(context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity target) ||
                target.LogicalName != "lab_warehousetransaction")
            {
                return;
            }

            if (!target.Contains("lab_quantity") || !target.Contains("lab_itemid") || !target.Contains("lab_transactiontype"))
            {
                return;
            }

            var quantity = (int)target["lab_quantity"];
            var itemRef = (EntityReference)target["lab_itemid"];
            var transactionType = (OptionSetValue)target["lab_transactiontype"];

            var service = localPluginContext.OrgSvcFactory.CreateOrganizationService(context.UserId);

            var item = service.Retrieve("lab_warehouseitem", itemRef.Id, new ColumnSet("lab_availablequantity"));
            var available = item.Contains("lab_availablequantity") ? (int)item["lab_availablequantity"] : 0;

            int updated;
            if (transactionType.Value == 100000000)
            {
                // Inbound
                updated = available + quantity;
            }
            else if (transactionType.Value == 100000001)
            {
                // Outbound
                updated = available - quantity;
            }
            else
            {
                return;
            }

            item["lab_availablequantity"] = updated;
            service.Update(item);
        }
    }
}
