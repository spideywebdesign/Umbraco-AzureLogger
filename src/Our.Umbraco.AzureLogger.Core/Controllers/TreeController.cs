﻿namespace Our.Umbraco.AzureLogger.Core.Controllers
{
    using global::Umbraco.Core;
    using global::Umbraco.Web.Models.Trees;
    using global::Umbraco.Web.Mvc;
    using global::Umbraco.Web.Trees;
    using System.Linq;
    using System.Net.Http.Formatting;
    using umbraco;
    using umbraco.BusinessLogic.Actions;
    using umbraco.interfaces;
    using UmbracoTreeController = global::Umbraco.Web.Trees.TreeController;

    [Tree("developer", "azureLoggerTree", "Azure Logger", "icon-folder", "icon-folder-open", true, 0)]
    [PluginController("AzureLogger")]
    public class TreeController : UmbracoTreeController
    {
        /// <summary>
        /// Called by Umbraco to get the nodes for the tree (the 'Reload nodes' menu option will trigger this)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="queryStrings"></param>
        /// <returns></returns>
        protected override TreeNodeCollection GetTreeNodes(string id, FormDataCollection queryStrings)
        {
            TreeNodeCollection treeNodeCollection = new TreeNodeCollection();

            if (this.IsRoot(id))
            {
                // Get Searches from the azure table
                TableService
                    .Instance
                    .ReadSearchItemTableEntities()
                    .OrderBy(x => x.Name)
                    .ForEach(
                        x => treeNodeCollection.Add(this.CreateTreeNode(
                                                            "searchItem|" + x.RowKey,
                                                            "-1",
                                                            queryStrings,
                                                            x.Name,
                                                            "icon-list",
                                                            false,
                                                            this.BuildRoute("ViewLog", x.RowKey))));
            }

            return treeNodeCollection;
        }

        protected override MenuItemCollection GetMenuForNode(string id, FormDataCollection queryStrings)
        {
            MenuItemCollection menuItemCollection = new MenuItemCollection();

            if (this.IsRoot(id))
            {
                //menuItemCollection.Items.Add(new MenuItem("ConnectionStatus", "Connection Status"));
                //menuItemCollection.Items.Add(new MenuItem("DeleteLogs", "Delete Logs")); // TODO: only if connected and there are logs to delete
                menuItemCollection.Items.Add<ActionNew>(ui.Text("actions", ActionNew.Instance.Alias), false); // loads view "Create.html"
                menuItemCollection.Items.Add<ActionRefresh>(ui.Text("actions", ActionRefresh.Instance.Alias), true);

            }
            else if (id.StartsWith("searchItem"))
            {
                //menuItemCollection.Items.Add(new MenuItem("SearchFilters", "Search Filters"));
                menuItemCollection.Items.Add<SearchFiltersAction>("Filters", false); // NOTE: render name differs - better for user
                menuItemCollection.Items.Add<ActionDelete>(ui.Text("actions", ActionDelete.Instance.Alias), true); // loads view "Delete.html"

                //menuItemCollection.Items.Add(new MenuItem("Refresh", "Refresh"));
            }

            return menuItemCollection;
        }

        private bool IsRoot(string id)
        {
            return id == Constants.System.Root.ToInvariantString();
        }

        /// <summary>
        /// Helper to specify the html file to use
        /// http://umbraco.github.io/Belle/#/tutorials/Creating-Editors-Trees
        /// https://our.umbraco.org/forum/umbraco-7/developing-umbraco-7-packages/46710-Optional-view-as-opposed-to-edithtml-for-tree-action
        /// </summary>
        /// <param name="htmlView">the html file</param>
        /// <param name="id">-1 indicates no id</param>
        /// <returns></returns>
        private string BuildRoute(string htmlView, string id = "-1")
        {
            if (string.IsNullOrWhiteSpace(htmlView)) { htmlView = "ViewLog"; }
            if (string.IsNullOrWhiteSpace(id)) { id = "-1"; } // id required in url, else fails to auto route

            // section/tree/method/id
            return string.Format("/developer/azureLoggerTree/{0}/{1}", htmlView, id);
        }

        //private static FormDataCollection Add(FormDataCollection querystring, string key, string value)
        //{
        //    return querystring;
        //    //queryStrings.ReadAsNameValueCollection().Add("filter", "level");
        //}
    }

    public class SearchFiltersAction : IAction
    {
        public string Alias
        {
            get { return "searchFilters"; }
        }

        public bool CanBePermissionAssigned
        {
            get { return false; }
        }

        public string Icon
        {
            get { return "filter"; }
        }

        public string JsFunctionName
        {
            get { return null; }
        }

        public string JsSource
        {
            get { return null; }
        }

        public char Letter
        {
            get { return 'f'; } // TODO: check this letter is available
        }

        public bool ShowInNotifier
        {
            get { return false; }
        }
    }
}
