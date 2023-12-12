using LINQPad;
using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.Linq;
using Z.EntityFramework.Extensions.Core.Infrastructure;
using Z.EntityFramework.Extensions.Core.SchemaObjectModel;

namespace CloudNimble.LinqPad.Drivers.EF6Core
{

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// It's not exactly clear how instances are managed within LinqPad. We'll need to talk to Joe Albahari about this.
    /// </remarks>
    public class EF6Driver : StaticDataContextDriver
    {

        #region Private Members

        //DbContext _dbContextInstance;
        //DbModel _model;
        Dictionary<string, ExplorerItem> _mappedExplorerItems = [];
        Dictionary<Type, string> _entityTypeToConceptualTypeMappings = [];
        Dictionary<string, ICustomMemberProvider> _mappedMemberProviders = [];

        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public override string Author => "BurnRate.io";

        /// <summary>
        /// 
        /// </summary>
        public override string Name => "EF6 + Microsoft.Data.SqlClient on .NET 6 and later";

        #endregion

        #region Constructors

        /// <summary>
        /// The static constructor for the EF6Driver class.
        /// </summary>
        /// <remarks>
        ///In case you missed it, these actions are only taken on first load.
        /// </remarks>
        static EF6Driver()
        {
            DbConfiguration.SetConfiguration(new MicrosoftSqlDbConfiguration());
            EnableDebugExceptions();
            LoadAssemblySafely("ModernWpf.dll");
            LoadAssemblySafely("ModernWpf.Controls.dll");
            LoadAssemblySafely("SharpVectors.Converters.Wpf.dll");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <returns></returns>
        public override string GetConnectionDescription(IConnectionInfo connectionInfo)
            => connectionInfo.CustomTypeInfo.GetCustomTypeDescription();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cxInfo"></param>
        /// <returns></returns>
        /// <remarks>
        /// At some point we should inspect the type and see if we should build better constructor arguments.
        /// </remarks>
        public override object[] GetContextConstructorArguments(IConnectionInfo cxInfo)
        {
            if (string.IsNullOrEmpty(cxInfo.DatabaseInfo.CustomCxString))
                return [];

            return [ cxInfo.DatabaseInfo.CustomCxString ];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cxInfo"></param>
        /// <returns></returns>
        public override ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo cxInfo)
        {
            if (string.IsNullOrEmpty(cxInfo.DatabaseInfo.CustomCxString))
                return [];

            return [ new ParameterDescriptor("param", "System.String") ];
        }

        ///// <summary>
        ///// T
        ///// </summary>
        ///// <param name="objectToWrite"></param>
        ///// <returns></returns>
        ///// <remarks>
        ///// This is called when the user calls Dump() on an object in the query window.
        ///// </remarks>
        //public override ICustomMemberProvider GetCustomDisplayMemberProvider(object objectToWrite)
        //{
        //    if (objectToWrite is null)
        //        return null;

        //    Utilities.Debug();

        //    if (!_entityTypeToConceptualTypeMappings.ContainsKey(objectToWrite.GetType()))
        //        return null;

        //    //if (!_mappedMemberProviders.ContainsKey(objectToWrite.GetType().FullName))
        //    //    _mappedMemberProviders[objectToWrite.GetType().FullName] = new EF6MemberProvider(_dbContextInstance, _model, objectToWrite);

        //    //return _mappedMemberProviders[objectToWrite.GetType().FullName];
        //    return new EF6MemberProvider(_dbContextInstance, _model, objectToWrite);
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="customType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override List<ExplorerItem> GetSchema(IConnectionInfo connectionInfo, Type customType)
        {
            if (customType is null)
                throw new ArgumentException("No custom type selected. Please check the properties of this connection.");

            //Utilities.Debug();

            var dbContextInstance = (DbContext)Activator.CreateInstance(customType, connectionInfo.DatabaseInfo.CustomCxString);
            var model = dbContextInstance.GetModel();

            _mappedExplorerItems = model.ConceptualModel.EntityTypes.ToDictionary(k => k.EntityTypeMapping.TypeName, v => new ExplorerItem(v.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table));

            var nodes = model.ConceptualModel.EntityContainers
                .OrderBy(c => c.Name)
                .Select(c =>
                {
                    return new ExplorerItem($"{c.Name} (EntityContainer)", ExplorerItemKind.Category, ExplorerIcon.Box)
                    {
                        Children = c.EntitySets
                            .OrderBy(d => d.Name)
                            .Select(d =>
                            {
                                // RWM: Here we're mapping the tables.
                                var entitySetItem = new ExplorerItem(d.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                                {
                                    IsEnumerable = true,
                                    Tag = d.EntityType.EntityTypeMapping.TypeName,
                                    Children = d.EntityType.Properties.Select(e =>
                                    {
                                        //RWM: Here we're mapping the columns
                                        return new ExplorerItem(GetPropertyName(e), ExplorerItemKind.Property, e.IsPrimaryKey ? ExplorerIcon.Key : ExplorerIcon.Column)
                                        {
                                            // RWM: We store the Property as a Tag here so we can use this type to exclude our Navigation Property lookup later.
                                            Tag = e
                                        };
                                    }).ToList()
                                };

                                //RWM: Now we're adding in the relationships.
                                entitySetItem.Children.AddRange(d.EntityType.NavigationProperties.Select(e => GetRelatedItem(d.EntityType, e)));

                                // RWM: Cache the types so we can fix up the hyperlinks on a second pass.
                                _mappedExplorerItems[d.EntityType.EntityTypeMapping.TypeName] = entitySetItem;

                                return entitySetItem;
                            }).ToList()
                    };  
                }).ToList();

            // RWM: Fix up the hyperlinks.
            _ = nodes.SelectMany(c => c.Children.SelectMany(d => d.Children))
                .Where(c => c.Tag is not Property)
                .Select(c =>
                {
                    c.HyperlinkTarget = _mappedExplorerItems[c.Tag.ToString()];
                    return c;
                })
                .ToList();

            return nodes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="context"></param>
        /// <param name="executionManager"></param>
        public override void InitializeContext(IConnectionInfo connectionInfo, object context, QueryExecutionManager executionManager)
        {
            base.InitializeContext(connectionInfo, context, executionManager);

            if (context is DbContext dbContext)
            {
                //if (_dbContextInstance != dbContext)
                //{
                //    _dbContextInstance = dbContext;
                //    _model = dbContext.GetModel();
                //    // RWM: We're using reflection here because the model types show the models' namespacing, which may be different than the DbSets.
                //    _entityTypeToConceptualTypeMappings = _dbContextInstance.GetType().GetProperties()
                //        .Where(c => c.PropertyType.IsGenericType && c.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                //        .ToDictionary(
                //            k => k.PropertyType.GenericTypeArguments[0],
                //            v => _model.ConceptualModel.EntityContainers
                //                    .SelectMany(e => e.EntitySets)
                //                    .Where(c => c.Name == v.Name)
                //                    .Select(c => c.EntityType.EntityTypeMapping.TypeName)
                //                    .FirstOrDefault());
                //}
                
                dbContext.Database.Log = executionManager.SqlTranslationWriter.WriteLine;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="dialogOptions"></param>
        /// <returns></returns>
        public override bool ShowConnectionDialog(IConnectionInfo connectionInfo, ConnectionDialogOptions dialogOptions)
            => new ConnectionDialog(connectionInfo).ShowDialog() == true;

        #endregion

        #region Private Methods

        /// <summary>
        /// 
        /// </summary>
        [Conditional("DEBUG")]
        private static void EnableDebugExceptions()
        {
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                if (args.Exception.StackTrace?.Contains(typeof(EF6Driver).Namespace) == true)
                    Utilities.Debug();
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static string GetPropertyName(Property property) =>
            $"{property.Name} ({property.TypeName}{(property.Nullable ? "?" : "")})";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="navProperty"></param>
        /// <returns></returns>
        private static ExplorerItem GetRelatedItem(SchemaEntityType entityType, NavigationProperty navProperty)
        {
            if (navProperty.FromRoleEnd.EntityType == entityType)
            {
                return new ExplorerItem(navProperty.Name, GetRelationshipItemKind(navProperty), GetRelationshipIcon(navProperty))
                {
                    // RWM: We have to set the HyperlinkTarget later, because we don't have the full list of ExplorerItems yet.
                    ToolTipText = $"Type: {navProperty.ToRoleEnd.EntityType.Name}",
                    Tag = navProperty.ToRoleEnd.EntityType.EntityTypeMapping.TypeName
                };
            }
            return new ExplorerItem(navProperty.Name, GetRelationshipItemKind(navProperty), GetRelationshipIcon(navProperty))
            {
                // RWM: We have to set the HyperlinkTarget later, because we don't have the full list of ExplorerItems yet.
                ToolTipText = $"Type: {navProperty.FromRoleEnd.EntityType.Name}",
                Tag = navProperty.FromRoleEnd.EntityType.EntityTypeMapping.TypeName
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="navProperty"></param>
        /// <returns></returns>
        private static ExplorerIcon GetRelationshipIcon(NavigationProperty navProperty)
        {
            return (navProperty.FromRoleEnd.Multiplicity, navProperty.ToRoleEnd.Multiplicity) switch
            {
                ("1", "1") => ExplorerIcon.OneToOne,
                ("*", "*") => ExplorerIcon.ManyToMany,
                ("*", "1") => ExplorerIcon.ManyToOne,
                _ => ExplorerIcon.OneToMany
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="navProperty"></param>
        /// <returns></returns>
        private static ExplorerItemKind GetRelationshipItemKind(NavigationProperty navProperty)
        {
            return (navProperty.FromRoleEnd.Multiplicity, navProperty.ToRoleEnd.Multiplicity) switch
            {
                ("1", "1") => ExplorerItemKind.ReferenceLink,
                ("*", "*") => ExplorerItemKind.CollectionLink,
                ("*", "1") => ExplorerItemKind.ReferenceLink,
                _ => ExplorerItemKind.CollectionLink
            };
        }

        #endregion

    }

}
